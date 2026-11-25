using kitchenOS_Scada.Api.Persistence.Entities;
using kitchenOS_Scada.Api.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace kitchenOS_Scada.Api.Persistence.Configurations;

internal sealed class DeviceEntityConfiguration : IEntityTypeConfiguration<DeviceEntity>
{
    public void Configure(EntityTypeBuilder<DeviceEntity> builder)
    {
        builder.ToTable("devices");

        builder.HasKey(device => device.DeviceId);

        builder.Property(device => device.DeviceId)
            .HasColumnName("device_id")
            .HasMaxLength(100);

        builder.Property(device => device.DeviceName)
            .HasColumnName("device_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(device => device.DeviceTypeId)
            .HasColumnName("device_type_id")
            .IsRequired();

        builder.HasOne(device => device.DeviceType)
            .WithMany(deviceType => deviceType.Devices)
            .HasForeignKey(device => device.DeviceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(device => device.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(device => device.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(device => device.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.Property(device => device.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(device => device.DeviceName);
        builder.HasIndex(device => new { device.DeviceTypeId, device.IsActive });

        builder.HasData(DeviceSeed.Data);
    }
}
