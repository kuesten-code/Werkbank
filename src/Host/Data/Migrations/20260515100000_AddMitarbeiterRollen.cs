using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    public partial class AddMitarbeiterRollen : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE host.""MitarbeiterRollen"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""SortOrder"" INT NOT NULL DEFAULT 0
                );

                INSERT INTO host.""MitarbeiterRollen"" (""Id"", ""Name"", ""SortOrder"") VALUES (1, 'Facharbeiter', 0), (2, 'Azubi', 1);
                SELECT setval(pg_get_serial_sequence('host.""MitarbeiterRollen""', 'Id'), 2);

                ALTER TABLE host.""TeamMembers"" ADD COLUMN ""MitarbeiterRolleId"" INT;

                UPDATE host.""TeamMembers"" SET ""MitarbeiterRolleId"" = CASE ""MitarbeiterTyp"" WHEN 0 THEN 1 WHEN 1 THEN 2 ELSE 1 END;

                ALTER TABLE host.""TeamMembers"" DROP COLUMN ""MitarbeiterTyp"";

                ALTER TABLE host.""TeamMembers"" ADD CONSTRAINT ""FK_TeamMembers_MitarbeiterRollen""
                    FOREIGN KEY (""MitarbeiterRolleId"") REFERENCES host.""MitarbeiterRollen"" (""Id"") ON DELETE SET NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""TeamMembers"" DROP CONSTRAINT IF EXISTS ""FK_TeamMembers_MitarbeiterRollen"";
                ALTER TABLE host.""TeamMembers"" ADD COLUMN ""MitarbeiterTyp"" INT NOT NULL DEFAULT 0;
                UPDATE host.""TeamMembers"" SET ""MitarbeiterTyp"" = CASE ""MitarbeiterRolleId"" WHEN 1 THEN 0 WHEN 2 THEN 1 ELSE 0 END;
                ALTER TABLE host.""TeamMembers"" DROP COLUMN ""MitarbeiterRolleId"";
                DROP TABLE IF EXISTS host.""MitarbeiterRollen"";
            ");
        }
    }
}
