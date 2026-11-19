using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

// Forwards alarm events from Redis to all currently connected API clients.
public sealed class AlarmEventHandler(
    IClientNotifier clientNotifier) : IEventHandler
{
    public string EventName => "alarm";

    public Task HandleAsync(
        EventEnvelope eventMessage,
        CancellationToken cancellationToken)
    {
        // Keep notification cancellation tied to the message-bus callback.
        return clientNotifier.Notify(eventMessage, cancellationToken);
    }
}
