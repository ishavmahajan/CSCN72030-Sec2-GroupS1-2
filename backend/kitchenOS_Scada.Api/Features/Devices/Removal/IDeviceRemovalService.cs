namespace kitchenOS_Scada.Api.Features.Devices.Removal;

public interface IDeviceRemovalService
{
    Task<DeviceOperationResult<bool>> RemoveAsync(
        string deviceId,
        CancellationToken cancellationToken = default);
}
