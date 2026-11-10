using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Simulator;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests.Simulator;

/// <summary>
/// Verifies that the device simulator loads and replaces device configurations correctly.
/// </summary>
[TestClass]
public class DeviceSimulatorLoadTests
{
    [TestMethod]
    public void Load_CreatesDevicesWithConfiguredNamesAndTypes()
    {
        var simulator = new DeviceSimulator();

        simulator.Load(
            """
            [
              { "deviceName": "Mixer-1", "deviceType": "MIXER" },
              { "deviceName": "Sink-1", "deviceType": "SINK" }
            ]
            """,
            DeviceSimulatorTestData.SimulationJson,
            new RecordingMessageBus());

        var devices = simulator.GetDevices();

        Assert.AreEqual(2, devices.Count);
        Assert.IsInstanceOfType(devices[0], typeof(Mixer));
        Assert.AreEqual("Mixer-1", devices[0].DeviceName);
        Assert.IsInstanceOfType(devices[1], typeof(Sink));
        Assert.AreEqual("Sink-1", devices[1].DeviceName);
    }

    [TestMethod]
    public void Load_ClearsDevicesFromPreviousConfiguration()
    {
        var simulator = new DeviceSimulator();
        var messageBus = new RecordingMessageBus();

        simulator.Load(
            """[{ "deviceName": "Mixer-1", "deviceType": "MIXER" }]""",
            DeviceSimulatorTestData.SimulationJson,
            messageBus);

        simulator.Load(
            """[{ "deviceName": "Blender-1", "deviceType": "BLENDER" }]""",
            DeviceSimulatorTestData.SimulationJson,
            messageBus);

        var devices = simulator.GetDevices();
        Assert.AreEqual(1, devices.Count);
        var device = devices.Single();
        Assert.IsInstanceOfType(device, typeof(Blender));
        Assert.AreEqual("Blender-1", device.DeviceName);
    }
}
