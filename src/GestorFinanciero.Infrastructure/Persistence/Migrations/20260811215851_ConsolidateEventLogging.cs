using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorFinanciero.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEventLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_events");

            migrationBuilder.CreateTable(
                name: "app_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Level = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    ExceptionType = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Extra = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_events_Level_OccurredAt",
                table: "app_events",
                columns: new[] { "Level", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_app_events_Module_OccurredAt",
                table: "app_events",
                columns: new[] { "Module", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_app_events_OccurredAt",
                table: "app_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_app_events_UserEmail_OccurredAt",
                table: "app_events",
                columns: new[] { "UserEmail", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_app_events_UserId_OccurredAt",
                table: "app_events",
                columns: new[] { "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_events");

            migrationBuilder.CreateTable(
                name: "auth_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_events", x => x.Id);
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
        }
    }
}
