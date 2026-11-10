using System.Text.Json;
using kitchenOS_Scada.Api.Enums;

namespace kitchenOS_Scada.Api.Simulator.Devices;

public class Burner
{
    public string Component { get; set; } = "";
    public double Temperature { get; set; }
    public double Setpoint { get; set; }
    public string Heating { get; set; } = "off";
    public double Power { get; set; }
    public double Energy { get; set; }
    public bool HeatingSetByCommand { get; set; }
    public List<string> HeatingChoices { get; set; } = new();
}

public class Stove : SimulatedDevice
{
    public List<Burner> Burners { get; set; } = new();

    public Stove() : base( DeviceTypes.Stove)
    {
    }

    public static Stove FromJson(JsonElement json)
    {
        var stove = new Stove();
        return stove;
        if (!json.TryGetProperty("burners", out var burners))
        {
            return stove;
        }

        foreach (var item in burners.EnumerateArray())
        {
            var burner = new Burner();
            burner.Component = item.GetProperty("component").GetString() ?? "";
            burner.Temperature = DeviceJson.ReadNumber(item, "temperature");
            burner.Setpoint = DeviceJson.ReadNumber(item, "setpoint");
            burner.Heating = DeviceJson.ReadText(item, "heating");
            burner.HeatingChoices = DeviceJson.ReadTextList(item, "heating");
            burner.Power = DeviceJson.ReadNumber(item, "power");
            burner.Energy = DeviceJson.ReadNumber(item, "energy");
            stove.Burners.Add(burner);
        }

        return stove;
    }

    public override void refreshFromJson()
    {
        
    }

    

    public override void UpdateProperties(Random random)
    {
        foreach (var burner in Burners)
        {
            burner.Heating = DeviceJson.Pick(random, burner.HeatingChoices, burner.Heating, burner.HeatingSetByCommand);
        }
    }

    public override List<PointReading> ReadPoints()
    {
        var points = new List<PointReading>();
        foreach (var burner in Burners)
        {
            points.Add(new PointReading("temperature", burner.Temperature, "C", burner.Component));
            points.Add(new PointReading("setpoint", burner.Setpoint, "C", burner.Component));
            points.Add(new PointReading("heating", burner.Heating, null, burner.Component));
            points.Add(new PointReading("power", burner.Power, "W", burner.Component));
            points.Add(new PointReading("energy", burner.Energy, "kWh", burner.Component));
        }

        return points;
    }

    public override bool TrySetValue<T>(string name, T value, string? component, out string? reason)
    {
        // var burner = Find(component);
        // if (burner == null)
        // {
        //     reason = "component is required";
        //     return false;
        // }

        // if (name == "temperature" && DeviceJson.TryUseNumber(value, new List<double>(), out var temperature, out reason))
        // {
        //     burner.Temperature = temperature;
        //     return true;
        // }

        // if (name == "setpoint" && DeviceJson.TryUseNumber(value, new List<double>(), out var setpoint, out reason))
        // {
        //     burner.Setpoint = setpoint;
        //     return true;
        // }

        // if (name == "heating" && DeviceJson.TryUseText(value, burner.HeatingChoices, out var heating, out reason))
        // {
        //     burner.Heating = heating;
        //     burner.HeatingSetByCommand = true;
        //     return true;
        // }

        reason = "unknown property";
        return false;
    }

    public override bool StartComponent(string? component)
    {
        var burner = Find(component);
        if (burner == null)
        {
            return false;
        }

        burner.Heating = "on";
        burner.HeatingSetByCommand = true;
        return true;
    }

    public override bool StopComponent(string? component)
    {
        var burner = Find(component);
        if (burner == null)
        {
            return false;
        }

        burner.Heating = "off";
        burner.HeatingSetByCommand = true;
        return true;
    }

    private Burner? Find(string? component)
    {
        foreach (var burner in Burners)
        {
            if (burner.Component == component)
            {
                return burner;
            }
        }

        return null;
    }
}
