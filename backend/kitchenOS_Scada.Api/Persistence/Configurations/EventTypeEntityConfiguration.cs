using kitchenOS_Scada.Api.Persistence.Entities;
using kitchenOS_Scada.Api.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace kitchenOS_Scada.Api.Persistence.Configurations;

internal sealed class EventTypeEntityConfiguration : IEntityTypeConfiguration<EventTypeEntity>
{
    public void Configure(EntityTypeBuilder<EventTypeEntity> builder)
    {
        builder.ToTable("event_types");
        builder.HasKey(eventType => eventType.EventTypeId);

        builder.Property(eventType => eventType.EventTypeId)
            .HasColumnName("event_type_id");

        builder.Property(eventType => eventType.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(eventType => eventType.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(eventType => eventType.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(eventType => eventType.Name).IsUnique();

        builder.HasData(EventTypeSeed.Data);
    }
}
