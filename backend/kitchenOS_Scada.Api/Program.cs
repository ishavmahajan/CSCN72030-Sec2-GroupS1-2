using System.Net.WebSockets;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Api.Notifier;
using StackExchange.Redis;
using kitchenOS_Scada.Api.Helpers;
using kitchenOS_Scada.Api.Infrastructure.Bus;
using kitchenOS_Scada.Api.Features.Devices;
using kitchenOS_Scada.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using kitchenOS_Scada.Api.Security;



var builder = WebApplication.CreateBuilder(args);

// Register the MVC and OpenAPI services used by the HTTP API.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddKitchenOsSecurity(builder.Configuration);
builder.Services.AddDeviceManagement();
builder.Services.AddKitchenOsPersistence(builder.Configuration);

// Queue commands for the separately hosted simulator through the message bus.
builder.Services.AddScoped<IDeviceCommandService, DeviceCommandService>();

// Allow the local frontend to call the API while keeping the policy scoped to
// the known development origin.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(builder.Configuration["Security:FrontendOrigin"] ?? "http://localhost:3020")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

string redisConnection =
    builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

// Share one Redis connection across the message bus and its event handlers.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddSingleton<RedisMessageBus>();
builder.Services.AddSingleton<IMessageBus>(services =>
    services.GetRequiredService<RedisMessageBus>());

builder.Services.AddSingleton<WebSocketClientNotifier>();
builder.Services.AddSingleton<IClientNotifier>(services =>
    services.GetRequiredService<WebSocketClientNotifier>());

// Register event handlers and start the hosted message-bus subscription loop.
builder.Services.AddSingleton<TelemetryEventHandler>();
builder.Services.AddSingleton<AlarmEventHandler>();
builder.Services.AddSingleton<CommandResultEventHandler>();
builder.Services.AddHostedService<MessageBusService>();



var app = builder.Build();
await app.InitializeKitchenOsIdentityAsync();

// Apply pending EF Core migrations before the API begins accepting requests.
// This keeps the database schema synchronized with the deployed application.
using (IServiceScope scope = app.Services.CreateScope())
{
    KitchenOsDbContext dbContext = scope.ServiceProvider
        .GetRequiredService<KitchenOsDbContext>();

    await dbContext.Database.MigrateAsync();
}

await app.Services.LoadDeviceToSimulator();

// Configure middleware before mapping HTTP and WebSocket endpoints.
app.UseWebSockets();
app.UseCors("Frontend");
app.UseHttpsRedirection();
app.UseKitchenOsSecurity();

if (app.Environment.IsDevelopment())
{
    // Expose the generated OpenAPI document only during development.
    app.MapOpenApi();
}

app.MapKitchenOsIdentity();
app.MapControllers();

// Provide a lightweight liveness endpoint for local orchestration and probes.
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "Healthy" });
})
.WithName("GetHealth").AllowAnonymous();

// Keep database readiness separate from process liveness so orchestration can
// distinguish an unavailable dependency from a stopped API process.
app.MapGet("/health/database", async (
    KitchenOsDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

    return canConnect
        ? Results.Ok(new { status = "Healthy" })
        : Results.Problem(
            "PostgreSQL is unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
})
.WithName("GetDatabaseHealth");

// Accept WebSocket clients and remove them from the notifier when they close
// normally, disconnect unexpectedly, or cancel the request.
app.Map("/ws", async (HttpContext context, WebSocketClientNotifier notifier) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using WebSocket socket =
        await context.WebSockets.AcceptWebSocketAsync();

    Guid clientId = notifier.AddClient(socket);
    byte[] buffer = new byte[4096];

    try
    {
        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await socket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                context.RequestAborted);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    context.RequestAborted);

                break;
            }
        }
    }
    catch (WebSocketException)
    {
        // The client disconnected unexpectedly.
    }
    catch (OperationCanceledException)
        when (context.RequestAborted.IsCancellationRequested)
    {
        // The request was cancelled.
    }
    finally
    {
        notifier.RemoveClient(clientId);
    }
}).WithName("WebSocketEndpoint").RequireAuthorization(AuthorizationPolicies.Monitor);
app.Run();


