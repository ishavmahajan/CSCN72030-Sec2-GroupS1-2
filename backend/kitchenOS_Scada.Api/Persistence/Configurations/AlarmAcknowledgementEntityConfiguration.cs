using kitchenOS_Scada.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace kitchenOS_Scada.Api.Persistence.Configurations;

internal sealed class AlarmAcknowledgementEntityConfiguration
    : IEntityTypeConfiguration<AlarmAcknowledgementEntity>
{
    public void Configure(EntityTypeBuilder<AlarmAcknowledgementEntity> builder)
    {
        builder.ToTable("alarm_acknowledgements");
        builder.HasKey(acknowledgement => acknowledgement.AlarmId);

        builder.Property(acknowledgement => acknowledgement.AlarmId)
            .HasColumnName("alarm_id");

        builder.Property(acknowledgement => acknowledgement.DeviceId)
            .HasColumnName("device_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(acknowledgement => acknowledgement.OperatorId)
            .HasColumnName("operator_id")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(acknowledgement => acknowledgement.OperatorName)
            .HasColumnName("operator_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(acknowledgement => acknowledgement.AcknowledgedAtUtc)
            .HasColumnName("acknowledged_at_utc")
            .IsRequired();

        builder.HasOne(acknowledgement => acknowledgement.Alarm)
            .WithOne(alarm => alarm.Acknowledgement)
            .HasForeignKey<AlarmAcknowledgementEntity>(acknowledgement => acknowledgement.AlarmId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
