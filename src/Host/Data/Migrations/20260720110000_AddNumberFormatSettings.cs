using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNumberFormatSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS host.""NumberFormatSettings"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""InvoiceFormat"" character varying(50) NOT NULL DEFAULT 'YYYY-XXXX',
                    ""QuoteFormat"" character varying(50) NOT NULL DEFAULT 'ANG-YYYY-XXXXX',
                    ""ProjectFormat"" character varying(50) NOT NULL DEFAULT 'P-YYYY-XXXX',
                    ""IncomingInvoiceFormat"" character varying(50) NOT NULL DEFAULT 'ER-YYYY-XXXX'
                );

                INSERT INTO host.""NumberFormatSettings"" (""InvoiceFormat"", ""QuoteFormat"", ""ProjectFormat"", ""IncomingInvoiceFormat"")
                SELECT COALESCE(NULLIF(""InvoiceNumberFormat"", ''), 'YYYY-XXXX'), 'ANG-YYYY-XXXXX', 'P-YYYY-XXXX', 'ER-YYYY-XXXX'
                FROM host.""Companies""
                LIMIT 1;

                INSERT INTO host.""NumberFormatSettings"" (""InvoiceFormat"", ""QuoteFormat"", ""ProjectFormat"", ""IncomingInvoiceFormat"")
                SELECT 'YYYY-XXXX', 'ANG-YYYY-XXXXX', 'P-YYYY-XXXX', 'ER-YYYY-XXXX'
                WHERE NOT EXISTS (SELECT 1 FROM host.""NumberFormatSettings"");

                ALTER TABLE host.""Companies"" DROP COLUMN IF EXISTS ""InvoiceNumberFormat"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""Companies"" ADD COLUMN IF NOT EXISTS ""InvoiceNumberFormat"" character varying(50);

                UPDATE host.""Companies""
                SET ""InvoiceNumberFormat"" = (SELECT ""InvoiceFormat"" FROM host.""NumberFormatSettings"" LIMIT 1);

                DROP TABLE IF EXISTS host.""NumberFormatSettings"";
            ");
        }
    }
}
