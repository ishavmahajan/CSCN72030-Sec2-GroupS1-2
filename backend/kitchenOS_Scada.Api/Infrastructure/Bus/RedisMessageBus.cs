using System.Text.Json;
using StackExchange.Redis;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;


public class RedisMessageBus : IMessageBus
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    public RedisMessageBus(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }
    public Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default) where T : class
    {
        throw new NotImplementedException();
    }

    public void Subscribe<T>(string channel, Func< T, CancellationToken, Task> handler) where T : class
    {
        throw new NotImplementedException();
    }
}