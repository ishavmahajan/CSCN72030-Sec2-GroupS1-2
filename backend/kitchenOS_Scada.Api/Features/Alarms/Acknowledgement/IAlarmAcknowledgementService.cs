using kitchenOS_Scada.Api.Features.Alarms.Contracts;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Features.Alarms.Acknowledgement;

public interface IAlarmAcknowledgementService
{
    Task<IReadOnlyList<AlarmStatusResponse>> GetRecentAsync(CancellationToken cancellationToken);

    Task<AlarmAcknowledgementResult> AcknowledgeAsync(
        AcknowledgeAlarmRequest request,
        string operatorId,
        string operatorName,
        CancellationToken cancellationToken);

    Task RecordAlarmAsync(EventEnvelope eventMessage, CancellationToken cancellationToken);
}

public sealed record AlarmAcknowledgementResult(
    AlarmAcknowledgementResultStatus Status,
    AlarmAcknowledgementResponse? Acknowledgement);

public enum AlarmAcknowledgementResultStatus
{
    Acknowledged,
    AlreadyAcknowledged,
    NotFound,
    DeviceMismatch,
    InvalidRequest
}
