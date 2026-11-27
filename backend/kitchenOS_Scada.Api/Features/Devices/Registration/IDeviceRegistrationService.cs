using kitchenOS_Scada.Api.Features.Devices.Contracts;

namespace kitchenOS_Scada.Api.Features.Devices.Registration;

public interface IDeviceRegistrationService
{
    Task<DeviceOperationResult<DeviceInventoryResponse>> RegisterAsync(
        RegisterDeviceRequest request,
        CancellationToken cancellationToken = default);
}
