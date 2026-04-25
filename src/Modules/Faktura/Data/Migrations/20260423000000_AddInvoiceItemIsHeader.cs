using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Faktura.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceItemIsHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE faktura.""InvoiceItems""
                ADD COLUMN IF NOT EXISTS ""IsHeader"" boolean NOT NULL DEFAULT false;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHeader",
                schema: "faktura",
                table: "InvoiceItems");
        }
    }
}
