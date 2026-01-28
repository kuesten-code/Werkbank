# Kuestencode.Rapport

Microservice f?r Zeiterfassung in der K?stencode Werkbank.

## Features

- Timer (Start/Stop) mit Kundenpflicht
- Manuelle Zeiteintr?ge (CRUD + Soft Delete)
- Projektzuordnung optional (mit Customer-Validation)
- Auswertungen nach Kunde/Projekt
- PDF- & CSV-Export (T?tigkeitsnachweis)
- E-Mail-Versand des T?tigkeitsnachweises
- Einstellungsseite inkl. Live-PDF-Vorschau

## Installation (Microservice)

```bash
cd src/Modules/Rapport
dotnet run
```

## Konfiguration

- `ConnectionStrings:DefaultConnection` (PostgreSQL)
- `ServiceUrls:Host` (Host API)
- `ServiceUrls:Self` (eigene URL f?r Registrierung)

## API Endpoints

- `POST /rapport/api/timesheets/pdf`  -> PDF-T?tigkeitsnachweis
- `POST /rapport/api/timesheets/csv`  -> CSV-Export

## Dependencies

- Kuestencode.Core
- Kuestencode.Shared.UI
- Entity Framework Core (PostgreSQL)
- MudBlazor
- QuestPDF

## Architektur

```
Kuestencode.Rapport/
+-- Controllers/
+-- Data/
|   +-- RapportDbContext.cs
|   +-- Repositories/
+-- Models/
+-- Services/
+-- Pages/
+-- Shared/
+-- RapportModule.cs      # Service registration
+-- ProgramApi.cs         # Service entry point (Microservice mode)
```
