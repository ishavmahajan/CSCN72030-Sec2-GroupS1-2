using System.ComponentModel.DataAnnotations;

namespace kitchenOS_Scada.Api.Features.Alarms.Contracts;

/// <summary>Identifies the alarm and device to acknowledge.</summary>
public sealed record AcknowledgeAlarmRequest
{
    [Range(1, long.MaxValue)]
    public long AlarmId { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public required string DeviceId { get; init; }
}
