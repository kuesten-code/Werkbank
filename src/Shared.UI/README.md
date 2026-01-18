# Kuestencode.Shared.UI

Wiederverwendbare Blazor Server UI-Komponenten und Layouts für alle Kuestencode-Module.

## Features

- **Components**: Wiederverwendbare Blazor-Komponenten wie `CustomerPicker`, `AddressForm`, `EmailComposer`
- **Layouts**: Basis-Layouts wie `ModuleLayout` mit Dark-Mode-Unterstützung
- **Theme**: Einheitliches MudBlazor-Theme (`KuestenCodeTheme`)
- **Assets**: Shared CSS und JavaScript Utilities

## Installation

```xml
<ProjectReference Include="..\Shared.UI\Kuestencode.Shared.UI.csproj" />
```

## Abhängigkeiten

- Kuestencode.Core
- MudBlazor 8.0.0

## Verwendung

### _Imports.razor

Füge folgende Zeilen zu deiner `_Imports.razor` hinzu:

```razor
@using Kuestencode.Core.Models
@using Kuestencode.Shared.UI
@using Kuestencode.Shared.UI.Components
@using Kuestencode.Shared.UI.Layouts
```

### CustomerPicker

```razor
<CustomerPicker
    @bind-SelectedCustomer="selectedCustomer"
    Customers="customers"
    Label="Kunde auswählen"
    Required="true" />
```

### AddressForm

```razor
<AddressForm
    Address="@myAddress"
    Disabled="false" />
```

### EmailComposer

```razor
@inject IDialogService DialogService

private async Task ShowEmailDialog()
{
    var parameters = new DialogParameters
    {
        ["Title"] = "E-Mail senden",
        ["InitialRecipient"] = "kunde@example.com"
    };

    var dialog = await DialogService.ShowAsync<EmailComposer>("E-Mail", parameters);
    var result = await dialog.Result;

    if (!result.Canceled && result.Data is EmailComposerResult emailResult)
    {
        // E-Mail versenden
    }
}
```

### ModuleLayout

```razor
@inherits LayoutComponentBase

<ModuleLayout Title="Mein Modul" Subtitle="Version 1.0">
    <NavMenuContent>
        <MyNavMenu />
    </NavMenuContent>
    <TopContent>
        <MyWarningBanner />
    </TopContent>
</ModuleLayout>
```

### Theme

```csharp
// In Program.cs oder _Host.cshtml
<MudThemeProvider Theme="@KuestenCodeTheme.Theme" />
```

## Komponenten-Übersicht

| Komponente | Beschreibung |
|------------|--------------|
| `CustomerPicker` | Autocomplete-Auswahl für Kunden |
| `AddressForm` | Formular für Adresseingabe |
| `EmailComposer` | Dialog für E-Mail-Erstellung mit CC/BCC |
| `ConfirmDialog` | Generischer Bestätigungsdialog |
| `ModuleLayout` | Basis-Layout mit AppBar, Drawer, Dark Mode |

## Assets

### CSS

```html
<link href="_content/Kuestencode.Shared.UI/css/shared.css" rel="stylesheet" />
```

### JavaScript

```html
<script src="_content/Kuestencode.Shared.UI/js/shared.js"></script>
```

## Architektur

```
Kuestencode.Shared.UI/
├── Components/           # Wiederverwendbare Blazor-Komponenten
├── Layouts/              # Basis-Layouts
├── wwwroot/
│   ├── css/             # Shared CSS
│   └── js/              # Shared JavaScript
├── _Imports.razor        # Globale Imports
└── KuestenCodeTheme.cs   # MudBlazor Theme
```
