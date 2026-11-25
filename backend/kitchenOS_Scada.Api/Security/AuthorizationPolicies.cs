using Microsoft.AspNetCore.Authorization;

namespace kitchenOS_Scada.Api.Security;

public static class AuthorizationPolicies
{
    public const string Monitor = "MonitorDevices";
    public const string Operate = "OperateDevices";
    public const string Administer = "AdministerDevices";

    public static void Configure(AuthorizationOptions options)
    {
        AuthorizationPolicy monitoring = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(DeviceRoles.Viewer, DeviceRoles.Operator, DeviceRoles.Administrator)
            .Build();
        options.AddPolicy(Monitor, monitoring);
        options.AddPolicy(Operate, policy => policy.RequireAuthenticatedUser()
            .RequireRole(DeviceRoles.Operator, DeviceRoles.Administrator));
        options.AddPolicy(Administer, policy => policy.RequireAuthenticatedUser()
            .RequireRole(DeviceRoles.Administrator));

        // New endpoints remain protected even if an attribute is accidentally omitted.
        options.DefaultPolicy = monitoring;
        options.FallbackPolicy = monitoring;
    }
}
