# Küstencode Werkbank - Installation

Dieses Verzeichnis enthält einen fertigen Docker-Compose-Stack für den Produktionsbetrieb.

## Voraussetzungen
- Docker
- Docker Compose (v2)

## Installation

1. In dieses Verzeichnis wechseln.
2. Einmalig starten:
   ```bash
   ./setup.sh
   ```
3. Öffne im Browser:
   http://localhost:8080

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

## Module und Ports

| Modul   | Port | Beschreibung |
|---------|------|--------------|
| Host    | 8080 | Zentrale Oberfläche, Kundenverwaltung, Reverse Proxy |
| Faktura | 8081 | Rechnungen, PDF, XRechnung |
| Rapport | 8082 | Zeiterfassung, Tätigkeitsnachweise |
| Offerte | 8083 | Angebote, PDF, E-Mail |
| Acta    | 8084 | Projektverwaltung, Aufgabenmanagement |
| Recepta | 8085 | Eingangsrechnungen, OCR, ZUGFeRD-Import |
| Saldo   | 8086 | EÜR, DATEV-Export, SKR03/SKR04 |

Alle Module sind über den Host unter http://localhost:8080 erreichbar:
- `/faktura`, `/rapport`, `/offerte`, `/acta`, `/recepta`, `/saldo`

## Versionen

- Host:    2.1.0 (`kuestencode/host:latest`)
- Faktura: 3.1.0 (`kuestencode/faktura:latest`)
- Rapport: 2.0.0 (`kuestencode/rapport:latest`)
- Offerte: 2.1.0 (`kuestencode/offerte:latest`)
- Acta:    2.1.0 (`kuestencode/acta:latest`)
- Recepta: 2.1.0 (`kuestencode/recepta:latest`)
- Saldo:   1.0.0 (`kuestencode/saldo:latest`)

## Konfiguration

Passwort und Datenbankzugang werden in der `docker-compose.yml` gesetzt:

```yaml
POSTGRES_PASSWORD: change_me
ConnectionStrings__DefaultConnection: ...Password=change_me
```

**Wichtig:** Das Passwort vor dem ersten Start ändern.
