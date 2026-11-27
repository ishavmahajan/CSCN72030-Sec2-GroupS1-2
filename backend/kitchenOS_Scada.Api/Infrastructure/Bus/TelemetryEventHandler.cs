using System.Text.Json;
using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Api.Persistence;
using kitchenOS_Scada.Api.Persistence.Entities;
using kitchenOS_Scada.Api.Persistence.Seed;
using kitchenOS_Scada.Contract.Events;
using Microsoft.EntityFrameworkCore;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

// Forwards telemetry events from Redis to connected WebSocket clients.
public sealed class TelemetryEventHandler(
    IClientNotifier clientNotifier,
    IDbContextFactory<KitchenOsDbContext>? dbContextFactory = null
    ) : IEventHandler
{
    public string EventName => "telemetry";

    public Task HandleAsync(
        EventEnvelope eventMessage,
        CancellationToken cancellationToken)
    {
        return HandleAndNotifyAsync(eventMessage, cancellationToken);
    }

    private async Task HandleAndNotifyAsync(
        EventEnvelope eventMessage,
        CancellationToken cancellationToken)
    {
        if (dbContextFactory is not null)
        {
            await using KitchenOsDbContext dbContext =
                await dbContextFactory.CreateDbContextAsync(cancellationToken);

            DateTimeOffset occurredAtUtc = DateTimeOffset.UtcNow;
            if (DateTimeOffset.TryParse(
                eventMessage.Timestamp,
                out DateTimeOffset parsedTimestamp))
            {
                occurredAtUtc = parsedTimestamp.ToUniversalTime();
            }

            dbContext.Events.Add(new EventEntity
            {
                DeviceId = eventMessage.DeviceId,
                EventTypeId = EventTypeSeed.TelemetryEventTypeId,
                OccurredAtUtc = occurredAtUtc,
                PayloadJson = JsonSerializer.Serialize(eventMessage.Message)
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Propagate the bus cancellation token so shutdown stops notification work.
        await clientNotifier.Notify(eventMessage, cancellationToken);
    }
}
