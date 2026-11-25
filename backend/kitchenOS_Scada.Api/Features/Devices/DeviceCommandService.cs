using System.Text.Json;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Contract.Messaging;
using kitchenOS_Scada.Api.Persistence.Repositories;

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
    IDeviceRepository repository,
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

        if (!await repository.ExistsAsync(deviceId, cancellationToken))
        {
            return new(null, "Device was not found.", DeviceNotFound: true);
        }

        string? validationError = ValidateArguments(request);
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

    private static string? ValidateArguments(SendDeviceCommandRequest request)
    {
        // Online checks do not require point-specific arguments.
        CommandArguments? arguments = request.Arguments;
        if (request.CommandName == "isOnline")
        {
            return null;
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

        bool validValue = arguments.Value.ValueKind switch
        {
            JsonValueKind.Number => arguments.Value.TryGetDouble(out double number) && double.IsFinite(number),
            JsonValueKind.String => !string.IsNullOrWhiteSpace(arguments.Value.GetString()),
            JsonValueKind.True or JsonValueKind.False => true,
            _ => false
        };

        return validValue ? null : "The value must be a finite number, non-empty string, or boolean.";
    }
}
