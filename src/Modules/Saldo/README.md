# Saldo – EÜR-Modul

Das Saldo-Modul berechnet die **Einnahmen-Überschuss-Rechnung (EÜR)** nach dem **Zufluss-/Abflussprinzip** (§ 4 Abs. 3 EStG). Es aggregiert Daten aus den Modulen **Faktura** (Einnahmen) und **Recepta** (Ausgaben).

---

## Architektur

```
Kuestencode.Werkbank.Saldo.Domain    – Entities, Enums, DTOs
Kuestencode.Werkbank.Saldo.Data      – DbContext, Repositories, Migrations
Kuestencode.Werkbank.Saldo           – Controller, Services, Blazor-Seiten
Kuestencode.Werkbank.Saldo.Tests     – Unit Tests
```

**Port:** 8087 (standalone) · Schema: `saldo`

---

## Zufluss-/Abflussprinzip

> Maßgeblich ist das **Zahlungsdatum** (`PaidDate`), nicht das Rechnungs-/Belegdatum.

- Eine Rechnung vom Dezember 2025, die im Januar 2026 bezahlt wird, zählt im **EÜR 2026**.
- Faktura-Rechnungen werden nach `PaidDate` gefiltert (`GET /api/invoice?status=Paid&paidFrom=&paidTo=`).
- Recepta-Belege werden nach `PaidDate` gefiltert (`GET /api/recepta/documents?status=Paid&paidFrom=&paidTo=`).

---

## Navigation

| Seite | Pfad | Rolle |
|-------|------|-------|
| Dashboard | `/saldo` | Admin, Büro |
| EÜR | `/saldo/euer` | Admin, Büro |
| Buchungen | `/saldo/buchungen` | Admin, Büro |
| USt-Übersicht | `/saldo/ust` | Admin, Büro |
| Export-Historie | `/saldo/export/historie` | Admin, Büro |
| Einstellungen | `/saldo/einstellungen` | Admin |
| Kategorie-Mapping | `/saldo/einstellungen/mapping` | Admin |

---

## Kontenrahmen

Unterstützt werden **SKR03** und **SKR04**. Der Kontenrahmen wird unter _Einstellungen_ konfiguriert.

### SKR03-Standardkonten

| Typ | USt-Satz | Konto |
|-----|----------|-------|
| Einnahmen 19 % | 19 % | 8400 |
| Einnahmen 7 % | 7 % | 8300 |
| Einnahmen 0 % | 0 % | 8120 |
| Bank | – | 1200 |

### SKR04-Standardkonten

| Typ | USt-Satz | Konto |
|-----|----------|-------|
| Einnahmen 19 % | 19 % | 4400 |
| Einnahmen 7 % | 7 % | 4300 |
| Einnahmen 0 % | 0 % | 4120 |
| Bank | – | 1800 |

Ausgaben-Konten werden über das **Kategorie-Mapping** (Recepta-Kategorien → DATEV-Konto) gesteuert. Unter _Einstellungen → Kategorie-Mapping_ können Standardmappings per Konto überschrieben werden.

---

## DATEV-Export

### Buchungsstapel (EXTF)

Der Export folgt der **DATEV EXTF-Spezifikation v700/21**:

- Encoding: **Windows-1252**
- Dezimaltrennzeichen: **Komma** (`,`)
- Feldtrenner: **Semikolon** (`;`)
- Datumformat Belegdatum: **TTMMJJJJ** (z. B. `15032026`)

**BU-Schlüssel (Steuerkennzeichen):**

| Typ | USt-Satz | BU-Schlüssel |
|-----|----------|--------------|
| Einnahme | 19 % | 3 |
| Einnahme | 7 % | 2 |
| Einnahme | 0 % | 0 |
| Ausgabe | 19 % | 9 |
| Ausgabe | 7 % | 8 |
| Ausgabe | 0 % | 0 |

### DATEV-Import

1. Datei `DATEV_Buchungsstapel_*.csv` öffnen.
2. In DATEV Kanzlei-Rechnungswesen: _Bearbeiten → Buchungen importieren → DATEV-Format (EXTF)_.
3. Datei auswählen, Mandant und Wirtschaftsjahr prüfen.
4. Import starten.

> **Hinweis:** DATEV erwartet Windows-1252-Encoding. Öffne die Datei nicht mit einem Editor, der sie in UTF-8 konvertiert, bevor du sie importierst.

### Belege-ZIP

Der ZIP-Export enthält:
- `Rechnungen/` – PDFs der bezahlten Faktura-Rechnungen
- `Belege/` – Dateien der bezahlten Recepta-Belege

Dateinamen-Schema: `{PaidDate}_{Typ}_{Nummer}.{ext}` (z. B. `2026-03-15_RE_RE-2026-001.pdf`)

---

## PDF-Report (EÜR)

Der Bericht wird mit **QuestPDF** generiert und enthält:

1. **Deckblatt** – Firma, Zeitraum, Erstellungsdatum
2. **Zusammenfassung** – Einnahmen/Ausgaben/Gewinn + USt-Übersicht
3. **Aufstellung Einnahmen** – Tabelle gruppiert nach Erlöskonto
4. **Aufstellung Ausgaben** – Tabelle gruppiert nach Kategorie/Konto
5. **Ausgaben nach Kategorie** – Summensicht mit Beleganzahl

Download: Button _"Als PDF exportieren"_ auf der EÜR-Seite (`/saldo/euer`).

---

## Einstellungen

| Feld | Beschreibung |
|------|--------------|
| Kontenrahmen | SKR03 oder SKR04 |
| Berater-Nr. | DATEV-Beraternummer (für EXTF-Header) |
| Mandanten-Nr. | DATEV-Mandantennummer (für EXTF-Header) |
| Wirtschaftsjahr-Beginn | Monat des Geschäftsjahresbeginns (Standard: 1 = Januar) |

> Beim Wechsel des Kontenrahmens werden alle bestehenden Kategorie-Overrides für den neuen Kontenrahmen neu erstellt. Bestehende Overrides des alten Rahmens bleiben erhalten.

---

## Tests

```bash
dotnet test src/Modules/Saldo/Kuestencode.Werkbank.Saldo.Tests/
```

Abgedeckte Bereiche:

| Test-Klasse | Abgedeckte Szenarien |
|-------------|---------------------|
| `KontoMappingServiceTests` | Fallback-Konten, Override-Vorrang, Standard-Mapping, Update/Reset |
| `SaldoAggregationServiceTests` | Saldo-Berechnung, Sortierung, USt-Gruppierung, Leerzeitraum |
| `DatevExportServiceTests` | Windows-1252-Encoding, BU-Schlüssel, Belegdatum-Format, Sonderzeichen, Dezimaltrennzeichen |
| `EuerServiceTests` | Zufluss-/Abflussprinzip, Kontenzuordnung, Fallback-Konto, Fehlerbehandlung |

---

## Test-Szenario (manuell)

1. **Faktura:** 3 Rechnungen anlegen; 2 davon auf „Bezahlt" setzen (mit Datum).
2. **Recepta:** 5 Belege anlegen (verschiedene Kategorien); 4 davon auf „Bezahlt" setzen.
3. **Saldo → Dashboard:** Einnahmen = Summe der 2 bezahlten Rechnungen (Netto), Ausgaben = Summe der 4 bezahlten Belege (Netto).
4. **Export → Buchungsstapel:** CSV öffnen → 6 Zeilen (2 + 4), BU-Schlüssel prüfen.
5. **EÜR → Als PDF exportieren:** Deckblatt, Zusammenfassung, Tabellen prüfen.

### Edge Cases

| Szenario | Verhalten |
|----------|-----------|
| Rechnung Dez. 2025, bezahlt Jan. 2026 | Zählt im EÜR 2026 (Zahlungsdatum maßgeblich) |
| Beleg ohne Kategorie-Mapping | Konto 4900 (SKR03) / 6300 (SKR04) |
| USt 0 % | BU-Schlüssel = 0 im DATEV-Export |
| Leerer Zeitraum | Leere Tabellen, Saldo = 0,00 € |
| Faktura/Recepta nicht erreichbar | Graceful degradation, Fehlermeldung im UI |
