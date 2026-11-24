using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace kitchenOS_Scada.Api.Security;

public static class SecurityApplicationBuilderExtensions
{
    public static IApplicationBuilder UseKitchenOsSecurity(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.Use(async (context, next) =>
        {
            bool mutation = !HttpMethods.IsGet(context.Request.Method)
                && !HttpMethods.IsHead(context.Request.Method)
                && !HttpMethods.IsOptions(context.Request.Method);
            if (mutation)
            {
                try
                {
                    await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "A valid CSRF token is required." });
                    return;
                }
            }
            if (context.WebSockets.IsWebSocketRequest && context.Request.Headers.TryGetValue("Origin", out var origin))
            {
                string frontend = context.RequestServices.GetRequiredService<IOptions<KitchenOsSecurityOptions>>().Value.FrontendOrigin;
                string ownOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
                if (origin.Count != 1 || (!string.Equals(origin[0], frontend, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(origin[0], ownOrigin, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }
            if (mutation && context.User.Identity?.IsAuthenticated == true)
            {
                ILogger logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KitchenOS.Security.Audit");
                string? subject = context.User.FindFirst("sub")?.Value;
                using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
                {
                    ["Subject"] = subject, ["TraceIdentifier"] = context.TraceIdentifier
                });
                await next(context);
                // Never log credentials, cookies, query strings, or command arguments.
                logger.LogInformation("User {Subject}: {Method} {Path} returned {StatusCode}",
                    subject, context.Request.Method, context.Request.Path.Value, context.Response.StatusCode);
                return;
            }
            // Do not keep a monitoring socket alive beyond its authentication ticket.
            if (context.WebSockets.IsWebSocketRequest)
            {
                AuthenticateResult ticket = await context.AuthenticateAsync();
                if (ticket.Properties?.ExpiresUtc is DateTimeOffset expires)
                {
                    TimeSpan remaining = expires - DateTimeOffset.UtcNow;
                    if (remaining <= TimeSpan.Zero) { context.Response.StatusCode = 401; return; }
                    using CancellationTokenSource expiry = new(remaining);
                    using CancellationTokenRegistration registration = expiry.Token.Register(context.Abort);
                    await next(context);
                    return;
                }
            }
            await next(context);
        });
        return app;
    }
}
