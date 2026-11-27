using kitchenOS_Scada.Contract.Events;

namespace kitchenOS.Simulator.Inventory;

public interface IDeviceInventoryReconciler
{
    Task ReconcileAsync(
        IReadOnlyCollection<DeviceInventoryDefinition> activeDevices,
        CancellationToken cancellationToken = default);
}
