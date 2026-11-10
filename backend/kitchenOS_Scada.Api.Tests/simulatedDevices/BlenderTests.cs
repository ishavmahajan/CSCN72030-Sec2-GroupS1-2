using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests;

/// <summary>
/// Verifies JSON loading and motor controls for the simulated blender.
/// </summary>
[TestClass]
public class BlenderTests
{
    [TestMethod]
    public void FromJson_ReadsMotorAndTheFirstSpeed()
    {
        // The simulator uses the first speed value from the telemetry list.
        var blender = Blender.FromJson(JsonSamples.Parse("""
            { "motor": "on", "speed": [8000, 12000], "power": 900, "energy": 6.1 }
            """));

        Assert.AreEqual("on", blender.Motor);
        Assert.AreEqual(8000, blender.Speed);
        Assert.AreEqual(900, blender.Power);
        Assert.AreEqual(6.1, blender.Energy);
    }

    [TestMethod]
    public void StartAndStop_ChangeTheMotor()
    {
        var blender = new Blender();

        // Lifecycle commands should switch the motor between on and off.
        Assert.IsTrue(blender.StartComponent(null));
        Assert.AreEqual("on", blender.Motor);

        Assert.IsTrue(blender.StopComponent(null));
        Assert.AreEqual("off", blender.Motor);
    }

    [TestMethod]
    public void TrySetValue_Speed_UpdatesSpeed()
    {
        var blender = new Blender();

        // A speed command should update the current blender speed.
        var accepted = blender.TrySetValue("speed", 12000.0, null, out var reason);

        Assert.IsTrue(accepted);
        Assert.IsNull(reason);
        Assert.AreEqual(12000, blender.Speed);
    }
}
