namespace kitchenOS_Scada.Api.Enums;

// Event types recognized by the API's event pipeline.
public enum EventNames
{
    TELEMETRY,
    COMMAND,
    ALARM,
    ALARMCLEARED,

}

public static class EventNamesExtensions
{
    // Convert event enum values to their client-facing names.
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
