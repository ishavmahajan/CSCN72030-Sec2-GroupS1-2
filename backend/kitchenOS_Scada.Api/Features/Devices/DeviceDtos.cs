using System.Text.Json;
using kitchenOS_Scada.Api.Simulator.Devices;
using kitchenOS_Scada.Contract.Messaging;

namespace kitchenOS_Scada.Api.Features.Devices;

/// <summary>Represents a device returned by the API.</summary>
public sealed record DeviceResponse(
    string DeviceId,
    string DeviceName,
    string Description,
    string DeviceType,
    IReadOnlyList<DevicePointResponse> ReadPoints);

/// <summary>Represents a readable point exposed by a device.</summary>
public sealed record DevicePointResponse(
    string Name,
    object Value,
    string? Unit,
    string? Component);

/// <summary>Request body for sending a command to a device.</summary>
public sealed record SendDeviceCommandRequest
{
    /// <summary>The command name accepted by the device.</summary>
    public string? CommandName { get; init; }

    /// <summary>Optional command-specific arguments.</summary>
    public CommandArguments? Arguments { get; init; }
}

/// <summary>Represents a command accepted for asynchronous processing.</summary>
public sealed record QueuedDeviceCommandResponse(
    string CommandId,
    string DeviceId,
    string Status);

internal static class DeviceDtoMappings
{
    public static DeviceResponse ToResponse(SimulatedDevice device) => new(
        device.DeviceId,
        device.DeviceName,
        device.Description,
        device.DeviceType.ToString(),
        device.ReadPoints()
            .Select(point => new DevicePointResponse(point.Name, point.Value, point.Unit, point.Component))
            .ToArray());

    public static CommandArguments CloneArguments(CommandArguments arguments) => new()
    {
        Component = arguments.Component,
        Name = arguments.Name,
        Mode = arguments.Mode,
        Unit = arguments.Unit,
        Value = arguments.Value.ValueKind == JsonValueKind.Undefined
            ? JsonSerializer.SerializeToElement<object?>(null)
            : arguments.Value.Clone()
    };
}
