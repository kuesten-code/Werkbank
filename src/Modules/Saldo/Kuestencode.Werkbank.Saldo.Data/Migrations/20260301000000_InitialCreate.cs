using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Saldo.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "saldo");

            migrationBuilder.CreateTable(
                name: "ExportLogs",
                schema: "saldo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportTyp = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ZeitraumVon = table.Column<DateOnly>(type: "date", nullable: false),
                    ZeitraumBis = table.Column<DateOnly>(type: "date", nullable: false),
                    AnzahlBuchungen = table.Column<int>(type: "integer", nullable: false),
                    DateiName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DateiGroesse = table.Column<long>(type: "bigint", nullable: false),
                    ExportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExportedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Konten",
                schema: "saldo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kontenrahmen = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    KontoNummer = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    KontoBezeichnung = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KontoTyp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UstSatz = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Konten", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaldoSettings",
                schema: "saldo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kontenrahmen = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BeraterNummer = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MandantenNummer = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WirtschaftsjahrBeginn = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaldoSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KategorieKontoMappings",
                schema: "saldo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kontenrahmen = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ReceiptaKategorie = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    KontoNummer = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsCustom = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KategorieKontoMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KategorieKontoMappings_Konten_Kontenrahmen_KontoNummer",
                        columns: x => new { x.Kontenrahmen, x.KontoNummer },
                        principalSchema: "saldo",
                        principalTable: "Konten",
                        principalColumns: new[] { "Kontenrahmen", "KontoNummer" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KategorieKontoMappings_Kontenrahmen_KontoNummer",
                schema: "saldo",
                table: "KategorieKontoMappings",
                columns: new[] { "Kontenrahmen", "KontoNummer" });

            migrationBuilder.CreateIndex(
                name: "IX_KategorieKontoMappings_Kontenrahmen_ReceiptaKategorie",
                schema: "saldo",
                table: "KategorieKontoMappings",
                columns: new[] { "Kontenrahmen", "ReceiptaKategorie" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Konten_Kontenrahmen_KontoNummer",
                schema: "saldo",
                table: "Konten",
                columns: new[] { "Kontenrahmen", "KontoNummer" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KategorieKontoMappings",
                schema: "saldo");

            migrationBuilder.DropTable(
                name: "ExportLogs",
                schema: "saldo");

            migrationBuilder.DropTable(
                name: "Konten",
                schema: "saldo");

            migrationBuilder.DropTable(
                name: "SaldoSettings",
                schema: "saldo");
        }
    }
}
