using System.Net;
using kitchenOS_Scada.Api.Tests.Security;
using kitchenOS_Scada.Api.Security;
using kitchenOS_Scada.Api.Features.Devices.Contracts;
using kitchenOS_Scada.Api.Features.Devices.Queries;
using kitchenOS_Scada.Api.Features.Devices.Registration;
using kitchenOS_Scada.Api.Features.Devices.Removal;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using kitchenOS.Simulator;
using kitchenOS_Scada.Api.Features.Alarms.Acknowledgement;
using kitchenOS_Scada.Api.Features.Alarms.Contracts;
using kitchenOS_Scada.Api.Features.Devices;
using kitchenOS_Scada.Api.Infrastructure.Bus;
using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Api.Persistence;
using kitchenOS_Scada.Api.Persistence.Entities;
using kitchenOS_Scada.Api.Persistence.Repositories;
using kitchenOS.Simulator.Devices;
using kitchenOS_Scada.Api.Tests.Simulator;
using kitchenOS_Scada.Contract.Events;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Contract.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kitchenOS_Scada.Api.Tests.Commands;

// Exercises command validation, publication, and WebSocket result delivery.
[TestClass]
public class DeviceCommandEndpointTests
{
    private readonly RecordingMessageBus _bus = new();
    private readonly TestIdentityProvider _identity = new();
    private readonly DeviceSimulator _simulator = new();
    private readonly CancellationTokenSource _shutdown = new();
    private WebApplication _app = default!;
    private HttpClient _client = default!;

    [TestInitialize]
    public async Task Initialize()
    {
        // Use an in-memory bus so endpoint tests verify behavior without Redis.
        _simulator.Load(
            """
            [
              {"deviceId":"MIX-001","deviceName":"Mixer","deviceType":"MIXER"},
              {"deviceId":"SNK-001","deviceName":"Sink","deviceType":"SINK"},
              {"deviceId":"OVN-001","deviceName":"Oven","deviceType":"OVEN"}
            ]
            """,
            """
            {
              "MIXER":{"motor":["off"],"speed":[120],"power":[750],"energy":[15]},
              "SINK":{"temperature":[42],"valve":["open"]},
              "OVEN":{"door":["closed"],"mode":["bake"]}
            }
            """,
            _bus);

        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _identity.Configure(builder.Services);
        UnusedInventoryServices inventory = new();
        builder.Services.AddSingleton<IMessageBus>(_bus);
        builder.Services.AddSingleton(_simulator);
        builder.Services.AddDeviceManagement();
        builder.Services.RemoveAll<IDeviceRepository>();
        builder.Services.AddSingleton<IDeviceRepository>(new FakeDeviceRepository(
            "MIX-001",
            "SNK-001",
            "OVN-001"));
        builder.Services.AddSingleton<IDeviceQueryService>(inventory);
        builder.Services.AddSingleton<IDeviceRegistrationService>(inventory);
        builder.Services.AddSingleton<IDeviceRemovalService>(inventory);
        builder.Services.AddScoped<IDeviceCommandService, DeviceCommandService>();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(DevicesController).Assembly);
        _app = builder.Build();
        await _identity.InitializeAsync(_app.Services);
        _app.UseKitchenOsSecurity();
        _app.MapKitchenOsIdentity();
        _app.MapControllers();
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()) };
        await _identity.LoginAsync(_client);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        // Stop background simulation work before disposing the test host.
        _shutdown.Cancel();
        _simulator.Stop();
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
        _shutdown.Dispose();
        _identity.Dispose();
    }

    [DataTestMethod]
    [DataRow("start")]
    [DataRow("stop")]
    [DataRow("isOnline")]
    public async Task ValidCommand_QueuesOnRequiredChannelWithUniqueIdAndUtcTimestamp(string commandName)
    {
        HttpResponseMessage first = await PostAsync("MIX-001", $$"""{"commandName":"{{commandName}}"}""");
        HttpResponseMessage second = await PostAsync("MIX-001", $$"""{"commandName":"{{commandName}}"}""");

        Assert.AreEqual(HttpStatusCode.Accepted, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.Accepted, second.StatusCode);
        QueuedDeviceCommandResponse queued = (await first.Content.ReadFromJsonAsync<QueuedDeviceCommandResponse>())!;
        DeviceCommand[] commands = _bus.Messages.Select(message => message.Message).OfType<DeviceCommand>().ToArray();
        Assert.AreEqual(2, commands.Length);
        Assert.AreEqual("queued", queued.Status);
        Assert.AreEqual("MIX-001", queued.DeviceId);
        Assert.AreEqual(queued.CommandId, commands[0].CommandId);
        Assert.AreNotEqual(commands[0].CommandId, commands[1].CommandId);
        Assert.IsTrue(Guid.TryParse(queued.CommandId, out _));
        Assert.AreEqual(commandName, commands[0].CommandName);
        Assert.AreEqual("MIX-001", commands[0].DeviceId);
        Assert.AreEqual(TimeSpan.Zero, DateTimeOffset.Parse(commands[0].Timestamp).Offset);
        Assert.IsTrue(commands[0].Timestamp.EndsWith("Z", StringComparison.Ordinal));
        Assert.IsTrue(_bus.Messages.All(message => message.Channel == "kitchenos:commands"));
        Assert.IsFalse(_bus.Messages.Any(message => message.Message is EventEnvelope));
    }

    [TestMethod]
    public async Task SetValue_PreservesPointAndNumericValue()
    {
        HttpResponseMessage response = await PostAsync("MIX-001",
            """{"commandName":"setValue","arguments":{"name":"speed","value":180,"unit":"rpm"}}""");
        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        DeviceCommand command = (DeviceCommand)_bus.Messages.Single().Message;
        Assert.AreEqual("speed", command.Arguments!.Name);
        Assert.AreEqual(180.0, command.Arguments.Value.GetDouble());
        Assert.AreEqual("rpm", command.Arguments.Unit);
    }

    [TestMethod]
    public async Task SetMode_PreservesModeAndSerializesMissingValue()
    {
        HttpResponseMessage response = await PostAsync("OVN-001",
            """{"commandName":"setMode","arguments":{"mode":"broil"}}""");
        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        DeviceCommand command = (DeviceCommand)_bus.Messages.Single().Message;
        DeviceCommand copy = JsonSerializer.Deserialize<DeviceCommand>(JsonSerializer.Serialize(command))!;
        Assert.AreEqual("broil", copy.Arguments!.Mode);
        Assert.AreEqual(JsonValueKind.Null, copy.Arguments.Value.ValueKind);
    }

    [DataTestMethod]
    [DataRow("{}")]
    [DataRow("null")]
    [DataRow("{\"commandName\":\"restart\"}")]
    [DataRow("{\"commandName\":\"setMode\"}")]
    [DataRow("{\"commandName\":\"setMode\",\"arguments\":{\"mode\":\" \"}}")]
    [DataRow("{\"commandName\":\"setValue\"}")]
    [DataRow("{\"commandName\":\"setValue\",\"arguments\":{\"name\":\"speed\"}}")]
    [DataRow("{\"commandName\":\"setValue\",\"arguments\":{\"name\":\"speed\",\"value\":null}}")]
    [DataRow("{\"commandName\":\"setValue\",\"arguments\":{\"name\":\"speed\",\"value\":\"fast\"}}")]
    [DataRow("{\"commandName\":\"setValue\",\"arguments\":{\"name\":\"speed\",\"value\":{}}}")]
    [DataRow("{\"commandName\":\"setValue\",\"arguments\":{\"name\":\"missing\",\"value\":1}}")]
    [DataRow("{\"commandName\":\"start\",\"arguments\":{\"component\":\"missing\"}}")]
    [DataRow("{broken json")]
    public async Task InvalidRequest_Returns400WithoutPublishing(string json)
    {
        HttpResponseMessage response = await PostAsync("MIX-001", json);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(0, _bus.Messages.Count);
    }

    [TestMethod]
    public async Task UnknownDevice_Returns404WithoutPublishing()
    {
        HttpResponseMessage response = await PostAsync("unknown", """{"commandName":"start"}""");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual(0, _bus.Messages.Count);
    }

    [TestMethod]
    public async Task MissingDeviceId_Returns400()
    {
        HttpResponseMessage response = await _client.PostAsync("/devices/commands",
            new StringContent("""{"commandName":"start"}""", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(0, _bus.Messages.Count);
    }

    [TestMethod]
    public async Task BusFailure_Returns503InsteadOfQueued()
    {
        _bus.PublishFailure = new TimeoutException("Bus unavailable");
        HttpResponseMessage response = await PostAsync("MIX-001", """{"commandName":"start"}""");
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.AreEqual(0, _bus.Messages.Count);
    }

    [TestMethod]
    public async Task QueueResponse_WaitsForPublishAcknowledgement()
    {
        TaskCompletionSource published = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _bus.PublishCompletion = published.Task;
        Task<HttpResponseMessage> pending = PostAsync("MIX-001", """{"commandName":"start"}""");
        Assert.IsFalse(pending.IsCompleted);
        published.SetResult();
        HttpResponseMessage result = await pending;
        Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
    }

    [DataTestMethod]
    [DataRow("MIX-001", "{\"commandName\":\"start\"}", "acknowledged")]
    [DataRow("MIX-001", "{\"commandName\":\"stop\"}", "acknowledged")]
    [DataRow("MIX-001", "{\"commandName\":\"isOnline\"}", "acknowledged")]
    [DataRow("MIX-001", "{\"commandName\":\"setValue\",\"arguments\":{\"name\":\"speed\",\"value\":180}}", "acknowledged")]
    [DataRow("MIX-001", "{\"commandName\":\"setValue\",\"arguments\":{\"name\":\"motor\",\"value\":\"on\"}}", "acknowledged")]
    [DataRow("OVN-001", "{\"commandName\":\"setMode\",\"arguments\":{\"mode\":\"broil\"}}", "acknowledged")]
    [DataRow("MIX-001", "{\"commandName\":\"setMode\",\"arguments\":{\"mode\":\"broil\"}}", "rejected")]
    [DataRow("SNK-001", "{\"commandName\":\"start\"}", "rejected")]
    public async Task SimulatorResult_ReachesSocketWithOriginalCommandId(string deviceId, string json, string expectedStatus)
    {
        await _simulator.Start(_shutdown.Token);
        await _bus.WaitForSubscriptionAsync("kitchenos:commands");
        foreach (SimulatedDevice device in _simulator.GetDevices())
        {
            await _bus.WaitForTelemetryAsync(device.DeviceId);
        }
        WebSocketClientNotifier notifier = new();
        using RecordingSocket socket = new();
        notifier.AddClient(socket);
        using MessageBusService service = new(_bus,
            new TelemetryEventHandler(notifier), new AlarmEventHandler(new RecordingAlarmService(notifier)),
            new CommandResultEventHandler(notifier), NullLogger<MessageBusService>.Instance);
        await service.StartAsync(_shutdown.Token);
        try
        {
            await _bus.WaitForSubscriptionAsync("kitchenos:events:command-results");
            HttpResponseMessage response = await PostAsync(deviceId, json);
            Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
            QueuedDeviceCommandResponse queued = (await response.Content.ReadFromJsonAsync<QueuedDeviceCommandResponse>())!;
            DeviceCommand command = _bus.Messages.Select(message => message.Message).OfType<DeviceCommand>().Single();
            Assert.IsFalse(_bus.Messages.Any(message => message.Message is EventEnvelope envelope &&
                envelope.EventName == "command_result"));

            await _bus.DispatchAsync("kitchenos:commands", command);
            EventEnvelope result = _bus.Messages.Where(message => message.Channel == "kitchenos:events:command-results")
                .Select(message => (EventEnvelope)message.Message).Single();
            CommandResultBody body = (CommandResultBody)result.Message;
            Assert.AreEqual(queued.CommandId, body.CommandId);
            Assert.AreEqual(deviceId, result.DeviceId);
            Assert.AreEqual(expectedStatus, body.Status);
            Assert.AreEqual(DateTimeKind.Utc, body.Timestamp.Kind);
            if (expectedStatus == "rejected")
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(body.Reason));
            }
            await _bus.DispatchAsync("kitchenos:events:command-results", result);
            using JsonDocument delivered = JsonDocument.Parse(socket.Messages.Single());
            Assert.AreEqual("command_result", delivered.RootElement.GetProperty("eventName").GetString());
            Assert.AreEqual(queued.CommandId, delivered.RootElement.GetProperty("message").GetProperty("commandId").GetString());

            if (command.CommandName == "setValue" && deviceId == "MIX-001" && command.Arguments!.Name == "speed")
            {
                Assert.AreEqual(180.0, ((Mixer)_simulator.GetDevices()[0]).Speed);
            }
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task ExistingTelemetryAndAlarmSubscriptions_StillForwardToClients()
    {
        RecordingNotifier notifier = new();
        using MessageBusService service = new(_bus,
            new TelemetryEventHandler(notifier), new AlarmEventHandler(new RecordingAlarmService(notifier)),
            new CommandResultEventHandler(notifier), NullLogger<MessageBusService>.Instance);
        await service.StartAsync(_shutdown.Token);
        try
        {
            await _bus.WaitForSubscriptionAsync(MessageChannels.CommandResultEvents);
            EventEnvelope telemetry = new() { EventName = "telemetry", Message = new TelemetryBody() };
            EventEnvelope alarm = new() { EventName = "alarm", Message = new TelemetryBody() };
            await _bus.DispatchAsync(MessageChannels.TelemetryEvents, telemetry);
            await _bus.DispatchAsync(MessageChannels.AlarmEvents, alarm);
            CollectionAssert.AreEqual(new[] { "telemetry", "alarm" }, notifier.Events.ToArray());
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task RemovingDevice_DeactivatesAndPublishesRemovalEventOnlyOnce()
    {
        FakeDeviceRepository repository = new("MIX-001");
        DeviceRemovalService service = new(repository, _bus);

        DeviceOperationResult<bool> removed = await service.RemoveAsync("MIX-001");
        DeviceRemoved removalEvent = _bus.Messages
            .Select(message => message.Message)
            .OfType<DeviceRemoved>()
            .Single();
        DeviceOperationResult<bool> repeated = await service.RemoveAsync("MIX-001");

        Assert.AreEqual(DeviceOperationError.None, removed.Error);
        Assert.AreEqual(MessageChannels.DeviceInventoryRemovals,
            _bus.Messages.Single().Channel);
        Assert.AreEqual("MIX-001", removalEvent.DeviceId);
        Assert.AreEqual(DeviceOperationError.NotFound, repeated.Error);
        Assert.AreEqual(1, _bus.Messages.Count);
    }

    private Task<HttpResponseMessage> PostAsync(string deviceId, string json) =>
        _client.PostAsync($"/devices/{deviceId}/commands", new StringContent(json, Encoding.UTF8, "application/json"));

    // Command tests never invoke inventory operations or require PostgreSQL.
    private sealed class UnusedInventoryServices : IDeviceQueryService, IDeviceRegistrationService, IDeviceRemovalService
    {
        public Task<IReadOnlyList<DeviceInventoryResponse>> GetActiveAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DeviceInventoryResponse?> GetByIdAsync(string deviceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DeviceOperationResult<DeviceInventoryResponse>> RegisterAsync(RegisterDeviceRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DeviceOperationResult<bool>> RemoveAsync(string deviceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeDeviceRepository(params string[] deviceIds) : IDeviceRepository
    {
        private readonly HashSet<string> _deviceIds =
            new(deviceIds, StringComparer.OrdinalIgnoreCase);

        public Task<bool> ExistsAsync(
            string deviceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_deviceIds.Contains(deviceId));
        }

        public Task<IReadOnlyList<DeviceEntity>> GetActiveAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<DeviceEntity?> GetByIdAsync(
            string deviceId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(
            DeviceEntity device,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeactivateAsync(
            string deviceId,
            DateTimeOffset deactivatedAtUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_deviceIds.Remove(deviceId));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingNotifier : IClientNotifier
    {
        public List<string> Events { get; } = [];
        public Task Notify(IEvent deviceEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(deviceEvent.EventName);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSocket : WebSocket
    {
        public List<string> Messages { get; } = [];
        public override WebSocketState State => WebSocketState.Open;
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override string? SubProtocol => null;
        public override void Abort() { }
        public override void Dispose() { }
        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();
        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            Messages.Add(Encoding.UTF8.GetString(buffer));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAlarmService(IClientNotifier notifier)
        : IAlarmAcknowledgementService
    {
        public Task<IReadOnlyList<AlarmStatusResponse>> GetRecentAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AlarmAcknowledgementResult> AcknowledgeAsync(
            AcknowledgeAlarmRequest request,
            string operatorId,
            string operatorName,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RecordAlarmAsync(
            EventEnvelope eventMessage,
            CancellationToken cancellationToken) =>
            notifier.Notify(eventMessage, cancellationToken);
    }
}

