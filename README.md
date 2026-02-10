# Küstencode Werkbank

Eine modulare, selbst gehostete Business-Software-Plattform für Selbstständige und kleine Unternehmen – ohne Cloud, ohne Abo, ohne unnötige Komplexität.

## Module

### Faktura

Rechnungsprogramm mit Fokus auf saubere Fakturierung und E-Rechnungen.

**Features:**
- Rechnungen erstellen, verwalten und verfolgen (Entwurf, Versendet, Bezahlt, Überfällig)
- PDF-Rechnungen mit anpassbaren Layouts
- E-Rechnungen nach EN16931-Standard (XRechnung, ZUGFeRD)
- GiroCode-QR-Codes für SEPA-Überweisungen
- Kundenverwaltung mit Adressdaten
- E-Mail-Versand mit konfigurierbaren Templates
- Dashboard mit Übersicht offener und überfälliger Rechnungen
- Anzahlungsrechnungen und Abschlagsrechnungen

### Offerte

Angebotserstellung und -verwaltung für professionelle Kundenakquise.

**Features:**
- Angebote erstellen, bearbeiten und verwalten
- Flexibler Statusworkflow (Entwurf → Versendet → Angenommen/Abgelehnt)
- PDF-Angebote mit anpassbaren Layouts und Farbschemata
- E-Mail-Versand mit konfigurierbaren HTML-Templates (Klar, Strukturiert, Betont)
- Gültigkeitsdatum-Tracking
- Angebot direkt in Rechnung umwandeln (Faktura-Integration)

### Rapport

Zeiterfassung und Tätigkeitsdokumentation für projektbasiertes Arbeiten.

**Features:**
- Timer-basierte Zeiterfassung mit Kunden- und Projektzuordnung
- Manuelle Zeiteinträge erstellen und bearbeiten
- Tätigkeitsnachweise als PDF und CSV exportieren
- Einstellungen mit Live-Vorschau für PDF-Layout
- Integration mit Faktura (Tätigkeiten an Rechnungen anhängen)

### Acta

Projektverwaltung und Aufgabenmanagement für strukturiertes Arbeiten.

**Features:**
- Projekte erstellen, bearbeiten und verwalten
- Statusworkflow mit State Machine (Entwurf → Aktiv → Pausiert → Abgeschlossen → Archiviert)
- Aufgabenverwaltung pro Projekt mit Sortierung und Zuweisung
- Automatische Projektnummern-Vergabe (P-YYYY-NNNN)
- Budget-Tracking und Fortschrittsanzeige
- Cross-Modul-Integration mit Rapport (Zeiterfassung auf Projekte buchen)

## Technologie-Stack

| Bereich | Technologie |
|---------|-------------|
| Framework | .NET 9, Blazor Server |
| UI | MudBlazor 8.0 (Material Design) |
| Datenbank | PostgreSQL 16, Entity Framework Core 9 |
| PDF | QuestPDF, iText7 (ZUGFeRD) |
| E-Mail | MailKit, MimeKit |
| Container | Docker, Docker Compose |

## Installation

Küstencode Werkbank wird als Docker-Compose-Stack betrieben (z.B. auf einem NAS oder Server im eigenen Netzwerk).

### Voraussetzungen

- Docker
- Docker Compose

### Schnellstart

```bash
git clone https://github.com/yourusername/Kuestencode_Werkbank.git
cd Kuestencode_Werkbank
docker compose up -d
```

Die Anwendung ist dann erreichbar unter:
- **Host/Übersicht:** http://localhost:8080
- **Faktura:** http://localhost:8080/faktura
- **Offerte:** http://localhost:8080/offerte
- **Rapport:** http://localhost:8080/rapport
- **Acta:** http://localhost:8080/acta

### Produktions-Deployment

Für Produktionsumgebungen liegt ein fertiger Stack im aktuellen Release. 
Dieser muss heruntergeladen und entpackt werden. 
Dann die setup shell (Linux) oder die Powershell (Windows) ausführen.

## Entwicklung

### Lokale Entwicklungsumgebung

```bash
# PostgreSQL starten
docker compose up -d postgres

# Host-Anwendung starten
cd src/Host
dotnet run
```

### Projektstruktur

```
src/
├── Core/                    # Gemeinsame Models, Interfaces, Validierung
├── Shared.UI/               # Wiederverwendbare Blazor-Komponenten
├── Host/                    # Hauptanwendung mit Dashboard und Kundenverwaltung
├── Modules/
│   ├── Faktura/             # Rechnungsmodul
│   ├── Offerte/             # Angebotsmodul
│   ├── Rapport/             # Zeiterfassungsmodul
│   └── Acta/                # Projektverwaltungsmodul
├── Kuestencode.Shared.Contracts/    # DTOs für Modul-Kommunikation
└── Kuestencode.Shared.ApiClients/   # HTTP-Clients für API-Aufrufe
```

## Architektur

Küstencode Werkbank folgt einer modularen Microservice-Architektur:

- **Host** agiert als zentrales Gateway mit Reverse-Proxy (Yarp)
- **Module** laufen als eigenständige Services mit eigenen Datenbank-Schemas
- **Kommunikation** erfolgt über REST-APIs und geteilte Contracts
- **Deployment** Als Container (Produktion)

Detaillierte Dokumentation: [ARCHITECTURE.md](ARCHITECTURE.md)

## Lizenz

MIT License – Copyright 2026 Kevin Schulze
