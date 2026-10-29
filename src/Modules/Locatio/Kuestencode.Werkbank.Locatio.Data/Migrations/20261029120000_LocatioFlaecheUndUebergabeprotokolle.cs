using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Locatio.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocatioFlaecheUndUebergabeprotokolle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE locatio.""Mieteinheiten""
                ADD COLUMN IF NOT EXISTS ""Flaeche"" numeric(10,2);

                ALTER TABLE locatio.""Gebaeude""
                ADD COLUMN IF NOT EXISTS ""AfaSatz"" numeric(5,2) NOT NULL DEFAULT 2;

                CREATE TABLE IF NOT EXISTS locatio.""Uebergabeprotokolle"" (
                    ""Id"" uuid NOT NULL,
                    ""MieteinheitId"" uuid NOT NULL,
                    ""Art"" character varying(20) NOT NULL,
                    ""Datum"" date NOT NULL,
                    ""Mieter"" character varying(200),
                    ""Notiz"" text,
                    ""DokumentId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_Uebergabeprotokolle"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Uebergabeprotokolle_Mieteinheiten_MieteinheitId"" FOREIGN KEY (""MieteinheitId"")
                        REFERENCES locatio.""Mieteinheiten"" (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_Uebergabeprotokolle_Dokumente_DokumentId"" FOREIGN KEY (""DokumentId"")
                        REFERENCES locatio.""Dokumente"" (""Id"") ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS ""IX_Uebergabeprotokolle_MieteinheitId"" ON locatio.""Uebergabeprotokolle"" (""MieteinheitId"");
                CREATE INDEX IF NOT EXISTS ""IX_Uebergabeprotokolle_DokumentId"" ON locatio.""Uebergabeprotokolle"" (""DokumentId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS locatio.""Uebergabeprotokolle"";
                ALTER TABLE locatio.""Gebaeude"" DROP COLUMN IF EXISTS ""AfaSatz"";
                ALTER TABLE locatio.""Mieteinheiten"" DROP COLUMN IF EXISTS ""Flaeche"";
            ");
        }
    }
}
