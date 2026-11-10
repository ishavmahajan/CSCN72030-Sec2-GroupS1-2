using System.Text.Json;
using kitchenOS_Scada.Api.Enums;

namespace kitchenOS_Scada.Api.Simulator.Devices;

public class Dishwasher : SimulatedDevice
{
    public double Temperature { get; set; }
    public string Cycle { get; set; } = "idle";
    public double WaterLevel { get; set; }
    public double Detergent { get; set; }
    public double Power { get; set; }
    public double Energy { get; set; }

    public Dishwasher() : base(DeviceTypes.Dishwasher)
    {
    }

    public static Dishwasher FromJson(JsonElement json)
    {
        var dishwasher = new Dishwasher();
        Random random = new Random();
        dishwasher.Temperature = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "temperature"), dishwasher.Temperature, false);
        dishwasher.Cycle = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "cycle"));
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
            new PointReading("cycle", Cycle, null, null),
            new PointReading("waterLevel", WaterLevel, "%", null),
            new PointReading("detergent", Detergent, "%", null),
            new PointReading("power", Power, "W", null),
            new PointReading("energy", Energy, "kWh", null)
        };
    }

    public override bool StartComponent(string? component)
    {
        Cycle = "running";
        return true;
    }

    public override void refreshFromJson()
    {
        Random random = new Random();
        JsonElement json = _json;
        Temperature = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "temperature"), Temperature, false);
        Cycle = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "cycle"));
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
