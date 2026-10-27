using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Saldo.Data.Migrations
{
    /// <inheritdoc />
    public partial class SaldoAddErmittlungsart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE saldo.""SaldoSettings""
                ADD COLUMN IF NOT EXISTS ""Ermittlungsart"" character varying(30) NOT NULL DEFAULT 'Euer';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE saldo.""SaldoSettings""
                DROP COLUMN IF EXISTS ""Ermittlungsart"";
            ");
        }
    }
}
