using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileTokenFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MobilePinFailedAttempts",
                schema: "host",
                table: "TeamMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MobilePinSet",
                schema: "host",
                table: "TeamMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MobileToken",
                schema: "host",
                table: "TeamMembers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MobileTokenLocked",
                schema: "host",
                table: "TeamMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PinHash",
                schema: "host",
                table: "TeamMembers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MobilePinFailedAttempts",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "MobilePinSet",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "MobileToken",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "MobileTokenLocked",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "PinHash",
                schema: "host",
                table: "TeamMembers");
        }
    }
}
