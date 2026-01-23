# Kuestencode.Faktura

Faktura-Modul (Rechnungsverwaltung) für die Kuestencode-Plattform.

## Features

- Rechnungserstellung und -verwaltung
- Kundenverwaltung
- PDF-Generierung mit mehreren Layouts
- XRechnung (XML) Export
- ZUGFeRD PDF (Hybrid mit eingebettetem XML)
- E-Mail-Versand mit verschiedenen Formaten
- GiroCode QR-Codes für Überweisungen
- Dashboard mit Statistiken

## Installation (Microservice)

Run Faktura as its own service and let the Host route to it via reverse proxy.

```bash
cd src/Modules/Faktura
dotnet run
```

## Abhängigkeiten

- Kuestencode.Core
- Kuestencode.Shared.UI
- QuestPDF (PDF-Generierung)
- iText7 (ZUGFeRD PDF)
- MailKit (E-Mail)
- Entity Framework Core (PostgreSQL)
- MudBlazor (UI)

## Architektur

```
Kuestencode.Faktura/
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Repositories/
├── Models/
│   ├── Invoice.cs
│   ├── InvoiceItem.cs
│   ├── DownPayment.cs
│   └── Enums/
├── Services/
│   ├── InvoiceService.cs
│   ├── XRechnungService.cs
│   ├── Email/
│   └── Pdf/
├── Pages/
│   ├── Invoices/
│   ├── Customers/
│   └── Settings/
├── Shared/
│   └── Faktura-spezifische Components
├── FakturaModule.cs      # Service registration
└── ProgramApi.cs         # Service entry point (Microservice mode)
```

## Models

### Invoice (BLEIBT in Faktura)
- InvoiceNumber
- InvoiceDate
- DueDate
- Status (Draft, Sent, Paid, Overdue, Cancelled)
- Items, DownPayments
- Discount Support

### Customer (KOMMT aus Core)
- Verwendet `Kuestencode.Core.Models.Customer`
- Navigation Property in Invoice

### Company (KOMMT aus Core)
- Verwendet `Kuestencode.Core.Models.Company`
- Firmendaten inkl. SMTP-Einstellungen

## Services

### Faktura-spezifisch
- `InvoiceService` - CRUD für Rechnungen
- `XRechnungService` - XML-Export nach EN16931
- `PdfGeneratorService` - PDF mit QuestPDF
- `InvoiceEmailService` - Rechnungsversand

### Aus Core
- `ICompanyService` → `CompanyService` (Faktura-Implementierung)
- `ICustomerService` → `CustomerService` (Faktura-Implementierung)
- `IEmailService` → Basis E-Mail-Funktionen

## Configuration

### appsettings.api.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=faktura;Username=postgres;Password=..."
  },
  "APPLY_MIGRATIONS": true,
  "ServiceUrls": {
    "Host": "http://localhost:8080"
  }
}
```

## Migration von InvoiceApp

Bei der Migration von `InvoiceApp` zu `Kuestencode.Faktura`:

### Namespace-Änderungen

```csharp
// Vorher
using InvoiceApp.Models;
using InvoiceApp.Services;
using InvoiceApp.Validation;

// Nachher
using Kuestencode.Core.Models;        // Customer, Company, Address, etc.
using Kuestencode.Core.Interfaces;    // IRepository, ICompanyService, etc.
using Kuestencode.Core.Validation;    // Iban, GermanPostalCode, etc.
using Kuestencode.Faktura.Models;     // Invoice, InvoiceItem, etc.
using Kuestencode.Faktura.Services;   // InvoiceService, XRechnungService, etc.
```

### Service registration

Service registration happens inside the Faktura service process. The Host only forwards requests via reverse proxy in microservice mode.

## E-Mail-Formate

- **Normal PDF** - Standard-Rechnung als PDF
- **ZUGFeRD PDF** - Hybrid-PDF mit eingebettetem XML (EN16931)
- **XRechnung XML** - Nur XML-Datei
- **XRechnung XML + PDF** - Beide als separate Anhänge
