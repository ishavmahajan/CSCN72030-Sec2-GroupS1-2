using kitchenOS_Scada.Contract.Enums;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Api.Notifier;

public class WebSocketClientNotifier : IClientNotifier
{
    private readonly ConcurrentDictionary<Guid, ClientConnection> clients = new();

    private static readonly JsonSerializerOptions JsonOptions = new(
        JsonSerializerDefaults.Web);

    private sealed class ClientConnection
    {
        public WebSocket Socket { get; }
        public SemaphoreSlim SendLock { get; } = new(1, 1);

        public ClientConnection(WebSocket socket)
        {
            Socket = socket;
        }
    }

    public Guid AddClient(WebSocket socket)
    {
        Guid clientId = Guid.NewGuid();
        clients.TryAdd(clientId, new ClientConnection(socket));
        return clientId;
    }

    public void RemoveClient(Guid clientId)
    {
        clients.TryRemove(clientId, out _);
    }

    public async Task Notify(
        IEvent deviceEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (clients.IsEmpty)
        {
            return;
        }

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            eventName = deviceEvent.EventName,
            deviceId = deviceEvent.DeviceId,
            deviceType = deviceEvent.DeviceType.ToFriendlyString(),
            timestamp = deviceEvent.Timestamp,
            message = (object)deviceEvent.Message
        }, JsonOptions);

        foreach (KeyValuePair<Guid, ClientConnection> entry in clients)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ClientConnection client = entry.Value;

            await client.SendLock.WaitAsync(cancellationToken);

            try
            {
                if (client.Socket.State != WebSocketState.Open)
                {
                    RemoveClient(entry.Key);
                    continue;
                }

                await client.Socket.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
            }
            catch (WebSocketException)
            {
                RemoveClient(entry.Key);
                client.Socket.Abort();
            }
            catch (ObjectDisposedException)
            {
                RemoveClient(entry.Key);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                RemoveClient(entry.Key);
                client.Socket.Abort();
                throw;
            }
            finally
            {
                client.SendLock.Release();
            }
        }
    }
}
