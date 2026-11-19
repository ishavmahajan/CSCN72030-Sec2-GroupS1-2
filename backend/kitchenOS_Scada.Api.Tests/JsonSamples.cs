using System.Text.Json;

namespace kitchenOS_Scada.Api.Tests;

// Shared JSON helper that returns a detached element safe to use after parsing.
public static class JsonSamples
{
    public static JsonElement Parse(string json)
    {
        // Clone before disposing the document that owns the root element.
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
