using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Faktura.Data.Migrations
{
    public partial class AddInvoiceItemUnit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE faktura.""InvoiceItems""
                ADD COLUMN IF NOT EXISTS ""Unit"" character varying(20) NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unit",
                schema: "faktura",
                table: "InvoiceItems");
        }
    }
}
