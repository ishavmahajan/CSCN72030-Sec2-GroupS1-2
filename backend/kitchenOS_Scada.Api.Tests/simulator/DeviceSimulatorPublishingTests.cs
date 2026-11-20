using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS.Simulator;
using kitchenOS_Scada.Contract.Events;
using kitchenOS_Scada.Contract.Infrastructure.Bus;

namespace kitchenOS_Scada.Api.Tests.Simulator;

/// <summary>
/// Verifies telemetry publication for every device loaded by the simulator.
/// </summary>
[TestClass]
public class DeviceSimulatorPublishingTests
{
    [TestMethod]
    public async Task PublishEvents_PublishesTelemetryForEachLoadedDevice()
    {
        var simulator = new DeviceSimulator();
        var messageBus = new RecordingMessageBus();

        simulator.Load(
            """
            [
              { "deviceName": "Mixer-1", "deviceType": "MIXER" },
              { "deviceName": "Sink-1", "deviceType": "SINK" }
            ]
            """,
            DeviceSimulatorTestData.SimulationJson,
            DeviceSimulatorTestData.AlarmJson,
            messageBus);

        await simulator.PublishEvents();

        var telemetryMessages = messageBus.Messages
            .Where(message => message.Channel == MessageChannels.TelemetryEvents)
            .ToArray();
        Assert.AreEqual(2, telemetryMessages.Length);
        Assert.IsTrue(messageBus.Messages.All(message =>
            message.Channel is MessageChannels.TelemetryEvents or MessageChannels.AlarmEvents));
        foreach (var message in telemetryMessages)
        {
            var envelope = message.Message as EventEnvelope;
            Assert.IsNotNull(envelope);
            Assert.AreEqual("telemetry", envelope.EventName);
            Assert.AreNotEqual(Guid.Empty.ToString(), envelope.DeviceId);
            Assert.IsInstanceOfType(envelope.Message, typeof(TelemetryBody));
        }
    }

    [TestMethod]
    public async Task PublishEvents_WhenRandomAlarmIsSelected_PublishesApplicableAlarm()
    {
        // Ensure the first random draw takes the 10% alarm-generation branch.
        int seed = Enumerable.Range(0, 1000)
            .First(candidate => new Random(candidate).NextDouble() < 0.1);
        var simulator = new DeviceSimulator(new Random(seed));
        var messageBus = new RecordingMessageBus();

        simulator.Load(
            """[{ "deviceId": "MIX-001", "deviceName": "Mixer-1", "deviceType": "MIXER" }]""",
            DeviceSimulatorTestData.SimulationJson,
            DeviceSimulatorTestData.AlarmJson,
            messageBus);

        await simulator.PublishEvents();

        var alarmMessage = messageBus.Messages.Single(message =>
            message.Channel == MessageChannels.AlarmEvents);
        var envelope = alarmMessage.Message as EventEnvelope;
        Assert.IsNotNull(envelope);
        Assert.AreEqual("alarm", envelope.EventName);
        Assert.AreEqual("MIX-001", envelope.DeviceId);
        Assert.IsInstanceOfType(envelope.Message, typeof(AlarmBody));

        var body = (AlarmBody)envelope.Message;
        Assert.AreEqual("fault", body.Code);
        Assert.AreEqual("critical", body.Severity);
        Assert.AreEqual("Mixer motor overload protection tripped", body.Text);
    }
}
