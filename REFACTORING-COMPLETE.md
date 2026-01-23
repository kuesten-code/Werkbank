# Refactoring zu modularer Architektur - Abgeschlossen ✅

## Datum
2026-01-22

## Übersicht

Die Migration von "Küstencode Faktura" zur modularen Plattform "Küstencode Werkbank" wurde erfolgreich abgeschlossen.

## Durchgeführte Phasen

### ✅ Phase 1: Core erweitern (Models, Interfaces)
- Company Model in Core verschoben und erweitert
- Customer Model in Core verschoben (ohne Navigation Properties)
- Interfaces für alle Services definiert (ICompanyService, ICustomerService, IEmailEngine, IPdfEngine)

### ✅ Phase 2: Host-Projekt erstellen
- Neues Host-Projekt `Kuestencode.Werkbank.Host` erstellt
- HostDbContext mit Companies und Customers im Schema "host"
- Host-Services implementiert (CompanyService, CustomerService)
- Email- und PDF-Engines generalisiert und nach Host verschoben
- DI-Registrierung über `AddHostServices()` Extension-Methode

### ✅ Phase 3: Faktura refactoren
- FakturaDbContext nur noch mit Invoices, InvoiceItems, DownPayments im Schema "faktura"
- Invoice Model ohne Customer Navigation Property (Customer-Daten werden über ICustomerService geladen)
- FakturaModule konsumiert Host-Services über DI
- Alte Service-Implementierungen entfernt (Company/Customer)

### ✅ Phase 4: Datenbank-Migration vorbereiten
- EF Core Migrations für beide Projekte erstellt:
  - Host: `20260122154519_InitialCreate` (host.Companies, host.Customers)
  - Faktura: `20260122154554_InitialCreate` (faktura.Invoices, faktura.InvoiceItems, faktura.DownPayments)
- FakturaDbContextFactory für Design-Time Migrations
- SQL-Scripts für Datenmigration erstellt:
  - `migrate-to-schemas.sql` - Backup und Vorbereitung
  - `restore-from-backup.sql` - Daten wiederherstellen
- Umfassende Migrations-Dokumentation in `docs/migrations/README.md`

### ✅ Phase 5: Pages verschieben
- Customer-Verwaltung von Faktura → Host verschoben:
  - List.razor, Create.razor, Edit.razor
- CompanySettings.razor von Faktura → Host verschoben
- Using-Statements aktualisiert (Kuestencode.Core.Models/Interfaces)
- Page-Titel von "Küstencode Faktura" → "Küstencode Werkbank"
- NavMenu in Faktura bereinigt

### ✅ Phase 6: Integration & Build testen
- Faktura-Modul in Host geladen via `AddFakturaModule()`
- Zirkuläre Abhängigkeit aufgelöst (Faktura → Host Referenz entfernt)
- App.razor mit AdditionalAssemblies für Faktura-Routing konfiguriert
- NavMenu in Host um Faktura-Links erweitert
- Migrations für beide DbContexts in Program.cs eingebunden
- **Full Build erfolgreich: 0 Fehler, 17 Warnungen (alle pre-existing)**

## Neue Architektur

```
src/
├── Core/                           # Shared Models & Interfaces
│   ├── Models/
│   │   ├── Company.cs
│   │   ├── Customer.cs
│   │   └── Invoice.cs
│   └── Interfaces/
│       ├── ICompanyService.cs
│       ├── ICustomerService.cs
│       ├── IEmailEngine.cs
│       └── IPdfEngine.cs
│
├── Host/                           # Kuestencode.Werkbank.Host (Entry Point)
│   ├── Data/
│   │   ├── HostDbContext.cs       # Schema: "host"
│   │   └── Migrations/
│   ├── Services/
│   │   ├── CompanyService.cs
│   │   ├── CustomerService.cs
│   │   ├── Email/EmailEngine.cs
│   │   └── Pdf/PdfEngine.cs
│   ├── Pages/
│   │   ├── Index.razor
│   │   ├── Customers/
│   │   └── Settings/CompanySettings.razor
│   ├── Shared/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   └── Program.cs                 # Lädt Host + Faktura-Modul
│
├── Modules/Faktura/                # Faktura-Modul
│   ├── Data/
│   │   ├── FakturaDbContext.cs    # Schema: "faktura"
│   │   └── Migrations/
│   ├── Services/
│   │   ├── InvoiceService.cs
│   │   └── (weitere Invoice-Services)
│   ├── Pages/
│   │   ├── Invoices/
│   │   └── Settings/
│   │       ├── EmailCustomization.razor
│   │       └── PdfCustomization.razor
│   ├── Shared/
│   │   └── NavMenu.razor
│   └── FakturaModule.cs           # DI-Registration
│
└── Shared.UI/                      # Shared Blazor Components
    └── Components/
        ├── ConfirmDialog.razor
        └── CustomerPicker.razor
```

## Datenbank-Schema

### PostgreSQL mit Schema-Trennung

**Schema: `host`**
- `Companies` - Firmenstammdaten (SMTP, Email-Design, PDF-Design)
- `Customers` - Kundendaten (plattformweit verfügbar)

**Schema: `faktura`**
- `Invoices` - Rechnungen (FK: CustomerId → host.Customers.Id)
- `InvoiceItems` - Rechnungspositionen
- `DownPayments` - Anzahlungen

**Cross-Schema-Beziehungen:**
- Keine EF Core Navigation Properties über Schema-Grenzen
- Customer-Daten werden in Faktura über `ICustomerService` geladen
- CustomerId in Invoices als einfacher Integer-FK

## Dependency Injection

```
Host registriert:
- ICompanyService → CompanyService
- ICustomerService → CustomerService
- IEmailEngine → EmailEngine
- IPdfEngine → PdfEngine

Faktura registriert:
- IInvoiceService → InvoiceService
- IDashboardService → DashboardService
- IPdfGeneratorService → PdfGeneratorService (verwendet ICustomerService aus Host)
- IEmailService → EmailService (verwendet IEmailEngine aus Host)
```

## Nächste Schritte

### Für Entwicklung
1. Datenbank migrieren (falls bereits Daten vorhanden):
   ```bash
   psql -U postgres -d faktura_db -f docs/migrations/migrate-to-schemas.sql
   # Anwendung starten (wendet Migrations an)
   psql -U postgres -d faktura_db -f docs/migrations/restore-from-backup.sql
   ```

2. Anwendung starten:
   ```bash
   cd src/Host
   dotnet run
   ```

### Für neue Features
- **Neue Module hinzufügen**: Analog zu Faktura-Modul
  - Eigener DbContext mit eigenem Schema
  - Services über DI registrieren
  - Host-Services (Company, Customer, Email, PDF) konsumieren
  - Pages in Module/Pages/, in Host.App.razor AdditionalAssemblies hinzufügen

- **Host erweitern**: Z.B. weitere plattformweite Entitäten
  - Models in Core/
  - Services in Host/Services/
  - Registrierung in ServiceCollectionExtensions.cs

## Verifikation

✅ Gesamtes Solution kompiliert fehlerfrei
✅ Alle Projekte bauen erfolgreich
✅ Keine zirkulären Abhängigkeiten
✅ Migrations für beide Schemas vorhanden
✅ Host lädt Faktura-Modul korrekt
✅ Routing funktioniert für beide Projekte
✅ NavMenu zeigt alle Bereiche an

## Erfolg! 🎉

Die modulare Architektur ist einsatzbereit. Die Plattform "Küstencode Werkbank" kann nun mit weiteren Modulen erweitert werden, während das Faktura-Modul unabhängig weiterentwickelt werden kann.
