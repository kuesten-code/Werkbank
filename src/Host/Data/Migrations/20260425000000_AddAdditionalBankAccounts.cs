using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalBankAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS host.""AdditionalBankAccounts"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" integer NOT NULL,
                    ""BankName"" character varying(100) NOT NULL,
                    ""Iban"" character varying(50) NOT NULL,
                    ""Bic"" character varying(11),
                    ""AccountHolder"" character varying(200),
                    ""SortOrder"" integer NOT NULL DEFAULT 0,
                    CONSTRAINT ""FK_AdditionalBankAccounts_Companies_CompanyId""
                        FOREIGN KEY (""CompanyId"") REFERENCES host.""Companies""(""Id"") ON DELETE CASCADE
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS host.""AdditionalBankAccounts"";");
        }
    }
}
