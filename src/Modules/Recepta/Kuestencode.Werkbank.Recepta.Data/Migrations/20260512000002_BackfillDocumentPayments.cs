using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Recepta.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillDocumentPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert one payment record for each fully-paid document that has no payment rows yet.
            migrationBuilder.Sql(@"
                INSERT INTO recepta.""DocumentPayments"" (""Id"", ""DocumentId"", ""Amount"", ""PaymentDate"", ""CreatedAt"")
                SELECT
                    gen_random_uuid(),
                    d.""Id"",
                    d.""AmountGross"",
                    d.""PaidDate"",
                    NOW()
                FROM recepta.""Documents"" d
                WHERE d.""Status"" = 'Paid'
                  AND d.""PaidDate"" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM recepta.""DocumentPayments"" p WHERE p.""DocumentId"" = d.""Id""
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM recepta.""DocumentPayments"" p
                WHERE EXISTS (
                    SELECT 1 FROM recepta.""Documents"" d
                    WHERE d.""Id"" = p.""DocumentId""
                      AND d.""Status"" = 'Paid'
                )
                AND (SELECT COUNT(*) FROM recepta.""DocumentPayments"" p2 WHERE p2.""DocumentId"" = p.""DocumentId"") = 1;
            ");
        }
    }
}
