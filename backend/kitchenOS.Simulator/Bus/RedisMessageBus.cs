using System.Text.Json;
using StackExchange.Redis;
using kitchenOS_Scada.Contract.Events;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
namespace kitchenOS.Simulator.Bus;

public class RedisMessageBus : IMessageBus
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    public RedisMessageBus(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default) where T : class
    {
        //Cancel the publish if a cancellation request was recieved
        cancellationToken.ThrowIfCancellationRequested();
        //Serialize a message to JSON.
        string jsonString = JsonSerializer.Serialize(message);
        //Get subscribers to recieve a message.
        ISubscriber subscriber = _connectionMultiplexer.GetSubscriber();
        
        subscriber.PublishAsync(RedisChannel.Literal(channel), jsonString);
        return Task.CompletedTask;
    }

    public void Subscribe<T>(string channel, Func< T, CancellationToken, Task> handler) where T : class
    {
        ISubscriber subscriber = _connectionMultiplexer.GetSubscriber();
        subscriber.Subscribe(RedisChannel.Literal(channel), async (_, redisMessage) =>
        {
            try
            {
                T? message = JsonSerializer.Deserialize<T>(
                    redisMessage.ToString());

                if (message is not null)
                {
                    await handler(message, CancellationToken.None);
                }
            }
            catch (JsonException ex)
            {
                // Log or otherwise handle malformed JSON.
                Console.WriteLine($"Could not parse message: {ex.Message}");
            }
        });
    }
}
