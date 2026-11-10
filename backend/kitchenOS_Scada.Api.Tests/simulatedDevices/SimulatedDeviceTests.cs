using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests;

/// <summary>
/// Verifies creation of each supported simulated device type.
/// </summary>
[TestClass]
public class SimulatedDeviceTests
{
    [DataTestMethod]
    [DataRow("WALK_IN_FREEZER", typeof(WalkInFreezer), """{"compressor":["on"],"door":["closed"]}""")]
    [DataRow("STOVE", typeof(Stove), "{}")]
    [DataRow("OVEN", typeof(Oven), """{"door":["closed"],"mode":["bake"]}""")]
    [DataRow("MIXER", typeof(Mixer), """{"motor":["off"]}""")]
    [DataRow("EXHAUST_FAN", typeof(ExhaustFan), """{"filter":["ok"],"powerState":["off"]}""")]
    [DataRow("BLENDER", typeof(Blender), "{}")]
    [DataRow("DISHWASHER", typeof(Dishwasher), """{"cycle":["idle"]}""")]
    [DataRow("SINK", typeof(Sink), "{}")]
    public void Create_KnownType_ReturnsThatDevice(string deviceType, Type expectedType, string json)
    {
        var device = SimulatedDevice.Create(deviceType, JsonSamples.Parse(json));

        Assert.IsInstanceOfType(device, expectedType);
        Assert.AreNotEqual(Guid.Empty, device.DeviceId);
    }

    [TestMethod]
    public void Create_UnknownType_Throws()
    {
        var json = JsonSamples.Parse("{}");

        Assert.ThrowsException<ArgumentException>(() => SimulatedDevice.Create("TOASTER", json));
    }
}
