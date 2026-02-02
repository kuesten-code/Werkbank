using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Offerte.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "offerte");

            migrationBuilder.CreateTable(
                name: "Angebote",
                schema: "offerte",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Angebotsnummer = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KundeId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Erstelldatum = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GueltigBis = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Referenz = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bemerkungen = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Einleitung = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Schlusstext = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    VersendetAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AngenommenAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AbgelehntAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AbgelaufenAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailGesendetAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailGesendetAn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmailAnzahl = table.Column<int>(type: "integer", nullable: false),
                    GedrucktAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DruckAnzahl = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Angebote", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Angebotspositionen",
                schema: "offerte",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AngebotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Menge = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Einzelpreis = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Steuersatz = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Rabatt = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Angebotspositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Angebotspositionen_Angebote_AngebotId",
                        column: x => x.AngebotId,
                        principalSchema: "offerte",
                        principalTable: "Angebote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Angebote_Angebotsnummer",
                schema: "offerte",
                table: "Angebote",
                column: "Angebotsnummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Angebotspositionen_AngebotId",
                schema: "offerte",
                table: "Angebotspositionen",
                column: "AngebotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Angebotspositionen",
                schema: "offerte");

            migrationBuilder.DropTable(
                name: "Angebote",
                schema: "offerte");
        }
    }
}
