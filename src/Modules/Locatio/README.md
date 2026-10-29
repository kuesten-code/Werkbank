# Locatio – Vermietungsverwaltung

Locatio verwaltet vermietete Gebäude und Einheiten einer vermögensverwaltenden Gesellschaft (Einkünfte aus Vermietung und Verpachtung). Es läuft in einer eigenen Werkbank-Instanz zusammen mit **Saldo** und liefert diesem die steuerlich relevanten Buchungen.

**Port:** 8087 (standalone) · Schema: `locatio`

---

## Architektur

```
Kuestencode.Werkbank.Locatio.Domain   – Entities, Enums, DTOs, Rechenregeln (AfA, Buchungssperre)
Kuestencode.Werkbank.Locatio.Data     – DbContext, Migrations
Kuestencode.Werkbank.Locatio          – Controller, Services, Blazor-Seiten
Kuestencode.Werkbank.Locatio.Tests    – Unit Tests
```

Die Services arbeiten direkt mit `IDbContextFactory<LocatioDbContext>`; eigene Repositories gibt es nicht. Dateianhänge (Grundriss, Belege, NK-Abrechnungen, Übergabeprotokolle) liegen in der Datenbank und sind damit im Backup enthalten.

---

## Fachliche Regeln

| Thema | Regel |
|-------|-------|
| Fremdeigentum | Reine Markierung. Keine Einnahmen, Kosten oder AfA, keine Anschaffungsdaten. |
| Kostenposten | `Betrag` = tatsächlich getragener Anteil, nur dieser geht an Saldo. Gesamtbetrag und Verteilschlüssel sind reine Dokumentation, es gibt keine automatische Aufteilung. |
| Darlehen | Zins und Tilgung getrennt. Nur der Zins ist Werbungskosten; Tilgung geht nie an Saldo (auch nicht als Kostenposten der Kategorie „Tilgung“). |
| USt | Nur Gewerbe mit Option: Miete netto, USt getrennt. Ohne Option ist Brutto = Netto. Der Satz wird beim Erfassen eines Mieteingangs festgeschrieben. |
| AfA | Je Einheit (jede Wohnung ist ein eigenes Wirtschaftsgut – nötig für Einzelverkauf und unterschiedliche Nutzung), linear, im Anschaffungs- und Verkaufsjahr monatsgenau. Grundlage sind die Anschaffungsdaten der Einheit (Einzelerwerb); fehlen sie, wird das Gebäude nach Fläche auf alle eigenen Einheiten ohne eigene Anschaffungsdaten verteilt (fehlt einer davon die Fläche, keine AfA). Grund und Boden wird nicht abgeschrieben; Anschaffungsnebenkosten werden im Verhältnis Gebäude zu Boden aufgeteilt. Gebucht wird je Einheit oder für alle Einheiten eines Gebäudes als Kostenposten der AfA-Kategorie. |
| Übergabeprotokolle | Ablage je Einheit (Einzug/Auszug, Datum, Mieter, Notiz) mit Pflicht-Datei. Rein dokumentarisch, keine Buchung. |
| Veräußerung | Zweistufig (prüfen → bestätigen). Ab Verkaufsdatum sind Mieteingänge, Kosten, NK-Abrechnungen und Darlehenszinsen gesperrt. Laufende Mietverhältnisse enden am Vortag, mit Warnung. Gebäudeverkauf setzt alle eigenen Einheiten auf „veräußert“. Datensätze bleiben archiviert. |
| Veräußerungsgewinn | Nur Orientierungswert (Preis − Kosten − (Anschaffungskosten − angesetzte AfA)), Hinweis bei Haltedauer unter 10 Jahren, Export als PDF/CSV. Keine Buchung in Saldo. |

---

## Saldo-Anbindung

`GET /api/locatio/saldo-buchungen?from=YYYY-MM-DD&to=YYYY-MM-DD` liefert `LocatioBuchungDto` nach Zahlungsdatum:

| Quelle | Saldo |
|--------|-------|
| Mieteingang | Einnahme (netto, USt getrennt) |
| Kostenposten (außer Tilgung) | Ausgabe, getragener Anteil |
| Darlehensrate | Ausgabe, nur Zinsanteil |
| NK-Abrechnung | Nachzahlung = Einnahme, Erstattung = Ausgabe |
| AfA | Ausgabe (als Kostenposten der AfA-Kategorie) |

Das Zielkonto kommt aus der Kategorie (`/locatio/kategorien`). Die Startwerte sind SKR03-Vorschläge und sollten mit dem Steuerberater abgestimmt werden. In Saldo `ServiceUrls__Locatio` setzen und unter _Einstellungen_ die Ermittlungsart „Vermietung und Verpachtung“ wählen.

---

## Tests

```bash
dotnet test src/Modules/Locatio/Kuestencode.Werkbank.Locatio.Tests/
```
