using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

// Forwards command acknowledgements and rejections to connected clients.
public sealed class CommandResultEventHandler(
    IClientNotifier clientNotifier) : IEventHandler
{
    public string EventName => "command_result";

    public Task HandleAsync(
        EventEnvelope eventMessage,
        CancellationToken cancellationToken)
    {
        // Reuse the notifier's common serialization and client fan-out behavior.
        return clientNotifier.Notify(eventMessage, cancellationToken);
    }
}
