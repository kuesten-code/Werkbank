using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Recepta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS recepta.""DocumentPayments"" (
                    ""Id""          uuid            NOT NULL DEFAULT gen_random_uuid(),
                    ""DocumentId""  uuid            NOT NULL,
                    ""Amount""      decimal(18,2)   NOT NULL,
                    ""PaymentDate"" date            NOT NULL,
                    ""Notes""       text            NULL,
                    ""CreatedAt""   timestamptz     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_DocumentPayments"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_DocumentPayments_Documents"" FOREIGN KEY (""DocumentId"")
                        REFERENCES recepta.""Documents"" (""Id"") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_DocumentPayments_DocumentId""
                    ON recepta.""DocumentPayments"" (""DocumentId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS recepta.""DocumentPayments"";
            ");
        }
    }
}
