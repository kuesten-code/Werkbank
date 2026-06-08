using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Rapport.Migrations
{
    public partial class AddBreakMinutesToTimeEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                ADD COLUMN IF NOT EXISTS ""BreakMinutes"" integer NOT NULL DEFAULT 0;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries"" DROP COLUMN IF EXISTS ""BreakMinutes"";
            ");
        }
    }
}
