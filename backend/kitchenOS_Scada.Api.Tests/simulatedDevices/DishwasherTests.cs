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
              "cycle": ["running"],
              "waterLevel": [80],
              "detergent": [40],
              "power": [2100],
              "energy": [30.2]
            }
            """));

        Assert.AreEqual(60, dishwasher.Temperature);
        Assert.AreEqual("running", dishwasher.Cycle);
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
}
