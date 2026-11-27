namespace kitchenOS_Scada.Contract.Infrastructure.Bus;

// Canonical Redis channel names shared by publishers and subscribers.
public static class MessageChannels
{
    public const string Commands = "kitchenos:commands";

    public const string Events = "kitchenos:events";

    public const string TelemetryEvents = "kitchenos:events:telemetry";

    public const string AlarmEvents = "kitchenos:events:alarms";

    public const string CommandResultEvents = "kitchenos:events:command-results";

    public const string DeviceInventory = "kitchenos:events:device-inventory-initialization";

    public const string DeviceInventoryUpdates = "kitchenos:events:device-inventory-updates";

}

