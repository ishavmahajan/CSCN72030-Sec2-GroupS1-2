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
            .Include(alarm => alarm.Device)
                .ThenInclude(device => device!.DeviceType)
            .OrderByDescending(alarm => alarm.AlarmId)
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
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.AlarmId == request.AlarmId, cancellationToken);

        if (alarm is null)
        {
            return new(AlarmAcknowledgementResultStatus.NotFound, null);
        }

        if (!string.Equals(alarm.DeviceId, request.DeviceId, StringComparison.Ordinal))
        {
            return new(AlarmAcknowledgementResultStatus.DeviceMismatch, null);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset acknowledgedAtUtc = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
        IQueryable<AlarmEntity> matchingAlarms = dbContext.Alarms
            .Where(item => item.DeviceId == alarm.DeviceId && item.Code == alarm.Code);

        // Preserve earlier acknowledgements and update the matching occurrences in one statement.
        int updated = await matchingAlarms
            .Where(item => item.AcknowledgedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.AcknowledgedByOperatorId, operatorId)
                .SetProperty(item => item.AcknowledgedByOperator, operatorName)
                .SetProperty(item => item.AcknowledgedAtUtc, acknowledgedAtUtc),
                cancellationToken);

        AlarmEntity? selected = await dbContext.Alarms
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.AlarmId == request.AlarmId, cancellationToken);

        if (updated > 0)
        {
            List<AlarmEntity> acknowledgements = await matchingAlarms
                .AsNoTracking()
                .Where(item => item.AcknowledgedAtUtc == acknowledgedAtUtc && item.AcknowledgedByOperatorId == operatorId)
                .Include(item => item.Device)
                    .ThenInclude(device => device!.DeviceType)
                .ToListAsync(cancellationToken);

            foreach (AlarmEntity acknowledgement in acknowledgements)
            {
                await clientNotifier.Notify(CreateAcknowledgedEvent(acknowledgement), cancellationToken);
            }
        }

        if (selected is null)
        {
            return new(AlarmAcknowledgementResultStatus.NotFound, null);
        }

        bool alreadyAcknowledged = updated == 0
            || selected.AcknowledgedAtUtc != acknowledgedAtUtc
            || selected.AcknowledgedByOperatorId != operatorId;
        return new(
            updated > 0 ? AlarmAcknowledgementResultStatus.Acknowledged : AlarmAcknowledgementResultStatus.AlreadyAcknowledged,
            ToAcknowledgementResponse(selected, alreadyAcknowledged));
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
            alarm.AcknowledgedByOperatorId,
            alarm.AcknowledgedByOperator,
            alarm.AcknowledgedAtUtc);
    }

    private static AlarmAcknowledgementResponse ToAcknowledgementResponse(
        AlarmEntity alarm,
        bool alreadyAcknowledged)
    {
        return new AlarmAcknowledgementResponse(
            alarm.AlarmId,
            alarm.DeviceId,
            alarm.AcknowledgedByOperatorId!,
            alarm.AcknowledgedByOperator!,
            alarm.AcknowledgedAtUtc!.Value,
            alreadyAcknowledged);
    }

    private static EventEnvelope CreateAcknowledgedEvent(AlarmEntity alarm)
    {
        return new EventEnvelope
        {
            EventName = "alarmAcknowledged",
            DeviceId = alarm.DeviceId,
            DeviceType = DeviceTypeExtensions.FromFriendlyString(alarm.Device!.DeviceType.Name),
            Timestamp = alarm.AcknowledgedAtUtc!.Value.ToString("O", CultureInfo.InvariantCulture),
            Message = CreateNotificationMessage(alarm)
        };
    }

    private static AlarmBody CreateNotificationMessage(AlarmEntity alarm)
    {
        return new AlarmBody
        {
            AlarmId = alarm.AlarmId,
            Code = alarm.Code,
            Severity = alarm.Severity,
            Text = alarm.Text,
            Timestamp = alarm.OccurredAtUtc.UtcDateTime,
            IsActive = alarm.IsActive,
            AcknowledgedByOperatorId = alarm.AcknowledgedByOperatorId,
            AcknowledgedByOperator = alarm.AcknowledgedByOperator,
            AcknowledgedAtUtc = alarm.AcknowledgedAtUtc
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
