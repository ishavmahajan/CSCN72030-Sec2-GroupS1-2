using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using kitchenOS.Simulator;
using kitchenOS_Scada.Api.Commands;
using kitchenOS_Scada.Api.Infrastructure.Bus;
using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Api.Simulator.Devices;
using kitchenOS_Scada.Api.Tests.Simulator;
using kitchenOS_Scada.Contract.Events;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Contract.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kitchenOS_Scada.Api.Tests.Commands;

[TestClass]
public class DeviceCommandEndpointTests
{
    private readonly RecordingMessageBus _bus = new();
    private readonly DeviceSimulator _simulator = new();
    private readonly CancellationTokenSource _shutdown = new();
    private WebApplication _app = default!;
    private HttpClient _client = default!;

    [TestInitialize]
    public async Task Initialize()
    {
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

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IMessageBus>(_bus);
        _app = builder.Build();
        DeviceCommandEndpoint.MapEndpoints(_app, _simulator);
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()) };
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _shutdown.Cancel();
        _simulator.Stop();
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
        _shutdown.Dispose();
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
        QueuedCommandResponse queued = (await first.Content.ReadFromJsonAsync<QueuedCommandResponse>())!;
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
        Task<IResult> pending = DeviceCommandEndpoint.SendAsync("MIX-001",
            new CommandRequest { CommandName = "start" }, _simulator, _bus,
            NullLogger<DeviceCommandEndpoint>.Instance);
        Assert.IsFalse(pending.IsCompleted);
        published.SetResult();
        IResult result = await pending;
        Assert.AreEqual(202, ((Microsoft.AspNetCore.Http.IStatusCodeHttpResult)result).StatusCode);
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
            new TelemetryEventHandler(notifier), new AlarmEventHandler(notifier),
            new CommandResultEventHandler(notifier), NullLogger<MessageBusService>.Instance);
        await service.StartAsync(_shutdown.Token);
        try
        {
            await _bus.WaitForSubscriptionAsync("kitchenos:events:command-results");
            HttpResponseMessage response = await PostAsync(deviceId, json);
            Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
            QueuedCommandResponse queued = (await response.Content.ReadFromJsonAsync<QueuedCommandResponse>())!;
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
            new TelemetryEventHandler(notifier), new AlarmEventHandler(notifier),
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

    private Task<HttpResponseMessage> PostAsync(string deviceId, string json) =>
        _client.PostAsync($"/devices/{deviceId}/commands", new StringContent(json, Encoding.UTF8, "application/json"));

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
}
