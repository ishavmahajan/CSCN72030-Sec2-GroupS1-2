using System.Text.Json;
using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Contract.Events;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Contract.Messaging;
using kitchenOS.Simulator.Devices;


namespace kitchenOS.Simulator;

public class DeviceSimulator : IDeviceSimulator
{
    private static readonly TimeSpan AlarmInterval = TimeSpan.FromSeconds(20);
    private readonly List<SimulatedDevice> _devices = [];
    private readonly Dictionary<string, CancellationTokenSource> _deviceStops = [];
    private readonly Dictionary<string, Task> _deviceTasks = [];
    private readonly Dictionary<string, Task> _alarmTasks = [];
    private readonly object _deviceLock = new();
    private Dictionary<string, DeviceAlarm[]> _alarms = new(StringComparer.OrdinalIgnoreCase);
    private readonly Random _random;
    private IMessageBus? _messageBus;
    private bool _running;
    private bool _commandsSubscribed;
    private bool _inventoryUpdatesSubscribed;

    // An optional random source keeps alarm generation deterministic in tests.
    public DeviceSimulator(Random? random = null)
    {
        _random = random ?? new Random();
    }

    public void Load(string devicesString, string simulationString, IMessageBus messageBus)
    {
        var parsedSimulation = JsonDocument.Parse(simulationString);
        JsonElement parsedSimulationRoot = parsedSimulation.RootElement;
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        // _devices.Clear();

        //listen for device inventory messages  on the message bus
        _messageBus.Subscribe(MessageChannels.DeviceInventory, async (List<DeviceInventoryDefinition> devices, CancellationToken cancellationToken) =>
        {
            await StopAllDeviceSimulationsAsync();
            lock (_deviceLock)
            {
                _devices.Clear();
            }

            foreach (DeviceInventoryDefinition definition in devices)
            { 
                string deviceType = definition.DeviceType.ToFriendlyString();
                JsonElement dummyData = parsedSimulationRoot.GetProperty(deviceType);
                SimulatedDevice device = SimulatedDevice.Create(deviceType, dummyData.Clone());
                device.setDeviceId(definition.DeviceId);
                device.setDeviceName(definition.DeviceName);
                device.setDescription(definition.Description);
                device.setJson(dummyData.Clone());
                await StartDeviceSimulation(device, cancellationToken);
                lock (_deviceLock)
                {
                    _devices.Add(device);
                }
            }
        });

        SubscribeToDeviceInventoryUpdates(parsedSimulationRoot);

    }

    private void SubscribeToDeviceInventoryUpdates(JsonElement simulationRoot)
    {
        if (_messageBus is null || _inventoryUpdatesSubscribed)
        {
            return;
        }

        _messageBus.Subscribe<DeviceRegistered>(
            MessageChannels.DeviceInventoryUpdates,
            (integrationEvent, cancellationToken) =>
                OnDeviceRegisteredAsync(integrationEvent, simulationRoot, cancellationToken));

        _inventoryUpdatesSubscribed = true;
    }

    private async Task OnDeviceRegisteredAsync(
        DeviceRegistered integrationEvent,
        JsonElement simulationRoot,
        CancellationToken cancellationToken)
    {
        DeviceInventoryDefinition definition = integrationEvent.Device;
        string deviceType = definition.DeviceType.ToFriendlyString();

        if (!simulationRoot.TryGetProperty(deviceType, out JsonElement deviceJson))
        {
            Console.WriteLine(
                $"Cannot start device {definition.DeviceId}: no simulation configuration exists for {deviceType}.");
            return;
        }

        lock (_deviceLock)
        {
            if (_deviceTasks.ContainsKey(definition.DeviceId))
            {
                return;
            }
        }

        SimulatedDevice device = SimulatedDevice.Create(deviceType, deviceJson.Clone());
        device.setDeviceId(definition.DeviceId);
        device.setDeviceName(definition.DeviceName);
        device.setDescription(definition.Description);
        device.setJson(deviceJson.Clone());

        await StartDeviceSimulation(device, cancellationToken);

        lock (_deviceLock)
        {
            if (_devices.All(existing => existing.DeviceId != device.DeviceId))
            {
                _devices.Add(device);
            }
        }

        Console.WriteLine($"Added and started simulation for device {device.DeviceId}.");
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
        // if (_devices.Count == 0)
        // {
        //     throw new InvalidOperationException("Load devices before publishing alarms.");
        // }

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

        lock (_deviceLock)
        {
            if (_deviceTasks.ContainsKey(device.DeviceId))
            {
                return Task.CompletedTask;
            }

            CancellationTokenSource deviceStop =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task deviceTask = RunDeviceSimulationAsync(device, deviceStop.Token);
            Task alarmTask = RunAlarmSimulationAsync(device, deviceStop.Token);

            _deviceStops[device.DeviceId] = deviceStop;
            _deviceTasks[device.DeviceId] = deviceTask;
            _alarmTasks[device.DeviceId] = alarmTask;
            _running = true;
        }

        return Task.CompletedTask;
    }

    public void Stop()
    {
        _running = false;
        StopAsync().GetAwaiter().GetResult();
    }

    public async Task StopAsync()
    {
        _running = false;
        await StopAllDeviceSimulationsAsync();
    }

    public async Task StopDeviceSimulationAsync(string deviceId)
    {
        CancellationTokenSource? deviceStop;
        Task? deviceTask;
        Task? alarmTask;

        lock (_deviceLock)
        {
            _deviceStops.Remove(deviceId, out deviceStop);
            _deviceTasks.Remove(deviceId, out deviceTask);
            _alarmTasks.Remove(deviceId, out alarmTask);
            _devices.RemoveAll(device => device.DeviceId == deviceId);
        }

        if (deviceStop is null)
        {
            return;
        }

        try
        {
            deviceStop.Cancel();

            List<Task> tasks = new(2);
            if (deviceTask is not null)
            {
                tasks.Add(deviceTask);
            }

            if (alarmTask is not null)
            {
                tasks.Add(alarmTask);
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }
        finally
        {
            deviceStop.Dispose();
        }
    }

    private async Task StopAllDeviceSimulationsAsync()
    {
        string[] deviceIds;
        lock (_deviceLock)
        {
            deviceIds = _deviceStops.Keys.ToArray();
        }

        foreach (string deviceId in deviceIds)
        {
            await StopDeviceSimulationAsync(deviceId);
        }
    }

    private async Task RunDeviceSimulationAsync(
        SimulatedDevice device,
        CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"Starting device simulation for {device.DeviceName}...");

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Updating device properties and publishing telemetry for {device.DeviceName} every {device.RefreshRate} ms...");

                device.refreshFromJson();
                await PublishTelemetry(device, cancellationToken);
                await Task.Delay(device.RefreshRate, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Simulation cancelled for {device.DeviceId}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Simulation error for {device.DeviceId}: {ex}");
        }
    }

    private async Task RunAlarmSimulationAsync(
        SimulatedDevice device,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DeviceAlarm[] configuredAlarms = _alarms.TryGetValue(
                    device.DeviceType.ToFriendlyString(),
                    out DeviceAlarm[]? alarms)
                    ? alarms
                    : [];

                if (configuredAlarms.Length > 0)
                {
                    DeviceAlarm selectedAlarm;
                    lock (_random)
                    {
                        selectedAlarm = configuredAlarms[_random.Next(configuredAlarms.Length)];
                    }

                    await PublishAlarm(device, selectedAlarm, cancellationToken);
                }

                await Task.Delay(AlarmInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Alarm simulation cancelled for {device.DeviceId}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Alarm simulation error for {device.DeviceId}: {ex}");
        }
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
                if (accepted)
                {
                    await PublishTelemetry(device, cancellationToken);
                }
                break;
            case "start":
                accepted = device.StartComponent(arguments?.Component);
                 if (accepted)
                {
                    await PublishTelemetry(device, cancellationToken);
                }
                reason = accepted ? null : "The device or component cannot be started.";
                break;
            case "stop":
                accepted = device.StopComponent(arguments?.Component);
                  if (accepted)
                {
                    await PublishTelemetry(device, cancellationToken);
                }
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
                  if (accepted)
                {
                    await PublishTelemetry(device, cancellationToken);
                }
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


