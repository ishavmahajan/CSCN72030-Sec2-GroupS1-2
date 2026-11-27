namespace kitchenOS_Scada.Api.Features.Devices.Contracts;

public sealed record RegisterDeviceRequest(
    string DeviceName,
    int DeviceTypeId,
    string? Description);
