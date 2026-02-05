# Kuestencode.Acta

Microservice für Projektverwaltung und Aufgabenmanagement in der Küstencode Werkbank.

## Features

- Projekte erstellen, bearbeiten und verwalten
- Statusworkflow mit State Machine (Entwurf, Aktiv, Pausiert, Abgeschlossen, Archiviert)
- Aufgabenverwaltung pro Projekt (CRUD, Sortierung, Status)
- Automatische Projektnummern-Vergabe (P-YYYY-NNNN)
- Budget-Tracking (netto)
- Projektadresse und Beschreibung
- Fortschrittsanzeige basierend auf Aufgabenstatus
- Cross-Modul-Integration mit Rapport (Zeiterfassung auf Projekte buchen)
- Cross-Modul-Integration mit Rapport (Aufwand-Karte auf Projektdetailseite)

## Projekt-Statusübergänge

```
                    ┌──────────────┐
                    │    Draft     │
                    │  (Entwurf)   │
                    └──────┬───────┘
                           │ Freigeben
                           ▼
              ┌──────────────────────────┐
      ┌───────│         Active           │───────┐
      │       │        (Aktiv)           │       │
      │       └──────────────────────────┘       │
      │ Pausieren                    Abschließen │
      ▼                                          ▼
┌───────────┐                          ┌──────────────┐
│  Paused   │                          │  Completed   │
│(Pausiert) │                          │(Abgeschlossen│
└─────┬─────┘                          └───┬──────┬───┘
      │ Fortsetzen                         │      │
      └────────► Active ◄─────────────────┘      │ Archivieren
                         Reaktivieren             ▼
                                          ┌──────────────┐
                                          │   Archived   │
                                          │ (Archiviert) │
                                          └──────────────┘
                                            Terminal-Status
```

**Bearbeitbar** in: Draft, Active, Paused
**Terminal-Status**: Archived (keine weiteren Übergänge möglich)

## Installation (Microservice)

```bash
cd src/Modules/Acta/Kuestencode.Werkbank.Acta
dotnet run
```

Erreichbar unter `http://localhost:8084` (direkt) bzw. `http://localhost:8080/acta` (über Host-Proxy).

## Konfiguration

| Variable | Beschreibung |
|----------|-------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL-Verbindungsstring |
| `ServiceUrls:Host` | URL des Host-Services |
| `ServiceUrls:Self` | Eigene URL für Modul-Registrierung |
| `APPLY_MIGRATIONS` | Automatische DB-Migrationen (`true`/`false`) |

## API Endpoints

### Projekte

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| `GET` | `/api/acta/projects` | Alle Projekte (Filter: `?status=Active&customerId=1`) |
| `GET` | `/api/acta/projects/{id}` | Projekt nach ID (Guid) |
| `POST` | `/api/acta/projects` | Neues Projekt erstellen |
| `PUT` | `/api/acta/projects/{id}` | Projekt aktualisieren |
| `POST` | `/api/acta/projects/{id}/status` | Status ändern |
| `DELETE` | `/api/acta/projects/{id}` | Projekt löschen (nur Draft) |
| `GET` | `/api/acta/projects/{id}/summary` | Projektzusammenfassung |
| `GET` | `/api/acta/projects/external` | Projekte für andere Module (mit ExternalId) |
| `GET` | `/api/acta/projects/external/{externalId}` | Einzelnes Projekt per ExternalId |

### Aufgaben

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| `GET` | `/api/acta/tasks/project/{projectId}` | Aufgaben eines Projekts |
| `GET` | `/api/acta/tasks/{id}` | Aufgabe nach ID |
| `POST` | `/api/acta/tasks/project/{projectId}` | Neue Aufgabe erstellen |
| `PUT` | `/api/acta/tasks/{id}` | Aufgabe aktualisieren |
| `POST` | `/api/acta/tasks/{id}/complete` | Aufgabe abschließen |
| `POST` | `/api/acta/tasks/{id}/reopen` | Aufgabe wieder öffnen |
| `POST` | `/api/acta/tasks/project/{projectId}/reorder` | Aufgaben umsortieren |
| `DELETE` | `/api/acta/tasks/{id}` | Aufgabe löschen |

## Dependencies

- Kuestencode.Core
- Kuestencode.Shared.UI
- Kuestencode.Shared.Contracts
- Kuestencode.Shared.ApiClients
- Entity Framework Core (PostgreSQL)
- MudBlazor

## Architektur

```
Kuestencode.Werkbank.Acta/               # Hauptprojekt (Blazor Server + API)
├── Controllers/
│   ├── ProjectsController.cs             # Projekt-CRUD + External-Endpoints
│   └── TasksController.cs               # Aufgaben-CRUD
├── Pages/
│   └── Projekte/
│       ├── Index.razor                   # Projektliste
│       ├── Details.razor                 # Projektdetails (Tabs: Stammdaten, Aufgaben, Übersicht)
│       ├── Neu.razor                     # Neues Projekt
│       └── Bearbeiten.razor              # Projekt bearbeiten
├── Services/
│   ├── Interfaces/
│   │   ├── IProjectService.cs            # Projekt-Service Interface
│   │   └── IProjectTaskService.cs        # Aufgaben-Service Interface
│   └── Implementation/
│       ├── ProjectService.cs             # Projekt-Logik mit Validierung
│       ├── ProjectTaskService.cs         # Aufgaben-Logik
│       ├── ApiCompanyService.cs          # Firmendaten vom Host
│       └── ApiCustomerService.cs         # Kundendaten vom Host
├── Shared/
│   ├── Components/
│   │   ├── ProjectStatusBadge.razor      # Status-Anzeige
│   │   ├── ProjectTaskList.razor         # Aufgabenliste
│   │   └── TaskEditDialog.razor          # Aufgaben-Dialog
│   └── Dialogs/
│       └── ConfirmDialog.razor           # Bestätigungsdialog
├── ActaModule.cs                         # Service-Registrierung
└── ProgramApi.cs                         # Entry Point (Microservice)

Kuestencode.Werkbank.Acta.Domain/         # Domain-Schicht
├── Entities/
│   ├── Project.cs                        # Projekt-Entity (Guid ID, ExternalId, Status, Budget, ...)
│   └── ProjectTask.cs                    # Aufgaben-Entity (Guid ID, Status, Zuweisung, Sortierung)
├── Enums/
│   ├── ProjectStatus.cs                  # Draft, Active, Paused, Completed, Archived
│   └── ProjectTaskStatus.cs              # Open, Completed
├── Dtos/
│   ├── ProjectDtos.cs                    # Filter, Create, Update DTOs
│   └── ProjectTaskDtos.cs               # Aufgaben-DTOs
└── Services/
    ├── ProjectStateMachine.cs            # Statische State Machine (Übergangstabelle)
    └── ProjectStatusService.cs           # Statusübergangs-Logik mit Validierung

Kuestencode.Werkbank.Acta.Data/           # Datenzugriff
├── ActaDbContext.cs                      # DbContext (Schema: "acta")
├── Repositories/
│   ├── IProjectRepository.cs
│   ├── ProjectRepository.cs
│   ├── IProjectTaskRepository.cs
│   └── ProjectTaskRepository.cs
└── Migrations/                           # EF Core Migrationen
```

## Datenbank

Schema: `acta`

### Tabelle: Projects

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | uuid (PK) | Eindeutige Projekt-ID |
| ProjectNumber | varchar (unique) | Projektnummer (P-YYYY-NNNN) |
| Name | varchar(200) | Projektname |
| Description | varchar(2000) | Beschreibung (optional) |
| CustomerId | int (FK) | Kunde (Host-Schema) |
| Address, PostalCode, City | varchar | Projektadresse (optional) |
| Status | varchar(20) | Projektstatus (als String) |
| ExternalId | int (unique, nullable) | ID für Cross-Modul-Verknüpfung |
| BudgetNet | decimal(18,2) | Budget netto (optional) |
| StartDate, TargetDate | date | Start-/Zieldatum (optional) |
| CompletedAt | timestamp | Abschlusszeitpunkt |
| CreatedAt, UpdatedAt | timestamp | Automatische Zeitstempel |

### Tabelle: Tasks

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | uuid (PK) | Eindeutige Aufgaben-ID |
| ProjectId | uuid (FK) | Zugehöriges Projekt (Cascade Delete) |
| Title | varchar(500) | Aufgabentitel |
| Notes | varchar(2000) | Notizen (optional) |
| Status | varchar(20) | Open / Completed |
| AssignedUserId | uuid (FK, nullable) | Zugewiesener Benutzer (Host-Schema) |
| SortOrder | int | Sortierungsreihenfolge |
| TargetDate | date | Fälligkeitsdatum (optional) |
| CompletedAt | timestamp | Abschlusszeitpunkt |
| CreatedAt | timestamp | Erstellungszeitpunkt |

## Cross-Modul-Integration

### Rapport-Integration

Acta-Projekte können in Rapport für die Zeiterfassung verwendet werden:

- **ExternalId**: Jedes Acta-Projekt erhält automatisch eine `int ExternalId`, die als Brücke zu Rapport dient (Rapport arbeitet intern mit `int` Projekt-IDs)
- **Projekt-Export**: Der Endpoint `/api/acta/projects/external` liefert alle aktiven Projekte im leichtgewichtigen Format (`ActaProjectDto`) für andere Module
- **Statusfilter**: Nur Projekte mit Status Active, Paused oder Completed werden exportiert (Draft und Archived sind ausgeschlossen)
- **Aufwand-Karte**: Die Projektdetailseite zeigt automatisch eine Aufwand-Karte mit gebuchten Stunden aus Rapport, sofern Rapport im Stack aktiv ist

### Datenfluss

```
Rapport (Projektliste)  →  Host (Proxy)  →  Acta (External-Endpoint)
Acta (Aufwand-Karte)    →  Host (Proxy)  →  Rapport (Dashboard-API)
```
