using System.Text.Json;

namespace kitchenOS_Scada.Api.Simulator.Devices;

public static class DeviceJson
{
    public static double ReadNumber(JsonElement json, string name)
    {
        if (!json.TryGetProperty(name, out var value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.GetDouble();
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                return item.GetDouble();
            }
        }

        return 0;
    }

    public static List<double> ReadNumberList(JsonElement json, string name)
    {
        var choices = new List<double>();
        if (!json.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return choices;
        }

        foreach (var item in value.EnumerateArray())
        {
            choices.Add(item.GetDouble());
        }

        return choices;
    }

    public static string ReadText(JsonElement json, string name)
    {
        if (!json.TryGetProperty(name, out var value))
        {
            return "";
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? "";
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                return item.GetString() ?? "";
            }
        }

        return "";
    }

    public static List<string> ReadTextList(JsonElement json, string name)
    {
        var choices = new List<string>();
        if (!json.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return choices;
        }

        foreach (var item in value.EnumerateArray())
        {
            choices.Add(item.GetString() ?? "");
        }

        return choices;
    }

    public static bool ReadBool(JsonElement json, string name)
    {
        if (!json.TryGetProperty(name, out var value))
        {
            return false;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        return false;
    }

    public static double Pick(Random random, List<double> choices, double current, bool lockedByCommand)
    {
        if (lockedByCommand || choices.Count == 0)
        {
            return current;
        }

        return choices[random.Next(choices.Count)];
    }

    public static string Pick(Random random, List<string> choices, string current, bool lockedByCommand)
    {
        if (lockedByCommand || choices.Count == 0)
        {
            return current;
        }

        return choices[random.Next(choices.Count)];
    }

    public static bool TryUseNumber(JsonElement value, List<double> choices, out double number, out string? reason)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            number = 0;
            reason = "value must be a number";
            return false;
        }

        number = value.GetDouble();
        if (choices.Count > 0 && !choices.Contains(number))
        {
            reason = "value is not allowed";
            return false;
        }

        reason = null;
        return true;
    }

    public static bool TryUseText(JsonElement value, List<string> choices, out string text, out string? reason)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            text = "";
            reason = "value must be a string";
            return false;
        }

        text = value.GetString() ?? "";
        if (choices.Count > 0 && !choices.Contains(text))
        {
            reason = "value is not allowed";
            return false;
        }

        reason = null;
        return true;
    }
}
