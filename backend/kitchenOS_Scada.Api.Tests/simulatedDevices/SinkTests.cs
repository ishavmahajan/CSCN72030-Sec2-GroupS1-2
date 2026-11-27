using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Contract.Enums;
using kitchenOS.Simulator.Devices;

namespace kitchenOS_Scada.Api.Tests;

/// <summary>
/// Verifies telemetry loading, point mapping, and property updates for the simulated sink.
/// </summary>
[TestClass]
public class SinkTests
{
    [TestMethod]
    public void FromJson_ReadsEachProperty()
    {
        var sink = Sink.FromJson(JsonSamples.Parse("""
            {
              "temperature": 42,
              "setpoint": 40,
              "mode": "MANUAL",
              "flow": 6.5,
              "consumption": 1280,
              "leak": false,
              "valve": "open"
            }
            """));

        Assert.AreEqual(DeviceTypes.Sink, sink.DeviceType);
        Assert.AreEqual(42, sink.Temperature);
        Assert.AreEqual(40, sink.Setpoint);
        Assert.AreEqual("MANUAL", sink.Mode);
        Assert.AreEqual(6.5, sink.Flow);
        Assert.AreEqual(1280, sink.Consumption);
        Assert.IsFalse(sink.Leak);
        Assert.AreEqual("open", sink.Valve);
    }

    [TestMethod]
    public void ReadPoints_UsesSinkNamesAndUnits()
    {
        var sink = Sink.FromJson(JsonSamples.Parse("""
            { "temperature": 42, "setpoint": 40, "mode": "MANUAL", "flow": 6.5, "consumption": 1280, "leak": false, "valve": "open" }
            """));

        var points = sink.ReadPoints();

        Assert.AreEqual("C", Point(points, "temperature").Unit);
        Assert.AreEqual(40.0, Point(points, "setpoint").Value);
        Assert.AreEqual("C", Point(points, "setpoint").Unit);
        Assert.AreEqual("MANUAL", Point(points, "mode").Value);
        Assert.AreEqual("L/min", Point(points, "flow").Unit);
        Assert.AreEqual("L", Point(points, "consumption").Unit);
        Assert.AreEqual(false, Point(points, "leak").Value);
        Assert.AreEqual("open", Point(points, "valve").Value);
        Assert.IsFalse(points.Any(point => point.Name is "power" or "energy"));
    }

    [TestMethod]
    public void TrySetValue_Setpoint_UpdatesTheProperty()
    {
        var sink = new Sink();

        var accepted = sink.TrySetValue("setpoint", 55.0, null, out var reason);

        Assert.IsTrue(accepted);
        Assert.IsNull(reason);
        Assert.AreEqual(55, sink.Setpoint);
    }

    [TestMethod]
    public void TrySetValue_Temperature_IsReadOnly()
    {
        var sink = new Sink { Temperature = 42 };

        var accepted = sink.TrySetValue("temperature", 55.0, null, out var reason);

        Assert.IsFalse(accepted);
        Assert.AreEqual("unknown property", reason);
        Assert.AreEqual(42, sink.Temperature);
    }

    [TestMethod]
    public void TrySetMode_UpdatesMode()
    {
        var sink = new Sink();

        var accepted = sink.TrySetMode("DRAIN", null, out var reason);

        Assert.IsTrue(accepted);
        Assert.IsNull(reason);
        Assert.AreEqual("DRAIN", sink.Mode);
    }

    [TestMethod]
    public void TrySetValue_UnknownName_IsRejected()
    {
        var sink = new Sink();

        var accepted = sink.TrySetValue("speed", 10.0, null, out var reason);

        Assert.IsFalse(accepted);
        Assert.AreEqual("unknown property", reason);
    }

    private static PointReading Point(List<PointReading> points, string name)
    {
        return points.Single(point => point.Name == name);
    }
}
