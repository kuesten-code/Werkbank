# Küstencode Werkbank - Installation

Dieses Verzeichnis enthält einen fertigen Docker-Compose-Stack für den Produktionsbetrieb.

## Voraussetzungen
- Docker
- Docker Compose (v2)

## Installation

1. In dieses Verzeichnis wechseln.
2. `.env` anlegen:
   ```bash
   cp .env.example .env
   ```
3. `.env` anpassen (mind. Passwort und JWT-Secret setzen, siehe unten).
4. Einmalig starten:
   ```bash
   ./setup.sh
   ```
5. Öffne im Browser:
   http://localhost:8080

## Konfiguration (.env)

Alle Secrets werden in der `.env` Datei verwaltet. Diese Datei wird nie ins Repository eingecheckt.

| Variable            | Beschreibung |
|---------------------|--------------|
| `POSTGRES_PASSWORD` | Datenbankpasswort – beliebig wählen, vor dem ersten Start setzen |
| `JWT_SECRET`        | Mindestens 32 Zeichen, zufällig generieren: `openssl rand -base64 32` |
| `HOST_PORT`         | Externer Port (Standard: 8080) – anpassen wenn der Port bereits belegt ist |

Beispiel `.env`:
```
POSTGRES_PASSWORD=sicheres_passwort_hier
JWT_SECRET=abcdefghijklmnopqrstuvwxyz123456
HOST_PORT=8080
```

## Stoppen

```bash
./shutdown.sh
```

## Aktualisieren

```bash
./update-werkbank.sh
```

Optionen:
- `-f` / `--force` – Container neu starten, auch wenn kein Update gefunden
- `-c` / `--check` – Nur prüfen und Images laden, Container nicht neu starten
- `-h` / `--help`  – Hilfe anzeigen

## Erreichbarkeit

Alle Module sind über den Host unter http://localhost:8080 erreichbar:

| Pfad       | Modul   | Beschreibung |
|------------|---------|--------------|
| `/`        | Host    | Zentrale Oberfläche, Kundenverwaltung |
| `/faktura` | Faktura | Rechnungen, PDF, XRechnung |
| `/rapport` | Rapport | Zeiterfassung, Tätigkeitsnachweise |
| `/offerte` | Offerte | Angebote, PDF, E-Mail |
| `/acta`    | Acta    | Projektverwaltung, Aufgabenmanagement |
| `/recepta` | Recepta | Eingangsrechnungen, OCR, ZUGFeRD-Import |
| `/saldo`   | Saldo   | EÜR, DATEV-Export, SKR03/SKR04 |

Die Modul-Ports (8081–8086) sind intern und nicht nach außen exponiert.

## Versionen

Versionen werden aus den Docker Images gezogen (`DOCKER_IMAGE_TAG`).
Aktueller Stand: alle Images auf `:latest`.
