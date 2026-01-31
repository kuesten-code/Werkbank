using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kuestencode.Werkbank.Offerte.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "offerte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmailLayout = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmailPrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    EmailAccentColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    EmailGreeting = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmailClosing = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfLayout = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PdfPrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    PdfAccentColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    PdfHeaderText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfFooterText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PdfValidityNotice = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings",
                schema: "offerte");
        }
    }
}
