using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

// Defines the adapter used to route one event type from the message bus.
public interface IEventHandler
{
    string EventName { get; }

    Task HandleAsync(EventEnvelope eventMessage, CancellationToken cancellationToken);
}
