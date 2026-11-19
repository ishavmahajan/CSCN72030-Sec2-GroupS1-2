using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Notifier;

// Sends normalized device events to every subscribed client connection.
public interface IClientNotifier
{
    Task Notify(IEvent deviceEvent, CancellationToken cancellationToken = default);
}
