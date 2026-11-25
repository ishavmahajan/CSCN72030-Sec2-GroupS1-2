using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace kitchenOS_Scada.Api.Security;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddKitchenOsSecurity(
        this IServiceCollection services, IConfiguration configuration)
    {
        string connection = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is required for Identity.");
        services.AddDbContext<IdentityDataContext>(options => options.UseNpgsql(connection,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.ClaimsIdentity.UserIdClaimType = "sub";
        }).AddEntityFrameworkStores<IdentityDataContext>().AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "KitchenOS.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
            // API clients need status codes, including on the raw WebSocket route.
            options.Events.OnRedirectToLogin = context => Status(context, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => Status(context, StatusCodes.Status403Forbidden);
        });
        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<IHostEnvironment>((options, environment) => options.Cookie.SecurePolicy =
                environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always);
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "KitchenOS.Csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.AddOptions<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>()
            .Configure<IHostEnvironment>((options, environment) => options.Cookie.SecurePolicy =
                environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("IdentityLogin", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        services.AddAuthorization(AuthorizationPolicies.Configure);
        services.AddHttpContextAccessor();
        services.Configure<BootstrapAdministratorOptions>(configuration.GetSection("Identity:BootstrapAdministrator"));
        services.Configure<KitchenOsSecurityOptions>(configuration.GetSection("Security"));
        services.AddDataProtection().SetApplicationName("KitchenOS")
            .PersistKeysToFileSystem(new DirectoryInfo(configuration["Security:KeyRingPath"]
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KitchenOS", "keys")));
        return services;
    }

    private static Task Status(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}

public sealed class KitchenOsSecurityOptions
{
    public string FrontendOrigin { get; set; } = "http://localhost:3020";
}

public sealed class BootstrapAdministratorOptions
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}
