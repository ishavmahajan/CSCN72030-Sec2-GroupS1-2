using Microsoft.EntityFrameworkCore;

namespace kitchenOS_Scada.Api.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddKitchenOsPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is required.");

        services.AddDbContext<KitchenOsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure()));

        return services;
    }
}
