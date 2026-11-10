using System.Text.Json;
using kitchenOS_Scada.Api.Enums;

namespace kitchenOS_Scada.Api.Simulator.Devices;

public class Sink : SimulatedDevice
{
    public double Temperature { get; set; }
    public double Flow { get; set; }
    public double Consumption { get; set; }
    public bool Leak { get; set; }
    public string Valve { get; set; } = "open";

    private readonly List<string> valveChoices = new();
    private bool valveSetByCommand;

    public Sink() : base( DeviceTypes.Sink)
    {
    }

    public static Sink FromJson(JsonElement json)
    {
        var sink = new Sink();
        sink.Temperature = DeviceJson.ReadNumber(json, "temperature");
        sink.Flow = DeviceJson.ReadNumber(json, "flow");
        sink.Consumption = DeviceJson.ReadNumber(json, "consumption");
        sink.Leak = DeviceJson.ReadBool(json, "leak");
        sink.Valve = DeviceJson.ReadText(json, "valve");
        sink.valveChoices.AddRange(DeviceJson.ReadTextList(json, "valve"));
        return sink;
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
            new PointReading("flow", Flow, "L/min", null),
            new PointReading("consumption", Consumption, "L", null),
            new PointReading("leak", Leak, null, null),
            new PointReading("valve", Valve, null, null)
        };
    }

    public override bool TrySetValue(string name, JsonElement value, string? component, out string? reason)
    {
        if (name == "temperature" && DeviceJson.TryUseNumber(value, new List<double>(), out var temperature, out reason))
        {
            Temperature = temperature;
            return true;
        }

        if (name == "flow" && DeviceJson.TryUseNumber(value, new List<double>(), out var flow, out reason))
        {
            Flow = flow;
            return true;
        }

        if (name == "consumption" && DeviceJson.TryUseNumber(value, new List<double>(), out var consumption, out reason))
        {
            Consumption = consumption;
            return true;
        }

        if (name == "leak" && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            Leak = value.GetBoolean();
            reason = null;
            return true;
        }

        if (name == "valve" && DeviceJson.TryUseText(value, valveChoices, out var valve, out reason))
        {
            Valve = valve;
            valveSetByCommand = true;
            return true;
        }

        reason = "unknown property";
        return false;
    }
}
