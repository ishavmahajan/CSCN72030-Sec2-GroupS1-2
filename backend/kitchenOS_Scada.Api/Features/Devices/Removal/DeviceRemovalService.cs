using kitchenOS_Scada.Api.Persistence.Repositories;
using kitchenOS_Scada.Contract.Events;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using StackExchange.Redis;

namespace kitchenOS_Scada.Api.Features.Devices.Removal;

public sealed class DeviceRemovalService(
    IDeviceRepository repository,
    IMessageBus messageBus) : IDeviceRemovalService
{
    private readonly IDeviceRepository _repository = repository;
    private readonly IMessageBus _messageBus = messageBus;

    public async Task<DeviceOperationResult<bool>> RemoveAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool deactivated = await _repository.DeactivateAsync(
            deviceId,
            now,
            cancellationToken);

        if (!deactivated)
        {
            return new(false, DeviceOperationError.NotFound,
                $"Device '{deviceId}' was not found.");
        }

        await _repository.SaveChangesAsync(cancellationToken);

        try
        {
            await _messageBus.PublishAsync(
                MessageChannels.DeviceInventoryRemovals,
                new DeviceRemoved(Guid.NewGuid(), deviceId, now),
                cancellationToken);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            return new(false, DeviceOperationError.EventPublicationFailed,
                "The device was removed, but the simulator bus is unavailable.");
        }

        return new(true, DeviceOperationError.None);
    }
}
