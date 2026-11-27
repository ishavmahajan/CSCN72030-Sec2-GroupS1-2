using kitchenOS_Scada.Api.Persistence.Repositories;
using kitchenOS_Scada.Contract.Infrastructure.Bus;

namespace kitchenOS_Scada.Api.Features.Devices.Removal;

public sealed class DeviceRemovalService(
    IDeviceRepository repository,
    IMessageBus messageBus) : IDeviceRemovalService
{
    private readonly IDeviceRepository _repository = repository;
    private readonly IMessageBus _messageBus = messageBus;

    public Task<DeviceOperationResult<bool>> RemoveAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
