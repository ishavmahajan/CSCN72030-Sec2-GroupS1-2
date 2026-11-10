using System.Net.WebSockets;
using kitchenOS_Scada.Api.Infrastructure.Bus;
<<<<<<< HEAD
using kitchenOS_Scada.Api.Simulator;
=======
using kitchenOS_Scada.Api.Notifier;
>>>>>>> 015e4add6fd1db1e49a95694fa9c1591c648bf2d
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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

app.UseWebSockets();
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
});

app.Run();
