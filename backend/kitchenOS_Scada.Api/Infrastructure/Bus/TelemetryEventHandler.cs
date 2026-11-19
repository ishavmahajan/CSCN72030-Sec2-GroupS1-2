using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

// Forwards telemetry events from Redis to connected WebSocket clients.
public sealed class TelemetryEventHandler(
    IClientNotifier clientNotifier) : IEventHandler
{
    public string EventName => "telemetry";

    public Task HandleAsync(
        EventEnvelope eventMessage,
        CancellationToken cancellationToken)
    {
        // Propagate the bus cancellation token so shutdown stops notification work.
        return clientNotifier.Notify(eventMessage, cancellationToken);
    }
}
