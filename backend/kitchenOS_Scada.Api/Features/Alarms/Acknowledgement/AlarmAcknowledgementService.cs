using System.Globalization;
using kitchenOS_Scada.Api.Features.Alarms.Contracts;
using kitchenOS_Scada.Api.Notifier;
using kitchenOS_Scada.Api.Persistence;
using kitchenOS_Scada.Api.Persistence.Entities;
using kitchenOS_Scada.Api.Persistence.Seed;
using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Contract.Events;
using Microsoft.EntityFrameworkCore;

namespace kitchenOS_Scada.Api.Features.Alarms.Acknowledgement;

public sealed class AlarmAcknowledgementService(
    IDbContextFactory<KitchenOsDbContext> dbContextFactory,
    IClientNotifier clientNotifier) : IAlarmAcknowledgementService
{
    public async Task<IReadOnlyList<AlarmStatusResponse>> GetRecentAsync(
        CancellationToken cancellationToken)
    {
        await using KitchenOsDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<AlarmEntity> alarms = await dbContext.Alarms
            .AsNoTracking()
            .Include(alarm => alarm.Acknowledgement)
            .Include(alarm => alarm.Device)
                .ThenInclude(device => device!.DeviceType)
            .OrderByDescending(alarm => alarm.AlarmId)
            .Take(5)
            .ToListAsync(cancellationToken);

        return alarms.Select(ToStatusResponse).ToArray();
    }

    public async Task<AlarmAcknowledgementResult> AcknowledgeAsync(
        AcknowledgeAlarmRequest request,
        string operatorId,
        string operatorName,
        CancellationToken cancellationToken)
    {
        if (request.AlarmId <= 0
            || string.IsNullOrWhiteSpace(request.DeviceId)
            || request.DeviceId.Length > 100
            || string.IsNullOrWhiteSpace(operatorId)
            || string.IsNullOrWhiteSpace(operatorName))
        {
            return new(AlarmAcknowledgementResultStatus.InvalidRequest, null);
        }

        await using KitchenOsDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AlarmEntity? alarm = await dbContext.Alarms
            .Include(item => item.Acknowledgement)
            .Include(item => item.Device)
                .ThenInclude(device => device!.DeviceType)
            .SingleOrDefaultAsync(item => item.AlarmId == request.AlarmId, cancellationToken);

        if (alarm is null)
        {
            return new(AlarmAcknowledgementResultStatus.NotFound, null);
        }

        if (!string.Equals(alarm.DeviceId, request.DeviceId, StringComparison.Ordinal))
        {
            return new(AlarmAcknowledgementResultStatus.DeviceMismatch, null);
        }

        if (alarm.Acknowledgement is not null)
        {
            return new(
                AlarmAcknowledgementResultStatus.AlreadyAcknowledged,
                ToAcknowledgementResponse(alarm.Acknowledgement, alreadyAcknowledged: true));
        }

        AlarmAcknowledgementEntity acknowledgement = new()
        {
            AlarmId = alarm.AlarmId,
            DeviceId = alarm.DeviceId,
            OperatorId = operatorId,
            OperatorName = operatorName,
            AcknowledgedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.AlarmAcknowledgements.Add(acknowledgement);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await using KitchenOsDbContext retryContext =
                await dbContextFactory.CreateDbContextAsync(cancellationToken);
            AlarmAcknowledgementEntity? existing = await retryContext.AlarmAcknowledgements
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.AlarmId == alarm.AlarmId, cancellationToken);
            if (existing is null)
            {
                throw;
            }

            return new(
                AlarmAcknowledgementResultStatus.AlreadyAcknowledged,
                ToAcknowledgementResponse(existing, alreadyAcknowledged: true));
        }

        AlarmAcknowledgementResponse response =
            ToAcknowledgementResponse(acknowledgement, alreadyAcknowledged: false);
        await clientNotifier.Notify(CreateAcknowledgedEvent(alarm, acknowledgement), cancellationToken);

        return new(AlarmAcknowledgementResultStatus.Acknowledged, response);
    }

    public async Task RecordAlarmAsync(
        EventEnvelope eventMessage,
        CancellationToken cancellationToken)
    {
        if (eventMessage.Message is not AlarmBody alarmBody
            || string.IsNullOrWhiteSpace(eventMessage.DeviceId)
            || string.IsNullOrWhiteSpace(alarmBody.Code)
            || string.IsNullOrWhiteSpace(alarmBody.Severity)
            || string.IsNullOrWhiteSpace(alarmBody.Text))
        {
            throw new InvalidDataException("The alarm event payload is incomplete.");
        }

        DateTimeOffset occurredAtUtc = ParseOccurredAtUtc(eventMessage.Timestamp, alarmBody.Timestamp);
        AlarmEntity alarm = new()
        {
            DeviceId = eventMessage.DeviceId,
            EventTypeId = EventTypeSeed.AlarmEventTypeId,
            Code = alarmBody.Code,
            Severity = alarmBody.Severity,
            Text = alarmBody.Text,
            OccurredAtUtc = occurredAtUtc
        };

        await using KitchenOsDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Alarms.Add(alarm);
        await dbContext.SaveChangesAsync(cancellationToken);

        EventEnvelope notification = new()
        {
            EventName = "alarm",
            DeviceId = eventMessage.DeviceId,
            DeviceType = eventMessage.DeviceType,
            Timestamp = occurredAtUtc.ToString("O", CultureInfo.InvariantCulture),
            Message = CreateNotificationMessage(alarm)
        };
        await clientNotifier.Notify(notification, cancellationToken);
    }

    private static AlarmStatusResponse ToStatusResponse(AlarmEntity alarm)
    {
        return new AlarmStatusResponse(
            alarm.AlarmId,
            alarm.DeviceId,
            alarm.Device?.DeviceType.Name ?? string.Empty,
            alarm.Code,
            alarm.Severity,
            alarm.Text,
            alarm.OccurredAtUtc,
            alarm.IsActive,
            alarm.Acknowledgement?.OperatorId,
            alarm.Acknowledgement?.OperatorName,
            alarm.Acknowledgement?.AcknowledgedAtUtc);
    }

    private static AlarmAcknowledgementResponse ToAcknowledgementResponse(
        AlarmAcknowledgementEntity acknowledgement,
        bool alreadyAcknowledged)
    {
        return new AlarmAcknowledgementResponse(
            acknowledgement.AlarmId,
            acknowledgement.DeviceId,
            acknowledgement.OperatorId,
            acknowledgement.OperatorName,
            acknowledgement.AcknowledgedAtUtc,
            alreadyAcknowledged);
    }

    private static EventEnvelope CreateAcknowledgedEvent(
        AlarmEntity alarm,
        AlarmAcknowledgementEntity acknowledgement)
    {
        return new EventEnvelope
        {
            EventName = "alarmAcknowledged",
            DeviceId = alarm.DeviceId,
            DeviceType = DeviceTypeExtensions.FromFriendlyString(alarm.Device!.DeviceType.Name),
            Timestamp = acknowledgement.AcknowledgedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            Message = CreateNotificationMessage(alarm, acknowledgement)
        };
    }

    private static AlarmBody CreateNotificationMessage(
        AlarmEntity alarm,
        AlarmAcknowledgementEntity? acknowledgement = null)
    {
        return new AlarmBody
        {
            AlarmId = alarm.AlarmId,
            Code = alarm.Code,
            Severity = alarm.Severity,
            Text = alarm.Text,
            Timestamp = alarm.OccurredAtUtc.UtcDateTime,
            IsActive = alarm.IsActive,
            AcknowledgedByOperatorId = acknowledgement?.OperatorId,
            AcknowledgedByOperator = acknowledgement?.OperatorName,
            AcknowledgedAtUtc = acknowledgement?.AcknowledgedAtUtc
        };
    }

    private static DateTimeOffset ParseOccurredAtUtc(string eventTimestamp, DateTime bodyTimestamp)
    {
        if (DateTimeOffset.TryParse(
            eventTimestamp,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset parsedTimestamp))
        {
            return parsedTimestamp.ToUniversalTime();
        }

        DateTime utcTimestamp = DateTime.SpecifyKind(bodyTimestamp, DateTimeKind.Utc);
        return new DateTimeOffset(utcTimestamp);
    }

}
