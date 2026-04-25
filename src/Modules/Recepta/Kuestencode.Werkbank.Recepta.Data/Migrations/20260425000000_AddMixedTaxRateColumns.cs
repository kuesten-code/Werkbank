using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Recepta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMixedTaxRateColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE recepta.""Documents""
                ADD COLUMN IF NOT EXISTS ""AmountNet19"" numeric(18,2) NOT NULL DEFAULT 0;

                ALTER TABLE recepta.""Documents""
                ADD COLUMN IF NOT EXISTS ""AmountTax19"" numeric(18,2) NOT NULL DEFAULT 0;

                ALTER TABLE recepta.""Documents""
                ADD COLUMN IF NOT EXISTS ""AmountNet7"" numeric(18,2) NOT NULL DEFAULT 0;

                ALTER TABLE recepta.""Documents""
                ADD COLUMN IF NOT EXISTS ""AmountTax7"" numeric(18,2) NOT NULL DEFAULT 0;

                ALTER TABLE recepta.""Documents""
                ADD COLUMN IF NOT EXISTS ""AmountNet0"" numeric(18,2) NOT NULL DEFAULT 0;

                -- Bestehende Belege in die Steuerbuckets migrieren
                UPDATE recepta.""Documents""
                SET ""AmountNet19"" = ""AmountNet"",
                    ""AmountTax19"" = ""AmountTax""
                WHERE ""TaxRate"" = 19;

                UPDATE recepta.""Documents""
                SET ""AmountNet7"" = ""AmountNet"",
                    ""AmountTax7"" = ""AmountTax""
                WHERE ""TaxRate"" = 7;

                UPDATE recepta.""Documents""
                SET ""AmountNet0"" = ""AmountNet""
                WHERE ""TaxRate"" = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE recepta.""Documents"" DROP COLUMN IF EXISTS ""AmountNet19"";
                ALTER TABLE recepta.""Documents"" DROP COLUMN IF EXISTS ""AmountTax19"";
                ALTER TABLE recepta.""Documents"" DROP COLUMN IF EXISTS ""AmountNet7"";
                ALTER TABLE recepta.""Documents"" DROP COLUMN IF EXISTS ""AmountTax7"";
                ALTER TABLE recepta.""Documents"" DROP COLUMN IF EXISTS ""AmountNet0"";
            ");
        }
    }
}
