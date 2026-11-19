using System.Net.WebSockets;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Api.Simulator;
using kitchenOS_Scada.Api.Notifier;
using StackExchange.Redis;
using kitchenOS_Scada.Api.Helpers;
using kitchenOS.Simulator;
using kitchenOS_Scada.Api.Infrastructure.Bus;
using kitchenOS_Scada.Api.Features.Devices;


var builder = WebApplication.CreateBuilder(args);

// Register the MVC and OpenAPI services used by the HTTP API.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Keep the simulator and command service in-process so API requests can use the
// same device state that produces telemetry for connected clients.
builder.Services.AddSingleton<DeviceSimulator>();
builder.Services.AddSingleton<IDeviceCommandService, DeviceCommandService>();

// Allow the local frontend to call the API while keeping the policy scoped to
// the known development origin.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:3020")
            .AllowAnyHeader()
            .AllowAnyMethod();
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

// Load the initial simulation before serving requests so device endpoints have
// a complete, consistent view of the simulated devices.
var simulator = app.Services.GetRequiredService<DeviceSimulator>();
string devicesJson = FileHelper.GetSimulationData("devices.json");
string simulationJson = FileHelper.GetSimulationData("simulation.json");
simulator.Load(devicesJson, simulationJson, app.Services.GetRequiredService<IMessageBus>());

// Configure middleware before mapping HTTP and WebSocket endpoints.
app.UseWebSockets();
app.UseCors("Frontend");
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    // Expose the generated OpenAPI document only during development.
    app.MapOpenApi();
}

app.MapControllers();

// Provide a lightweight liveness endpoint for local orchestration and probes.
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "Healthy" });
})
.WithName("GetHealth");

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
}).WithName("WebSocketEndpoint");
app.Run();

