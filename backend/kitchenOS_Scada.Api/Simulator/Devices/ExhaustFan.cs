using System.Text.Json;
using kitchenOS_Scada.Api.Enums;

namespace kitchenOS_Scada.Api.Simulator.Devices;

public class ExhaustFan : SimulatedDevice
{
    public double FanSpeed { get; set; }
    public double Airflow { get; set; }
    public string Filter { get; set; } = "ok";
    public string PowerState { get; set; } = "off";
    public double Power { get; set; }
    public double Energy { get; set; }

    public ExhaustFan() : base( DeviceTypes.ExhaustFan)
    {
    }

    public static ExhaustFan FromJson(JsonElement json)
    {
        var fan = new ExhaustFan();
        Random random = new Random();
        fan.FanSpeed = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "fanSpeed"), fan.FanSpeed, false);
        fan.Airflow = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "airflow"), fan.Airflow, false);
        fan.Filter = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "filter"), fan.Filter, false);
        fan.PowerState = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "powerState"), fan.PowerState, false);
        fan.Power = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "power"), fan.Power, false);
        fan.Energy = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "energy"), fan.Energy, false);
        return fan;
    }

    public override void refreshFromJson()
    {
        Random random = new Random();
        JsonElement json = _json;
        FanSpeed = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "fanSpeed"), FanSpeed, false);
        Airflow = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "airflow"), Airflow, false);
        Filter = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "filter"), Filter, false);
        PowerState = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "powerState"), PowerState, false);
        Power = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "power"), Power, false);
        Energy = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "energy"), Energy, false);
    }

    public override List<PointReading> ReadPoints()
    {
        return new List<PointReading>
        {
            new PointReading("fanSpeed", FanSpeed, "%", null),
            new PointReading("airflow", Airflow, "m3/h", null),
            new PointReading("filter", Filter, null, null),
            new PointReading("powerState", PowerState, null, null),
            new PointReading("power", Power, "W", null),
            new PointReading("energy", Energy, "kWh", null)
        };
    }

    public override bool TrySetValue<T>(string name, T value, string? component, out string? reason)
    {
        if (name == "fanSpeed" && value is double fanSpeed)
        {
            FanSpeed = fanSpeed;
            reason = null;
            return true;
        }

        if (name == "powerState" && value is string powerState)
        {
            PowerState = powerState;
            reason = null;
            return true;
        }

        reason = "unknown property";
        return false;
    }

    public override bool StartComponent(string? component)
    {
        PowerState = "on";
        return true;
    }

    public override bool StopComponent(string? component)
    {
        PowerState = "off";
        return true;
    }
}
