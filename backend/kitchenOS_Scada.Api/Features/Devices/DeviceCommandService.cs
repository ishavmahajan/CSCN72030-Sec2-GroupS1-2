using System.Text.Json;
using kitchenOS.Simulator;
using kitchenOS_Scada.Api.Simulator.Devices;
using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Contract.Messaging;

namespace kitchenOS_Scada.Api.Features.Devices;

// Validates device commands, creates bus messages, and publishes them asynchronously.
public interface IDeviceCommandService
{
    Task<DeviceCommandSubmissionResult> SubmitAsync(
        string deviceId,
        SendDeviceCommandRequest request,
        CancellationToken cancellationToken);
}

public sealed record DeviceCommandSubmissionResult(
    DeviceCommand? Command,
    string? Error,
    bool DeviceNotFound = false);

public sealed class DeviceCommandService(
    DeviceSimulator simulator,
    IMessageBus messageBus)
    : IDeviceCommandService
{
    public async Task<DeviceCommandSubmissionResult> SubmitAsync(
        string deviceId,
        SendDeviceCommandRequest request,
        CancellationToken cancellationToken)
    {
        // Reject unsupported commands before looking up a device or publishing anything.
        if (string.IsNullOrWhiteSpace(request.CommandName) ||
            request.CommandName is not ("start" or "stop" or "setValue" or "setMode" or "isOnline"))
        {
            return new(null, "A supported command name is required.");
        }

        SimulatedDevice? device = simulator.GetDevices()
            .Find(candidate => candidate.DeviceId == deviceId);
        if (device is null)
        {
            return new(null, "Device was not found.", DeviceNotFound: true);
        }

        string? validationError = ValidateArguments(device, request);
        if (validationError is not null)
        {
            return new(null, validationError);
        }

        DeviceCommand command = new()
        {
            CommandId = Guid.NewGuid().ToString(),
            DeviceId = deviceId,
            CommandName = request.CommandName,
            Timestamp = DateTime.UtcNow.ToString("O"),
            Arguments = request.Arguments is null
                ? null
                : DeviceDtoMappings.CloneArguments(request.Arguments)
        };

        // Publish only after all request, device, and argument validation succeeds.
        await messageBus.PublishAsync(
            MessageChannels.Commands,
            command,
            cancellationToken);

        return new(command, null);
    }

    private static string? ValidateArguments(
        SimulatedDevice device,
        SendDeviceCommandRequest request)
    {
        // Online checks do not require point-specific arguments.
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
