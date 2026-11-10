using kitchenOS_Scada.Api.Infrastructure.Bus;

namespace kitchenOS_Scada.Api.Simulator;

public interface IDeviceSimulator
{
    void Load(string simulationFilePath, IMessageBus messageBus);
    void UpdateProperties();
    Task PublishEvents(CancellationToken cancellationToken = default);
    Task Start(CancellationToken cancellationToken = default);
    void Stop();
}
