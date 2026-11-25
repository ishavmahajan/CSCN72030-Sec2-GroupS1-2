using kitchenOS_Scada.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace kitchenOS_Scada.Api.Persistence.Repositories;

public sealed class DeviceRepository(KitchenOsDbContext dbContext) : IDeviceRepository
{
    private readonly KitchenOsDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<DeviceEntity>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .Include(d => d.DeviceType)
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceEntity?> GetByIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .Include(device => device.DeviceType)
            .SingleOrDefaultAsync(
                device => device.DeviceId == deviceId && device.IsActive,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .AnyAsync(
                device => device.DeviceId == deviceId && device.IsActive,
                cancellationToken);
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
