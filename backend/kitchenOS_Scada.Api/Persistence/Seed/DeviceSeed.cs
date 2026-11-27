using kitchenOS_Scada.Api.Persistence.Entities;

namespace kitchenOS_Scada.Api.Persistence.Seed;

internal static class DeviceSeed
{
    private static readonly DateTimeOffset SeededAtUtc =
        new(2026, 11, 25, 0, 0, 0, TimeSpan.Zero);

    public static readonly DeviceEntity[] Data =
    [
        new()
        {
            DeviceId = "FRZ-001",
            DeviceName = "Walk-In-Freezer-1",
            DeviceTypeId = 1,
            Description = "Primary walk-in freezer for storing frozen ingredients at -18°C.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            DeviceId = "FRZ-002",
            DeviceName = "Walk-In-Freezer-2",
            DeviceTypeId = 1,
            Description = "Backup walk-in freezer for additional frozen storage at -18°C.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            DeviceId = "OVN-001",
            DeviceName = "Oven-1",
            DeviceTypeId = 3,
            Description = "Primary commercial oven for daily baking and roasting.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            DeviceId = "OVN-002",
            DeviceName = "Oven-2",
            DeviceTypeId = 3,
            Description = "Backup commercial oven for additional baking capacity.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            DeviceId = "MIX-001",
            DeviceName = "Mixer-1",
            DeviceTypeId = 4,
            Description = "Primary commercial mixer for daily food preparation.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            DeviceId = "MIX-002",
            DeviceName = "Mixer-2",
            DeviceTypeId = 4,
            Description = "Backup commercial mixer for additional food preparation.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            DeviceId = "FAN-001",
            DeviceName = "Exhaust-Fan-1",
            DeviceTypeId = 5,
            Description = "Primary exhaust fan for removing smoke, heat, and cooking odors.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            DeviceId = "FAN-002",
            DeviceName = "Exhaust-Fan-2",
            DeviceTypeId = 5,
            Description = "Secondary exhaust fan for supplementary kitchen ventilation.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            DeviceId = "BLD-001",
            DeviceName = "Blender-1",
            DeviceTypeId = 6,
            Description = "Primary commercial blender for sauces, soups, and food preparation.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            DeviceId = "DSH-001",
            DeviceName = "Dishwasher-1",
            DeviceTypeId = 7,
            Description = "Primary commercial dishwasher for cleaning and sanitizing kitchen utensils.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new()
        {
            DeviceId = "SNK-001",
            DeviceName = "Sink-1",
            DeviceTypeId = 8,
            Description = "Main kitchen sink for handwashing and equipment cleaning.",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        }
    ];
}
