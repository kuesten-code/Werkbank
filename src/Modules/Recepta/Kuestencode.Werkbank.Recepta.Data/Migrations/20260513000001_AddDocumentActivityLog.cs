using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Recepta.Data.Migrations
{
    public partial class AddDocumentActivityLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS recepta.""DocumentActivityLogs"" (
                    ""Id""             uuid                     NOT NULL DEFAULT gen_random_uuid(),
                    ""UserName""       character varying(200)   NOT NULL,
                    ""DocumentNumber"" character varying(50)    NOT NULL,
                    ""Action""         character varying(500)   NOT NULL,
                    ""CreatedAt""      timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_DocumentActivityLogs"" PRIMARY KEY (""Id"")
                );

                CREATE INDEX IF NOT EXISTS ""IX_DocumentActivityLogs_CreatedAt""
                    ON recepta.""DocumentActivityLogs"" (""CreatedAt"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS recepta.""DocumentActivityLogs"";
            ");
        }
    }
}
