namespace kitchenOS_Scada.Api.Enums;
public enum DeviceTypes
{
    WalkInFreezer,
    Stove,
    Oven,
    Mixer,
    ExhaustFan,
    Blender ,
    Dishwasher,
    Sink
}

public static class DeviceTypeExtensions
{
    public static string ToFriendlyString(this DeviceTypes deviceType)
    {
        return deviceType switch
        {
            DeviceTypes.WalkInFreezer => "WALK_IN_FREEZER",
            DeviceTypes.Stove => "STOVE",
            DeviceTypes.Oven => "OVEN",
            DeviceTypes.Mixer => "MIXER",
            DeviceTypes.ExhaustFan => "EXHAUST_FAN",
            DeviceTypes.Blender => "BLENDER",
            DeviceTypes.Dishwasher => "DISHWASHER",
            DeviceTypes.Sink => "SINK",
            _ => deviceType.ToString()
        };
    }

    public static DeviceTypes FromFriendlyString(string friendlyString)
    {
        return friendlyString switch
        {
            "WALK_IN_FREEZER" => DeviceTypes.WalkInFreezer,
            "STOVE" => DeviceTypes.Stove,
            "OVEN" => DeviceTypes.Oven,
            "MIXER" => DeviceTypes.Mixer,
            "EXHAUST_FAN" => DeviceTypes.ExhaustFan,
            "BLENDER" => DeviceTypes.Blender,
            "DISHWASHER" => DeviceTypes.Dishwasher,
            "SINK" => DeviceTypes.Sink,
            _ => throw new ArgumentException($"Unknown device type: {friendlyString}")
        };
    }
}