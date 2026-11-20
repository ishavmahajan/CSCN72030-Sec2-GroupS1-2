using System.Text.Json;
using kitchenOS_Scada.Contract.Enums;

namespace kitchenOS_Scada.Api.Simulator.Devices;

public class Sink : SimulatedDevice
{
    public double Temperature { get; set; }
    public double Setpoint { get; set; }
    public string Mode { get; set; } = "MANUAL";
    public double Flow { get; set; }
    public double Consumption { get; set; }
    public bool Leak { get; set; }
    public string Valve { get; set; } = "open";

    private readonly List<string> valveChoices = new();
    private bool valveSetByCommand;

    public override int RefreshRate { get; } = 91000;


    public Sink() : base( DeviceTypes.Sink)
    {
    }

    public static Sink FromJson(JsonElement json)
    {
        var sink = new Sink();
        sink.Temperature = DeviceJson.ReadNumber(json, "temperature");
        sink.Setpoint = DeviceJson.ReadNumber(json, "setpoint");
        sink.Mode = DeviceJson.ReadText(json, "mode");
        sink.Flow = DeviceJson.ReadNumber(json, "flow");
        sink.Consumption = DeviceJson.ReadNumber(json, "consumption");
        sink.Leak = DeviceJson.ReadBool(json, "leak");
        sink.Valve = DeviceJson.ReadText(json, "valve");
        // sink.valveChoices.AddRange(DeviceJson.ReadTextList(json, "valve"));
        return sink;
    }

    public override void refreshFromJson()
    {
        Random random = new Random();
        JsonElement json = _json;
        Temperature = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "temperature"), Temperature, false);
        // Setpoint = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "setpoint"), Setpoint, false);
        // Mode = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "mode"));
        Flow = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "flow"), Flow, false);
        Consumption = DeviceJson.Pick(random, DeviceJson.ReadNumberList(json, "consumption"), Consumption, false);
        Leak = DeviceJson.Pick(random, DeviceJson.ReadBoolList(json, "leak"), Leak, false);
        Valve = DeviceJson.Pick(random, DeviceJson.ReadTextList(json, "valve"));
    }

    public override void UpdateProperties(Random random)
    {
        Valve = DeviceJson.Pick(random, valveChoices, Valve, valveSetByCommand);
    }

    public override List<PointReading> ReadPoints()
    {
        return new List<PointReading>
        {
            new PointReading("temperature", Temperature, "C", null),
            new PointReading("setpoint", Setpoint, "C", null),
            new PointReading("mode", Mode, null, null),
            new PointReading("flow", Flow, "L/min", null),
            new PointReading("consumption", Consumption, "L", null),
            new PointReading("leak", Leak, null, null),
            new PointReading("valve", Valve, null, null)
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
