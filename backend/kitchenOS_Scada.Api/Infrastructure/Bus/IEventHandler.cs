using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

public interface IEventHandler
{
    string EventName { get; }

    Task HandleAsync(EventEnvelope eventMessage, CancellationToken cancellationToken);
}
