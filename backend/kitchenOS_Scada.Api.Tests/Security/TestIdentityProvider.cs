using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using kitchenOS_Scada.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace kitchenOS_Scada.Api.Tests.Security;

// Generated test keys never leave the test process. Production still uses OIDC discovery.
internal sealed class TestIdentityProvider : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly RsaSecurityKey _key;
    public const string Issuer = "https://identity.kitchenos.test";
    public const string Audience = "kitchenos-test-api";

    public TestIdentityProvider() => _key = new RsaSecurityKey(_rsa) { KeyId = "test-key" };

    public void Configure(IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = Issuer,
                ["Authentication:Audience"] = Audience
            }).Build();
        services.AddKitchenOsSecurity(configuration);
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            OpenIdConnectConfiguration metadata = new() { Issuer = Issuer };
            metadata.SigningKeys.Add(new RsaSecurityKey(_rsa.ExportParameters(false)) { KeyId = _key.KeyId });
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
        });
    }

    public string Token(string? role = DeviceRoles.Operator, bool expired = false,
        string issuer = Issuer, string audience = Audience, bool includeSubject = true)
    {
        List<Claim> claims = [];
        if (includeSubject) claims.Add(new Claim("sub", "test-operator-01"));
        if (role is not null) claims.Add(new Claim("roles", role));
        DateTime now = DateTime.UtcNow;
        JwtSecurityToken token = new(issuer, audience, claims,
            now.AddMinutes(-10), expired ? now.AddMinutes(-1) : now.AddMinutes(5),
            new SigningCredentials(_key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose() => _rsa.Dispose();
}
