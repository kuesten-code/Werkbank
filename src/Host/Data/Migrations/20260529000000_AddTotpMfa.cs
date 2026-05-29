using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    public partial class AddTotpMfa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""TeamMembers""
                    ADD COLUMN IF NOT EXISTS ""TotpSecret"" TEXT,
                    ADD COLUMN IF NOT EXISTS ""MfaEnabled"" BOOLEAN NOT NULL DEFAULT FALSE,
                    ADD COLUMN IF NOT EXISTS ""MfaRecoveryCodes"" TEXT;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""TeamMembers""
                    DROP COLUMN IF EXISTS ""TotpSecret"",
                    DROP COLUMN IF EXISTS ""MfaEnabled"",
                    DROP COLUMN IF EXISTS ""MfaRecoveryCodes"";
            ");
        }
    }
}
