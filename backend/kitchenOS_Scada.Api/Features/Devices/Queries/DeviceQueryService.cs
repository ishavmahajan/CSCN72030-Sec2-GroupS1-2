using kitchenOS_Scada.Api.Features.Devices.Contracts;
using kitchenOS_Scada.Api.Persistence.Repositories;
using kitchenOS_Scada.Api.Persistence.Entities;

namespace kitchenOS_Scada.Api.Features.Devices.Queries;

public sealed class DeviceQueryService(IDeviceRepository repository) : IDeviceQueryService
{
    private readonly IDeviceRepository _repository = repository;

    public async Task<IReadOnlyList<DeviceInventoryResponse>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DeviceEntity> devices = await _repository.GetActiveAsync(cancellationToken);

        return devices.Select(d => new DeviceInventoryResponse(
            d.DeviceId,
            d.DeviceName,
            d.DeviceTypeId,
            d.DeviceType.Name,
            d.Description,
            d.IsActive,
            d.CreatedAtUtc,
            d.UpdatedAtUtc)).ToList();
    }

    public Task<DeviceInventoryResponse?> GetByIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
