using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorFinanciero.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthEventsAndSeederTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "seeder_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seeder_executions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_events_Email_OccurredAt",
                table: "auth_events",
                columns: new[] { "Email", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_auth_events_EventType_Result",
                table: "auth_events",
                columns: new[] { "EventType", "Result" });

            migrationBuilder.CreateIndex(
                name: "IX_auth_events_OccurredAt",
                table: "auth_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_seeder_executions_Key",
                table: "seeder_executions",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_events");

            migrationBuilder.DropTable(
                name: "seeder_executions");
        }
    }
}
