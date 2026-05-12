using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Faktura.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillInvoicePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert one payment record for each fully-paid invoice that has no payment rows yet.
            // TotalGross is computed from items with discount handling:
            //   DiscountType: None=0, Percentage=1, Absolute=2
            migrationBuilder.Sql(@"
                WITH ItemSums AS (
                    SELECT
                        ii.""InvoiceId"",
                        SUM(ii.""Quantity"" * ii.""UnitPrice"")                           AS ""TotalNet"",
                        SUM(ii.""Quantity"" * ii.""UnitPrice"" * ii.""VatRate"" / 100.0) AS ""TotalVat""
                    FROM faktura.""InvoiceItems"" ii
                    GROUP BY ii.""InvoiceId""
                )
                INSERT INTO faktura.""InvoicePayments"" (""InvoiceId"", ""Amount"", ""PaymentDate"", ""CreatedAt"")
                SELECT
                    i.""Id"",
                    GREATEST(
                        CASE
                            WHEN i.""DiscountType"" = 1 AND i.""DiscountValue"" IS NOT NULL THEN
                                (its.""TotalNet"" + its.""TotalVat"") * (1 - i.""DiscountValue"" / 100.0)
                            WHEN i.""DiscountType"" = 2 AND i.""DiscountValue"" IS NOT NULL AND its.""TotalNet"" > 0 THEN
                                (its.""TotalNet"" - i.""DiscountValue"")
                                + its.""TotalVat"" * ((its.""TotalNet"" - i.""DiscountValue"") / its.""TotalNet"")
                            ELSE
                                its.""TotalNet"" + its.""TotalVat""
                        END,
                        0
                    ),
                    i.""PaidDate"",
                    NOW()
                FROM faktura.""Invoices"" i
                LEFT JOIN ItemSums its ON its.""InvoiceId"" = i.""Id""
                WHERE i.""Status"" = 2
                  AND i.""PaidDate"" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM faktura.""InvoicePayments"" p WHERE p.""InvoiceId"" = i.""Id""
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove backfilled payments (those created by this migration).
            // Safe because invoices with multiple payments would have been added manually.
            migrationBuilder.Sql(@"
                DELETE FROM faktura.""InvoicePayments"" p
                WHERE EXISTS (
                    SELECT 1 FROM faktura.""Invoices"" i
                    WHERE i.""Id"" = p.""InvoiceId""
                      AND i.""Status"" = 2
                )
                AND (SELECT COUNT(*) FROM faktura.""InvoicePayments"" p2 WHERE p2.""InvoiceId"" = p.""InvoiceId"") = 1;
            ");
        }
    }
}
