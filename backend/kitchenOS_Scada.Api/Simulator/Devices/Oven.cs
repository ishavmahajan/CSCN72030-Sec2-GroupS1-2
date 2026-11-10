using System.Text.Json;
using kitchenOS_Scada.Api.Enums;

namespace kitchenOS_Scada.Api.Simulator.Devices;

public class Oven : SimulatedDevice
{
    public double Temperature { get; set; }
    public double Setpoint { get; set; }
    public double Timer { get; set; }
    public string Door { get; set; } = "closed";
    public string Mode { get; set; } = "bake";
    public double Power { get; set; }
    public double Energy { get; set; }

    public Oven() : base( DeviceTypes.Oven)
    {
    }

    public static Oven FromJson(JsonElement json)
    {
        var oven = new Oven();
        oven.Temperature = DeviceJson.ReadNumber(json, "temperature");
        oven.Setpoint = DeviceJson.ReadNumber(json, "setpoint");
        oven.Timer = DeviceJson.ReadNumber(json, "timer");
        oven.Door = DeviceJson.ReadText(json, "door");
        oven.Mode = DeviceJson.ReadText(json, "mode");
        oven.Power = DeviceJson.ReadNumber(json, "power");
        oven.Energy = DeviceJson.ReadNumber(json, "energy");
        return oven;
    }

    public override List<PointReading> ReadPoints()
    {
        return new List<PointReading>
        {
            new PointReading("temperature", Temperature, "C", null),
            new PointReading("setpoint", Setpoint, "C", null),
            new PointReading("timer", Timer, "s", null),
            new PointReading("door", Door, null, null),
            new PointReading("mode", Mode, null, null),
            new PointReading("power", Power, "W", null),
            new PointReading("energy", Energy, "kWh", null)
        };
    }

    public override bool TrySetValue(string name, JsonElement value, string? component, out string? reason)
    {
        if (name == "setpoint" && DeviceJson.TryUseNumber(value, new List<double>(), out var setpoint, out reason))
        {
            Setpoint = setpoint;
            return true;
        }

        reason = "unknown property";
        return false;
    }

    public override bool StartComponent(string? component)
    {
        Mode = "bake";
        return true;
    }

    public override bool StopComponent(string? component)
    {
        Mode = "off";
        return true;
    }

    public override bool TrySetMode(string mode, string? component, out string? reason)
    {
        Mode = mode;
        reason = null;
        return true;
    }
}
