using kitchenOS_Scada.Api.Persistence.Entities;
using kitchenOS_Scada.Api.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace kitchenOS_Scada.Api.Persistence.Configurations;

internal sealed class DeviceTypeEntityConfiguration : IEntityTypeConfiguration<DeviceTypeEntity>
{
    public void Configure(EntityTypeBuilder<DeviceTypeEntity> builder)
    {
        builder.ToTable("device_types");
        builder.HasKey(deviceType => deviceType.DeviceTypeId);

        builder.Property(deviceType => deviceType.DeviceTypeId)
            .HasColumnName("device_type_id");

        builder.Property(deviceType => deviceType.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(deviceType => deviceType.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(deviceType => deviceType.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(deviceType => deviceType.Name).IsUnique();

        builder.HasData(DeviceTypeSeed.Data);
    }
}
