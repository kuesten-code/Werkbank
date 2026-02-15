using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                schema: "host",
                table: "TeamMembers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpires",
                schema: "host",
                table: "TeamMembers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetToken",
                schema: "host",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpires",
                schema: "host",
                table: "TeamMembers");
        }
    }
}
