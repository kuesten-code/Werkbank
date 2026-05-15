using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Rapport.Migrations
{
    public partial class RefactorMitarbeiterRolleOnTimeEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                ADD COLUMN IF NOT EXISTS ""MitarbeiterRolleId"" integer NULL;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                ADD COLUMN IF NOT EXISTS ""MitarbeiterRolleName"" character varying(100) NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE rapport.""TimeEntries""
                SET ""MitarbeiterRolleId"" = CASE ""MitarbeiterTyp""
                    WHEN 'Azubi' THEN 2
                    ELSE 1
                END,
                ""MitarbeiterRolleName"" = CASE ""MitarbeiterTyp""
                    WHEN 'Azubi' THEN 'Azubi'
                    ELSE 'Facharbeiter'
                END
                WHERE ""MitarbeiterTyp"" IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                DROP COLUMN IF EXISTS ""MitarbeiterTyp"";
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                ADD COLUMN IF NOT EXISTS ""MitarbeiterTyp"" character varying(20) NOT NULL DEFAULT 'Facharbeiter';
            ");

            migrationBuilder.Sql(@"
                UPDATE rapport.""TimeEntries""
                SET ""MitarbeiterTyp"" = CASE ""MitarbeiterRolleId""
                    WHEN 2 THEN 'Azubi'
                    ELSE 'Facharbeiter'
                END;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                ALTER COLUMN ""MitarbeiterTyp"" DROP DEFAULT;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                DROP COLUMN IF EXISTS ""MitarbeiterRolleId"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                DROP COLUMN IF EXISTS ""MitarbeiterRolleName"";
            ");
        }
    }
}
