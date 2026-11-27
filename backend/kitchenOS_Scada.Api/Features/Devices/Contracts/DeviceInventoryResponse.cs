namespace kitchenOS_Scada.Api.Features.Devices.Contracts;

public sealed record DeviceInventoryResponse(
    string DeviceId,
    string DeviceName,
    int DeviceTypeId,
    string DeviceType,
    string Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<DeviceTelemetryPointResponse>? ReadPoints = null,
    DateTimeOffset? LastTelemetryAtUtc = null);

/// <summary>Represents the latest persisted telemetry point for a device.</summary>
public sealed record DeviceTelemetryPointResponse(
    string Name,
    object Value,
    string? Unit,
    string? Component);
