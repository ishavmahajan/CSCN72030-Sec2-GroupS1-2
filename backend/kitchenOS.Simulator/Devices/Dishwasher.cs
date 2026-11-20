using System.Text.Json;
using kitchenOS_Scada.Contract.Enums;

namespace kitchenOS_Scada.Api.Simulator.Devices;

// Simulates dishwasher cycle, temperature, water, detergent, and energy points.
public class Dishwasher : SimulatedDevice
{
    public double Temperature { get; set; }
    public double Setpoint { get; set; }
    public string Cycle { get; set; } = "idle";
    public string Mode { get; set; } = "standby";
    public double WaterLevel { get; set; }
    public double Detergent { get; set; }
    public double Power { get; set; }
    public override int RefreshRate { get; } = 55000;

    public double Energy { get; set; }

    public Dishwasher() : base(DeviceTypes.Dishwasher)
    {
    }

    public static Dishwasher FromJson(JsonElement json)
    {
        // Initialize the dishwasher from the configured simulation choices.
        var dishwasher = new Dishwasher();
        Random random = new Random();
        dishwasher.Temperature = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "temperature"), dishwasher.Temperature, false);
        dishwasher.Setpoint = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "setpoint"), dishwasher.Setpoint, false);
        dishwasher.Cycle = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "cycle"));
        dishwasher.Mode = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "mode"), dishwasher.Mode, false);
        dishwasher.WaterLevel = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "waterLevel"), dishwasher.WaterLevel, false);
        dishwasher.Detergent = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "detergent"), dishwasher.Detergent, false);
        dishwasher.Power = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "power"), dishwasher.Power, false);
        dishwasher.Energy = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "energy"), dishwasher.Energy, false);
        return dishwasher;
    }



    public override List<PointReading> ReadPoints()
    {
        return new List<PointReading>
        {
            new PointReading("temperature", Temperature, "C", null),
            new PointReading("setpoint", Setpoint, "C", null),
            new PointReading("cycle", Cycle, null, null),
            new PointReading("mode", Mode, null, null),
            new PointReading("waterLevel", WaterLevel, "%", null),
            new PointReading("detergent", Detergent, "%", null),
            new PointReading("power", Power, "W", null),
            new PointReading("energy", Energy, "kWh", null)
            
        };
    }

    public override bool TrySetValue<T>(string name, T value, string? component, out string? reason)
    {
        if (name == "setpoint" && value is double setpoint)
        {
            Setpoint = setpoint;
            reason = null;
            return true;
        }

        if (name == "cycle" && value is string cycle)
        {
            Cycle = cycle;
            reason = null;
            return true;
        }

        reason = "unknown property";
        return false;
    }

    public override bool TrySetMode(string mode, string? component, out string? reason)
    {
        Mode = mode;
        reason = null;
        return true;
    }

    public override bool StartComponent(string? component)
    {
        // Starting the dishwasher transitions its cycle to running.
        Cycle = "running";
        return true;
    }

    public override void refreshFromJson()
    {
        Random random = new Random();
        JsonElement json = _json;
        Temperature = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "temperature"), Temperature, false);
        Setpoint = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "setpoint"), Setpoint, false);
        Cycle = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "cycle"));
        Mode = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "mode"), Mode, false);
        WaterLevel = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "waterLevel"), WaterLevel, false);
        Detergent = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "detergent"), Detergent, false);
        Power = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "power"), Power, false);
        Energy = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "energy"), Energy, false);
        
    }

    public override bool StopComponent(string? component)
    {
        Cycle = "idle";
        return true;
    }

    
}
