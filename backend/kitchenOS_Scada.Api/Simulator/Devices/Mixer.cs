using System.Text.Json;
using kitchenOS_Scada.Api.Enums;

namespace kitchenOS_Scada.Api.Simulator.Devices;

public class Mixer : SimulatedDevice
{
    public string Motor { get; set; } = "off";
    public string Mode { get; set; } = "idle";
    public double Speed { get; set; }
    public double Power { get; set; }
    public double Energy { get; set; }

    public override int RefreshRate { get; } = 46000;

    private readonly List<double> speedChoices = new();
    private bool speedSetByCommand;

    public Mixer() : base( DeviceTypes.Mixer)
    {
    }

    public static Mixer FromJson(JsonElement json)
    {
        var mixer = new Mixer();
        Random random = new Random();
        mixer.Motor = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "motor"));
        mixer.Mode = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "mode"), mixer.Mode, false);
        mixer.Speed = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "speed"), mixer.Speed, false);
        mixer.Power = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "power"), mixer.Power, false);
        mixer.Energy = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "energy"), mixer.Energy, false);
        mixer.setJson(json);
        return mixer;
    }

    public override void refreshFromJson()
    {
        Random random = new Random();
        JsonElement json = _json;
        Motor = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "motor"));
        Mode = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "mode"), Mode, false);
        Speed = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "speed"), Speed, false);
        Power = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "power"), Power, false);
        Energy = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "energy"), Energy, false);
    }

    public override void UpdateProperties(Random random)
    {
        Speed = DeviceJson.Pick(random, speedChoices, Speed, speedSetByCommand);
    }

    public override List<PointReading> ReadPoints()
    {
        return new List<PointReading>
        {
            new PointReading("motor", Motor, null, null),
            new PointReading("mode", Mode, null, null),
            new PointReading("speed", Speed, "rpm", null),
            new PointReading("power", Power, "W", null),
            new PointReading("energy", Energy, "kWh", null)
        };
    }

    public override bool TrySetValue<T>(string name, T value, string? component, out string? reason)
    {
        if (name == "speed" && value is double speed)
        {
            Speed = speed;
            speedSetByCommand = true;
            reason = null;
            return true;
        }
        if (name == "motor" && value is string motor)
        {
            Motor = motor;
            reason = null;
            return true;
        }

        reason = "unknown property";
        return false;
    }

    public override bool StartComponent(string? component)
    {
        Motor = "on";
        return true;
    }

    public override bool StopComponent(string? component)
    {
        Motor = "off ";
        return true;
    }

}
