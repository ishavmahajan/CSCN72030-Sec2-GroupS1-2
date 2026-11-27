using kitchenOS_Scada.Contract.Events;

namespace kitchenOS.Simulator.Inventory;

public sealed class DeviceRuntimeManager(DeviceSimulator simulator) : IDeviceRuntimeManager
{
    private readonly DeviceSimulator _simulator = simulator;

    public Task StartAsync(
        DeviceInventoryDefinition device,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task StopAsync(
        string deviceId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task UpdateAsync(
        DeviceInventoryDefinition device,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public bool IsRunning(string deviceId) =>
        throw new NotImplementedException();
}
