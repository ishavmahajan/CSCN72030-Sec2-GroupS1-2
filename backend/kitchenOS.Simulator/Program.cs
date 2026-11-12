using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Api.Simulator;
using StackExchange.Redis;

string redisConnection = Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ?? "localhost:6379";
using IConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(redisConnection);

var simulator = new DeviceSimulator();
string devicesJson = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "SimulatorFiles", "devices.json"));
string simulationJson = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "SimulatorFiles", "simulation.json"));

simulator.Load(devicesJson, simulationJson, new RedisEventPublisher(connection));
await simulator.Start();

await Task.Delay(Timeout.Infinite);

internal sealed class RedisEventPublisher : IMessageBus
{
    private readonly ISubscriber _subscriber;

    public RedisEventPublisher(IConnectionMultiplexer connection)
    {
        _subscriber = connection.GetSubscriber();
    }

    public Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _subscriber.PublishAsync(
            RedisChannel.Literal(channel),
            System.Text.Json.JsonSerializer.Serialize(message));
    }

    public void Subscribe<T>(string channel, Func<T, CancellationToken, Task> handler)
        where T : class
    {
        throw new NotSupportedException("The simulator only publishes events.");
    }
}
