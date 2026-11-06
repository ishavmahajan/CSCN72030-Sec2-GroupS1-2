using kitchenOS_Scada.Api.Events;

namespace kitchenOS_Scada.Api.Logger;

public interface IEventLogger
{
    Task Log(IEvent deviceEvent, CancellationToken cancellationToken = default);
}
