using kitchenOS_Scada.Api.Persistence.Entities;

namespace kitchenOS_Scada.Api.Persistence.Seed;

internal static class EventTypeSeed
{
    public static readonly EventTypeEntity[] Data =
    [
        new()
        {
            EventTypeId = 1,
            Name = "telemetry",
            Description = "Device telemetry event"
        }
    ];
}
