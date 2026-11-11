using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Simulator;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests.Simulator;

/// <summary>
/// Verifies the simulator's current property-update behavior.
/// </summary>
[TestClass]
public class DeviceSimulatorUpdateTests
{
    [TestMethod]
    public void UpdateProperties_DoesNotChangeCommandValue()
    {
        var simulator = new DeviceSimulator();

        simulator.Load(
            """[{ "deviceName": "Mixer-1", "deviceType": "MIXER" }]""",
            DeviceSimulatorTestData.SimulationJson,
            new RecordingMessageBus());

        var devices = simulator.GetDevices();
        Assert.AreEqual(1, devices.Count);
        var mixer = devices.Single() as Mixer;
        Assert.IsNotNull(mixer);
        mixer.TrySetValue("speed", 999.0, null, out _);

        simulator.UpdateProperties();

        Assert.AreEqual(999.0, mixer.Speed);
    }
}
