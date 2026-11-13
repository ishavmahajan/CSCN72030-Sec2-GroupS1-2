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

        await simulator.PublishEvents();

        Assert.AreEqual(2, messageBus.Messages.Count);
        Assert.IsTrue(messageBus.Messages.All(message => message.Channel == MessageChannels.TelemetryEvents));
        foreach (var message in messageBus.Messages)
        {
            var envelope = message.Message as EventEnvelope;
            Assert.IsNotNull(envelope);
            Assert.AreEqual("telemetry", envelope.EventName);
            Assert.AreNotEqual(Guid.Empty.ToString(), envelope.DeviceId);
            Assert.IsInstanceOfType(envelope.Message, typeof(TelemetryBody));
        }
    }
}
