using System.Text.Json;
using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Contract.Events;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Contract.Messaging;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS.Simulator;

public class DeviceSimulator : IDeviceSimulator
{
    private readonly List<SimulatedDevice> _devices = [];
    private Dictionary<string, DeviceAlarm[]> _alarms = new(StringComparer.OrdinalIgnoreCase);
    private readonly Random _random;
    private IMessageBus? _messageBus;
    private bool _running;
    private bool _commandsSubscribed;

    // An optional random source keeps alarm generation deterministic in tests.
    public DeviceSimulator(Random? random = null)
    {
        _random = random ?? new Random();
    }

    public void Load(string devicesString, string simulationString, IMessageBus messageBus)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _devices.Clear();

        using var parsedDevices = JsonDocument.Parse(devicesString);
        JsonElement parsedDevicesRoot = parsedDevices.RootElement;

        var parsedSimulation = JsonDocument.Parse(simulationString);
        JsonElement parsedSimulationRoot = parsedSimulation.RootElement;

        foreach (JsonElement item in parsedDevicesRoot.EnumerateArray())
        {
            string deviceType = item.GetProperty("deviceType").GetString() ?? "";
            JsonElement dummyData = parsedSimulationRoot.GetProperty(deviceType);
            SimulatedDevice device = SimulatedDevice.Create(deviceType, dummyData);
            string deviceName = item.GetProperty("deviceName").GetString() ?? "";
            string description = item.TryGetProperty("description", out JsonElement descriptionProperty)
                ? descriptionProperty.GetString() ?? ""
                : "";
            string deviceId = item.TryGetProperty("deviceId", out JsonElement deviceIdProperty)
                ? deviceIdProperty.GetString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString();

            device.setDeviceName(deviceName)
                .setDescription(description)
                .setJson(dummyData.Clone())
                .setDeviceId(deviceId);
            // device.Description = item.GetProperty("description").GetString() ?? "No description available";
            _devices.Add(device);
        }
    }

    public void LoadAlarms(string alarmsString)
    {
        var parsedAlarms = JsonSerializer.Deserialize<Dictionary<string, DeviceAlarm[]>>(
            alarmsString,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new JsonException("Alarm configuration must be a JSON object.");
        _alarms = new Dictionary<string, DeviceAlarm[]>(parsedAlarms, StringComparer.OrdinalIgnoreCase);
    }

    // Returns the configured alarm definitions for a loaded device.
    public IReadOnlyList<DeviceAlarm> GetAlarms(string deviceId)
    {
        SimulatedDevice? device = _devices.Find(candidate => candidate.DeviceId == deviceId);
        if (device is null)
        {
            throw new KeyNotFoundException($"Device '{deviceId}' was not found.");
        }

        string deviceType = device.DeviceType.ToFriendlyString();
        if (!_alarms.TryGetValue(deviceType, out DeviceAlarm[]? alarms))
        {
            throw new KeyNotFoundException($"No alarm configuration was found for device type '{deviceType}'.");
        }

        return Array.AsReadOnly(alarms);
    }

    public void UpdateProperties()
    {
        // foreach (var device in _devices)
        // {
        //     device.UpdateProperties(_random);
        // }
    }

    public async Task PublishEvents(CancellationToken cancellationToken = default)
    {
        foreach (var device in _devices)
        {
            await PublishTelemetry(device, cancellationToken);
        }
    }

    public async Task PublishAlarms(CancellationToken cancellationToken = default)
    {
        if (_devices.Count == 0)
        {
            throw new InvalidOperationException("Load devices before publishing alarms.");
        }

        SimulatedDevice selectedDevice;
        DeviceAlarm selectedAlarm;
        lock (_random)
        {
            var candidates = _devices
                .Select(device => (Device: device, Alarms: GetAlarms(device.DeviceId)))
                .Where(candidate => candidate.Alarms.Count > 0)
                .ToArray();
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException("No configured alarms are available for the loaded devices.");
            }

            var selected = candidates[_random.Next(candidates.Length)];
            selectedDevice = selected.Device;
            selectedAlarm = selected.Alarms[_random.Next(selected.Alarms.Count)];
        }

        await PublishAlarm(selectedDevice, selectedAlarm, cancellationToken);
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        SubscribeToCommands(cancellationToken);

        foreach  (var device in _devices)
        {
            await StartDeviceSimulation(device, cancellationToken);
        }
      
    }

    private void SubscribeToCommands(CancellationToken cancellationToken)
    {
        if (_messageBus is null || _commandsSubscribed)
        {
            return;
        }

        _running = true;
        Bus.Subscribe<DeviceCommand>(MessageChannels.Commands,
            (command, _) => OnCommand(command, cancellationToken));
        _commandsSubscribed = true;
    }

    public Task StartDeviceSimulation(SimulatedDevice device, CancellationToken cancellationToken)
    {
          if (_messageBus is null)
        {
            throw new InvalidOperationException("Call Load before Start.");
        }

        _running = true;

        Console.WriteLine("Starting device simulation for {device.DeviceName}...");

        _ = Task.Run(async () =>
        {
            try
            {
                while (_running && !cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"Updating device properties and publishing telemetry for {device.DeviceName} every {device.RefreshRate} ms...");

                    device.refreshFromJson();
                    await PublishTelemetry(device, cancellationToken);

                    await Task.Delay(device.RefreshRate, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Simulation cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Simulation error: {ex}");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public void Stop()
    {
        _running = false;
    }

    private async Task OnCommand(DeviceCommand command, CancellationToken cancellationToken)
    {
        if (!_running || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        SimulatedDevice? device = _devices.Find(candidate => candidate.DeviceId == command.DeviceId);
        if (device is null)
        {
            await PublishResult(command, null, "rejected", "Device was not found.", cancellationToken);
            return;
        }

        CommandArguments? arguments = command.Arguments;
        bool accepted;
        string? reason = null;

        switch (command.CommandName)
        {
            case "isOnline":
                await PublishTelemetry(device, cancellationToken);
                accepted = true;
                break;
            case "setValue":
                object? value = arguments?.Value.ValueKind switch
                {
                    JsonValueKind.Number when arguments.Value.TryGetDouble(out double number)
                        && double.IsFinite(number) => number,
                    JsonValueKind.String => arguments.Value.GetString(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => null
                };
                if (string.IsNullOrWhiteSpace(arguments?.Name) || value is null)
                {
                    accepted = false;
                    reason = "setValue requires a point name and a scalar value.";
                    break;
                }
                accepted = device.TrySetValue(arguments.Name, value, arguments.Component, out reason);
                break;
            case "start":
                accepted = device.StartComponent(arguments?.Component);
                reason = accepted ? null : "The device or component cannot be started.";
                break;
            case "stop":
                accepted = device.StopComponent(arguments?.Component);
                reason = accepted ? null : "The device or component cannot be stopped.";
                break;
            case "setMode":
                if (string.IsNullOrWhiteSpace(arguments?.Mode))
                {
                    accepted = false;
                    reason = "setMode requires a non-empty mode.";
                    break;
                }
                accepted = device.TrySetMode(arguments.Mode, arguments.Component, out reason);
                break;
            default:
                accepted = false;
                reason = "The command is not supported.";
                break;
        }

        await PublishResult(command, device, accepted ? "acknowledged" : "rejected", reason, cancellationToken);
    }

    private Task PublishAlarm(
        SimulatedDevice device,
        DeviceAlarm selectedAlarm,
        CancellationToken cancellationToken)
    {
        DateTime timestamp = DateTime.UtcNow;
        var envelope = new EventEnvelope
        {
            EventName = "alarm",
            DeviceId = device.DeviceId,
            DeviceType = device.DeviceType,
            Timestamp = timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Message = new AlarmBody
            {
                Timestamp = timestamp,
                Code = selectedAlarm.Code,
                Severity = selectedAlarm.Severity,
                Text = selectedAlarm.Text
            }
        };

        return Bus.PublishAsync(MessageChannels.AlarmEvents, envelope, cancellationToken);
    }

    private Task PublishTelemetry(SimulatedDevice device, CancellationToken cancellationToken)
    {
        var timestamp = UtcNow();
        var envelope = new EventEnvelope
        {
            EventName = "telemetry",
            DeviceId = device.DeviceId.ToString(),
            DeviceType = device.DeviceType,
            Timestamp = timestamp,
            Message = new TelemetryBody
            {
                Timestamp = DateTime.UtcNow,
                Values = device.ReadPoints().Select(point => new TelemetryPoint
                {
                    Component = point.Component,
                    Name = point.Name,
                    Value = point.Value,
                    Unit = point.Unit
                }).ToList()
            }
        };

        return Bus.PublishAsync(MessageChannels.TelemetryEvents, envelope, cancellationToken);
    }


    private Task PublishResult(
        DeviceCommand command,
        SimulatedDevice? device,
        string status,
        string? reason,
        CancellationToken cancellationToken)
    {
        var timestamp = UtcNow();
        var envelope = new EventEnvelope
        {
            EventName = "command_result",
            DeviceId = command.DeviceId,
            DeviceType = device?.DeviceType ?? default,
            Timestamp = timestamp,
            Message = new CommandResultBody
            {
                Timestamp = DateTime.UtcNow,
                CommandId = command.CommandId,
                Status = status,
                Reason = reason
            }
        };

        return Bus.PublishAsync(MessageChannels.CommandResultEvents, envelope, cancellationToken);
    }

    private IMessageBus Bus => _messageBus ?? throw new InvalidOperationException("Call Load before publishing.");

    private static string UtcNow() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    public List<SimulatedDevice> GetDevices()
    {
        return _devices;
    }
}


