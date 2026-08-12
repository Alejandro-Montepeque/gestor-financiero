using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorFinanciero.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptsCount = table.Column<int>(type: "integer", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationToken = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_registrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pending_registrations_Email",
                table: "pending_registrations",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pending_registrations_VerificationToken",
                table: "pending_registrations",
                column: "VerificationToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pending_registrations");
        }
    }
}
