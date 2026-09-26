namespace kitchenOS_Scada.Api.Infrastructure.Messaging;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : class;
}
