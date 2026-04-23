using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Faktura.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceIsReverseCharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE faktura.""Invoices""
                ADD COLUMN IF NOT EXISTS ""IsReverseCharge"" boolean NOT NULL DEFAULT false;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReverseCharge",
                schema: "faktura",
                table: "Invoices");
        }
    }
}
