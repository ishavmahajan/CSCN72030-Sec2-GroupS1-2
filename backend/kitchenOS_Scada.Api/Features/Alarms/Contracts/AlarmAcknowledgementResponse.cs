namespace kitchenOS_Scada.Api.Features.Alarms.Contracts;

/// <summary>Describes who acknowledged an alarm and when.</summary>
public sealed record AlarmAcknowledgementResponse(
    long AlarmId,
    string DeviceId,
    string OperatorId,
    string OperatorName,
    DateTimeOffset AcknowledgedAtUtc,
    bool AlreadyAcknowledged);
