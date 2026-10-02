using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Faktura.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDownPaymentNetAmountAndVatRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bestandsdaten: effektiver Steuersatz der Abschlagsrechnung (falls verknüpft) bzw. der Rechnung selbst,
            // identisch zur bisherigen anteiligen Aufteilung im PDF. Rabatte wirken auf Netto und MwSt gleich und kürzen sich heraus.
            migrationBuilder.Sql(@"
                ALTER TABLE faktura.""DownPayments"" ADD COLUMN IF NOT EXISTS ""NetAmount"" numeric(18,2) NOT NULL DEFAULT 0;
                ALTER TABLE faktura.""DownPayments"" ADD COLUMN IF NOT EXISTS ""VatRate"" numeric(5,2) NOT NULL DEFAULT 0;

                UPDATE faktura.""DownPayments"" dp
                SET ""VatRate"" = COALESCE((
                    SELECT ROUND(100 * SUM(ROUND(ii.""Quantity"" * ii.""UnitPrice"", 2) * ii.""VatRate"" / 100)
                                 / NULLIF(SUM(ROUND(ii.""Quantity"" * ii.""UnitPrice"", 2)), 0), 2)
                    FROM faktura.""InvoiceItems"" ii
                    WHERE ii.""InvoiceId"" = COALESCE(dp.""SourceInvoiceId"", dp.""InvoiceId"")
                      AND ii.""IsHeader"" = false
                ), 0);

                UPDATE faktura.""DownPayments""
                SET ""NetAmount"" = ROUND(""Amount"" / (1 + ""VatRate"" / 100), 2);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE faktura.""DownPayments"" DROP COLUMN IF EXISTS ""VatRate"";
                ALTER TABLE faktura.""DownPayments"" DROP COLUMN IF EXISTS ""NetAmount"";
            ");
        }
    }
}
