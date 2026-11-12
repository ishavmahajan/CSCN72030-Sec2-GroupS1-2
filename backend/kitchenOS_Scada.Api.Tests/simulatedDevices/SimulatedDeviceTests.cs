using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests;

/// <summary>
/// Verifies creation of each supported simulated device type.
/// </summary>
[TestClass]
public class SimulatedDeviceTests
{
    [TestMethod]
    [DataRow("WALK_IN_FREEZER", typeof(WalkInFreezer), """{"compressor":["on"],"door":["closed"]}""", DisplayName = "Creates a walk-in freezer")]
    [DataRow("STOVE", typeof(Stove), "{}", DisplayName = "Creates a stove")]
    [DataRow("OVEN", typeof(Oven), """{"door":["closed"],"mode":["bake"]}""", DisplayName = "Creates an oven")]
    [DataRow("MIXER", typeof(Mixer), """{"motor":["off"]}""", DisplayName = "Creates a mixer")]
    [DataRow("EXHAUST_FAN", typeof(ExhaustFan), """{"filter":["ok"],"powerState":["off"]}""", DisplayName = "Creates an exhaust fan")]
    [DataRow("BLENDER", typeof(Blender), "{}", DisplayName = "Creates a blender")]
    [DataRow("DISHWASHER", typeof(Dishwasher), """{"cycle":["idle"]}""", DisplayName = "Creates a dishwasher")]
    [DataRow("SINK", typeof(Sink), "{}", DisplayName = "Creates a sink")]
    public void Create_KnownType_ReturnsThatDevice(string deviceType, Type expectedType, string json)
    {
        var device = SimulatedDevice.Create(deviceType, JsonSamples.Parse(json));

        Assert.IsInstanceOfType(device, expectedType);
        Assert.AreNotEqual(string.Empty, device.DeviceId);
    }

    [TestMethod]
    public void Create_UnknownType_Throws()
    {
        var json = JsonSamples.Parse("{}");

        Assert.ThrowsExactly<ArgumentException>(() => SimulatedDevice.Create("TOASTER", json));
    }
}
