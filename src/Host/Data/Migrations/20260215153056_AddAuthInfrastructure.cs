using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                schema: "host",
                table: "TeamMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "InviteAcceptedAt",
                schema: "host",
                table: "TeamMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteToken",
                schema: "host",
                table: "TeamMembers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InviteTokenExpires",
                schema: "host",
                table: "TeamMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLockedByAdmin",
                schema: "host",
                table: "TeamMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutUntil",
                schema: "host",
                table: "TeamMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                schema: "host",
                table: "TeamMembers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                schema: "host",
                table: "TeamMembers",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "WerkbankSettings",
                schema: "host",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: true),
                    AuthEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WerkbankSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WerkbankSettings",
                schema: "host");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "InviteAcceptedAt",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "InviteToken",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "InviteTokenExpires",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "IsLockedByAdmin",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "LockoutUntil",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "host",
                table: "TeamMembers");
        }
    }
}
