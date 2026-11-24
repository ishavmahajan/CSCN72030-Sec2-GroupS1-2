using kitchenOS_Scada.Contract.Events;

namespace kitchenOS.Simulator.Inventory;

public interface IDeviceRuntimeManager
{
    Task StartAsync(
        DeviceInventoryDefinition device,
        CancellationToken cancellationToken = default);

    Task StopAsync(
        string deviceId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        DeviceInventoryDefinition device,
        CancellationToken cancellationToken = default);

    bool IsRunning(string deviceId);
}
