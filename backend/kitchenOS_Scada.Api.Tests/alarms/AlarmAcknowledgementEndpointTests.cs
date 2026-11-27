using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using kitchenOS_Scada.Api.Features.Alarms;
using kitchenOS_Scada.Api.Features.Alarms.Acknowledgement;
using kitchenOS_Scada.Api.Features.Alarms.Contracts;
using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Api.Persistence;
using kitchenOS_Scada.Api.Persistence.Entities;
using kitchenOS_Scada.Api.Security;
using kitchenOS_Scada.Api.Tests.Security;
using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Contract.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kitchenOS_Scada.Api.Tests.Alarms;

[TestClass]
public sealed class AlarmAcknowledgementEndpointTests
{
    private readonly TestIdentityProvider _identity = new();
    private readonly SqliteConnection _database = new("Data Source=:memory:");
    private readonly CookieContainer _cookies = new();
    private readonly WebSocketClientNotifier _notifier = new();
    private WebApplication _app = default!;
    private HttpClient _client = default!;
    private long _alarmId;
    private Guid _socketId;
    private RecordingSocket? _socket;

    [TestInitialize]
    public async Task Initialize()
    {
        _database.Open();
        WebApplicationBuilder builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _identity.Configure(builder.Services);
        builder.Services.AddSingleton<IDbContextFactory<KitchenOsDbContext>>(
            new TestDbContextFactory(_database));
        builder.Services.AddSingleton(_notifier);
        builder.Services.AddSingleton<IClientNotifier>(_notifier);
        builder.Services.AddAlarmAcknowledgement();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(AlarmAcknowledgementController).Assembly);

        _app = builder.Build();
        await _identity.InitializeAsync(_app.Services);
        await using (KitchenOsDbContext dbContext = new(
            new DbContextOptionsBuilder<KitchenOsDbContext>()
                .UseSqlite(_database)
                .Options))
        {
            await dbContext.Database.EnsureCreatedAsync();
            AlarmEntity alarm = new()
            {
                DeviceId = "MIX-001",
                EventTypeId = 2,
                Code = "MIXER_OVERHEAT",
                Severity = "critical",
                Text = "Mixer exceeded its temperature limit.",
                OccurredAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.Alarms.Add(alarm);
            await dbContext.SaveChangesAsync();
            _alarmId = alarm.AlarmId;
        }

        _app.UseKitchenOsSecurity();
        _app.MapKitchenOsIdentity();
        _app.MapControllers();
        await _app.StartAsync();
        _client = new HttpClient(new HttpClientHandler
        {
            CookieContainer = _cookies,
            AllowAutoRedirect = false
        })
        {
            BaseAddress = new Uri(_app.Urls.Single())
        };
        await _identity.LoginAsync(_client);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_socketId != Guid.Empty) _notifier.RemoveClient(_socketId);
        _socket?.Dispose();
        _client?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
        _identity.Dispose();
        _database.Dispose();
    }

    [TestMethod]
    public async Task Acknowledge_PersistsOperatorUtcTimeAndBroadcastsUpdatedAlarm()
    {
        _socket = new RecordingSocket();
        _socketId = _notifier.AddClient(_socket);

        using HttpResponseMessage response = await PostAcknowledgementAsync(_alarmId, "MIX-001");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        AlarmAcknowledgementResponse acknowledgement =
            (await response.Content.ReadFromJsonAsync<AlarmAcknowledgementResponse>())!;
        Assert.AreEqual(_alarmId, acknowledgement.AlarmId);
        Assert.AreEqual("MIX-001", acknowledgement.DeviceId);
        Assert.IsFalse(acknowledgement.AlreadyAcknowledged);
        Assert.AreEqual(TimeSpan.Zero, acknowledgement.AcknowledgedAtUtc.Offset);
        Assert.IsFalse(string.IsNullOrWhiteSpace(acknowledgement.OperatorId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(acknowledgement.OperatorName));

        using JsonDocument notification = JsonDocument.Parse(_socket.Messages.Single());
        Assert.AreEqual("alarmAcknowledged", notification.RootElement.GetProperty("eventName").GetString());
        Assert.AreEqual(_alarmId, notification.RootElement.GetProperty("message").GetProperty("alarmId").GetInt64());
        Assert.AreEqual(acknowledgement.OperatorId,
            notification.RootElement.GetProperty("message").GetProperty("acknowledgedByOperatorId").GetString());

        using HttpResponseMessage listResponse = await _client.GetAsync("/alarms");
        Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);
        AlarmStatusResponse persistedAlarm =
            (await listResponse.Content.ReadFromJsonAsync<AlarmStatusResponse[]>())!.Single();
        Assert.AreEqual(acknowledgement.OperatorId, persistedAlarm.AcknowledgedByOperatorId);
        Assert.AreEqual(acknowledgement.AcknowledgedAtUtc, persistedAlarm.AcknowledgedAtUtc);
    }

    [TestMethod]
    public async Task Acknowledge_IsIdempotentAndDoesNotNotifyTwice()
    {
        using HttpResponseMessage first = await PostAcknowledgementAsync(_alarmId, "MIX-001");
        using HttpResponseMessage second = await PostAcknowledgementAsync(_alarmId, "MIX-001");

        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, second.StatusCode);
        AlarmAcknowledgementResponse original =
            (await first.Content.ReadFromJsonAsync<AlarmAcknowledgementResponse>())!;
        AlarmAcknowledgementResponse duplicate =
            (await second.Content.ReadFromJsonAsync<AlarmAcknowledgementResponse>())!;
        Assert.IsFalse(original.AlreadyAcknowledged);
        Assert.IsTrue(duplicate.AlreadyAcknowledged);
        Assert.AreEqual(original.OperatorId, duplicate.OperatorId);
        Assert.AreEqual(original.AcknowledgedAtUtc, duplicate.AcknowledgedAtUtc);
    }

    [TestMethod]
    public async Task Acknowledge_UpdatesAllExistingOccurrencesForOnlyTheMatchingDeviceAndCode()
    {
        IAlarmAcknowledgementService service = _app.Services.GetRequiredService<IAlarmAcknowledgementService>();

        Task RecordAsync(string deviceId, DeviceTypes deviceType, string code) => service.RecordAlarmAsync(
            new EventEnvelope
            {
                DeviceId = deviceId,
                DeviceType = deviceType,
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
                Message = new AlarmBody { Code = code, Severity = "critical", Text = "Test occurrence" }
            },
            CancellationToken.None);

        await RecordAsync("MIX-001", DeviceTypes.Mixer, "MIXER_OVERHEAT");
        await RecordAsync("MIX-001", DeviceTypes.Mixer, "UNRELATED");
        await RecordAsync("OVN-001", DeviceTypes.Oven, "MIXER_OVERHEAT");
        _socket = new RecordingSocket();
        _socketId = _notifier.AddClient(_socket);

        using HttpResponseMessage response = await PostAcknowledgementAsync(_alarmId, "MIX-001");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(2, _socket.Messages.Count);
        await RecordAsync("MIX-001", DeviceTypes.Mixer, "MIXER_OVERHEAT");

        using HttpResponseMessage listResponse = await _client.GetAsync("/alarms");
        AlarmStatusResponse[] alarms = (await listResponse.Content.ReadFromJsonAsync<AlarmStatusResponse[]>())!;
        AlarmStatusResponse[] acknowledged = alarms.Where(item => item.AcknowledgedAtUtc is not null).ToArray();
        Assert.AreEqual(5, alarms.Length);
        Assert.AreEqual(2, acknowledged.Length);
        Assert.IsTrue(acknowledged.All(item => item.DeviceId == "MIX-001" && item.Code == "MIXER_OVERHEAT"));
        Assert.AreEqual(acknowledged[0].AcknowledgedAtUtc, acknowledged[1].AcknowledgedAtUtc);
        Assert.IsNull(alarms[0].AcknowledgedAtUtc);
    }

    [TestMethod]
    public async Task Acknowledge_UnknownAlarmReturnsNotFound()
    {
        using HttpResponseMessage response = await PostAcknowledgementAsync(long.MaxValue, "MIX-001");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual("Alarm not found.", problem.RootElement.GetProperty("title").GetString());
    }

    [TestMethod]
    public async Task AlarmFeed_PersistsAndBroadcastsNewAlarmIdsAndKeepsOlderAlarms()
    {
        _socket = new RecordingSocket();
        _socketId = _notifier.AddClient(_socket);
        IAlarmAcknowledgementService service =
            _app.Services.GetRequiredService<IAlarmAcknowledgementService>();

        for (int index = 0; index < 6; index++)
        {
            DateTime timestamp = DateTime.UtcNow.AddSeconds(index);
            await service.RecordAlarmAsync(new EventEnvelope
            {
                EventName = "alarm",
                DeviceId = "MIX-001",
                DeviceType = DeviceTypes.Mixer,
                Timestamp = timestamp.ToString("O"),
                Message = new AlarmBody
                {
                    Timestamp = timestamp,
                    Code = $"MIXER_ALARM_{index}",
                    Severity = "warning",
                    Text = $"Test alarm {index}"
                }
            }, CancellationToken.None);
        }

        using HttpResponseMessage response = await _client.GetAsync("/alarms");
        AlarmStatusResponse[] alarms = (await response.Content.ReadFromJsonAsync<AlarmStatusResponse[]>())!;
        Assert.AreEqual(7, alarms.Length);
        Assert.AreEqual(_alarmId, alarms[^1].AlarmId);
        Assert.AreEqual("MIXER_ALARM_5", alarms[0].Code);
        Assert.AreEqual(6, _socket.Messages.Count);
        using JsonDocument firstNotification = JsonDocument.Parse(_socket.Messages[0]);
        Assert.AreEqual("alarm", firstNotification.RootElement.GetProperty("eventName").GetString());
        Assert.IsTrue(firstNotification.RootElement.GetProperty("message").GetProperty("alarmId").GetInt64() > 0);
    }

    [TestMethod]
    public async Task Acknowledge_InvalidOrMismatchedRequestReturnsBadRequest()
    {
        using HttpResponseMessage invalid = await _client.PostAsJsonAsync(
            "/alarms/acknowledgements",
            new { alarmId = 0, deviceId = "" });
        using HttpResponseMessage mismatch = await PostAcknowledgementAsync(_alarmId, "OVN-001");

        Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, mismatch.StatusCode);
    }

    private Task<HttpResponseMessage> PostAcknowledgementAsync(long alarmId, string deviceId) =>
        _client.PostAsJsonAsync("/alarms/acknowledgements", new { alarmId, deviceId });

    private sealed class TestDbContextFactory(SqliteConnection connection)
        : IDbContextFactory<KitchenOsDbContext>
    {
        private readonly DbContextOptions<KitchenOsDbContext> _options =
            new DbContextOptionsBuilder<KitchenOsDbContext>()
                .UseSqlite(connection)
                .Options;

        public KitchenOsDbContext CreateDbContext() => new(_options);

        public Task<KitchenOsDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class RecordingSocket : WebSocket
    {
        public List<string> Messages { get; } = [];
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => WebSocketState.Open;
        public override string? SubProtocol => null;

        public override void Abort() { }
        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken) => Task.CompletedTask;
        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken) => Task.CompletedTask;
        public override void Dispose() { }
        public override Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            Messages.Add(Encoding.UTF8.GetString(buffer));
            return Task.CompletedTask;
        }
    }
}
