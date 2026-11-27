namespace kitchenOS_Scada.Api.Persistence.Entities;

public sealed class AlarmEntity
{
    public long AlarmId { get; set; }

    public string DeviceId { get; set; } = null!;

    public int EventTypeId { get; set; }

    public string Code { get; set; } = null!;

    public string Severity { get; set; } = null!;

    public string Text { get; set; } = null!;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public EventTypeEntity EventType { get; set; } = null!;

    public DeviceEntity Device { get; set; } = null!;

    public AlarmAcknowledgementEntity? Acknowledgement { get; set; }
}
