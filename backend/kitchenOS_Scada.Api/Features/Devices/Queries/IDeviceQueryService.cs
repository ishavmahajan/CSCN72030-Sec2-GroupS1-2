using kitchenOS_Scada.Api.Features.Devices.Contracts;

namespace kitchenOS_Scada.Api.Features.Devices.Queries;

public interface IDeviceQueryService
{
    Task<IReadOnlyList<DeviceInventoryResponse>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<DeviceInventoryResponse?> GetByIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default);
}
