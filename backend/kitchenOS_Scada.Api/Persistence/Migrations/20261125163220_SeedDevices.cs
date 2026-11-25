using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace kitchenOS_Scada.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "devices",
                columns: new[] { "device_id", "created_at_utc", "description", "device_name", "device_type_id", "is_active", "updated_at_utc" },
                values: new object[,]
                {
                    { "BLD-001", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Primary commercial blender for sauces, soups, and food preparation.", "Blender-1", 6, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "DSH-001", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Primary commercial dishwasher for cleaning and sanitizing kitchen utensils.", "Dishwasher-1", 7, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "FAN-001", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Primary exhaust fan for removing smoke, heat, and cooking odors.", "Exhaust-Fan-1", 5, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "FAN-002", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Secondary exhaust fan for supplementary kitchen ventilation.", "Exhaust-Fan-2", 5, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "FRZ-001", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Primary walk-in freezer for storing frozen ingredients at -18°C.", "Walk-In-Freezer-1", 1, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "FRZ-002", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Backup walk-in freezer for additional frozen storage at -18°C.", "Walk-In-Freezer-2", 1, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "MIX-001", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Primary commercial mixer for daily food preparation.", "Mixer-1", 4, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "MIX-002", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Backup commercial mixer for additional food preparation.", "Mixer-2", 4, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "OVN-001", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Primary commercial oven for daily baking and roasting.", "Oven-1", 3, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "OVN-002", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Backup commercial oven for additional baking capacity.", "Oven-2", 3, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "SNK-001", new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Main kitchen sink for handwashing and equipment cleaning.", "Sink-1", 8, true, new DateTimeOffset(new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "BLD-001");

            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "DSH-001");

            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "FAN-001");

            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "FAN-002");

            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "FRZ-001");

            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "FRZ-002");

            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "MIX-001");

            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "MIX-002");

            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "OVN-001");

            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "OVN-002");

            migrationBuilder.DeleteData(
                table: "devices",
                keyColumn: "device_id",
                keyValue: "SNK-001");
        }
    }
}
