using System.Text.Json;
using kitchenOS_Scada.Api.Enums;
using kitchenOS_Scada.Api.Infrastructure.Bus;
using kitchenOS_Scada.Api.Simulator.Devices;

namespace kitchenOS_Scada.Api.Simulator;

public class DeviceSimulator : IDeviceSimulator
{
    public const string EventsChannel = "kitchenos:events";
    public const string CommandsChannel = "kitchenos:commands";

    private readonly List<SimulatedDevice> _devices = [];
    private readonly Random _random = new();
    private IMessageBus? _messageBus;
    private bool _running;

    public void Load(string devicesString, string simulationString, IMessageBus messageBus)
    {
        _messageBus = messageBus;
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
            device.setDeviceName(item.GetProperty("deviceName").GetString() ?? "").setJson(dummyData.Clone());
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
        foreach  (var device in _devices)
        {
            await StartDeviceSimulation(device, cancellationToken);
        }
      
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
                    //Todo:  await PublishTelemetry(device, cancellationToken);

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
        if (!_running)
        {
            return;
        }

        var device = _devices.FirstOrDefault(item => item.DeviceId == command.DeviceId);
        // If the device is not found and the command has no arguments, we ignore it
        if (device is null && command.Arguments?.Value is null)
        {
            return;
        }

        var arguments = command.Arguments;
        switch (command.CommandName)
        {
            case "isOnline":
                await PublishTelemetry(device, cancellationToken);
                return;
            case "setValue":
                var accepted = device.TrySetValue(
                    arguments?.Name ?? "",
                    arguments?.Value ?? default,
                    arguments?.Component,
                    out var setReason);
                await PublishResult(
                    device,
                    command.CommandId,
                    accepted ? "acknowledged" : "rejected",
                    accepted ? null : setReason,
                    cancellationToken);
                return;
            case "start":
                await FinishCommand(device, command.CommandId, device.StartComponent(arguments?.Component), "component is required", cancellationToken);
                return;
            case "stop":
                await FinishCommand(device, command.CommandId, device.StopComponent(arguments?.Component), "component is required", cancellationToken);
                return;
            case "setMode":
                var modeAccepted = device.TrySetMode(arguments?.Mode ?? "", arguments?.Component, out var modeReason);
                if (!modeAccepted && modeReason == "setMode is not accepted")
                {
                    return;
                }

                await PublishResult(
                    device,
                    command.CommandId,
                    modeAccepted ? "acknowledged" : "rejected",
                    modeAccepted ? null : modeReason,
                    cancellationToken);
                return;
        }
    }

    private async Task FinishCommand(
        SimulatedDevice device,
        string commandId,
        bool accepted,
        string reason,
        CancellationToken cancellationToken)
    {
        if (!accepted && reason == "component is required" && device.DeviceType != DeviceTypes.Stove)
        {
            return;
        }

        await PublishResult(
            device,
            commandId,
            accepted ? "acknowledged" : "rejected",
            accepted ? null : reason,
            cancellationToken);
    }

    private Task PublishTelemetry(SimulatedDevice device, CancellationToken cancellationToken)
    {
        var timestamp = UtcNow();
        var envelope = new EventEnvelope
        {
            EventName = "telemetry",
            DeviceId = device.DeviceId.ToString(),
            DeviceType = device.DeviceType.ToFriendlyString(),
            Timestamp = timestamp,
            Message = new TelemetryBody
            {
                Timestamp = timestamp,
                Values = device.ReadPoints().Select(point => new TelemetryPoint
                {
                    Component = point.Component,
                    Name = point.Name,
                    Value = point.Value,
                    Unit = point.Unit
                }).ToList()
            }
        };

        return Bus.PublishAsync(EventsChannel, envelope, cancellationToken);
    }


    private Task PublishResult(
        SimulatedDevice device,
        string commandId,
        string status,
        string? reason,
        CancellationToken cancellationToken)
    {
        var timestamp = UtcNow();
        var envelope = new EventEnvelope
        {
            EventName = "command_result",
            DeviceId = device.DeviceId.ToString(),
            DeviceType = device.DeviceType.ToFriendlyString(),
            Timestamp = timestamp,
            Message = new CommandResultBody
            {
                Timestamp = timestamp,
                CommandId = commandId,
                Status = status,
                Reason = reason
            }
        };

        return Bus.PublishAsync(EventsChannel, envelope, cancellationToken);
    }

    private IMessageBus Bus => _messageBus ?? throw new InvalidOperationException("Call Load before publishing.");

    private static string UtcNow() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    public List<SimulatedDevice> GetDevices()
    {
        return _devices;
    }
}

public class DeviceCommand
{
    public string CommandId { get; set; } = "";
    public string CommandName { get; set; } = "";
    public Guid DeviceId { get; set; } = Guid.Empty;
    public string Timestamp { get; set; } = "";
    public CommandArguments? Arguments { get; set; }
}

public class CommandArguments
{
    public string? Component { get; set; }
    public string? Name { get; set; }
    public JsonElement Value { get; set; }
    public string? Unit { get; set; }
    public string? Mode { get; set; }
}

public class EventEnvelope
{
    public string EventName { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public object Message { get; set; } = new();
}

public class TelemetryBody
{
    public string Timestamp { get; set; } = "";
    public List<TelemetryPoint> Values { get; set; } = [];
}

public class TelemetryPoint
{
    public string? Component { get; set; }
    public string Name { get; set; } = "";
    public object Value { get; set; } = "";
    public string? Unit { get; set; }
}

public class CommandResultBody
{
    public string Timestamp { get; set; } = "";
    public string CommandId { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
}
