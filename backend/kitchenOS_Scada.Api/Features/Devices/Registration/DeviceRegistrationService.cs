using kitchenOS_Scada.Api.Features.Devices.Contracts;
using kitchenOS_Scada.Api.Persistence.Entities;
using kitchenOS_Scada.Api.Persistence.Repositories;
using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Contract.Events;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using StackExchange.Redis;

namespace kitchenOS_Scada.Api.Features.Devices.Registration;

public sealed class DeviceRegistrationService(
    IDeviceRepository repository,
    IMessageBus messageBus) : IDeviceRegistrationService
{
    private readonly IDeviceRepository _repository = repository;
    private readonly IMessageBus _messageBus = messageBus;

    public async Task<DeviceOperationResult<DeviceInventoryResponse>> RegisterAsync(
        RegisterDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId) ||
            string.IsNullOrWhiteSpace(request.DeviceName))
        {
            return new(null, DeviceOperationError.ValidationFailed,
                "Device ID and device name are required.");
        }

        if (request.DeviceId.Length > 100 || request.DeviceName.Length > 200 ||
            request.Description?.Length > 1000)
        {
            return new(null, DeviceOperationError.ValidationFailed,
                "The device ID, name, or description exceeds its maximum length.");
        }

        string deviceId = request.DeviceId.Trim();
        if (await _repository.ExistsAsync(deviceId, cancellationToken))
        {
            return new(null, DeviceOperationError.Duplicate,
                $"Device '{deviceId}' already exists.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DeviceEntity device = new()
        {
            DeviceId = deviceId,
            DeviceName = request.DeviceName.Trim(),
            DeviceTypeId = request.DeviceTypeId,
            Description = request.Description?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        try
        {
            await _repository.AddAsync(device, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new(null, DeviceOperationError.InvalidDeviceType,
                $"Device type '{request.DeviceTypeId}' does not exist.");
        }

        DeviceTypes deviceType;
        try
        {
            deviceType = DeviceTypeExtensions.FromFriendlyString(device.DeviceType.Name);
        }
        catch (ArgumentException)
        {
            return new(null, DeviceOperationError.InvalidDeviceType,
                "The persisted device type is not supported by the simulator.");
        }

        DeviceInventoryDefinition definition = new(
            device.DeviceId,
            device.DeviceName,
            deviceType,
            device.Description);

        try
        {
            await _messageBus.PublishAsync(
                MessageChannels.DeviceInventoryUpdates,
                new DeviceRegistered(Guid.NewGuid(), definition, now),
                cancellationToken);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            return new(null, DeviceOperationError.EventPublicationFailed,
                "The device was saved, but the simulator bus is unavailable.");
        }

        DeviceInventoryResponse response = new(
            device.DeviceId,
            device.DeviceName,
            device.DeviceTypeId,
            device.DeviceType.Name,
            device.Description,
            device.IsActive,
            device.CreatedAtUtc,
            device.UpdatedAtUtc,
            []);

        return new(response, DeviceOperationError.None);
    }
}
