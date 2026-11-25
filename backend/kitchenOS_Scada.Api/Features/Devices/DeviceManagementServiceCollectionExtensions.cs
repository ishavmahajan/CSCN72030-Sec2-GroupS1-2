using kitchenOS_Scada.Api.Features.Devices.Queries;
using kitchenOS_Scada.Api.Features.Devices.Registration;
using kitchenOS_Scada.Api.Features.Devices.Removal;
using kitchenOS_Scada.Api.Persistence.Repositories;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Api.Persistence.Entities;
using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Contract.Events;


namespace kitchenOS_Scada.Api.Features.Devices;

public static class DeviceManagementServiceCollectionExtensions
{
    public static IServiceCollection AddDeviceManagement(this IServiceCollection services)
    {
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IDeviceQueryService, DeviceQueryService>();
        services.AddScoped<IDeviceRegistrationService, DeviceRegistrationService>();
        services.AddScoped<IDeviceRemovalService, DeviceRemovalService>();

        return services;
    }

    //load up the device 
    public static async Task LoadDeviceToSimulator(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        IServiceProvider scopedServices = scope.ServiceProvider;

        //load device repository 
        IDeviceRepository deviceRepository = scopedServices.GetRequiredService<IDeviceRepository>();
        //load message bus
        IMessageBus messageBus = scopedServices.GetRequiredService<IMessageBus>();
        IReadOnlyList<DeviceEntity> devices = await deviceRepository.GetActiveAsync(cancellationToken);
        List<DeviceInventoryDefinition> inventory = devices
            .Select(device => new DeviceInventoryDefinition(
                device.DeviceId,
                device.DeviceName,
                DeviceTypeExtensions.FromFriendlyString(device.DeviceType.Name),
                device.Description))
            .ToList();

        await messageBus.PublishAsync(MessageChannels.DeviceInventory, inventory, cancellationToken);

    }
}
