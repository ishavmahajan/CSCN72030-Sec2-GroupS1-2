using kitchenOS_Scada.Api.Infrastructure.Bus;

namespace kitchenOS_Scada.Api.Simulator;

public interface IDeviceSimulator
{
    void Load(string simulationFilePath, RedisMessageBus messageBus);
    void Start();
}
