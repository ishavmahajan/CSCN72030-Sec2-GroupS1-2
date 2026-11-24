using System.Net;
using System.Net.Http.Json;
using kitchenOS_Scada.Api.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace kitchenOS_Scada.Api.Tests.Security;

// Uses the real Identity managers, EF store, password hasher, and cookie handler.
internal sealed class TestIdentityProvider : IDisposable
{
    public const string Password = "KitchenOS_Test_42!";
    private readonly SqliteConnection _database = new("Data Source=:memory:");
    public TestClock Clock { get; } = new();

    public void Configure(IServiceCollection services)
    {
        _database.Open();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = "Host=unused;Database=unused" }).Build();
        services.AddKitchenOsSecurity(configuration);
        services.RemoveAll<DbContextOptions<IdentityDataContext>>();
        services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<IdentityDataContext>>();
        services.AddDbContext<IdentityDataContext>(options => options.UseSqlite(_database));
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme,
            options => options.TimeProvider = Clock);
    }

    public async Task InitializeAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDataContext>().Database.EnsureCreatedAsync();
        await IdentityInitializationExtensions.InitializeAccountsAsync(scope.ServiceProvider);
        UserManager<IdentityUser> users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        foreach (string role in new[] { DeviceRoles.Viewer, DeviceRoles.Operator, DeviceRoles.Administrator, "NoRole" })
        {
            IdentityUser user = new() { UserName = Email(role), Email = Email(role) };
            Ensure(await users.CreateAsync(user, Password));
            if (role != "NoRole") Ensure(await users.AddToRoleAsync(user, role));
        }
    }

    public static string Email(string role) => role.ToLowerInvariant() + "@kitchenos.test";

    public async Task LoginAsync(HttpClient client, string role = DeviceRoles.Operator)
    {
        await RefreshCsrfAsync(client);
        using HttpResponseMessage response = await client.PostAsJsonAsync("/auth/login", new { email = Email(role), password = Password });
        if (response.StatusCode != HttpStatusCode.NoContent)
            throw new InvalidOperationException("Test login failed: " + await response.Content.ReadAsStringAsync());
        await RefreshCsrfAsync(client);
    }

    public static async Task RefreshCsrfAsync(HttpClient client)
    {
        Dictionary<string, string>? csrf = await client.GetFromJsonAsync<Dictionary<string, string>>("/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!["token"]);
    }

    public void Dispose() => _database.Dispose();
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(",", result.Errors.Select(error => error.Code)));
    }

    internal sealed class TestClock : TimeProvider
    {
        private TimeSpan _offset;
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + _offset;
        public void Advance(TimeSpan time) => _offset += time;
    }
}
