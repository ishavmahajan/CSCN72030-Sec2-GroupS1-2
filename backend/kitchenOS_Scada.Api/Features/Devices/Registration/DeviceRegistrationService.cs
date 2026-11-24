using kitchenOS_Scada.Api.Features.Devices.Contracts;
using kitchenOS_Scada.Api.Persistence.Repositories;
using kitchenOS_Scada.Contract.Infrastructure.Bus;

namespace kitchenOS_Scada.Api.Features.Devices.Registration;

public sealed class DeviceRegistrationService(
    IDeviceRepository repository,
    IMessageBus messageBus) : IDeviceRegistrationService
{
    private readonly IDeviceRepository _repository = repository;
    private readonly IMessageBus _messageBus = messageBus;

    public Task<DeviceOperationResult<DeviceInventoryResponse>> RegisterAsync(
        RegisterDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
