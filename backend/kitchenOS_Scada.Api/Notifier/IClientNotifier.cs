using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Notifier;

public interface IClientNotifier
{
    Task Notify(IEvent deviceEvent, CancellationToken cancellationToken = default);
}
