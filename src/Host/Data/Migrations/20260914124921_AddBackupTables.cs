using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS host.""BackupSettings"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Schedule"" character varying(50) NOT NULL DEFAULT '0 3 * * *',
                    ""Enabled"" boolean NOT NULL DEFAULT false,
                    ""EncryptionEnabled"" boolean NOT NULL DEFAULT false,
                    ""EncryptionPassword"" character varying(500) NULL,
                    ""KeepDaily"" integer NOT NULL DEFAULT 7,
                    ""KeepWeekly"" integer NOT NULL DEFAULT 4,
                    ""KeepMonthly"" integer NOT NULL DEFAULT 12,
                    ""KeepYearly"" integer NOT NULL DEFAULT 10,
                    ""AlertOnFailure"" boolean NOT NULL DEFAULT true,
                    ""AlertEmail"" character varying(200) NULL,
                    ""AlertWebhookUrl"" character varying(500) NULL,
                    ""WarnAfterDays"" integer NOT NULL DEFAULT 3,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                INSERT INTO host.""BackupSettings"" (""Id"")
                SELECT 1
                WHERE NOT EXISTS (SELECT 1 FROM host.""BackupSettings"");

                SELECT setval(pg_get_serial_sequence('host.""BackupSettings""', 'Id'), GREATEST((SELECT MAX(""Id"") FROM host.""BackupSettings""), 1));

                CREATE TABLE IF NOT EXISTS host.""BackupTargets"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""BackupSettingsId"" integer NOT NULL,
                    ""Name"" character varying(100) NOT NULL,
                    ""Type"" integer NOT NULL,
                    ""Enabled"" boolean NOT NULL DEFAULT true,
                    ""Path"" character varying(500) NULL,
                    ""Host"" character varying(200) NULL,
                    ""Port"" integer NULL,
                    ""Username"" character varying(100) NULL,
                    ""Password"" character varying(500) NULL,
                    ""PrivateKey"" text NULL,
                    ""AccessKey"" character varying(200) NULL,
                    ""SecretKey"" character varying(500) NULL,
                    ""Region"" character varying(50) NULL,
                    ""LastBackupAt"" timestamp with time zone NULL,
                    ""LastBackupSuccess"" boolean NULL,
                    ""LastBackupError"" character varying(1000) NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""FK_BackupTargets_BackupSettings_BackupSettingsId"" FOREIGN KEY (""BackupSettingsId"")
                        REFERENCES host.""BackupSettings"" (""Id"") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_BackupTargets_BackupSettingsId"" ON host.""BackupTargets"" (""BackupSettingsId"");

                CREATE TABLE IF NOT EXISTS host.""BackupHistory"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""BackupTargetId"" integer NOT NULL,
                    ""StartedAt"" timestamp with time zone NOT NULL,
                    ""CompletedAt"" timestamp with time zone NULL,
                    ""Status"" integer NOT NULL,
                    ""ErrorMessage"" character varying(2000) NULL,
                    ""FileName"" character varying(200) NOT NULL,
                    ""FileSizeBytes"" bigint NULL,
                    ""Type"" integer NOT NULL,
                    ""IsManual"" boolean NOT NULL DEFAULT false,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""FK_BackupHistory_BackupTargets_BackupTargetId"" FOREIGN KEY (""BackupTargetId"")
                        REFERENCES host.""BackupTargets"" (""Id"") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_BackupHistory_BackupTargetId"" ON host.""BackupHistory"" (""BackupTargetId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS host.""BackupHistory"";
                DROP TABLE IF EXISTS host.""BackupTargets"";
                DROP TABLE IF EXISTS host.""BackupSettings"";
            ");
        }
    }
}
