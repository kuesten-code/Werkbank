using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Recepta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSkontoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE recepta.""Documents""
                ADD COLUMN IF NOT EXISTS ""SkontoPercent"" decimal(5,2) NULL,
                ADD COLUMN IF NOT EXISTS ""SkontoDays""   integer       NULL,
                ADD COLUMN IF NOT EXISTS ""SkontoApplied"" boolean      NOT NULL DEFAULT false;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE recepta.""Documents""
                DROP COLUMN IF EXISTS ""SkontoPercent"",
                DROP COLUMN IF EXISTS ""SkontoDays"",
                DROP COLUMN IF EXISTS ""SkontoApplied"";
            ");
        }
    }
}
