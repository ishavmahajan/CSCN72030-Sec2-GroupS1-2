using kitchenOS_Scada.Api.Persistence.Entities;

namespace kitchenOS_Scada.Api.Persistence.Seed;

internal static class EventTypeSeed
{
    public const int TelemetryEventTypeId = 1;

    public static readonly EventTypeEntity[] Data =
    [
        new()
        {
            EventTypeId = TelemetryEventTypeId,
            Name = "telemetry",
            Description = "Device telemetry event"
        }
    ];
}
