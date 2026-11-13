using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

public sealed class CommandResultEventHandler(
    IClientNotifier clientNotifier) : IEventHandler
{
    public string EventName => "command_result";

    public Task HandleAsync(
        EventEnvelope eventMessage,
        CancellationToken cancellationToken)
    {
        return clientNotifier.Notify(eventMessage, cancellationToken);
    }
}
