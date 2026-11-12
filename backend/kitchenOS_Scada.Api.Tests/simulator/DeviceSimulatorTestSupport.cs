using kitchenOS_Scada.Contract.Infrastructure.Bus;

namespace kitchenOS_Scada.Api.Tests.Simulator;

internal static class DeviceSimulatorTestData
{
    public const string SimulationJson = """
        {
          "MIXER": {
            "motor": ["off"],
            "speed": [120],
            "power": [750],
            "energy": [15.2]
          },
          "SINK": {
            "temperature": [42],
            "flow": [6.5],
            "consumption": [1280],
            "leak": [false],
            "valve": ["open"]
          },
          "BLENDER": {
            "motor": ["off"],
            "speed": [8000],
            "power": [900],
            "energy": [6.1]
          }
        }
        """;
}

internal sealed class RecordingMessageBus : IMessageBus
{
    public List<(string Channel, object Message)> Messages { get; } = [];

    public Task PublishAsync<T>(
        string channel,
        T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        Messages.Add((channel, message));
        return Task.CompletedTask;
    }

    public void Subscribe<T>(string channel, Func<T, CancellationToken, Task> handler)
        where T : class
    {
    }
}
