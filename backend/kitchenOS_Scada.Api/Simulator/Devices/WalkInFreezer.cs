using System.Text.Json;
using kitchenOS_Scada.Api.Enums;

namespace kitchenOS_Scada.Api.Simulator.Devices;

public class WalkInFreezer : SimulatedDevice
{
    public double Temperature { get; set; }
    public double Setpoint { get; set; }
    public string Compressor { get; set; } = "off";
    public string Door { get; set; } = "closed";
    public double Power { get; set; }
    public double Energy { get; set; }
    public string Mode { get; set; } = "";

    private readonly List<string> doorChoices = new();
    private bool doorSetByCommand;

    public WalkInFreezer() : base( DeviceTypes.WalkInFreezer)
    {
    }

    public static WalkInFreezer FromJson(JsonElement json)
    {
        var device = new WalkInFreezer();
        device.Temperature = DeviceJson.ReadNumber(json, "temperature");
        device.Setpoint = DeviceJson.ReadNumber(json, "setpoint");
        device.Compressor = DeviceJson.ReadText(json, "compressor");
        device.Door = DeviceJson.ReadText(json, "door");
        device.doorChoices.AddRange(DeviceJson.ReadTextList(json, "door"));
        device.Power = DeviceJson.ReadNumber(json, "power");
        device.Energy = DeviceJson.ReadNumber(json, "energy");
        return device;
    }

    public override void UpdateProperties(Random random)
    {
        Door = DeviceJson.Pick(random, doorChoices, Door, doorSetByCommand);
    }

    public override List<PointReading> ReadPoints()
    {
        return new List<PointReading>
        {
            new PointReading("temperature", Temperature, "C", null),
            new PointReading("setpoint", Setpoint, "C", null),
            new PointReading("compressor", Compressor, null, null),
            new PointReading("door", Door, null, null),
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

        if (name == "door" && DeviceJson.TryUseText(value, doorChoices, out var door, out reason))
        {
            Door = door;
            doorSetByCommand = true;
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
}
