namespace kitchenOS_Scada.Api.Enums;

public enum AlarmSeverity
{
    INFO,
    WARNING,
    CRITICAL
}


public static class AlarmSeverityExtensions
{
    public static string GetAlarmSeverity(this AlarmSeverity alarmSeverity)
    {
        return alarmSeverity switch
        {
            AlarmSeverity.INFO => "info",
            AlarmSeverity.WARNING => "warning",
            AlarmSeverity.CRITICAL => "critical",
            _ => throw new ArgumentOutOfRangeException(nameof(alarmSeverity), alarmSeverity, null)
        };
    }

    public static AlarmSeverity GetAlarmSeverityEnum(string alarmSeverity)
    {
        return alarmSeverity switch
        {
            "info" => AlarmSeverity.INFO,
            "warning" => AlarmSeverity.WARNING,
            "critical" => AlarmSeverity.CRITICAL,
            _ => throw new ArgumentOutOfRangeException(nameof(alarmSeverity), alarmSeverity, null)
        };
    }
}