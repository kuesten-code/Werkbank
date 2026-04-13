using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Faktura.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceProjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                schema: "faktura",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ProjectId",
                schema: "faktura",
                table: "Invoices",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_ProjectId",
                schema: "faktura",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                schema: "faktura",
                table: "Invoices");
        }
    }
}
