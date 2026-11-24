using kitchenOS_Scada.Api.Features.Devices.Contracts;
using kitchenOS_Scada.Api.Persistence.Repositories;

namespace kitchenOS_Scada.Api.Features.Devices.Queries;

public sealed class DeviceQueryService(IDeviceRepository repository) : IDeviceQueryService
{
    private readonly IDeviceRepository _repository = repository;

    public Task<IReadOnlyList<DeviceInventoryResponse>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<DeviceInventoryResponse?> GetByIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
