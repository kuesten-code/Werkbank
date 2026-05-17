using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Acta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjektBerechneteAufwaende : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS acta.""ProjektBerechneteAufwaende"" (
                    ""Id"" uuid NOT NULL,
                    ""ProjectId"" uuid NOT NULL,
                    ""Belegnummer"" character varying(100) NOT NULL,
                    ""Lieferant"" character varying(200) NOT NULL,
                    ""Netto"" numeric(18,2) NOT NULL,
                    ""Brutto"" numeric(18,2) NOT NULL,
                    ""BerechnedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_ProjektBerechneteAufwaende"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_ProjektBerechneteAufwaende_Projects_ProjectId""
                        FOREIGN KEY (""ProjectId"") REFERENCES acta.""Projects"" (""Id"") ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ProjektBerechneteAufwaende_ProjectId_Belegnummer""
                    ON acta.""ProjektBerechneteAufwaende"" (""ProjectId"", ""Belegnummer"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS acta.""ProjektBerechneteAufwaende"";");
        }
    }
}
