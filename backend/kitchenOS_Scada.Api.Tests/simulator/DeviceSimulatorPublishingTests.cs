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
            messageBus);
        simulator.LoadAlarms(DeviceSimulatorTestData.AlarmJson);

        await simulator.PublishEvents();

        var telemetryMessages = messageBus.Messages
            .Where(message => message.Channel == MessageChannels.TelemetryEvents)
            .ToArray();
        Assert.AreEqual(2, telemetryMessages.Length);
        Assert.IsTrue(messageBus.Messages.All(message =>
            message.Channel == MessageChannels.TelemetryEvents));
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
    public async Task PublishAlarms_PublishesExactlyOneApplicableRandomAlarm()
    {
        var simulator = new DeviceSimulator(new Random(1234));
        var messageBus = new RecordingMessageBus();

        simulator.Load(
            """
            [
              { "deviceId": "MIX-001", "deviceName": "Mixer-1", "deviceType": "MIXER" },
              { "deviceId": "SNK-001", "deviceName": "Sink-1", "deviceType": "SINK" }
            ]
            """,
            DeviceSimulatorTestData.SimulationJson,
            messageBus);
        simulator.LoadAlarms(DeviceSimulatorTestData.AlarmJson);

        await simulator.PublishAlarms();

        var alarmMessages = messageBus.Messages
            .Where(message => message.Channel == MessageChannels.AlarmEvents)
            .ToArray();
        Assert.AreEqual(1, alarmMessages.Length);

        var alarmMessage = alarmMessages[0];
        var envelope = alarmMessage.Message as EventEnvelope;
        Assert.IsNotNull(envelope);
        Assert.AreEqual("alarm", envelope.EventName);
        Assert.IsTrue(envelope.DeviceId is "MIX-001" or "SNK-001");
        Assert.IsInstanceOfType(envelope.Message, typeof(AlarmBody));

        var body = (AlarmBody)envelope.Message;
        var applicableAlarms = simulator.GetAlarms(envelope.DeviceId);
        Assert.IsTrue(applicableAlarms.Any(alarm =>
            alarm.Code == body.Code &&
            alarm.Severity == body.Severity &&
            alarm.Text == body.Text));
    }
}
