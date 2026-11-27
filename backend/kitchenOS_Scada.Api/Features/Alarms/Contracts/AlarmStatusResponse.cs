namespace kitchenOS_Scada.Api.Features.Alarms.Contracts;

/// <summary>Represents a persisted alarm and its current acknowledgement state.</summary>
public sealed record AlarmStatusResponse(
    long AlarmId,
    string DeviceId,
    string DeviceType,
    string Code,
    string Severity,
    string Text,
    DateTimeOffset OccurredAtUtc,
    bool IsActive,
    string? AcknowledgedByOperatorId,
    string? AcknowledgedByOperator,
    DateTimeOffset? AcknowledgedAtUtc);
