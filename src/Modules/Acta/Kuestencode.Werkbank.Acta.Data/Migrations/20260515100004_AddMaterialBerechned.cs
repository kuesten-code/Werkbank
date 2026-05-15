using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Acta.Data.Migrations
{
    public partial class AddMaterialBerechned : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE acta.""Projects""
                ADD COLUMN IF NOT EXISTS ""MaterialBerechnedNetto"" decimal(18,2) NOT NULL DEFAULT 0;

                ALTER TABLE acta.""Projects""
                ADD COLUMN IF NOT EXISTS ""MaterialBerechnedBrutto"" decimal(18,2) NOT NULL DEFAULT 0;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE acta.""Projects"" DROP COLUMN IF EXISTS ""MaterialBerechnedBrutto"";
                ALTER TABLE acta.""Projects"" DROP COLUMN IF EXISTS ""MaterialBerechnedNetto"";
            ");
        }
    }
}
