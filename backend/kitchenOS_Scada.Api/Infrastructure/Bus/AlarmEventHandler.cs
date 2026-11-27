using kitchenOS_Scada.Api.Features.Alarms.Acknowledgement;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

// Persists alarm events and forwards their generated IDs to connected clients.
public sealed class AlarmEventHandler(
    IAlarmAcknowledgementService alarmAcknowledgementService) : IEventHandler
{
    public string EventName => "alarm";

    public Task HandleAsync(
        EventEnvelope eventMessage,
        CancellationToken cancellationToken)
    {
        return alarmAcknowledgementService.RecordAlarmAsync(eventMessage, cancellationToken);
    }
}
