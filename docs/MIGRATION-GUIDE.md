# Migration Guide: InvoiceApp zu Kuestencode Modular

Dieses Dokument beschreibt die Migration von `InvoiceApp` zur modularen `Kuestencode`-Architektur.

## Übersicht der neuen Struktur

```
Kuestencode/
├── src/
│   ├── Core/                       # Kuestencode.Core
│   │   ├── Models/                 # Company, Customer, Address, etc.
│   │   ├── Interfaces/             # IRepository, IEmailService, etc.
│   │   ├── Services/               # CoreEmailService
│   │   ├── Validation/             # IBAN, PLZ, etc.
│   │   └── Enums/                  # Country, PaymentMethod
│   │
│   ├── Shared.UI/                  # Kuestencode.Shared.UI
│   │   ├── Components/             # CustomerPicker, AddressForm, etc.
│   │   ├── Layouts/                # ModuleLayout
│   │   └── wwwroot/                # CSS, JS
│   │
│   └── Modules/
│       └── Faktura/                # Kuestencode.Faktura
│           ├── Models/             # Invoice, InvoiceItem, etc.
│           ├── Services/           # InvoiceService, XRechnungService, etc.
│           ├── Pages/              # Blazor Pages
│           └── Data/               # DbContext, Repositories
│
├── tests/
│   ├── Kuestencode.Core.Tests/
│   └── Kuestencode.Faktura.Tests/
│
├── Kuestencode.sln
└── docker-compose.modular.yml
```

## Phase 1: Namespace-Migration

### Using-Statement-Änderungen

```csharp
// === VORHER ===
using InvoiceApp.Models;
using InvoiceApp.Services;
using InvoiceApp.Validation;
using InvoiceApp.Data.Repositories;

// === NACHHER ===

// Für Core-Typen (Company, Customer, etc.)
using Kuestencode.Core.Models;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Validation;
using Kuestencode.Core.Enums;

// Für Faktura-spezifische Typen (Invoice, InvoiceItem, etc.)
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Data.Repositories;

// Für UI-Komponenten
using Kuestencode.Shared.UI;
using Kuestencode.Shared.UI.Components;
using Kuestencode.Shared.UI.Layouts;
```

### Welche Typen wohin?

| Alter Namespace | Neuer Namespace | Typen |
|-----------------|-----------------|-------|
| `InvoiceApp.Models.Company` | `Kuestencode.Core.Models.Company` | Company |
| `InvoiceApp.Models.Customer` | `Kuestencode.Core.Models.Customer` | Customer |
| `InvoiceApp.Models.Invoice` | `Kuestencode.Faktura.Models.Invoice` | Invoice |
| `InvoiceApp.Models.InvoiceItem` | `Kuestencode.Faktura.Models.InvoiceItem` | InvoiceItem |
| `InvoiceApp.Models.InvoiceStatus` | `Kuestencode.Faktura.Models.InvoiceStatus` | InvoiceStatus |
| `InvoiceApp.Validation.*` | `Kuestencode.Core.Validation.*` | IbanAttribute, etc. |
| `InvoiceApp.Services.CompanyService` | `Kuestencode.Faktura.Services.CompanyService` | CompanyService |
| `InvoiceApp.Services.InvoiceService` | `Kuestencode.Faktura.Services.InvoiceService` | InvoiceService |
| `InvoiceApp.Shared.KuestenCodeTheme` | `Kuestencode.Shared.UI.KuestenCodeTheme` | KuestenCodeTheme |

## Phase 2: Service-Registrierung

### Vorher (Program.cs)

```csharp
// Viele einzelne Service-Registrierungen
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IXRechnungService, XRechnungService>();
// ... 30+ weitere Zeilen
```

### Nachher (Program.cs)

```csharp
using Kuestencode.Faktura;

// Eine Zeile für alle Faktura-Services
builder.Services.AddFakturaModule(builder.Configuration);
```

## Phase 3: DbContext-Migration

### Vorher

```csharp
using InvoiceApp.Data;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Nachher

```csharp
// DbContext wird durch AddFakturaModule() registriert
builder.Services.AddFakturaModule(builder.Configuration);

// Migrations anwenden (optional)
if (configuration.GetValue("APPLY_MIGRATIONS", true))
{
    await FakturaModule.ApplyMigrationsAsync(app.Services);
}
```

## Phase 4: Razor-Dateien aktualisieren

### _Imports.razor

```razor
@* === VORHER === *@
@using InvoiceApp
@using InvoiceApp.Shared
@using InvoiceApp.Models
@using InvoiceApp.Services

@* === NACHHER === *@
@using Kuestencode.Core.Models
@using Kuestencode.Core.Interfaces
@using Kuestencode.Shared.UI
@using Kuestencode.Shared.UI.Components
@using Kuestencode.Shared.UI.Layouts
@using Kuestencode.Faktura
@using Kuestencode.Faktura.Models
@using Kuestencode.Faktura.Services
```

### Beispiel: Page-Migration

```razor
@* === VORHER === *@
@page "/invoices/create"
@using InvoiceApp.Models
@using InvoiceApp.Services
@inject IInvoiceService InvoiceService
@inject ICompanyService CompanyService

@* === NACHHER === *@
@page "/invoices/create"
@using Kuestencode.Core.Models
@using Kuestencode.Faktura.Models
@using Kuestencode.Faktura.Services
@inject IInvoiceService InvoiceService
@inject ICompanyService CompanyService
```

## Phase 5: Shared Components verwenden

### CustomerPicker

```razor
@* === VORHER (eigene Implementierung) === *@
<MudAutocomplete T="Customer" @bind-Value="selectedCustomer" ... />

@* === NACHHER (aus Shared.UI) === *@
<CustomerPicker
    @bind-SelectedCustomer="selectedCustomer"
    Customers="customers"
    Required="true" />
```

### ModuleLayout

```razor
@* === VORHER (MainLayout.razor manuell) === *@
@inherits LayoutComponentBase
<MudThemeProvider Theme="@KuestenCodeTheme.Theme" ...>
...

@* === NACHHER (aus Shared.UI) === *@
<ModuleLayout Title="Küstencode Faktura">
    <NavMenuContent>
        <NavMenu />
    </NavMenuContent>
    <TopContent>
        <CompanyDataWarning />
    </TopContent>
</ModuleLayout>
```

## Phase 6: Docker-Migration

### Vorher

```yaml
# docker-compose.yml
services:
  invoiceapp:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: invoiceapp_web
```

### Nachher

```yaml
# docker-compose.modular.yml
services:
  faktura:
    build:
      context: .
      dockerfile: src/Modules/Faktura/Dockerfile
    container_name: kuestencode_faktura
```

## Breaking Changes

### 1. Namespace-Änderungen
Alle `InvoiceApp.*` Namespaces werden zu `Kuestencode.*`.

### 2. Projekt-Referenzen
```xml
<!-- Neu in Kuestencode.Faktura.csproj -->
<ProjectReference Include="..\..\Core\Kuestencode.Core.csproj" />
<ProjectReference Include="..\..\Shared.UI\Kuestencode.Shared.UI.csproj" />
```

### 3. Assembly-Namen
- `InvoiceApp.dll` → `Kuestencode.Faktura.dll`

### 4. Docker Entry Point
```dockerfile
# Vorher
ENTRYPOINT ["dotnet", "InvoiceApp.dll"]

# Nachher
ENTRYPOINT ["dotnet", "Kuestencode.Faktura.dll"]
```

## Validierungscheckliste

Nach der Migration sollten folgende Punkte geprüft werden:

- [ ] `Kuestencode.Core` kompiliert standalone
- [ ] `Kuestencode.Shared.UI` kompiliert mit Core-Referenz
- [ ] `Kuestencode.Faktura` kompiliert mit beiden Referenzen
- [ ] Unit Tests laufen erfolgreich
- [ ] Docker-Image baut erfolgreich
- [ ] Anwendung startet ohne Fehler
- [ ] Datenbank-Migrations funktionieren
- [ ] Alle bisherigen Features funktionieren:
  - [ ] Rechnungen erstellen/bearbeiten
  - [ ] Kunden verwalten
  - [ ] PDF generieren
  - [ ] XRechnung exportieren
  - [ ] E-Mail versenden
  - [ ] Dashboard anzeigen
- [ ] Keine Duplikate (Customer nur in Core, nicht in Faktura)

## Befehle für die Migration

```bash
# 1. Neue Solution bauen
dotnet build Kuestencode.sln

# 2. Tests ausführen
dotnet test Kuestencode.sln

# 3. Docker-Image bauen
docker-compose -f docker-compose.modular.yml build

# 4. Container starten
docker-compose -f docker-compose.modular.yml up -d

# 5. Logs prüfen
docker-compose -f docker-compose.modular.yml logs -f faktura
```

## Häufige Probleme

### Problem: "Type 'Customer' not found"
**Lösung:** `using Kuestencode.Core.Models;` hinzufügen

### Problem: "Cannot resolve service ICompanyService"
**Lösung:** `builder.Services.AddFakturaModule(...)` aufrufen

### Problem: "DbContext not registered"
**Lösung:** Connection String in appsettings.json prüfen

### Problem: Docker-Build schlägt fehl
**Lösung:** Build-Context und Dockerfile-Pfade im docker-compose prüfen
