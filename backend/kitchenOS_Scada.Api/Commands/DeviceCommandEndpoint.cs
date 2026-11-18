using System.Text.Json;
using kitchenOS.Simulator;
using kitchenOS_Scada.Api.Simulator.Devices;
using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Contract.Messaging;
using StackExchange.Redis;

namespace kitchenOS_Scada.Api.Commands;

public sealed class CommandRequest
{
    public string? CommandName { get; set; }
    public CommandArguments? Arguments { get; set; }
}

public sealed record QueuedCommandResponse(string CommandId, string DeviceId, string Status);

public sealed class DeviceCommandEndpoint
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints, DeviceSimulator simulator)
    {
        endpoints.MapPost("/devices/{deviceId}/commands", (
            string deviceId,
            CommandRequest? request,
            IMessageBus messageBus,
            ILogger<DeviceCommandEndpoint> logger,
            CancellationToken cancellationToken) =>
                SendAsync(deviceId, request, simulator, messageBus, logger, cancellationToken));

        endpoints.MapPost("/devices/commands", () =>
            Results.BadRequest(new { error = "Device ID is required." }));
    }

    public static async Task<IResult> SendAsync(
        string? deviceId,
        CommandRequest? request,
        DeviceSimulator simulator,
        IMessageBus messageBus,
        ILogger<DeviceCommandEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return Results.BadRequest(new { error = "Device ID is required." });
        }

        if (request?.CommandName is not ("start" or "stop" or "setValue" or "setMode" or "isOnline"))
        {
            return Results.BadRequest(new { error = "A supported command name is required." });
        }

        SimulatedDevice? device = simulator.GetDevices().Find(candidate => candidate.DeviceId == deviceId);
        if (device is null)
        {
            return Results.NotFound(new { error = "Device was not found." });
        }

        string? validationError = ValidateArguments(device, request);
        if (validationError is not null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        CommandArguments? arguments = request.Arguments;
        DeviceCommand command = new()
        {
            CommandId = Guid.NewGuid().ToString(),
            DeviceId = deviceId,
            CommandName = request.CommandName,
            Timestamp = DateTime.UtcNow.ToString("O"),
            Arguments = arguments is null ? null : new CommandArguments
            {
                Component = arguments.Component,
                Name = arguments.Name,
                Mode = arguments.Mode,
                Unit = arguments.Unit,
                Value = arguments.Value.ValueKind == JsonValueKind.Undefined
                    ? JsonSerializer.SerializeToElement<object?>(null)
                    : arguments.Value.Clone()
            }
        };

        try
        {
            await messageBus.PublishAsync(MessageChannels.Commands, command, cancellationToken);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            logger.LogError(exception, "Could not queue command for device {DeviceId}", deviceId);
            return Results.Problem("The command bus is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Accepted(value: new QueuedCommandResponse(command.CommandId, deviceId, "queued"));
    }

    private static string? ValidateArguments(SimulatedDevice device, CommandRequest request)
    {
        CommandArguments? arguments = request.Arguments;
        if (request.CommandName == "isOnline")
        {
            return null;
        }

        if (device.DeviceType == DeviceTypes.Stove && string.IsNullOrWhiteSpace(arguments?.Component))
        {
            return "A burner component is required for this device.";
        }

        List<PointReading> points = device.ReadPoints();
        if (!string.IsNullOrWhiteSpace(arguments?.Component) &&
            !points.Exists(point => point.Component == arguments.Component))
        {
            return "The component does not exist on this device.";
        }

        if (request.CommandName == "setMode" && string.IsNullOrWhiteSpace(arguments?.Mode))
        {
            return "setMode requires a non-empty mode.";
        }

        if (request.CommandName != "setValue")
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(arguments?.Name))
        {
            return "setValue requires a point name and a value.";
        }

        PointReading? selectedPoint = points.Find(point =>
            point.Name == arguments.Name && point.Component == arguments.Component);
        if (selectedPoint is null)
        {
            return "The point does not exist on this device.";
        }

        bool validValue = selectedPoint.Value switch
        {
            double => arguments.Value.ValueKind == JsonValueKind.Number &&
                arguments.Value.TryGetDouble(out double number) && double.IsFinite(number),
            string => arguments.Value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(arguments.Value.GetString()),
            bool => arguments.Value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            _ => false
        };

        return validValue ? null : "The value must match the point's number, text, or boolean type.";
    }
}
