using kitchenOS_Scada.Contract.Events;

namespace kitchenOS.Simulator.Inventory;

public sealed class DeviceInventoryReconciler(IDeviceRuntimeManager runtimeManager)
    : IDeviceInventoryReconciler
{
    private readonly IDeviceRuntimeManager _runtimeManager = runtimeManager;

    public Task ReconcileAsync(
        IReadOnlyCollection<DeviceInventoryDefinition> activeDevices,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
