using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Rapport.Migrations
{
    /// <inheritdoc />
    public partial class AddMitarbeiterTypToTimeEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Spalte mit NOT NULL DEFAULT hinzufügen – befüllt alle Altdaten sofort mit 'Facharbeiter'
            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                ADD COLUMN ""MitarbeiterTyp"" character varying(20) NOT NULL DEFAULT 'Facharbeiter';
            ");

            // DB-Default wieder entfernen – die Anwendung liefert den Wert immer selbst
            migrationBuilder.Sql(@"
                ALTER TABLE rapport.""TimeEntries""
                ALTER COLUMN ""MitarbeiterTyp"" DROP DEFAULT;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MitarbeiterTyp",
                schema: "rapport",
                table: "TimeEntries");
        }
    }
}
