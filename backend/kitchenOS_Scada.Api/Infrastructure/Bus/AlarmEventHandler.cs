using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

public sealed class AlarmEventHandler(
    IClientNotifier clientNotifier) : IEventHandler
{
    public string EventName => "alarm";

    public Task HandleAsync(
        EventEnvelope eventMessage,
        CancellationToken cancellationToken)
    {
        return clientNotifier.Notify(eventMessage, cancellationToken);
    }
}
