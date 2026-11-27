using kitchenOS_Scada.Contract.Enums;

namespace kitchenOS_Scada.Contract.Events;

/// <summary>Inventory data required to create a runtime simulated device.</summary>
public sealed record DeviceInventoryDefinition(
    string DeviceId,
    string DeviceName,
    DeviceTypes DeviceType,
    string Description);

/// <summary>Published after a device has been added to the authoritative inventory.</summary>
public sealed record DeviceRegistered(
    Guid EventId,
    DeviceInventoryDefinition Device,
    DateTimeOffset OccurredAtUtc);

/// <summary>Published after an existing device's inventory metadata has changed.</summary>
public sealed record DeviceUpdated(
    Guid EventId,
    DeviceInventoryDefinition Device,
    DateTimeOffset OccurredAtUtc);

/// <summary>Published after a device has been removed from the active inventory.</summary>
public sealed record DeviceRemoved(
    Guid EventId,
    string DeviceId,
    DateTimeOffset OccurredAtUtc);
