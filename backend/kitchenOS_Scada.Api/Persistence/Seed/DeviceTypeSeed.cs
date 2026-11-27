using kitchenOS_Scada.Api.Persistence.Entities;

namespace kitchenOS_Scada.Api.Persistence.Seed;

internal static class DeviceTypeSeed
{
    public static readonly DeviceTypeEntity[] Data =
    [
        new() { DeviceTypeId = 1, Name = "WALK_IN_FREEZER", Description = "Walk-in freezer" },
        new() { DeviceTypeId = 2, Name = "STOVE", Description = "Commercial stove" },
        new() { DeviceTypeId = 3, Name = "OVEN", Description = "Commercial oven" },
        new() { DeviceTypeId = 4, Name = "MIXER", Description = "Commercial mixer" },
        new() { DeviceTypeId = 5, Name = "EXHAUST_FAN", Description = "Kitchen exhaust fan" },
        new() { DeviceTypeId = 6, Name = "BLENDER", Description = "Commercial blender" },
        new() { DeviceTypeId = 7, Name = "DISHWASHER", Description = "Commercial dishwasher" },
        new() { DeviceTypeId = 8, Name = "SINK", Description = "Kitchen sink" }
    ];
}
