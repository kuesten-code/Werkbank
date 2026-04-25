using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Offerte.Data.Migrations
{
    public partial class AddAngebotspositionEinheit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE offerte.""Angebotspositionen""
                ADD COLUMN IF NOT EXISTS ""Einheit"" character varying(20) NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Einheit",
                schema: "offerte",
                table: "Angebotspositionen");
        }
    }
}
