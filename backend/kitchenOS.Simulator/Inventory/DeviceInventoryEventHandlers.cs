using kitchenOS_Scada.Contract.Events;

namespace kitchenOS.Simulator.Inventory;

public sealed class DeviceInventoryEventHandlers(IDeviceRuntimeManager runtimeManager)
{
    private readonly IDeviceRuntimeManager _runtimeManager = runtimeManager;

    public Task HandleRegisteredAsync(
        DeviceRegistered integrationEvent,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task HandleUpdatedAsync(
        DeviceUpdated integrationEvent,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task HandleRemovedAsync(
        DeviceRemoved integrationEvent,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
