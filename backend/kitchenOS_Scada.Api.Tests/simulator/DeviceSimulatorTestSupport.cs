using kitchenOS_Scada.Contract.Infrastructure.Bus;
using System.Collections.Concurrent;
using System.Text.Json;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Tests.Simulator;

// Deterministic simulator data shared by tests that exercise device behavior.
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
    private readonly SemaphoreSlim _published = new(0);
    private readonly ConcurrentQueue<(string Channel, object Message)> _messages = new();
    private readonly ConcurrentDictionary<string, Func<object, Task>> _handlers = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _subscriptions = new();

    public IReadOnlyList<(string Channel, object Message)> Messages => _messages.ToArray();
    public Exception? PublishFailure { get; set; }
    public Task? PublishCompletion { get; set; }

    public async Task PublishAsync<T>(
        string channel,
        T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        // Capture published messages while preserving cancellation and failure behavior.
        cancellationToken.ThrowIfCancellationRequested();
        if (PublishFailure is not null)
        {
            throw PublishFailure;
        }
        if (PublishCompletion is not null)
        {
            await PublishCompletion.WaitAsync(cancellationToken);
        }
        _messages.Enqueue((channel, message));
        _published.Release();
    }

    public void Subscribe<T>(string channel, Func<T, CancellationToken, Task> handler)
        where T : class
    {
        // Serialize through JSON to mirror the contract crossing the real bus.
        _handlers[channel] = message => handler(
            JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(message))!,
            CancellationToken.None);
        _subscriptions.GetOrAdd(channel, _ => new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();
    }

    public Task WaitForSubscriptionAsync(string channel) =>
        _subscriptions.GetOrAdd(channel, _ => new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously)).Task.WaitAsync(TimeSpan.FromSeconds(5));

    public async Task WaitForTelemetryAsync(string deviceId)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        while (!_messages.Any(message => message.Message is EventEnvelope envelope &&
            envelope.EventName == "telemetry" && envelope.DeviceId == deviceId))
        {
            await _published.WaitAsync(timeout.Token);
        }
    }

    public Task DispatchAsync(string channel, object message) => _handlers[channel](message);
}
