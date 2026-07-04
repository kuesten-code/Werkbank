using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKostensatzToTeamMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""TeamMembers""
                ADD COLUMN IF NOT EXISTS ""Kostensatz"" numeric(10,2) NOT NULL DEFAULT 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""TeamMembers"" DROP COLUMN IF EXISTS ""Kostensatz"";
            ");
        }
    }
}
