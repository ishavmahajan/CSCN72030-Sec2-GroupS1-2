namespace kitchenOS_Scada.Api.Persistence.Entities;

public sealed class DeviceEntity
{
    public string DeviceId { get; set; } = null!;

    public string DeviceName { get; set; } = null!;

    public int DeviceTypeId { get; set; }

    public DeviceTypeEntity DeviceType { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<EventEntity> Events { get; set; } = [];

    public ICollection<AlarmEntity> Alarms { get; set; } = [];
}
