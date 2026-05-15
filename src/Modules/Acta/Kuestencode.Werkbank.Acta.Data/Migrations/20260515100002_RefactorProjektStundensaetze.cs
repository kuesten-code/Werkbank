using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Acta.Data.Migrations
{
    public partial class RefactorProjektStundensaetze : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE acta.""ProjektStundensaetze""
                ADD COLUMN IF NOT EXISTS ""RolleId"" integer NOT NULL DEFAULT 1;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE acta.""ProjektStundensaetze""
                ADD COLUMN IF NOT EXISTS ""RolleName"" character varying(100) NOT NULL DEFAULT '';
            ");

            migrationBuilder.Sql(@"
                UPDATE acta.""ProjektStundensaetze""
                SET ""RolleId"" = CASE ""MitarbeiterTyp""
                    WHEN 'Azubi' THEN 2
                    ELSE 1
                END,
                ""RolleName"" = CASE ""MitarbeiterTyp""
                    WHEN 'Azubi' THEN 'Azubi'
                    ELSE 'Facharbeiter'
                END;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE acta.""ProjektStundensaetze""
                ALTER COLUMN ""RolleId"" DROP DEFAULT;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE acta.""ProjektStundensaetze""
                ALTER COLUMN ""RolleName"" DROP DEFAULT;
            ");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS acta.""IX_ProjektStundensaetze_ProjectId_MitarbeiterTyp"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE acta.""ProjektStundensaetze""
                DROP COLUMN IF EXISTS ""MitarbeiterTyp"";
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ProjektStundensaetze_ProjectId_RolleId""
                ON acta.""ProjektStundensaetze"" (""ProjectId"", ""RolleId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS acta.""IX_ProjektStundensaetze_ProjectId_RolleId"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE acta.""ProjektStundensaetze""
                ADD COLUMN IF NOT EXISTS ""MitarbeiterTyp"" character varying(20) NOT NULL DEFAULT 'Facharbeiter';
            ");

            migrationBuilder.Sql(@"
                UPDATE acta.""ProjektStundensaetze""
                SET ""MitarbeiterTyp"" = CASE ""RolleId""
                    WHEN 2 THEN 'Azubi'
                    ELSE 'Facharbeiter'
                END;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE acta.""ProjektStundensaetze""
                ALTER COLUMN ""MitarbeiterTyp"" DROP DEFAULT;
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ProjektStundensaetze_ProjectId_MitarbeiterTyp""
                ON acta.""ProjektStundensaetze"" (""ProjectId"", ""MitarbeiterTyp"");
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE acta.""ProjektStundensaetze""
                DROP COLUMN IF EXISTS ""RolleId"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE acta.""ProjektStundensaetze""
                DROP COLUMN IF EXISTS ""RolleName"";
            ");
        }
    }
}
