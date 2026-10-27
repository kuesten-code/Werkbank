using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Faktura.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceSkonto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE faktura.""Invoices"" ADD COLUMN IF NOT EXISTS ""SkontoType"" integer NOT NULL DEFAULT 0;
                ALTER TABLE faktura.""Invoices"" ADD COLUMN IF NOT EXISTS ""SkontoValue"" numeric(18,6) NULL;
                ALTER TABLE faktura.""Invoices"" ADD COLUMN IF NOT EXISTS ""SkontoDays"" integer NULL;
                ALTER TABLE faktura.""Invoices"" ADD COLUMN IF NOT EXISTS ""SkontoApplied"" boolean NOT NULL DEFAULT false;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE faktura.""Invoices"" DROP COLUMN IF EXISTS ""SkontoType"";
                ALTER TABLE faktura.""Invoices"" DROP COLUMN IF EXISTS ""SkontoValue"";
                ALTER TABLE faktura.""Invoices"" DROP COLUMN IF EXISTS ""SkontoDays"";
                ALTER TABLE faktura.""Invoices"" DROP COLUMN IF EXISTS ""SkontoApplied"";
            ");
        }
    }
}
