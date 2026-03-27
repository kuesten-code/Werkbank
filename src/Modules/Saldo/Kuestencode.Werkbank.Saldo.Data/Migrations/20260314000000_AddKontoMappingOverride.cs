using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Saldo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKontoMappingOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KontoMappingOverrides",
                schema: "saldo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kontenrahmen = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Kategorie = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    KontoNummer = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KontoMappingOverrides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KontoMappingOverrides_Kontenrahmen_Kategorie",
                schema: "saldo",
                table: "KontoMappingOverrides",
                columns: new[] { "Kontenrahmen", "Kategorie" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KontoMappingOverrides",
                schema: "saldo");
        }
    }
}
