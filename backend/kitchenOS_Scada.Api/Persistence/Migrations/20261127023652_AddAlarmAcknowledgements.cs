using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kitchenOS_Scada.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlarmAcknowledgements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alarm_acknowledgements",
                columns: table => new
                {
                    alarm_id = table.Column<long>(type: "bigint", nullable: false),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    operator_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    operator_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    acknowledged_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alarm_acknowledgements", x => x.alarm_id);
                    table.ForeignKey(
                        name: "FK_alarm_acknowledgements_alarms_alarm_id",
                        column: x => x.alarm_id,
                        principalTable: "alarms",
                        principalColumn: "alarm_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "event_types",
                columns: new[] { "event_type_id", "description", "is_active", "name" },
                values: new object[] { 2, "Device alarm event", true, "alarm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alarm_acknowledgements");

            migrationBuilder.DeleteData(
                table: "event_types",
                keyColumn: "event_type_id",
                keyValue: 2);
        }
    }
}
