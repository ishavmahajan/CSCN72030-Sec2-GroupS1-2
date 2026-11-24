namespace kitchenOS_Scada.Api.Security;

public static class SecurityApplicationBuilderExtensions
{
    public static IApplicationBuilder UseKitchenOsSecurity(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.Use(async (context, next) =>
        {
            // Only validated claims reach audit-sensitive HTTP operations.
            // Never log bearer tokens, query strings, or command argument bodies.
            bool mutation = !HttpMethods.IsGet(context.Request.Method)
                && !HttpMethods.IsHead(context.Request.Method)
                && !HttpMethods.IsOptions(context.Request.Method);
            if (mutation && context.User.Identity?.IsAuthenticated == true)
            {
                ILogger logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KitchenOS.Security.Audit");
                string? subject = context.User.FindFirst("sub")?.Value;
                string? issuer = context.User.FindFirst("iss")?.Value;
                using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
                {
                    ["Subject"] = subject,
                    ["Issuer"] = issuer,
                    ["TraceIdentifier"] = context.TraceIdentifier
                });
                await next(context);
                logger.LogInformation("User {Subject} from {Issuer}: {Method} {Path} returned {StatusCode}",
                    subject, issuer, context.Request.Method, context.Request.Path.Value,
                    context.Response.StatusCode);
                return;
            }
            await next(context);
        });
        return app;
    }
}
