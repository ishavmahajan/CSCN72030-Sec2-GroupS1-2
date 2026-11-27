using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace kitchenOS_Scada.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "device_types",
                columns: new[] { "device_type_id", "description", "is_active", "name" },
                values: new object[,]
                {
                    { 1, "Walk-in freezer", true, "WALK_IN_FREEZER" },
                    { 2, "Commercial stove", true, "STOVE" },
                    { 3, "Commercial oven", true, "OVEN" },
                    { 4, "Commercial mixer", true, "MIXER" },
                    { 5, "Kitchen exhaust fan", true, "EXHAUST_FAN" },
                    { 6, "Commercial blender", true, "BLENDER" },
                    { 7, "Commercial dishwasher", true, "DISHWASHER" },
                    { 8, "Kitchen sink", true, "SINK" }
                });

            migrationBuilder.InsertData(
                table: "event_types",
                columns: new[] { "event_type_id", "description", "is_active", "name" },
                values: new object[] { 1, "Device telemetry event", true, "telemetry" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "device_types",
                keyColumn: "device_type_id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "device_types",
                keyColumn: "device_type_id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "device_types",
                keyColumn: "device_type_id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "device_types",
                keyColumn: "device_type_id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "device_types",
                keyColumn: "device_type_id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "device_types",
                keyColumn: "device_type_id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "device_types",
                keyColumn: "device_type_id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "device_types",
                keyColumn: "device_type_id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "event_types",
                keyColumn: "event_type_id",
                keyValue: 1);
        }
    }
}
