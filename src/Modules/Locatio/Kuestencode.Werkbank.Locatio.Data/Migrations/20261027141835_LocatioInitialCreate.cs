using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuestencode.Werkbank.Locatio.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocatioInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE SCHEMA IF NOT EXISTS locatio;

                CREATE TABLE IF NOT EXISTS locatio.""Dokumente"" (
                    ""Id"" uuid NOT NULL,
                    ""Dateiname"" character varying(255) NOT NULL,
                    ""ContentType"" character varying(100) NOT NULL,
                    ""Groesse"" bigint NOT NULL,
                    ""Inhalt"" bytea NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_Dokumente"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS locatio.""Gebaeude"" (
                    ""Id"" uuid NOT NULL,
                    ""Name"" character varying(200) NOT NULL,
                    ""Strasse"" character varying(200),
                    ""Plz"" character varying(10),
                    ""Ort"" character varying(100),
                    ""AbrechnerUrl"" character varying(500),
                    ""Notizen"" text,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""AnschaffungsDatum"" date,
                    ""AnschaffungskostenGebaeude"" numeric(18,2),
                    ""AnschaffungskostenGrundUndBoden"" numeric(18,2),
                    ""Anschaffungsnebenkosten"" numeric(18,2),
                    ""VerkaufsDatum"" date,
                    ""Veraeusserungspreis"" numeric(18,2),
                    ""Veraeusserungskosten"" numeric(18,2),
                    CONSTRAINT ""PK_Gebaeude"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS locatio.""Kostenkategorien"" (
                    ""Id"" uuid NOT NULL,
                    ""Name"" character varying(100) NOT NULL,
                    ""Typ"" character varying(30) NOT NULL,
                    ""KontoNummer"" character varying(10),
                    ""Sortierung"" integer NOT NULL,
                    ""IstSystem"" boolean NOT NULL,
                    CONSTRAINT ""PK_Kostenkategorien"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS locatio.""Darlehen"" (
                    ""Id"" uuid NOT NULL,
                    ""GebaeudeId"" uuid NOT NULL,
                    ""Bezeichnung"" character varying(200) NOT NULL,
                    ""Kreditgeber"" character varying(200),
                    ""Notiz"" text,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_Darlehen"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Darlehen_Gebaeude_GebaeudeId"" FOREIGN KEY (""GebaeudeId"") REFERENCES locatio.""Gebaeude"" (""Id"") ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS locatio.""Mieteinheiten"" (
                    ""Id"" uuid NOT NULL,
                    ""GebaeudeId"" uuid NOT NULL,
                    ""Bezeichnung"" character varying(100) NOT NULL,
                    ""Art"" character varying(20) NOT NULL,
                    ""Status"" character varying(20) NOT NULL,
                    ""Kaltmiete"" numeric(18,2) NOT NULL,
                    ""NebenkostenVorauszahlung"" numeric(18,2) NOT NULL,
                    ""Miteigentumsanteil"" character varying(50),
                    ""Mieter"" character varying(200),
                    ""VertragsBeginn"" date,
                    ""VertragsEnde"" date,
                    ""UstOption"" boolean NOT NULL,
                    ""UstSatz"" numeric(5,2) NOT NULL,
                    ""AfaSatz"" numeric(5,2) NOT NULL,
                    ""GrundrissId"" uuid,
                    ""Notizen"" text,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""AnschaffungsDatum"" date,
                    ""AnschaffungskostenGebaeude"" numeric(18,2),
                    ""AnschaffungskostenGrundUndBoden"" numeric(18,2),
                    ""Anschaffungsnebenkosten"" numeric(18,2),
                    ""VerkaufsDatum"" date,
                    ""Veraeusserungspreis"" numeric(18,2),
                    ""Veraeusserungskosten"" numeric(18,2),
                    CONSTRAINT ""PK_Mieteinheiten"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Mieteinheiten_Dokumente_GrundrissId"" FOREIGN KEY (""GrundrissId"") REFERENCES locatio.""Dokumente"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_Mieteinheiten_Gebaeude_GebaeudeId"" FOREIGN KEY (""GebaeudeId"") REFERENCES locatio.""Gebaeude"" (""Id"") ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS locatio.""Darlehenszahlungen"" (
                    ""Id"" uuid NOT NULL,
                    ""DarlehenId"" uuid NOT NULL,
                    ""Zahlungsdatum"" date NOT NULL,
                    ""Zinsanteil"" numeric(18,2) NOT NULL,
                    ""Tilgungsanteil"" numeric(18,2) NOT NULL,
                    ""Notiz"" text,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_Darlehenszahlungen"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Darlehenszahlungen_Darlehen_DarlehenId"" FOREIGN KEY (""DarlehenId"") REFERENCES locatio.""Darlehen"" (""Id"") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS locatio.""Kostenposten"" (
                    ""Id"" uuid NOT NULL,
                    ""GebaeudeId"" uuid,
                    ""MieteinheitId"" uuid,
                    ""KategorieId"" uuid NOT NULL,
                    ""Betrag"" numeric(18,2) NOT NULL,
                    ""Gesamtbetrag"" numeric(18,2),
                    ""Verteilschluessel"" character varying(200),
                    ""Zahlungsdatum"" date NOT NULL,
                    ""Beschreibung"" character varying(300) NOT NULL,
                    ""Notiz"" text,
                    ""BelegId"" uuid,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_Kostenposten"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""CK_Kostenposten_Zuordnung"" CHECK (""GebaeudeId"" IS NOT NULL OR ""MieteinheitId"" IS NOT NULL),
                    CONSTRAINT ""FK_Kostenposten_Dokumente_BelegId"" FOREIGN KEY (""BelegId"") REFERENCES locatio.""Dokumente"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_Kostenposten_Gebaeude_GebaeudeId"" FOREIGN KEY (""GebaeudeId"") REFERENCES locatio.""Gebaeude"" (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_Kostenposten_Kostenkategorien_KategorieId"" FOREIGN KEY (""KategorieId"") REFERENCES locatio.""Kostenkategorien"" (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_Kostenposten_Mieteinheiten_MieteinheitId"" FOREIGN KEY (""MieteinheitId"") REFERENCES locatio.""Mieteinheiten"" (""Id"") ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS locatio.""Mieteingaenge"" (
                    ""Id"" uuid NOT NULL,
                    ""MieteinheitId"" uuid NOT NULL,
                    ""Zahlungsdatum"" date NOT NULL,
                    ""Monat"" date NOT NULL,
                    ""Kaltmiete"" numeric(18,2) NOT NULL,
                    ""Nebenkosten"" numeric(18,2) NOT NULL,
                    ""UstSatz"" numeric(5,2) NOT NULL,
                    ""Notiz"" text,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_Mieteingaenge"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Mieteingaenge_Mieteinheiten_MieteinheitId"" FOREIGN KEY (""MieteinheitId"") REFERENCES locatio.""Mieteinheiten"" (""Id"") ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS locatio.""Nebenkostenabrechnungen"" (
                    ""Id"" uuid NOT NULL,
                    ""MieteinheitId"" uuid NOT NULL,
                    ""Jahr"" integer NOT NULL,
                    ""Ergebnis"" numeric(18,2) NOT NULL,
                    ""Zahlungsdatum"" date NOT NULL,
                    ""DokumentId"" uuid,
                    ""Notiz"" text,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_Nebenkostenabrechnungen"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Nebenkostenabrechnungen_Dokumente_DokumentId"" FOREIGN KEY (""DokumentId"") REFERENCES locatio.""Dokumente"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_Nebenkostenabrechnungen_Mieteinheiten_MieteinheitId"" FOREIGN KEY (""MieteinheitId"") REFERENCES locatio.""Mieteinheiten"" (""Id"") ON DELETE RESTRICT
                );
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Darlehen_GebaeudeId"" ON locatio.""Darlehen"" (""GebaeudeId"");
                CREATE INDEX IF NOT EXISTS ""IX_Darlehenszahlungen_DarlehenId"" ON locatio.""Darlehenszahlungen"" (""DarlehenId"");
                CREATE INDEX IF NOT EXISTS ""IX_Darlehenszahlungen_Zahlungsdatum"" ON locatio.""Darlehenszahlungen"" (""Zahlungsdatum"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Kostenkategorien_Name"" ON locatio.""Kostenkategorien"" (""Name"");
                CREATE INDEX IF NOT EXISTS ""IX_Kostenposten_BelegId"" ON locatio.""Kostenposten"" (""BelegId"");
                CREATE INDEX IF NOT EXISTS ""IX_Kostenposten_GebaeudeId"" ON locatio.""Kostenposten"" (""GebaeudeId"");
                CREATE INDEX IF NOT EXISTS ""IX_Kostenposten_KategorieId"" ON locatio.""Kostenposten"" (""KategorieId"");
                CREATE INDEX IF NOT EXISTS ""IX_Kostenposten_MieteinheitId"" ON locatio.""Kostenposten"" (""MieteinheitId"");
                CREATE INDEX IF NOT EXISTS ""IX_Kostenposten_Zahlungsdatum"" ON locatio.""Kostenposten"" (""Zahlungsdatum"");
                CREATE INDEX IF NOT EXISTS ""IX_Mieteingaenge_MieteinheitId"" ON locatio.""Mieteingaenge"" (""MieteinheitId"");
                CREATE INDEX IF NOT EXISTS ""IX_Mieteingaenge_Zahlungsdatum"" ON locatio.""Mieteingaenge"" (""Zahlungsdatum"");
                CREATE INDEX IF NOT EXISTS ""IX_Mieteinheiten_GebaeudeId"" ON locatio.""Mieteinheiten"" (""GebaeudeId"");
                CREATE INDEX IF NOT EXISTS ""IX_Mieteinheiten_GrundrissId"" ON locatio.""Mieteinheiten"" (""GrundrissId"");
                CREATE INDEX IF NOT EXISTS ""IX_Nebenkostenabrechnungen_DokumentId"" ON locatio.""Nebenkostenabrechnungen"" (""DokumentId"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Nebenkostenabrechnungen_MieteinheitId_Jahr"" ON locatio.""Nebenkostenabrechnungen"" (""MieteinheitId"", ""Jahr"");
            ");

            // Startwerte; Kontonummern sind SKR03-Vorschläge und in den Locatio-Kategorien änderbar.
            migrationBuilder.Sql(@"
                INSERT INTO locatio.""Kostenkategorien"" (""Id"", ""Name"", ""Typ"", ""KontoNummer"", ""Sortierung"", ""IstSystem"") VALUES
                    ('4c0a7100-0000-4000-8000-000000000001', 'Mieteinnahmen',                        'Mieteinnahme',          '2750', 1,  true),
                    ('4c0a7100-0000-4000-8000-000000000002', 'Nebenkostenabrechnung',                'Nebenkostenabrechnung', '2750', 2,  true),
                    ('4c0a7100-0000-4000-8000-000000000003', 'Nebenkosten (Gas, Wasser, Strom allg.)', 'Werbungskosten',      '2350', 10, false),
                    ('4c0a7100-0000-4000-8000-000000000004', 'Darlehenszinsen',                      'Darlehenszinsen',       '2120', 20, true),
                    ('4c0a7100-0000-4000-8000-000000000005', 'Tilgung',                              'Tilgung',               NULL,   21, true),
                    ('4c0a7100-0000-4000-8000-000000000006', 'Hausgeld',                             'Werbungskosten',        '2350', 30, false),
                    ('4c0a7100-0000-4000-8000-000000000007', 'Sonstiges',                            'Werbungskosten',        '2350', 40, false),
                    ('4c0a7100-0000-4000-8000-000000000008', 'Abschreibung (AfA)',                   'Afa',                   '4830', 50, true)
                ON CONFLICT (""Id"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS locatio.""Nebenkostenabrechnungen"";
                DROP TABLE IF EXISTS locatio.""Mieteingaenge"";
                DROP TABLE IF EXISTS locatio.""Kostenposten"";
                DROP TABLE IF EXISTS locatio.""Darlehenszahlungen"";
                DROP TABLE IF EXISTS locatio.""Mieteinheiten"";
                DROP TABLE IF EXISTS locatio.""Darlehen"";
                DROP TABLE IF EXISTS locatio.""Kostenkategorien"";
                DROP TABLE IF EXISTS locatio.""Gebaeude"";
                DROP TABLE IF EXISTS locatio.""Dokumente"";
            ");
        }
    }
}
