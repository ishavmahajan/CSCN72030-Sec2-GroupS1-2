using kitchenOS_Scada.Api.Events;

namespace kitchenOS_Scada.Api.Notifier;

public interface IClientNotifier
{
    Task Notify(IEvent deviceEvent, CancellationToken cancellationToken = default);
}
