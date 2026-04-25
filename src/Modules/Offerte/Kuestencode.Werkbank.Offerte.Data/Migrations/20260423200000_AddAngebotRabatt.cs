using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Offerte.Data.Migrations
{
    public partial class AddAngebotRabatt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE offerte.""Angebote""
                ADD COLUMN IF NOT EXISTS ""RabattTyp"" integer NOT NULL DEFAULT 0;

                ALTER TABLE offerte.""Angebote""
                ADD COLUMN IF NOT EXISTS ""RabattWert"" decimal(18,2) NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RabattTyp",
                schema: "offerte",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "RabattWert",
                schema: "offerte",
                table: "Angebote");
        }
    }
}
