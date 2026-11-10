using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests;

/// <summary>
/// Verifies telemetry loading, mode controls, and door updates for the simulated oven.
/// </summary>
[TestClass]
public class OvenTests
{
    [TestMethod]
    public void FromJson_PicksTheOnlyListedValue()
    {
        var oven = Oven.FromJson(JsonSamples.Parse("""
            {
              "temperature": [176],
              "setpoint": [180],
              "timer": [1200],
              "door": ["closed"],
              "mode": ["bake"],
              "power": [3200],
              "energy": [88.4]
            }
            """));

        Assert.AreEqual(176, oven.Temperature);
        Assert.AreEqual(180, oven.Setpoint);
        Assert.AreEqual(1200, oven.Timer);
        Assert.AreEqual("closed", oven.Door);
        Assert.AreEqual("bake", oven.Mode);
    }

    [TestMethod]
    public void StartAndStop_ChangeTheMode()
    {
        var oven = new Oven { Mode = "idle" };

        oven.StartComponent(null);
        Assert.AreEqual("bake", oven.Mode);

        oven.StopComponent(null);
        Assert.AreEqual("off", oven.Mode);
    }

    [TestMethod]
    public void TrySetValue_Door_UpdatesTheDoor()
    {
        var oven = new Oven();

        var accepted = oven.TrySetValue("door", "open", null, out var reason);

        Assert.IsTrue(accepted);
        Assert.IsNull(reason);
        Assert.AreEqual("open", oven.Door);
    }
}
