using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Contract.Events;
using kitchenOS_Scada.Contract.Infrastructure.Bus;

namespace kitchenOS_Scada.Api.Infrastructure.Bus;

public sealed class MessageBusService(
    IMessageBus messageBus,
    TelemetryEventHandler telemetryEventHandler,
    AlarmEventHandler alarmEventHandler,
    CommandResultEventHandler commandResultEventHandler,
    ILogger<MessageBusService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Register all message-bus subscriptions when the application starts.
        SubscribeToTelemetryEvents(stoppingToken);
        SubscribeToAlarmEvents(stoppingToken);
        SubscribeToCommandResultEvents(stoppingToken);

        // Keep the hosted service alive while the message-bus callbacks are active.
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Message bus subscription service is stopping");
        }
    }

    // Telemetry contains the current readings and state reported by a device.
    private void SubscribeToTelemetryEvents(CancellationToken stoppingToken)
    {
        messageBus.Subscribe<EventEnvelope>(
            MessageChannels.TelemetryEvents,
            (eventMessage, _) => HandleEventAsync(
                eventMessage,
                telemetryEventHandler,
                stoppingToken));

        logger.LogInformation(
            "Subscribed to telemetry events channel {Channel}",
            MessageChannels.TelemetryEvents);
    }

    // Alarms represent abnormal or safety-related device conditions.
    private void SubscribeToAlarmEvents(CancellationToken stoppingToken)
    {
        messageBus.Subscribe<EventEnvelope>(
            MessageChannels.AlarmEvents,
            (eventMessage, _) => HandleEventAsync(
                eventMessage,
                alarmEventHandler,
                stoppingToken));

        logger.LogInformation(
            "Subscribed to alarm events channel {Channel}",
            MessageChannels.AlarmEvents);
    }

    // Command results report whether a device accepted or rejected a command.
    private void SubscribeToCommandResultEvents(CancellationToken stoppingToken)
    {
        messageBus.Subscribe<EventEnvelope>(
            MessageChannels.CommandResultEvents,
            (eventMessage, _) => HandleEventAsync(
                eventMessage,
                commandResultEventHandler,
                stoppingToken));

        logger.LogInformation(
            "Subscribed to command-result events channel {Channel}",
            MessageChannels.CommandResultEvents);
    }

    // Provides common error handling for all event handlers.
    private async Task HandleEventAsync(
        EventEnvelope eventMessage,
        IEventHandler eventHandler,
        CancellationToken cancellationToken)
    {
        try
        {
            await eventHandler.HandleAsync(eventMessage, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Application shutdown is expected to cancel in-flight notifications.
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to process event {EventName} for device {DeviceId}",
                eventMessage.EventName,
                eventMessage.DeviceId);
        }
    }
}
