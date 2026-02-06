using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Acta.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProjectCustomerIdToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // uuid -> int is not safely castable in PostgreSQL. For dev/test we drop and re-create the column.
            migrationBuilder.DropColumn(
                name: "CustomerId",
                schema: "acta",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                schema: "acta",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerId",
                schema: "acta",
                table: "Projects");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                schema: "acta",
                table: "Projects",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);
        }
    }
}
