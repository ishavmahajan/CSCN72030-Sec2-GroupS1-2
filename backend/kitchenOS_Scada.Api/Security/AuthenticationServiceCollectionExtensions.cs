using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace kitchenOS_Scada.Api.Security;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddKitchenOsSecurity(
        this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Authentication");
        services.AddOptions<IdentityProviderOptions>().Bind(section)
            .Validate(options => Uri.TryCreate(options.Authority, UriKind.Absolute, out Uri? uri)
                && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.Query)
                && string.IsNullOrEmpty(uri.Fragment),
                "Authentication:Authority must be the identity provider's HTTPS issuer URL.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
                "Authentication:Audience must identify the KitchenOS API.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RoleClaimType),
                "Authentication:RoleClaimType must identify the provider's role claim.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<Microsoft.Extensions.Options.IOptions<IdentityProviderOptions>>((options, provider) =>
            {
                IdentityProviderOptions identity = provider.Value;
                options.Authority = identity.Authority;
                options.Audience = identity.Audience;
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub",
                    RoleClaimType = identity.RoleClaimType
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Browser WebSockets cannot supply Authorization headers.
                        // Do not accept URL credentials on ordinary HTTP endpoints.
                        if (!context.Request.Headers.ContainsKey("Authorization")
                            && context.Request.Path.Equals(new PathString("/ws"))
                            && context.HttpContext.WebSockets.IsWebSocketRequest
                            && context.Request.Query.TryGetValue("access_token", out var token)
                            && token.Count == 1)
                        {
                            context.Token = token[0];
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Principal?.FindFirst("sub")?.Value))
                        {
                            context.Fail("A stable subject is required.");
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization(AuthorizationPolicies.Configure);
        services.AddHttpContextAccessor();
        return services;
    }
}

public sealed class IdentityProviderOptions
{
    public string Authority { get; set; } = "";
    public string Audience { get; set; } = "";
    public string RoleClaimType { get; set; } = "roles";
}
