using Microsoft.Extensions.DependencyInjection;

namespace kitchenOS.Simulator.Inventory;

public static class SimulatorInventoryServiceCollectionExtensions
{
    public static IServiceCollection AddDeviceInventoryRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceRuntimeManager, DeviceRuntimeManager>();
        services.AddSingleton<IDeviceInventoryReconciler, DeviceInventoryReconciler>();
        services.AddSingleton<DeviceInventoryEventHandlers>();

        return services;
    }
}
