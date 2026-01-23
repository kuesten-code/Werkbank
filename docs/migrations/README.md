# Datenbank-Migration zu Schema-basierter Architektur

## Überblick

Die Anwendung wurde von einer einzelnen Datenbank-Struktur im `public`-Schema zu einer modularen Schema-basierten Architektur migriert:

- **`host`-Schema**: Plattformweite Daten (Companies, Customers)
- **`faktura`-Schema**: Faktura-Modul-spezifische Daten (Invoices, InvoiceItems, DownPayments)

## Migration für neue Installationen

Für neue Installationen werden die Migrations automatisch beim ersten Start angewendet. Keine weiteren Schritte erforderlich.

## Migration für bestehende Installationen

Wenn Sie bereits eine Datenbank mit Daten im `public`-Schema haben, folgen Sie diesen Schritten:

### Schritt 1: Backup erstellen

```bash
pg_dump -U postgres -d faktura_db > backup_before_migration.sql
```

### Schritt 2: Migration vorbereiten

Führen Sie das Vorbereitungs-Script aus:

```bash
psql -U postgres -d faktura_db -f migrate-to-schemas.sql
```

Dieses Script:
- Erstellt Backup-Tabellen der bestehenden Daten
- Löscht die alten Tabellen im `public`-Schema
- Bereitet die Datenbank für die neuen Schemas vor

### Schritt 3: EF Core Migrations anwenden

Starten Sie die Anwendung. Die EF Core Migrations werden automatisch angewendet und erstellen:
- Schema `host` mit Tabellen `Companies` und `Customers`
- Schema `faktura` mit Tabellen `Invoices`, `InvoiceItems`, `DownPayments`

Alternativ können Sie die Migrations manuell anwenden:

```bash
# Host-Migrations
cd src/Host
dotnet ef database update

# Faktura-Migrations
cd src/Modules/Faktura
dotnet ef database update
```

### Schritt 4: Daten wiederherstellen

Führen Sie das Restore-Script aus:

```bash
psql -U postgres -d faktura_db -f restore-from-backup.sql
```

Dieses Script:
- Kopiert alle Daten aus den Backup-Tabellen in die neuen Schema-Strukturen
- Aktualisiert die Sequenzen (IDs)
- Behält die Backup-Tabellen zur Sicherheit bei

### Schritt 5: Verifizierung

Starten Sie die Anwendung und prüfen Sie:
- Alle Firmendaten sind vorhanden
- Alle Kunden sind vorhanden
- Alle Rechnungen mit Items und Anzahlungen sind vorhanden
- Funktionalität (Rechnungen erstellen, PDF generieren, E-Mails versenden) funktioniert

### Schritt 6: Aufräumen (optional)

Wenn alles funktioniert, können Sie die Backup-Tabellen entfernen:

```sql
DROP TABLE IF EXISTS public._backup_companies;
DROP TABLE IF EXISTS public._backup_customers;
DROP TABLE IF EXISTS public._backup_invoices;
DROP TABLE IF EXISTS public._backup_invoiceitems;
DROP TABLE IF EXISTS public._backup_downpayments;
```

## Rollback

Falls Probleme auftreten, können Sie zum vorherigen Stand zurückkehren:

```bash
# Datenbank komplett zurücksetzen
psql -U postgres -d postgres -c "DROP DATABASE faktura_db;"
psql -U postgres -d postgres -c "CREATE DATABASE faktura_db;"

# Backup wiederherstellen
psql -U postgres -d faktura_db < backup_before_migration.sql
```

## Technische Details

### Schema-Struktur

**host-Schema:**
- `Companies`: Firmenstammdaten mit SMTP, Email- und PDF-Einstellungen
- `Customers`: Kundendaten (plattformweit, von allen Modulen nutzbar)

**faktura-Schema:**
- `Invoices`: Rechnungen (referenziert `CustomerId` aus `host.Customers`)
- `InvoiceItems`: Rechnungspositionen
- `DownPayments`: Anzahlungen

### Cross-Schema-Beziehungen

`Invoices.CustomerId` ist ein einfacher INT-FK zu `host.Customers.Id`. Die Beziehung wird auf Anwendungsebene verwaltet (nicht via EF Core Navigation Properties), da EF Core keine Cross-Schema-Navigation Properties unterstützt.

### Migrations-Historie

Beide Schemas haben ihre eigene `__EFMigrationsHistory`-Tabelle:
- `host.__EFMigrationsHistory`
- `faktura.__EFMigrationsHistory`
