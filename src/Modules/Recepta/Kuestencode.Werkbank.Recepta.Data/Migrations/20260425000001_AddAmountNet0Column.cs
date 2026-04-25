using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Recepta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAmountNet0Column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE recepta.""Documents""
                ADD COLUMN IF NOT EXISTS ""AmountNet0"" numeric(18,2) NOT NULL DEFAULT 0;

                UPDATE recepta.""Documents""
                SET ""AmountNet0"" = ""AmountNet""
                WHERE ""TaxRate"" = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE recepta.""Documents"" DROP COLUMN IF EXISTS ""AmountNet0"";
            ");
        }
    }
}
