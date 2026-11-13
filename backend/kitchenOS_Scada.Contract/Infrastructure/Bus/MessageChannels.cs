namespace kitchenOS_Scada.Contract.Infrastructure.Bus;

public static class MessageChannels
{
    public const string Commands = "kitchenos:commands";

    public const string Events = "kitchenos:events";

    public const string TelemetryEvents = "kitchenos:events:telemetry";

    public const string AlarmEvents = "kitchenos:events:alarms";

    public const string CommandResultEvents = "kitchenos:events:command-results";
}
