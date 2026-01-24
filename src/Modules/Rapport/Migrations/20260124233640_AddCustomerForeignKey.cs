using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Rapport.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Customers_CustomerId",
                schema: "rapport",
                table: "TimeEntries",
                column: "CustomerId",
                principalSchema: "host",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Customers_CustomerId",
                schema: "rapport",
                table: "TimeEntries");
        }
    }
}
