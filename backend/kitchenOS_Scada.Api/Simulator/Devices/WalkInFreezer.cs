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

    public override int RefreshRate { get; } = 101000;


    private readonly List<string> doorChoices = new();
    private bool doorSetByCommand;

    public WalkInFreezer() : base( DeviceTypes.WalkInFreezer)
    {
    }

    public static WalkInFreezer FromJson(JsonElement json)
    {
        var device = new WalkInFreezer();
        Random random = new Random();
        device.Temperature = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "temperature"), device.Temperature, false);
        device.Setpoint = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "setpoint"), device.Setpoint, false);
        device.Compressor = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "compressor"));
        device.Door = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "door"));
        // device.doorChoices.AddRange(DeviceJson.ReadTextList(json, "door"));
        device.Power = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "power"), device.Power, false);
        device.Energy = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "energy"), device.Energy, false);
        return device;
    }

    public override void refreshFromJson()
    {
        Random random = new Random();
        JsonElement json = _json;
        Temperature = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "temperature"), Temperature, false);
        Setpoint = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "setpoint"), Setpoint, false);
        Compressor = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "compressor"));
        Door = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "door"));
        Power = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "power"), Power, false);
        Energy = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "energy"), Energy, false);
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

    public override bool TrySetValue<T>(string name, T value, string? component, out string? reason)
    {
        if (name == "setpoint" && value is double setpoint)
        {
            Setpoint = setpoint;
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

    public override bool TrySetMode(string mode, string? component, out string? reason)
    {
        Mode = mode;
        reason = null;
        return true;
    }
}
