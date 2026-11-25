using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS.Simulator.Devices;

namespace kitchenOS.Simulator;

public interface IDeviceSimulator
{
    void Load(string devicesString, string simulationString, IMessageBus messageBus);
    void LoadAlarms(string alarmsString);
    IReadOnlyList<DeviceAlarm> GetAlarms(string deviceId);
    Task PublishAlarms(CancellationToken cancellationToken = default);
    // void UpdateProperties();
    // Task PublishEvents(CancellationToken cancellationToken = default);
     Task Start(CancellationToken cancellationToken = default);
     Task StartDeviceSimulation(SimulatedDevice device, CancellationToken cancellationToken);
     Task StopDeviceSimulationAsync(string deviceId);
     Task StopAsync();

    void Stop();
}
