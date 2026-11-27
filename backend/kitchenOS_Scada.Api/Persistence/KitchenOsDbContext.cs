using kitchenOS_Scada.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace kitchenOS_Scada.Api.Persistence;

public sealed class KitchenOsDbContext(DbContextOptions<KitchenOsDbContext> options)
    : DbContext(options)
{
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();

    public DbSet<DeviceTypeEntity> DeviceTypes => Set<DeviceTypeEntity>();

    public DbSet<EventEntity> Events => Set<EventEntity>();

    public DbSet<EventTypeEntity> EventTypes => Set<EventTypeEntity>();

    public DbSet<AlarmEntity> Alarms => Set<AlarmEntity>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KitchenOsDbContext).Assembly);
    }
}
