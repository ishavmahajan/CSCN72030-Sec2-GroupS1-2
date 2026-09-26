namespace kitchenOS_Scada.Api.Enums;
public enum EventNames
{
    TELEMETRY,
    COMMAND,
    ALARM,
    ALARMCLEARED,

}

public static class EventNamesExtensions
{
    public static string GetEventName(this EventNames eventName)
    {
        return eventName switch
        {
            EventNames.TELEMETRY => "telemetry",
            EventNames.COMMAND => "command",
            EventNames.ALARM => "alarm",
            EventNames.ALARMCLEARED => "alarmCleared",
            _ => throw new ArgumentOutOfRangeException(nameof(eventName), eventName, null)
        };
    }

    public static EventNames GetEventNameEnum(string eventName)
    {
        return eventName switch
        {
            "telemetry" => EventNames.TELEMETRY,
            "command" => EventNames.COMMAND,
            "alarm" => EventNames.ALARM,
            "alarmCleared" => EventNames.ALARMCLEARED,
            _ => throw new ArgumentOutOfRangeException(nameof(eventName), eventName, null)
        };
    }
}