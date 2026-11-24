namespace kitchenOS_Scada.Api.Features.Devices.Contracts;

public sealed record DeviceInventoryResponse(
    string DeviceId,
    string DeviceName,
    int DeviceTypeId,
    string DeviceType,
    string Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
