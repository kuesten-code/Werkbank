using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupTargetSftpHostKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""BackupTargets""
                    ADD COLUMN IF NOT EXISTS ""SftpHostKeyFingerprint"" text NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""BackupTargets""
                    DROP COLUMN IF EXISTS ""SftpHostKeyFingerprint"";
            ");
        }
    }
}
