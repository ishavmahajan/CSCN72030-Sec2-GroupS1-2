using System.Text.Json;
using StackExchange.Redis;

namespace kitchenOS_Scada.Api.Infrastructure.Messaging;


public class RedisMessageBus : IMessageBus
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        
    }

    public void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : class
    {
        throw new NotImplementedException();
    }
}