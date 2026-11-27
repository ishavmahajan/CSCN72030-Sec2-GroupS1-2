using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kitchenOS_Scada.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreAcknowledgementsOnAlarms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "acknowledged_at_utc",
                table: "alarms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "acknowledged_by_operator",
                table: "alarms",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "acknowledged_by_operator_id",
                table: "alarms",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE alarms AS alarm
                SET acknowledged_at_utc = acknowledgement.acknowledged_at_utc,
                    acknowledged_by_operator = acknowledgement.operator_name,
                    acknowledged_by_operator_id = acknowledgement.operator_id
                FROM alarm_acknowledgements AS acknowledgement
                WHERE alarm.alarm_id = acknowledgement.alarm_id;
                """);

            migrationBuilder.DropTable(
                name: "alarm_acknowledgements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alarm_acknowledgements",
                columns: table => new
                {
                    alarm_id = table.Column<long>(type: "bigint", nullable: false),
                    acknowledged_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    operator_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    operator_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
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

            migrationBuilder.Sql("""
                INSERT INTO alarm_acknowledgements
                    (alarm_id, acknowledged_at_utc, device_id, operator_id, operator_name)
                SELECT alarm_id, acknowledged_at_utc, device_id, acknowledged_by_operator_id, acknowledged_by_operator
                FROM alarms
                WHERE acknowledged_at_utc IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "acknowledged_at_utc",
                table: "alarms");

            migrationBuilder.DropColumn(
                name: "acknowledged_by_operator",
                table: "alarms");

            migrationBuilder.DropColumn(
                name: "acknowledged_by_operator_id",
                table: "alarms");
        }
    }
}
