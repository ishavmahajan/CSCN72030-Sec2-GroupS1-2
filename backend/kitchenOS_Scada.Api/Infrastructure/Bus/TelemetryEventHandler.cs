using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

public sealed class TelemetryEventHandler(
    IClientNotifier clientNotifier) : IEventHandler
{
    public string EventName => "telemetry";

    public Task HandleAsync(
        EventEnvelope eventMessage,
        CancellationToken cancellationToken)
    {
        return clientNotifier.Notify(eventMessage, cancellationToken);
    }
}
