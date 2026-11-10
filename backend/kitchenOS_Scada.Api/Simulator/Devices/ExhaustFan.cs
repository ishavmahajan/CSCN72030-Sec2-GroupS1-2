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
        fan.FanSpeed = DeviceJson.ReadNumber(json, "fanSpeed");
        fan.Airflow = DeviceJson.ReadNumber(json, "airflow");
        fan.Filter = DeviceJson.ReadText(json, "filter");
        fan.PowerState = DeviceJson.ReadText(json, "powerState");
        fan.Power = DeviceJson.ReadNumber(json, "power");
        fan.Energy = DeviceJson.ReadNumber(json, "energy");
        return fan;
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

    public override bool TrySetValue(string name, JsonElement value, string? component, out string? reason)
    {
        if (name == "fanSpeed" && DeviceJson.TryUseNumber(value, new List<double>(), out var fanSpeed, out reason))
        {
            FanSpeed = fanSpeed;
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
