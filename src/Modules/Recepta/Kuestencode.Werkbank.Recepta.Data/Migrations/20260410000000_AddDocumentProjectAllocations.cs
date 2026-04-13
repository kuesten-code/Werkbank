using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Recepta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentProjectAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Neue Tabelle für Projekt-Zuteilungen
            migrationBuilder.CreateTable(
                name: "DocumentProjectAllocations",
                schema: "recepta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocatedNet = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedTax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedGross = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentProjectAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentProjectAllocations_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "recepta",
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Unique-Index: pro Beleg kann ein Projekt nur einmal vorkommen
            migrationBuilder.CreateIndex(
                name: "IX_DocumentProjectAllocations_DocumentId_ProjectId",
                schema: "recepta",
                table: "DocumentProjectAllocations",
                columns: new[] { "DocumentId", "ProjectId" },
                unique: true);

            // Index für schnelle Projekt-seitige Abfragen
            migrationBuilder.CreateIndex(
                name: "IX_DocumentProjectAllocations_ProjectId",
                schema: "recepta",
                table: "DocumentProjectAllocations",
                column: "ProjectId");

            // Bestehende Einzel-Projekt-Zuteilungen migrieren:
            // Belege mit gesetztem ProjectId werden als 100%-Allokation übernommen
            migrationBuilder.Sql(@"
                INSERT INTO recepta.""DocumentProjectAllocations"" (""Id"", ""DocumentId"", ""ProjectId"", ""AllocatedNet"", ""AllocatedTax"", ""AllocatedGross"")
                SELECT gen_random_uuid(), ""Id"", ""ProjectId"", ""AmountNet"", ""AmountTax"", ""AmountGross""
                FROM recepta.""Documents""
                WHERE ""ProjectId"" IS NOT NULL;
            ");

            // ProjectId-Spalte aus Documents entfernen
            migrationBuilder.DropColumn(
                name: "ProjectId",
                schema: "recepta",
                table: "Documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ProjectId-Spalte wiederherstellen
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                schema: "recepta",
                table: "Documents",
                type: "uuid",
                nullable: true);

            // Erste Zuteilung pro Beleg als ProjectId zurückschreiben (Best-Effort)
            migrationBuilder.Sql(@"
                UPDATE recepta.""Documents"" d
                SET ""ProjectId"" = a.""ProjectId""
                FROM (
                    SELECT DISTINCT ON (""DocumentId"") ""DocumentId"", ""ProjectId""
                    FROM recepta.""DocumentProjectAllocations""
                    ORDER BY ""DocumentId""
                ) a
                WHERE d.""Id"" = a.""DocumentId"";
            ");

            migrationBuilder.DropTable(
                name: "DocumentProjectAllocations",
                schema: "recepta");
        }
    }
}
