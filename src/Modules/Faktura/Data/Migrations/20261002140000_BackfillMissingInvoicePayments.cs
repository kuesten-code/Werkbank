using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Faktura.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMissingInvoicePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Zwischen Einführung der Zahlungen (BackfillInvoicePayments) und der Umstellung von "Als beglichen markieren"
            // auf Zahlungsbuchung wurde nur der Status gesetzt. Diese Rechnungen erhalten die fehlende Zahlung über den Restbetrag.
            // Brutto wird wie in Invoice.TotalGross berechnet: Positionen kaufmännisch gerundet, Rabatt anteilig auf Netto und MwSt.
            // DiscountType: None=0, Percentage=1, Absolute=2
            migrationBuilder.Sql(@"
                WITH ItemSums AS (
                    SELECT
                        ii.""InvoiceId"",
                        SUM(ROUND(ii.""Quantity"" * ii.""UnitPrice"", 2)) AS ""TotalNet"",
                        SUM(ROUND(ROUND(ii.""Quantity"" * ii.""UnitPrice"", 2) * ii.""VatRate"" / 100, 2)) AS ""TotalVat""
                    FROM faktura.""InvoiceItems"" ii
                    WHERE ii.""IsHeader"" = false
                    GROUP BY ii.""InvoiceId""
                ),
                Totals AS (
                    SELECT
                        i.""Id"",
                        i.""PaidDate"",
                        its.""TotalNet"",
                        its.""TotalVat"",
                        CASE
                            WHEN i.""DiscountType"" = 1 AND i.""DiscountValue"" IS NOT NULL THEN its.""TotalNet"" * i.""DiscountValue"" / 100
                            WHEN i.""DiscountType"" = 2 AND i.""DiscountValue"" IS NOT NULL THEN i.""DiscountValue""
                            ELSE 0
                        END AS ""Discount""
                    FROM faktura.""Invoices"" i
                    JOIN ItemSums its ON its.""InvoiceId"" = i.""Id""
                    WHERE i.""Status"" = 2
                ),
                Gross AS (
                    SELECT
                        t.""Id"",
                        t.""PaidDate"",
                        CASE WHEN t.""TotalNet"" = 0 THEN 0
                             ELSE (t.""TotalNet"" - t.""Discount"") + t.""TotalVat"" * (t.""TotalNet"" - t.""Discount"") / t.""TotalNet""
                        END AS ""TotalGross""
                    FROM Totals t
                ),
                Remaining AS (
                    SELECT
                        g.""Id"",
                        g.""PaidDate"",
                        ROUND(g.""TotalGross"" - COALESCE((SELECT SUM(p.""Amount"") FROM faktura.""InvoicePayments"" p WHERE p.""InvoiceId"" = g.""Id""), 0), 2) AS ""Amount""
                    FROM Gross g
                )
                INSERT INTO faktura.""InvoicePayments"" (""InvoiceId"", ""Amount"", ""PaymentDate"", ""Notes"", ""CreatedAt"")
                SELECT r.""Id"", r.""Amount"", COALESCE(r.""PaidDate"", NOW()), 'Als beglichen markiert (nachgetragen)', NOW()
                FROM Remaining r
                WHERE r.""Amount"" <> 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM faktura.""InvoicePayments"" WHERE ""Notes"" = 'Als beglichen markiert (nachgetragen)';
            ");
        }
    }
}
