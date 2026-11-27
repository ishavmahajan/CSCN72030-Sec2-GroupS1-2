namespace kitchenOS_Scada.Api.Persistence.Entities;

public sealed class EventTypeEntity
{
    public int EventTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<EventEntity> Events { get; set; } = [];

    public ICollection<AlarmEntity> Alarms { get; set; } = [];
}
