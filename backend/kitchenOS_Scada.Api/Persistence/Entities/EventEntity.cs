namespace kitchenOS_Scada.Api.Persistence.Entities;

public sealed class EventEntity
{
    public long EventId { get; set; }

    public string DeviceId { get; set; } = null!;

    public int EventTypeId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public EventTypeEntity EventType { get; set; } = null!;
}
