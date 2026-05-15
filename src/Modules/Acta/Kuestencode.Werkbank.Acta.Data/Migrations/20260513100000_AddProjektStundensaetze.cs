using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Acta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjektStundensaetze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjektStundensaetze",
                schema: "acta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MitarbeiterTyp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Stundensatz = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjektStundensaetze", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjektStundensaetze_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "acta",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjektStundensaetze_ProjectId_MitarbeiterTyp",
                schema: "acta",
                table: "ProjektStundensaetze",
                columns: new[] { "ProjectId", "MitarbeiterTyp" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjektStundensaetze",
                schema: "acta");
        }
    }
}
