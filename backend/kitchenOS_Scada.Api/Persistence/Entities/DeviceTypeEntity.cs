namespace kitchenOS_Scada.Api.Persistence.Entities;

public sealed class DeviceTypeEntity
{
    public int DeviceTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<DeviceEntity> Devices { get; set; } = [];
}
