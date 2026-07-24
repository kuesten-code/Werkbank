using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditNotePdfTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""Companies""
                    ADD COLUMN IF NOT EXISTS ""PdfCreditNoteHeaderText"" character varying(500) NULL,
                    ADD COLUMN IF NOT EXISTS ""PdfCreditNoteFooterText"" character varying(1000) NULL,
                    ADD COLUMN IF NOT EXISTS ""PdfCreditNotePaymentNotice"" character varying(500) NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE host.""Companies""
                    DROP COLUMN IF EXISTS ""PdfCreditNoteHeaderText"",
                    DROP COLUMN IF EXISTS ""PdfCreditNoteFooterText"",
                    DROP COLUMN IF EXISTS ""PdfCreditNotePaymentNotice"";
            ");
        }
    }
}
