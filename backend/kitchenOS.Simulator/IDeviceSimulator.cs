using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS.Simulator;

public interface IDeviceSimulator
{
    void Load(string devicesString, string simulationString, string alarmsString, IMessageBus messageBus);
    IReadOnlyList<DeviceAlarm> GetAlarms(string deviceId);
    // void UpdateProperties();
    // Task PublishEvents(CancellationToken cancellationToken = default);
     Task Start(CancellationToken cancellationToken = default);
     Task StartDeviceSimulation(SimulatedDevice device, CancellationToken cancellationToken);

    void Stop();
}
