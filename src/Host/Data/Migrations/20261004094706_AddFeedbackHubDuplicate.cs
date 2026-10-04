using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackHubDuplicate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""FeedbackReports""
                    ADD COLUMN IF NOT EXISTS ""HubDuplicateOfId"" integer NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_FeedbackReports_FeedbackReports_HubDuplicateOfId') THEN
                        ALTER TABLE host.""FeedbackReports""
                            ADD CONSTRAINT ""FK_FeedbackReports_FeedbackReports_HubDuplicateOfId"" FOREIGN KEY (""HubDuplicateOfId"")
                            REFERENCES host.""FeedbackReports"" (""Id"") ON DELETE SET NULL;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS ""IX_FeedbackReports_HubDuplicateOfId"" ON host.""FeedbackReports"" (""HubDuplicateOfId"");

                -- Duplikat-Verweise über Kundengrenzen hinweg sind ab jetzt nur noch als interne
                -- Hub-Duplikate zulässig: bestehende Verweise entsprechend umhängen.
                UPDATE host.""FeedbackReports"" r
                SET ""HubDuplicateOfId"" = r.""DuplicateOfId"",
                    ""DuplicateOfId"" = NULL,
                    ""UpdatedAt"" = CURRENT_TIMESTAMP
                FROM host.""FeedbackReports"" original
                WHERE original.""Id"" = r.""DuplicateOfId""
                  AND original.""InstanceId"" <> r.""InstanceId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""FeedbackReports""
                    DROP CONSTRAINT IF EXISTS ""FK_FeedbackReports_FeedbackReports_HubDuplicateOfId"";
                DROP INDEX IF EXISTS host.""IX_FeedbackReports_HubDuplicateOfId"";
                ALTER TABLE host.""FeedbackReports""
                    DROP COLUMN IF EXISTS ""HubDuplicateOfId"";
            ");
        }
    }
}
