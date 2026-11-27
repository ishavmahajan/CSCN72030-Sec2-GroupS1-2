using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace kitchenOS_Scada.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_types",
                columns: table => new
                {
                    device_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_types", x => x.device_type_id);
                });

            migrationBuilder.CreateTable(
                name: "event_types",
                columns: table => new
                {
                    event_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_types", x => x.event_type_id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    device_type_id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.device_id);
                    table.ForeignKey(
                        name: "FK_devices_device_types_device_type_id",
                        column: x => x.device_type_id,
                        principalTable: "device_types",
                        principalColumn: "device_type_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alarms",
                columns: table => new
                {
                    alarm_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alarms", x => x.alarm_id);
                    table.ForeignKey(
                        name: "FK_alarms_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "device_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_alarms_event_types_event_type_id",
                        column: x => x.event_type_id,
                        principalTable: "event_types",
                        principalColumn: "event_type_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    event_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type_id = table.Column<int>(type: "integer", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_events_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "device_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_events_event_types_event_type_id",
                        column: x => x.event_type_id,
                        principalTable: "event_types",
                        principalColumn: "event_type_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alarms_device_id_occurred_at_utc",
                table: "alarms",
                columns: new[] { "device_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_alarms_event_type_id",
                table: "alarms",
                column: "event_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_types_name",
                table: "device_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devices_device_name",
                table: "devices",
                column: "device_name");

            migrationBuilder.CreateIndex(
                name: "IX_devices_device_type_id_is_active",
                table: "devices",
                columns: new[] { "device_type_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_event_types_name",
                table: "event_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_events_device_id_occurred_at_utc",
                table: "events",
                columns: new[] { "device_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_events_event_type_id",
                table: "events",
                column: "event_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alarms");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "event_types");

            migrationBuilder.DropTable(
                name: "device_types");
        }
    }
}
