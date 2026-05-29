using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Contracta.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE SCHEMA IF NOT EXISTS contracta;

                CREATE TABLE IF NOT EXISTS contracta.""Wartungsvertraege"" (
                    ""Id""                  uuid                        NOT NULL,
                    ""Vertragsnummer""      text                        NOT NULL,
                    ""Bezeichnung""         text                        NOT NULL,
                    ""KundeId""             integer                     NOT NULL,
                    ""Startdatum""          timestamp with time zone    NOT NULL,
                    ""Enddatum""            timestamp with time zone,
                    ""Intervall""           integer                     NOT NULL DEFAULT 0,
                    ""CustomIntervallTage"" integer,
                    ""LetzteAbrechnung""    timestamp with time zone,
                    ""NaechsteAbrechnung""  timestamp with time zone,
                    ""Status""              integer                     NOT NULL DEFAULT 0,
                    ""Notizen""             text,
                    ""ErstelltAm""          timestamp with time zone    NOT NULL,
                    ""GeaendertAm""         timestamp with time zone,
                    CONSTRAINT ""PK_Wartungsvertraege"" PRIMARY KEY (""Id"")
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Wartungsvertraege_Vertragsnummer""
                    ON contracta.""Wartungsvertraege"" (""Vertragsnummer"");

                CREATE INDEX IF NOT EXISTS ""IX_Wartungsvertraege_KundeId""
                    ON contracta.""Wartungsvertraege"" (""KundeId"");

                CREATE INDEX IF NOT EXISTS ""IX_Wartungsvertraege_Status""
                    ON contracta.""Wartungsvertraege"" (""Status"");

                CREATE INDEX IF NOT EXISTS ""IX_Wartungsvertraege_NaechsteAbrechnung""
                    ON contracta.""Wartungsvertraege"" (""NaechsteAbrechnung"");

                CREATE TABLE IF NOT EXISTS contracta.""Vertragspositionen"" (
                    ""Id""                  uuid            NOT NULL,
                    ""WartungsvertragId""   uuid            NOT NULL,
                    ""Position""            integer         NOT NULL,
                    ""Text""                text            NOT NULL,
                    ""Menge""               numeric(18,4)   NOT NULL,
                    ""Einzelpreis""         numeric(18,4)   NOT NULL,
                    ""Steuersatz""          numeric(5,2)    NOT NULL,
                    CONSTRAINT ""PK_Vertragspositionen"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Vertragspositionen_Wartungsvertraege_WartungsvertragId""
                        FOREIGN KEY (""WartungsvertragId"")
                        REFERENCES contracta.""Wartungsvertraege"" (""Id"")
                        ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_Vertragspositionen_WartungsvertragId""
                    ON contracta.""Vertragspositionen"" (""WartungsvertragId"");

                CREATE TABLE IF NOT EXISTS contracta.""Abrechnungshistorie"" (
                    ""Id""                  uuid                        NOT NULL,
                    ""WartungsvertragId""   uuid                        NOT NULL,
                    ""Abrechnungsdatum""    timestamp with time zone    NOT NULL,
                    ""RechnungId""          integer,
                    ""Rechnungsnummer""     text,
                    ""Betrag""              numeric(18,2)               NOT NULL,
                    CONSTRAINT ""PK_Abrechnungshistorie"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Abrechnungshistorie_Wartungsvertraege_WartungsvertragId""
                        FOREIGN KEY (""WartungsvertragId"")
                        REFERENCES contracta.""Wartungsvertraege"" (""Id"")
                        ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_Abrechnungshistorie_WartungsvertragId""
                    ON contracta.""Abrechnungshistorie"" (""WartungsvertragId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS contracta.""Abrechnungshistorie"";
                DROP TABLE IF EXISTS contracta.""Vertragspositionen"";
                DROP TABLE IF EXISTS contracta.""Wartungsvertraege"";
            ");
        }
    }
}
