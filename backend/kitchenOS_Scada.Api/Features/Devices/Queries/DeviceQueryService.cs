using kitchenOS_Scada.Api.Features.Devices.Contracts;
using kitchenOS_Scada.Api.Persistence.Repositories;
using kitchenOS_Scada.Api.Persistence.Entities;
using kitchenOS_Scada.Contract.Events;
using System.Text.Json;

namespace kitchenOS_Scada.Api.Features.Devices.Queries;

public sealed class DeviceQueryService(IDeviceRepository repository) : IDeviceQueryService
{
    private readonly IDeviceRepository _repository = repository;

    public async Task<IReadOnlyList<DeviceInventoryResponse>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DeviceEntity> devices = await _repository.GetActiveAsync(cancellationToken);

        return devices.Select(Map).ToList();
    }

    public async Task<DeviceInventoryResponse?> GetByIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        DeviceEntity? device = await _repository.GetByIdAsync(deviceId, cancellationToken);
        return device is null ? null : Map(device);
    }

    private static DeviceInventoryResponse Map(DeviceEntity device)
    {
        EventEntity? telemetry = device.Events
            .OrderByDescending(eventEntity => eventEntity.OccurredAtUtc)
            .FirstOrDefault();

        IReadOnlyList<DeviceTelemetryPointResponse> points = [];
        if (telemetry is not null)
        {
            try
            {
                TelemetryBody? body = JsonSerializer.Deserialize<TelemetryBody>(telemetry.PayloadJson);
                points = body?.Values.Select(point => new DeviceTelemetryPointResponse(
                    point.Name,
                    point.Value,
                    point.Unit,
                    point.Component)).ToList() ?? [];
            }
            catch (JsonException)
            {
                // Ignore a malformed historical payload and still return the device.
            }
        }

        return new DeviceInventoryResponse(
            device.DeviceId,
            device.DeviceName,
            device.DeviceTypeId,
            device.DeviceType.Name,
            device.Description,
            device.IsActive,
            device.CreatedAtUtc,
            device.UpdatedAtUtc,
            points,
            telemetry?.OccurredAtUtc);
    }
}
