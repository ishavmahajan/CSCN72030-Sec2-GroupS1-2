using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests;

/// <summary>
/// Verifies telemetry loading and cycle controls for the simulated dishwasher.
/// </summary>
[TestClass]
public class DishwasherTests
{
    [TestMethod]
    public void FromJson_PicksTheOnlyListedValue()
    {
        var dishwasher = Dishwasher.FromJson(JsonSamples.Parse("""
            {
              "temperature": [60],
              "setpoint": [65],
              "cycle": ["running"],
              "mode": ["normalWash"],
              "waterLevel": [80],
              "detergent": [40],
              "power": [2100],
              "energy": [30.2]
            }
            """));

        Assert.AreEqual(60, dishwasher.Temperature);
        Assert.AreEqual(65, dishwasher.Setpoint);
        Assert.AreEqual("running", dishwasher.Cycle);
        Assert.AreEqual("normalWash", dishwasher.Mode);
        Assert.AreEqual(80, dishwasher.WaterLevel);
        Assert.AreEqual(40, dishwasher.Detergent);
    }

    [TestMethod]
    public void StartAndStop_ChangeTheCycle()
    {
        var dishwasher = new Dishwasher();

        dishwasher.StartComponent(null);
        Assert.AreEqual("running", dishwasher.Cycle);

        dishwasher.StopComponent(null);
        Assert.AreEqual("idle", dishwasher.Cycle);
    }

    [TestMethod]
    public void TrySetValue_Setpoint_UpdatesTheProperty()
    {
        var dishwasher = new Dishwasher();

        var accepted = dishwasher.TrySetValue("setpoint", 65.0, null, out var reason);

        Assert.IsTrue(accepted);
        Assert.IsNull(reason);
        Assert.AreEqual(65, dishwasher.Setpoint);
    }

    [TestMethod]
    public void TrySetValue_Cycle_UpdatesTheProperty()
    {
        var dishwasher = new Dishwasher();

        var accepted = dishwasher.TrySetValue("cycle", "washing", null, out var reason);

        Assert.IsTrue(accepted);
        Assert.IsNull(reason);
        Assert.AreEqual("washing", dishwasher.Cycle);
    }

    [TestMethod]
    public void TrySetValue_Temperature_IsReadOnly()
    {
        var dishwasher = new Dishwasher { Temperature = 60 };

        var accepted = dishwasher.TrySetValue("temperature", 70.0, null, out var reason);

        Assert.IsFalse(accepted);
        Assert.AreEqual("unknown property", reason);
        Assert.AreEqual(60, dishwasher.Temperature);
    }

    [TestMethod]
    public void TrySetMode_SupportedMode_UpdatesTheMode()
    {
        var dishwasher = new Dishwasher();

        var accepted = dishwasher.TrySetMode("sanitize", null, out var reason);

        Assert.IsTrue(accepted);
        Assert.IsNull(reason);
        Assert.AreEqual("sanitize", dishwasher.Mode);
    }

    [TestMethod]
    public void TrySetMode_UpdatesAnyRequestedMode()
    {
        var dishwasher = new Dishwasher();

        var accepted = dishwasher.TrySetMode("unknown", null, out var reason);

        Assert.IsTrue(accepted);
        Assert.IsNull(reason);
        Assert.AreEqual("unknown", dishwasher.Mode);
    }
}
