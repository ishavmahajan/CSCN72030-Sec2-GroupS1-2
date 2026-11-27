using kitchenOS_Scada.Api.Persistence.Entities;

namespace kitchenOS_Scada.Api.Persistence.Seed;

internal static class EventTypeSeed
{
    public const int TelemetryEventTypeId = 1;
    public const int AlarmEventTypeId = 2;

    public static readonly EventTypeEntity[] Data =
    [
        new()
        {
            EventTypeId = TelemetryEventTypeId,
            Name = "telemetry",
            Description = "Device telemetry event"
        },
        new()
        {
            EventTypeId = AlarmEventTypeId,
            Name = "alarm",
            Description = "Device alarm event"
        }
    ];
}
