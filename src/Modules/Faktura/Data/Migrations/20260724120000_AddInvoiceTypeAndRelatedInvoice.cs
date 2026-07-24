using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Faktura.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceTypeAndRelatedInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE faktura.""Invoices"" ADD COLUMN IF NOT EXISTS ""Type"" integer NOT NULL DEFAULT 0;
                ALTER TABLE faktura.""Invoices"" ADD COLUMN IF NOT EXISTS ""RelatedInvoiceId"" integer NULL;
                ALTER TABLE faktura.""Invoices"" ADD CONSTRAINT ""FK_Invoices_Invoices_RelatedInvoiceId""
                    FOREIGN KEY (""RelatedInvoiceId"") REFERENCES faktura.""Invoices"" (""Id"") ON DELETE SET NULL;
                CREATE INDEX IF NOT EXISTS ""IX_Invoices_RelatedInvoiceId"" ON faktura.""Invoices""(""RelatedInvoiceId"");
                CREATE INDEX IF NOT EXISTS ""IX_Invoices_Type"" ON faktura.""Invoices""(""Type"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS faktura.""IX_Invoices_Type"";
                DROP INDEX IF EXISTS faktura.""IX_Invoices_RelatedInvoiceId"";
                ALTER TABLE faktura.""Invoices"" DROP CONSTRAINT IF EXISTS ""FK_Invoices_Invoices_RelatedInvoiceId"";
                ALTER TABLE faktura.""Invoices"" DROP COLUMN IF EXISTS ""RelatedInvoiceId"";
                ALTER TABLE faktura.""Invoices"" DROP COLUMN IF EXISTS ""Type"";
            ");
        }
    }
}
