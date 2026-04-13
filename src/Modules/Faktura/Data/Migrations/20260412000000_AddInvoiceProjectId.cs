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
            migrationBuilder.Sql(@"
                ALTER TABLE faktura.""Invoices""
                ADD COLUMN IF NOT EXISTS ""ProjectId"" integer NULL;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Invoices_ProjectId""
                ON faktura.""Invoices"" (""ProjectId"");
            ");
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
