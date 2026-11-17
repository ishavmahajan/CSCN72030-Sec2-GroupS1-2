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
    private readonly Random _random = new();
    private IMessageBus? _messageBus;
    private bool _running;
    private bool _commandsSubscribed;

   
    

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


