using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Rapport.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerProjectIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_CustomerId_ProjectId",
                schema: "rapport",
                table: "TimeEntries",
                columns: new[] { "CustomerId", "ProjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_CustomerId_ProjectId",
                schema: "rapport",
                table: "TimeEntries");
        }
    }
}
