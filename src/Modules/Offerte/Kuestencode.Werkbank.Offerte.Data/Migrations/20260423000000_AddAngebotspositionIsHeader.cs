using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Offerte.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAngebotspositionIsHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE offerte.""Angebotspositionen""
                ADD COLUMN IF NOT EXISTS ""IsHeader"" boolean NOT NULL DEFAULT false;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHeader",
                schema: "offerte",
                table: "Angebotspositionen");
        }
    }
}
