using kitchenOS_Scada.Api.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kitchenOS_Scada.Api.Tests.Security;

[TestClass]
public sealed class AuthenticationConfigurationTests
{
    [TestMethod]
    [DataRow(null, null)]
    [DataRow("http://identity.test", "kitchenos")]
    [DataRow("https://identity.test", "")]
    public void MissingOrInsecureProviderConfiguration_IsRejected(string? authority, string? audience)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = authority,
                ["Authentication:Audience"] = audience
            }).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKitchenOsSecurity(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.ThrowsExactly<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<IdentityProviderOptions>>().Value);
    }
}
