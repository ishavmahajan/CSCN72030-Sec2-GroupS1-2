using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Logger;

// Abstraction for recording events without coupling handlers to a logger implementation.
public interface IEventLogger
{
    Task Log(IEvent deviceEvent, CancellationToken cancellationToken = default);
}
