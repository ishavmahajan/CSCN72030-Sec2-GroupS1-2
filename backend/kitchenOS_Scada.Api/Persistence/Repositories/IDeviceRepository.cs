using kitchenOS_Scada.Api.Persistence.Entities;

namespace kitchenOS_Scada.Api.Persistence.Repositories;

public interface IDeviceRepository
{
    Task<IReadOnlyList<DeviceEntity>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<DeviceEntity?> GetByIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string deviceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DeviceEntity device,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        string deviceId,
        DateTimeOffset deactivatedAtUtc,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
