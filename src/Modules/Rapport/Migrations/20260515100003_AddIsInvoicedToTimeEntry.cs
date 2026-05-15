using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Rapport.Migrations
{
    public partial class AddIsInvoicedToTimeEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                ADD COLUMN IF NOT EXISTS ""IsInvoiced"" boolean NOT NULL DEFAULT false;

                ALTER TABLE rapport.""TimeEntries""
                ADD COLUMN IF NOT EXISTS ""InvoicedAt"" timestamp with time zone NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries"" DROP COLUMN IF EXISTS ""InvoicedAt"";
                ALTER TABLE rapport.""TimeEntries"" DROP COLUMN IF EXISTS ""IsInvoiced"";
            ");
        }
    }
}
