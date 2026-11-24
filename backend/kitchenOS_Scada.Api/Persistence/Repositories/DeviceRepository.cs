using kitchenOS_Scada.Api.Persistence.Entities;

namespace kitchenOS_Scada.Api.Persistence.Repositories;

public sealed class DeviceRepository(KitchenOsDbContext dbContext) : IDeviceRepository
{
    private readonly KitchenOsDbContext _dbContext = dbContext;

    public Task<IReadOnlyList<DeviceEntity>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<DeviceEntity?> GetByIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(
        DeviceEntity device,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeactivateAsync(
        string deviceId,
        DateTimeOffset deactivatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
