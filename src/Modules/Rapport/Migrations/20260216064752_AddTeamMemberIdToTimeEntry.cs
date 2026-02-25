using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Rapport.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMemberIdToTimeEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeamMemberId",
                schema: "rapport",
                table: "TimeEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamMemberName",
                schema: "rapport",
                table: "TimeEntries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TimeEntryAudits",
                schema: "rapport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeEntryId = table.Column<int>(type: "integer", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Changes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeEntryAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_TeamMemberId",
                schema: "rapport",
                table: "TimeEntries",
                column: "TeamMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntryAudits_TimeEntryId",
                schema: "rapport",
                table: "TimeEntryAudits",
                column: "TimeEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimeEntryAudits",
                schema: "rapport");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_TeamMemberId",
                schema: "rapport",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "TeamMemberId",
                schema: "rapport",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "TeamMemberName",
                schema: "rapport",
                table: "TimeEntries");
        }
    }
}
