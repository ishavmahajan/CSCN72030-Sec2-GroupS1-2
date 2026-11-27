namespace kitchenOS_Scada.Api.Persistence.Entities;

public sealed class AlarmAcknowledgementEntity
{
    public long AlarmId { get; set; }

    public string DeviceId { get; set; } = null!;

    public string OperatorId { get; set; } = null!;

    public string OperatorName { get; set; } = null!;

    public DateTimeOffset AcknowledgedAtUtc { get; set; }

    public AlarmEntity Alarm { get; set; } = null!;
}
