namespace kitchenOS_Scada.Api.Features.Devices;

public enum DeviceOperationError
{
    None,
    NotFound,
    Duplicate,
    InvalidDeviceType,
    ValidationFailed,
    EventPublicationFailed
}

public sealed record DeviceOperationResult<T>(
    T? Value,
    DeviceOperationError Error,
    string? ErrorMessage = null);
