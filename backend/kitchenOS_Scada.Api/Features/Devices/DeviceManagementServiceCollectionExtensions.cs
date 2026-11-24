using kitchenOS_Scada.Api.Features.Devices.Queries;
using kitchenOS_Scada.Api.Features.Devices.Registration;
using kitchenOS_Scada.Api.Features.Devices.Removal;
using kitchenOS_Scada.Api.Persistence.Repositories;

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
}
