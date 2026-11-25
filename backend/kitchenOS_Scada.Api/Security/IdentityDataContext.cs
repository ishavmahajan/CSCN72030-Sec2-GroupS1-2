using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace kitchenOS_Scada.Api.Security;

// Identity owns its schema and migration history; device persistence is unchanged.
public sealed class IdentityDataContext(DbContextOptions<IdentityDataContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");
    }
}

public sealed class IdentityDataContextFactory : IDesignTimeDbContextFactory<IdentityDataContext>
{
    public IdentityDataContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<IdentityDataContext> options = new();
        options.UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Database=kitchenos;Username=kitchenos", postgres =>
            postgres.MigrationsHistoryTable("__EFMigrationsHistory", "identity"));
        return new IdentityDataContext(options.Options);
    }
}
