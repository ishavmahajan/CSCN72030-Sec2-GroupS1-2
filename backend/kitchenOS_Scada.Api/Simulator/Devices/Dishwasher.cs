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
        dishwasher.Temperature = DeviceJson.ReadNumber(json, "temperature");
        dishwasher.Cycle = DeviceJson.ReadText(json, "cycle");
        dishwasher.WaterLevel = DeviceJson.ReadNumber(json, "waterLevel");
        dishwasher.Detergent = DeviceJson.ReadNumber(json, "detergent");
        dishwasher.Power = DeviceJson.ReadNumber(json, "power");
        dishwasher.Energy = DeviceJson.ReadNumber(json, "energy");
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

    public override bool StopComponent(string? component)
    {
        Cycle = "idle";
        return true;
    }
}
