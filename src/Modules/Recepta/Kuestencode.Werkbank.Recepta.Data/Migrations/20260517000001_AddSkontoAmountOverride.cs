using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Recepta.Data.Migrations
{
    public partial class AddSkontoAmountOverride : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE recepta.""Documents""
                ADD COLUMN IF NOT EXISTS ""SkontoAmountOverride"" decimal(18,2) NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE recepta.""Documents""
                DROP COLUMN IF EXISTS ""SkontoAmountOverride"";
            ");
        }
    }
}
