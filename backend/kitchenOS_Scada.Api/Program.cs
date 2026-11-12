using System.Net.WebSockets;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS_Scada.Api.Simulator;
using kitchenOS_Scada.Api.Notifier;
using StackExchange.Redis;
using kitchenOS_Scada.Api.Simulator.Devices;
using kitchenOS_Scada.Api.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddSingleton<RedisMessageBus>();
builder.Services.AddSingleton<IMessageBus>(services =>
    services.GetRequiredService<RedisMessageBus>());

builder.Services.AddSingleton<WebSocketClientNotifier>();
builder.Services.AddSingleton<IClientNotifier>(services =>
    services.GetRequiredService<WebSocketClientNotifier>());



var app = builder.Build();

var simulator = new DeviceSimulator();
string devicesJson = FileHelper.GetSimulationData("devices.json");
string simulationJson = FileHelper.GetSimulationData("simulation.json");
simulator.Load(devicesJson, simulationJson, app.Services.GetRequiredService<IMessageBus>());
await simulator.Start();

app.UseWebSockets();
app.UseCors("Frontend");
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

}

app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "Healthy" });
})
.WithName("GetHealth");

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
app.MapGet("/devices", () =>
{
    List<SimulatedDevice> devices = simulator.GetDevices();
    return Results.Ok(devices.Select(d => new
    {
        d.DeviceId,
        d.DeviceType,
        d.DeviceName,
        d.Description,
        readPoints = d.ReadPoints()
    }));
}).WithName("GetDevices");
app.Run();
