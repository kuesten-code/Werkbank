using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameInvoiceNumberPrefixToFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""Companies"" RENAME COLUMN ""InvoiceNumberPrefix"" TO ""InvoiceNumberFormat"";
                ALTER TABLE host.""Companies"" ALTER COLUMN ""InvoiceNumberFormat"" TYPE character varying(50);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""Companies"" ALTER COLUMN ""InvoiceNumberFormat"" TYPE character varying(10);
                ALTER TABLE host.""Companies"" RENAME COLUMN ""InvoiceNumberFormat"" TO ""InvoiceNumberPrefix"";
            ");
        }
    }
}
