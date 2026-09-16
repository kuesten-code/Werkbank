# Küstencode Werkbank

Eine modulare Business-Software-Suite für kleine und mittelständische Unternehmen. Die Werkbank besteht aus einem gemeinsamen Kern und unabhängig aktivierbaren Modulen.

## Module

### Faktura (aktiv)
- **Rechnungsverwaltung** - Erstellen, bearbeiten und verwalten von Rechnungen
- **Kundenverwaltung** - Vollständige Verwaltung von Kundendaten
- **Firmenprofil** - Konfigurierbare Firmeninformationen
- **PDF-Generierung** - Automatische PDF-Erstellung mit QuestPDF
- **XRechnung-Export** - Standardkonforme XRechnung-Dateien für B2B und Behörden
- **E-Mail-Versand** - Rechnungsversand per E-Mail (MailKit)
- **QR-Code-Integration** - QR-Codes auf Rechnungen
- **Dashboard** - Übersicht über offene und überfällige Rechnungen

### Weitere Module (geplant)
- **Projekte** - Projektplanung, Lasten-/Pflichtenheft, Kunden-Projektstand

## Technologie-Stack

### Backend
- **.NET 9** - Neueste .NET Version
- **Blazor Server** - Interaktive Web-UI mit C#
- **Entity Framework Core 9** - ORM für Datenbankzugriff
- **PostgreSQL 16** - Relationale Datenbank

### UI Framework
- **MudBlazor 8.0** - Material Design UI-Framework

### PDF & Dokumente (Modul: Faktura)
- **QuestPDF** - Moderne PDF-Generierung
- **iText7** - PDF-Verarbeitung
- **QRCoder** - QR-Code-Generierung

### E-Mail
- **MailKit 4.9.0** - E-Mail-Versand
- **MimeKit 4.9.0** - MIME-Nachrichtenverarbeitung

### Deployment
- **Docker & Docker Compose** - Containerisierung
- **Npgsql 9.0.2** - PostgreSQL-Provider für EF Core

## Erste Schritte

### Voraussetzungen

- .NET 9 SDK
- Docker Desktop
- IDE (Visual Studio 2022, VS Code oder Rider)

### Installation mit Docker (Empfohlen)

1. **Komplettes System starten**
   ```bash
   docker-compose up -d
   ```

   Dies startet:
   - PostgreSQL Datenbank auf Port 5432
   - Blazor-Anwendung auf Port 8080

   **Wichtig:** Beim ersten Start werden automatisch alle Datenbankmigrationen angewendet. Dies kann einige Sekunden dauern.

2. **Anwendung öffnen**

   Öffnen Sie im Browser: `http://localhost:8080`

   **Hinweis:** Wenn die Anwendung noch nicht erreichbar ist, warten Sie kurz bis die Migrationen abgeschlossen sind. Sie können den Fortschritt mit `docker-compose logs -f invoiceapp` verfolgen.

### Lokale Entwicklung

1. **Nur Datenbank starten**
   ```bash
   docker-compose up -d postgres
   ```

2. **Datenbank Migrationen anwenden**
   ```bash
   dotnet ef database update --project src/Modules/Faktura/Kuestencode.Faktura.csproj
   ```

3. **Anwendung starten**
   ```bash
   dotnet restore
   dotnet run --project src/Modules/Faktura/Kuestencode.Faktura.csproj
   ```

Die Anwendung ist verfügbar unter: `https://localhost:5001` oder `http://localhost:5000`

## Projektstruktur

```
Kuestencode.Werkbank/
├── src/
│   ├── Core/                           # Kuestencode.Core - Shared Logik
│   │   ├── Enums/                      # Gemeinsame Enumerationen
│   │   ├── Extensions/                 # Extension Methods
│   │   ├── Interfaces/                 # Shared Interfaces
│   │   ├── Models/                     # Basis-Datenmodelle
│   │   ├── Services/                   # Gemeinsame Services (z.B. Email)
│   │   └── Validation/                 # Validierungsattribute
│   │
│   ├── Shared.UI/                      # Kuestencode.Shared.UI - UI-Komponenten
│   │   ├── Components/                 # Wiederverwendbare Blazor-Komponenten
│   │   ├── Layouts/                    # Gemeinsame Layouts
│   │   └── wwwroot/                    # Shared Static Assets
│   │
│   └── Modules/
│       └── Faktura/                    # Kuestencode.Faktura
│           ├── Data/                   # DbContext, Repositories
│           ├── Migrations/             # EF Core Migrations
│           ├── Models/                 # Faktura-spezifische Models
│           ├── Pages/                  # Razor Pages
│           ├── Services/               # Faktura-Services
│           │   ├── Email/
│           │   └── Pdf/
│           ├── Shared/                 # Modul-interne Komponenten
│           └── wwwroot/
│
├── tests/
│   ├── Kuestencode.Core.Tests/
│   └── Kuestencode.Faktura.Tests/
│
├── Kuestencode.Werkbank.sln
├── docker-compose.yml
└── Dockerfile
```

Siehe [ARCHITECTURE.md](ARCHITECTURE.md) für Details zur modularen Architektur.

## Konfiguration

### Datenbank-Verbindung

Die Connection-String ist in [appsettings.json](appsettings.json) konfiguriert:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=invoiceapp_dev;Username=postgres;Password=dev_password"
}
```

Für Docker-Deployment wird die Connection-String über Umgebungsvariablen gesetzt.

### E-Mail-Konfiguration

E-Mail-Einstellungen werden in der Datenbank pro Firma gespeichert.

## Docker Commands

**Gesamtes System starten:**
```bash
docker-compose up -d
```

**Nur Datenbank starten:**
```bash
docker-compose up -d postgres
```

**Logs anzeigen:**
```bash
docker-compose logs -f
```

**System stoppen:**
```bash
docker-compose down
```

**System zurücksetzen (ACHTUNG: Löscht alle Daten):**
```bash
docker-compose down -v
docker-compose up -d
```

**Neu bauen nach Code-Änderungen:**
```bash
docker-compose up -d --build
```

## Entwicklung

### Entity Framework Migrationen

**Automatische Migrationen (Docker):**

Bei Verwendung von Docker werden Migrationen automatisch beim Start des Containers angewendet:
- Beim Build wird automatisch eine InitialCreate-Migration erstellt, falls noch keine Migrationen vorhanden sind
- Beim Start der Anwendung wird `Database.Migrate()` ausgeführt, was alle ausstehenden Migrationen anwendet
- Dies funktioniert sowohl beim ersten Start als auch bei Updates

**Manuelle Migrationen (Lokale Entwicklung):**

**Neue Migration erstellen:**
```bash
dotnet ef migrations add <MigrationName> --project src/Modules/Faktura/Kuestencode.Faktura.csproj
```

**Migration anwenden:**
```bash
dotnet ef database update --project src/Modules/Faktura/Kuestencode.Faktura.csproj
```

**Migration rückgängig machen:**
```bash
dotnet ef database update <PreviousMigrationName> --project src/Modules/Faktura/Kuestencode.Faktura.csproj
```

**Hinweis:** Für lokale Entwicklung müssen Sie sicherstellen, dass die dotnet-ef Tools installiert sind:
```bash
dotnet tool install --global dotnet-ef
```

### Nützliche Links

- [MudBlazor Dokumentation](https://mudblazor.com/)
- [QuestPDF Dokumentation](https://www.questpdf.com/)
- [XRechnung Standard](https://www.xoev.de/xrechnung)
- [Blazor Dokumentation](https://learn.microsoft.com/de-de/aspnet/core/blazor/)

## Lizenz

Privates Projekt
