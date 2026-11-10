using System.Text.Json;

namespace kitchenOS_Scada.Api.Tests;

public static class JsonSamples
{
    public static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
