using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Enums;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests;

/// <summary>
/// Verifies JSON loading and lifecycle commands for the simulated mixer.
/// </summary>
[TestClass]
public class MixerTests
{
    [TestMethod]
    public void FromJson_PicksMotorAndSpeedFromTheLists()
    {
        // Mixer telemetry represents scalar values as single-item arrays.
        var mixer = Mixer.FromJson(JsonSamples.Parse("""
            { "motor": ["on"], "speed": [120], "power": [750], "energy": [15.2] }
            """));

        Assert.AreEqual(DeviceTypes.Mixer, mixer.DeviceType);
        Assert.AreEqual("on", mixer.Motor);
        Assert.AreEqual(120, mixer.Speed);
        Assert.AreEqual(750, mixer.Power);
        Assert.AreEqual(15.2, mixer.Energy);
    }

    [TestMethod]
    public void StartAndStop_ChangeTheMotor()
    {
        var mixer = new Mixer();

        // Starting and stopping should update the mixer motor state.
        mixer.StartComponent(null);
        Assert.AreEqual("on", mixer.Motor);

        mixer.StopComponent(null);
        // Preserve the simulator's current trailing-space state value.
        Assert.AreEqual("off ", mixer.Motor);
    }
}
