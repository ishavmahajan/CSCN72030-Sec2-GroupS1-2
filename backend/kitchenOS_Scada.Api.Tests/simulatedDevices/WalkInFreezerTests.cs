using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests;

/// <summary>
/// Verifies telemetry loading and setpoint or mode controls for the simulated freezer.
/// </summary>
[TestClass]
public class WalkInFreezerTests
{
    [TestMethod]
    public void FromJson_PicksTheOnlyListedValue()
    {
        var freezer = WalkInFreezer.FromJson(JsonSamples.Parse("""
            {
              "temperature": [-18.2],
              "setpoint": [-18],
              "compressor": ["on"],
              "door": ["closed"],
              "power": [860],
              "energy": [412.6]
            }
            """));

        Assert.AreEqual(DeviceTypes.WalkInFreezer, freezer.DeviceType);
        Assert.AreEqual(-18.2, freezer.Temperature);
        Assert.AreEqual(-18, freezer.Setpoint);
        Assert.AreEqual("on", freezer.Compressor);
        Assert.AreEqual("closed", freezer.Door);
        Assert.AreEqual(860, freezer.Power);
        Assert.AreEqual(412.6, freezer.Energy);
    }

    [TestMethod]
    public void StartAndStop_ChangeOnlyTheFreezerRunningState()
    {
        var freezer = new WalkInFreezer { Compressor = "on", Power = 860, Energy = 25 };
        Assert.IsTrue(freezer.IsRunning);
        Assert.IsTrue((bool)freezer.ReadPoints().Single(point => point.Name == "isRunning").Value);

        Assert.IsTrue(freezer.StopComponent(null));
        Assert.IsFalse(freezer.IsRunning);
        Assert.IsFalse((bool)freezer.ReadPoints().Single(point => point.Name == "isRunning").Value);

        Assert.IsTrue(freezer.StartComponent(null));
        Assert.IsTrue(freezer.IsRunning);
        Assert.IsTrue((bool)freezer.ReadPoints().Single(point => point.Name == "isRunning").Value);
    }

    [TestMethod]
    public void TrySetValue_SetpointAndMode_UpdateThoseProperties()
    {
        var freezer = new WalkInFreezer();

        var setpointAccepted = freezer.TrySetValue("setpoint", -20.0, null, out var setpointReason);
        var modeAccepted = freezer.TrySetMode("defrost", null, out var modeReason);

        Assert.IsTrue(setpointAccepted);
        Assert.IsNull(setpointReason);
        Assert.AreEqual(-20, freezer.Setpoint);
        Assert.IsTrue(modeAccepted);
        Assert.IsNull(modeReason);
        Assert.AreEqual("defrost", freezer.Mode);
    }
}
