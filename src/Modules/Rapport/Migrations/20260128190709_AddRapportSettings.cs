using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kuestencode.Rapport.Migrations
{
    /// <inheritdoc />
    public partial class AddRapportSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "rapport",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DefaultHourlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ShowHourlyRateInPdf = table.Column<bool>(type: "boolean", nullable: false),
                    CalculateTotalAmount = table.Column<bool>(type: "boolean", nullable: false),
                    RoundingMinutes = table.Column<int>(type: "integer", nullable: false),
                    StartOfWeek = table.Column<int>(type: "integer", nullable: false),
                    DefaultProjectId = table.Column<int>(type: "integer", nullable: true),
                    AutoStopTimerAfterHours = table.Column<int>(type: "integer", nullable: true),
                    EnableSounds = table.Column<bool>(type: "boolean", nullable: false),
                    PdfLayout = table.Column<int>(type: "integer", nullable: false),
                    PdfPrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PdfAccentColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PdfHeaderText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfFooterText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
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
                schema: "rapport");
        }
    }
}
