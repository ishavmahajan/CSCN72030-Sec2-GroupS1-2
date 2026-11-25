using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace kitchenOS_Scada.Api.Security;

public static class IdentityInitializationExtensions
{
    public static async Task InitializeKitchenOsIdentityAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDataContext>().Database.MigrateAsync();
        await InitializeAccountsAsync(scope.ServiceProvider);
    }

    public static async Task InitializeAccountsAsync(IServiceProvider services)
    {
        RoleManager<IdentityRole> roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (string role in new[] { DeviceRoles.Viewer, DeviceRoles.Operator, DeviceRoles.Administrator })
        {
            if (!await roles.RoleExistsAsync(role)) Ensure(await roles.CreateAsync(new IdentityRole(role)));
        }
        BootstrapAdministratorOptions bootstrap = services.GetRequiredService<IOptions<BootstrapAdministratorOptions>>().Value;
        if (string.IsNullOrWhiteSpace(bootstrap.Email) && string.IsNullOrWhiteSpace(bootstrap.Password)) return;
        if (string.IsNullOrWhiteSpace(bootstrap.Email) || string.IsNullOrWhiteSpace(bootstrap.Password))
            throw new InvalidOperationException("Both bootstrap administrator email and password must be configured.");

        UserManager<IdentityUser> users = services.GetRequiredService<UserManager<IdentityUser>>();
        // Bootstrap only creates the initial account; restart never resets passwords or grants an existing user extra access.
        if ((await users.GetUsersInRoleAsync(DeviceRoles.Administrator)).Count > 0) return;
        if (await users.FindByEmailAsync(bootstrap.Email) is not null)
            throw new InvalidOperationException("Bootstrap email already exists. Use a new email or provision the administrator explicitly.");
        IdentityUser user = new() { UserName = bootstrap.Email, Email = bootstrap.Email };
        Ensure(await users.CreateAsync(user, bootstrap.Password));
        IdentityResult assigned = await users.AddToRoleAsync(user, DeviceRoles.Administrator);
        if (!assigned.Succeeded)
        {
            await users.DeleteAsync(user);
            Ensure(assigned);
        }
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Identity initialization failed: " +
                string.Join(", ", result.Errors.Select(error => error.Code)));
    }
}
