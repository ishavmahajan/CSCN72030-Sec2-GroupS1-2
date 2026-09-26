namespace kitchenOS_Scada.Api.Infrastructure.Bus;

public interface IMessageBus
{
    Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default) where T : class;
    void Subscribe<T>(string channel, Func< T, CancellationToken, Task> handler) where T : class;
}
