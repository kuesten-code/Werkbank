using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Faktura.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE faktura.""InvoicePayments"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""InvoiceId"" integer NOT NULL,
                    ""Amount"" numeric(18,2) NOT NULL,
                    ""PaymentDate"" timestamp with time zone NOT NULL,
                    ""Notes"" text,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""FK_InvoicePayments_Invoices_InvoiceId"" FOREIGN KEY (""InvoiceId"")
                        REFERENCES faktura.""Invoices"" (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX ""IX_InvoicePayments_InvoiceId"" ON faktura.""InvoicePayments""(""InvoiceId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoicePayments",
                schema: "faktura");
        }
    }
}
