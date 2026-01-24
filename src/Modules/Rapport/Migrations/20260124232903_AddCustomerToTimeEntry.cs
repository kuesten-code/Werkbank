using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Rapport.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerToTimeEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                schema: "rapport",
                table: "TimeEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                schema: "rapport",
                table: "TimeEntries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_CustomerId",
                schema: "rapport",
                table: "TimeEntries",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_CustomerId",
                schema: "rapport",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                schema: "rapport",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                schema: "rapport",
                table: "TimeEntries");
        }
    }
}
