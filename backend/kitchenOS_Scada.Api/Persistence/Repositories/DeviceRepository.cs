using kitchenOS_Scada.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using kitchenOS_Scada.Api.Persistence.Seed;

namespace kitchenOS_Scada.Api.Persistence.Repositories;

public sealed class DeviceRepository(KitchenOsDbContext dbContext) : IDeviceRepository
{
    private readonly KitchenOsDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<DeviceEntity>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .Include(d => d.DeviceType)
            .Include(d => d.Events
                .Where(eventEntity => eventEntity.EventTypeId == EventTypeSeed.TelemetryEventTypeId)
                .OrderByDescending(eventEntity => eventEntity.OccurredAtUtc)
                .Take(1))
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceEntity?> GetByIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices
            .Include(device => device.DeviceType)
            .Include(device => device.Events
                .Where(eventEntity => eventEntity.EventTypeId == EventTypeSeed.TelemetryEventTypeId)
                .OrderByDescending(eventEntity => eventEntity.OccurredAtUtc)
                .Take(1))
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

    public async Task AddAsync(
        DeviceEntity device,
        CancellationToken cancellationToken = default)
    {
        DeviceTypeEntity? deviceType = await _dbContext.DeviceTypes
            .SingleOrDefaultAsync(
                type => type.DeviceTypeId == device.DeviceTypeId,
                cancellationToken);

        if (deviceType is null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(device.DeviceTypeId),
                device.DeviceTypeId,
                "The device type does not exist.");
        }

        device.DeviceType = deviceType;
        device.DeviceId = await GenerateDeviceIdAsync(
            deviceType.Name,
            cancellationToken);
        await _dbContext.Devices.AddAsync(device, cancellationToken);
    }

    private async Task<string> GenerateDeviceIdAsync(
        string deviceType,
        CancellationToken cancellationToken)
    {
        string prefix = deviceType switch
        {
            "WALK_IN_FREEZER" => "FRZ",
            "STOVE" => "STV",
            "OVEN" => "OVN",
            "MIXER" => "MIX",
            "EXHAUST_FAN" => "FAN",
            "BLENDER" => "BLD",
            "DISHWASHER" => "DSH",
            "SINK" => "SNK",
            _ => throw new ArgumentOutOfRangeException(nameof(deviceType), deviceType,
                "The device type is not supported by the simulator.")
        };

        List<string> existingIds = await _dbContext.Devices
            .AsNoTracking()
            .Where(candidate => candidate.DeviceId.StartsWith(prefix + "-"))
            .Select(candidate => candidate.DeviceId)
            .ToListAsync(cancellationToken);

        int nextNumber = existingIds
            .Select(id => int.TryParse(id[(prefix.Length + 1)..], out int number) ? number : 0)
            .DefaultIfEmpty()
            .Max() + 1;

        return $"{prefix}-{nextNumber:000}";
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
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
