using kitchenOS_Scada.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace kitchenOS_Scada.Api.Persistence.Configurations;

internal sealed class AlarmEntityConfiguration : IEntityTypeConfiguration<AlarmEntity>
{
    public void Configure(EntityTypeBuilder<AlarmEntity> builder)
    {
        builder.ToTable("alarms");
        builder.HasKey(alarm => alarm.AlarmId);

        builder.Property(alarm => alarm.AlarmId)
            .HasColumnName("alarm_id");

        builder.Property(alarm => alarm.DeviceId)
            .HasColumnName("device_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(alarm => alarm.EventTypeId)
            .HasColumnName("event_type_id")
            .IsRequired();

        builder.Property(alarm => alarm.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(alarm => alarm.Severity)
            .HasColumnName("severity")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(alarm => alarm.Text)
            .HasColumnName("text")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(alarm => alarm.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(alarm => alarm.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(alarm => alarm.AcknowledgedByOperatorId)
            .HasColumnName("acknowledged_by_operator_id")
            .HasMaxLength(450);

        builder.Property(alarm => alarm.AcknowledgedByOperator)
            .HasColumnName("acknowledged_by_operator")
            .HasMaxLength(256);

        builder.Property(alarm => alarm.AcknowledgedAtUtc)
            .HasColumnName("acknowledged_at_utc");

        builder.HasOne(alarm => alarm.EventType)
            .WithMany(eventType => eventType.Alarms)
            .HasForeignKey(alarm => alarm.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(alarm => alarm.Device)
            .WithMany(device => device.Alarms)
            .HasForeignKey(alarm => alarm.DeviceId)
            .HasPrincipalKey(device => device.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(alarm => new { alarm.DeviceId, alarm.OccurredAtUtc });
        builder.HasIndex(alarm => alarm.EventTypeId);
    }
}
