using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Acta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPinnedProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS acta.""PinnedProjects"" (
                    ""Id"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""ProjectId"" uuid NOT NULL,
                    ""PinnedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_PinnedProjects"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_PinnedProjects_Projects_ProjectId""
                        FOREIGN KEY (""ProjectId"") REFERENCES acta.""Projects"" (""Id"") ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PinnedProjects_UserId_ProjectId""
                    ON acta.""PinnedProjects"" (""UserId"", ""ProjectId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS acta.""PinnedProjects"";");
        }
    }
}
