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
        Random random = new Random();
        oven.Temperature = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "temperature"), oven.Temperature, false);
        oven.Setpoint = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "setpoint"), oven.Setpoint, false);
        oven.Timer = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "timer"), oven.Timer, false);
        oven.Door = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "door"));
        oven.Mode = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "mode"));
        oven.Power = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "power"), oven.Power, false);
        oven.Energy = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "energy"), oven.Energy, false);
        oven.setJson(json);
        return oven;
    }

    public override void refreshFromJson()
    {
        Random random = new Random();
        JsonElement json = _json;
        Temperature = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "temperature"), Temperature, false);
        Setpoint = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "setpoint"), Setpoint, false);
        Timer = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "timer"), Timer, false);
        Door = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "door"));
        Mode = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "mode"));
        Power = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "power"), Power, false);
        Energy = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "energy"), Energy, false);
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

    public override bool TrySetValue<T>(string name, T value, string? component, out string? reason)
    {

        if (name == "temperature" && value is double temperature)
        {
            Temperature = temperature;
            reason = null;
            return true;
        }

        if (name == "setpoint" && value is double setpoint)
        {
            Setpoint = setpoint;
            reason = null;
            return true;
        }

        if (name == "timer" && value is double timer)
        {
            Timer = timer;
            reason = null;
            return true;
        }

        if (name == "door" && value is string door)
        {
            Door = door;
            reason = null;
            return true;
        }
        if (name == "mode" && value is string mode)
        {
            Mode = mode;
            reason = null;
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
