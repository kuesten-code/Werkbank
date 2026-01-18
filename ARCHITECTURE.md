# Kuestencode Architektur

## Modulare Architektur-Übersicht

```
┌─────────────────────────────────────────────────────────────────┐
│                     Kuestencode Platform                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌───────────────────┐  ┌───────────────────┐  ┌─────────────┐  │
│  │  Kuestencode.Core │  │Kuestencode.Shared │  │   Future    │  │
│  │                   │  │       .UI         │  │   Modules   │  │
│  │  • Models         │  │                   │  │             │  │
│  │  • Interfaces     │  │  • Components     │  │  • CRM      │  │
│  │  • Validation     │  │  • Layouts        │  │  • Projekt  │  │
│  │  • Enums          │  │  • Theme          │  │  • Zeit     │  │
│  │  • Core Services  │  │  • CSS/JS         │  │  • ...      │  │
│  └───────────────────┘  └───────────────────┘  └─────────────┘  │
│           │                      │                     │        │
│           └──────────┬───────────┘                     │        │
│                      │                                 │        │
│                      ▼                                 │        │
│         ┌─────────────────────────┐                   │        │
│         │   Kuestencode.Faktura   │◄──────────────────┘        │
│         │                         │                             │
│         │  • Invoice Models       │                             │
│         │  • Invoice Services     │                             │
│         │  • PDF Generation       │                             │
│         │  • XRechnung/ZUGFeRD    │                             │
│         │  • Email Services       │                             │
│         │  • Blazor Pages         │                             │
│         └─────────────────────────┘                             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Projekt-Abhängigkeiten

```
Kuestencode.Core (Keine Abhängigkeiten - Basis-Bibliothek)
       │
       ▼
Kuestencode.Shared.UI (Abhängig von Core)
       │
       ▼
Kuestencode.Faktura (Abhängig von Core + Shared.UI)
```

## Schichten-Architektur

### Kuestencode.Core

Die Core-Bibliothek enthält **keine UI- oder Datenbank-Abhängigkeiten** und ist rein als Klassenbibliothek konzipiert.

```
Kuestencode.Core/
├── Models/               # POCO-Klassen
│   ├── BaseEntity.cs     # Basis mit Id, CreatedAt, UpdatedAt
│   ├── Company.cs        # Firmenstammdaten
│   ├── Customer.cs       # Kundenstammdaten
│   ├── Address.cs        # Adress-Value-Object
│   ├── BankAccount.cs    # Bankverbindung
│   └── SmtpConfiguration.cs
│
├── Interfaces/           # Abstraktionen
│   ├── IRepository<T>.cs # Generisches Repository
│   ├── ICompanyService.cs
│   ├── ICustomerService.cs
│   ├── IEmailService.cs
│   ├── IPdfService.cs
│   └── IDocumentService.cs
│
├── Services/             # Generische Implementierungen
│   └── CoreEmailService.cs
│
├── Validation/           # DataAnnotations
│   ├── IbanAttribute.cs
│   ├── GermanPostalCodeAttribute.cs
│   ├── FullNameAttribute.cs
│   └── CustomerNumberAttribute.cs
│
├── Enums/
│   ├── Country.cs
│   └── PaymentMethod.cs
│
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

### Kuestencode.Shared.UI

Die Shared.UI-Bibliothek enthält **wiederverwendbare Blazor-Komponenten** und das gemeinsame Theme.

```
Kuestencode.Shared.UI/
├── Components/           # Blazor-Komponenten
│   ├── CustomerPicker.razor
│   ├── AddressForm.razor
│   ├── EmailComposer.razor
│   └── ConfirmDialog.razor
│
├── Layouts/
│   └── ModuleLayout.razor
│
├── wwwroot/
│   ├── css/shared.css
│   └── js/shared.js
│
├── KuestenCodeTheme.cs   # MudBlazor Theme
└── _Imports.razor
```

### Kuestencode.Faktura

Das Faktura-Modul ist eine **vollständige Blazor Server Anwendung** mit eigener Datenbank.

```
Kuestencode.Faktura/
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Repositories/
│       ├── Repository<T>.cs
│       ├── CustomerRepository.cs
│       └── InvoiceRepository.cs
│
├── Models/               # Faktura-spezifische Models
│   ├── Invoice.cs
│   ├── InvoiceItem.cs
│   ├── DownPayment.cs
│   ├── InvoiceStatus.cs
│   ├── DiscountType.cs
│   ├── EmailAttachmentFormat.cs
│   ├── EmailLayout.cs
│   └── PdfLayout.cs
│
├── Services/
│   ├── CompanyService.cs    # ICompanyService Implementierung
│   ├── CustomerService.cs   # ICustomerService Implementierung
│   ├── InvoiceService.cs
│   ├── XRechnungService.cs
│   ├── DashboardService.cs
│   ├── PreviewService.cs
│   ├── Email/
│   │   ├── InvoiceEmailService.cs
│   │   ├── EmailTemplateRenderer.cs
│   │   └── ...
│   └── Pdf/
│       ├── PdfGeneratorService.cs
│       ├── PdfTemplateEngine.cs
│       └── Layouts/
│
├── Pages/
│   ├── Index.razor
│   ├── Invoices/
│   ├── Customers/
│   └── Settings/
│
├── Shared/
│   ├── MainLayout.razor
│   ├── NavMenu.razor
│   └── ...
│
├── FakturaModule.cs      # Service-Registrierung
├── Program.cs            # Entry Point
└── appsettings.json
```

## Design-Prinzipien

### 1. Separation of Concerns

- **Core**: Reine Business-Logik und Datenmodelle
- **Shared.UI**: Wiederverwendbare UI-Komponenten
- **Module**: Feature-spezifische Implementierungen

### 2. Dependency Inversion

Module implementieren Core-Interfaces:

```csharp
// Core definiert
public interface ICompanyService { ... }

// Faktura implementiert
public class CompanyService : ICompanyService { ... }
```

### 3. Open/Closed Principle

Neue Module können hinzugefügt werden, ohne Core zu ändern:

```csharp
// Neues Modul registriert sich selbst
builder.Services.AddFakturaModule(configuration);
builder.Services.AddCrmModule(configuration);  // Zukünftig
```

### 4. Single Responsibility

Jede Schicht hat eine klare Verantwortung:

| Schicht | Verantwortung |
|---------|---------------|
| Core | Datenmodelle, Interfaces, Validierung |
| Shared.UI | UI-Komponenten, Theme, Assets |
| Faktura | Rechnungsverwaltung, PDF, XRechnung |

## Erweiterbarkeit

### Neues Modul hinzufügen

1. Neues Projekt erstellen: `Kuestencode.{ModulName}`
2. Referenzen zu Core und Shared.UI hinzufügen
3. `{ModulName}Module.cs` mit Service-Registrierung erstellen
4. Modul in Solution hinzufügen

```csharp
// Beispiel: Zeiterfassung-Modul
public static class ZeitModule
{
    public static IServiceCollection AddZeitModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ZeitDbContext>(...);
        services.AddScoped<IZeitService, ZeitService>();
        return services;
    }
}
```

### Shared Components erweitern

1. Neue Komponente in Shared.UI hinzufügen
2. In `_Imports.razor` exportieren
3. In allen Modulen nutzbar

## Datenbank-Strategie

### Option A: Separate Datenbanken pro Modul (empfohlen für große Systeme)

```yaml
services:
  postgres_faktura:
    image: postgres:16
    environment:
      POSTGRES_DB: faktura

  postgres_crm:
    image: postgres:16
    environment:
      POSTGRES_DB: crm
```

### Option B: Shared Database mit Schema-Trennung (aktuell)

```csharp
// Faktura DbContext
modelBuilder.HasDefaultSchema("faktura");

// CRM DbContext (zukünftig)
modelBuilder.HasDefaultSchema("crm");
```

## Testing-Strategie

```
tests/
├── Kuestencode.Core.Tests/         # Unit Tests für Core
│   ├── ValidationTests.cs
│   └── ModelTests.cs
│
├── Kuestencode.Faktura.Tests/      # Integration Tests für Faktura
│   ├── InvoiceServiceTests.cs
│   └── XRechnungServiceTests.cs
│
└── Kuestencode.E2E.Tests/          # End-to-End Tests (optional)
    └── InvoiceWorkflowTests.cs
```

## Deployment

### Docker Compose (Entwicklung)

```bash
docker-compose -f docker-compose.modular.yml up -d
```

### Kubernetes (Produktion)

Jedes Modul kann als separater Microservice deployed werden:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: kuestencode-faktura
spec:
  template:
    spec:
      containers:
        - name: faktura
          image: kuestencode/faktura:latest
```
