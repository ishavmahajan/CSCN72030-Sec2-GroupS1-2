using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests;

/// <summary>
/// Verifies telemetry loading and power controls for the simulated exhaust fan.
/// </summary>
[TestClass]
public class ExhaustFanTests
{
    [TestMethod]
    public void FromJson_PicksTheOnlyListedValue()
    {
        var fan = ExhaustFan.FromJson(JsonSamples.Parse("""
            {
              "fanSpeed": [60],
              "airflow": [1200],
              "filter": ["ok"],
              "powerState": ["on"],
              "power": [410],
              "energy": [90.5]
            }
            """));

        Assert.AreEqual(60, fan.FanSpeed);
        Assert.AreEqual(1200, fan.Airflow);
        Assert.AreEqual("ok", fan.Filter);
        Assert.AreEqual("on", fan.PowerState);
    }

    [TestMethod]
    public void StartAndStop_ChangePowerState()
    {
        var fan = new ExhaustFan();

        fan.StartComponent(null);
        Assert.AreEqual("on", fan.PowerState);

        fan.StopComponent(null);
        Assert.AreEqual("off", fan.PowerState);
    }
}
