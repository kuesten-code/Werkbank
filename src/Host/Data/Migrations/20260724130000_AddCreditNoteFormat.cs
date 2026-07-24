using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditNoteFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""NumberFormatSettings""
                    ADD COLUMN IF NOT EXISTS ""CreditNoteFormat"" character varying(50) NOT NULL DEFAULT 'GS-YYYY-XXXX';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""NumberFormatSettings"" DROP COLUMN IF EXISTS ""CreditNoteFormat"";
            ");
        }
    }
}
