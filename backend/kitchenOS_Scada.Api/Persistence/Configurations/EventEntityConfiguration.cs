using kitchenOS_Scada.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace kitchenOS_Scada.Api.Persistence.Configurations;

internal sealed class EventEntityConfiguration : IEntityTypeConfiguration<EventEntity>
{
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder.ToTable("events");
        builder.HasKey(eventEntity => eventEntity.EventId);

        builder.Property(eventEntity => eventEntity.EventId)
            .HasColumnName("event_id");

        builder.Property(eventEntity => eventEntity.DeviceId)
            .HasColumnName("device_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(eventEntity => eventEntity.EventTypeId)
            .HasColumnName("event_type_id")
            .IsRequired();

        builder.Property(eventEntity => eventEntity.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(eventEntity => eventEntity.PayloadJson)
            .HasColumnName("payload_json")
            .IsRequired();

        builder.HasOne(eventEntity => eventEntity.EventType)
            .WithMany(eventType => eventType.Events)
            .HasForeignKey(eventEntity => eventEntity.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DeviceEntity>()
            .WithMany(device => device.Events)
            .HasForeignKey(eventEntity => eventEntity.DeviceId)
            .HasPrincipalKey(device => device.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(eventEntity => new { eventEntity.DeviceId, eventEntity.OccurredAtUtc });
        builder.HasIndex(eventEntity => eventEntity.EventTypeId);
    }
}
