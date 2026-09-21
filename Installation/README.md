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
| `DOCKER_GID`        | Gruppen-ID des Docker-Sockets für die Docker-Steuerung. Leer lassen, `./setup.sh` trägt sie ein. Manuell: `stat -c %g /var/run/docker.sock` (Docker Desktop: `0`) |

Beispiel `.env`:
```
POSTGRES_PASSWORD=sicheres_passwort_hier
JWT_SECRET=abcdefghijklmnopqrstuvwxyz123456
HOST_PORT=8080
DOCKER_GID=
```

**Bewahre die `.env` (und das Backup-Verschlüsselungspasswort) getrennt vom Server auf** – sie ist nicht im Backup enthalten, wird aber für eine Wiederherstellung benötigt.

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

## Modulsteuerung

Unter *Einstellungen → Modulsteuerung* lassen sich einzelne Module stoppen und wieder starten, z. B. um
Arbeitsspeicher zu sparen. Gestoppte Module bleiben auch nach einem Server-Neustart aus. Module, die du aus
der `docker-compose.yml` entfernst, erscheinen dort nicht.

Dafür läuft der Container `docker-control` ([wollomatic/socket-proxy](https://github.com/wollomatic/socket-proxy)).
Er ist der einzige mit Zugriff auf den Docker-Socket, erlaubt nur Status sowie Stoppen/Starten der
Werkbank-Container (nicht des Hosts, keine fremden Container) und ist nur über ein internes Netzwerk erreichbar.
Kommen eigene Module hinzu oder werden Container umbenannt, müssen die Namen in der Regex des Proxys angepasst
werden (`-allowGET` / `-allowPOST`) sowie `DockerControl__ContainerNamePattern` beim Host.

## Backup und Wiederherstellung

Unter *Einstellungen → Backup* werden der komplette `data/`-Ordner (Datenbank, Uploads, Keys) automatisch
oder manuell auf SFTP, S3, WebDAV oder ein lokales Ziel gesichert (mit Rotation und optionaler Verschlüsselung).

**Nicht im Backup:** `.env` (`POSTGRES_PASSWORD`, `JWT_SECRET`), `docker-compose.yml`, Reverse-Proxy und
Zertifikate.

Die Wiederherstellung läuft ebenfalls in der Oberfläche: Die Werkbank hält dafür Module und Datenbank kurz an,
prüft das Backup, tauscht die Daten und startet alles wieder. Startet die Datenbank mit den wiederhergestellten
Daten nicht, wird automatisch der vorherige Stand zurückgeholt.

**Umzug auf einen neuen Server:**
1. `.env` des alten Servers sichern – `POSTGRES_PASSWORD` und `JWT_SECRET` müssen **identisch** bleiben (das
   Datenbankpasswort steckt in den wiederhergestellten Daten).
2. Neu installieren (diese Anleitung) und die gesicherte `.env` verwenden.
3. Ersteinrichtung durchführen, unter *Backup* das alte Ziel neu anlegen und – falls verschlüsselt – das
   Verschlüsselungspasswort eintragen.
4. Backup wiederherstellen (mit Passwort des neuen Admins und `RESTORE` bestätigen). Danach gelten die Benutzer
   des alten Servers.
5. `docker compose restart host` – der Host lädt damit die wiederhergestellten Schlüssel; anschließend SMTP und
   Backup-Ziel einmal testen.
6. Den alten Server abschalten, damit nicht zwei Instanzen in dasselbe Backup-Ziel schreiben.

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

## Reverse Proxy (nginx)

Wenn der Stack hinter nginx (oder einem anderen Proxy) läuft, **müssen** WebSocket-Upgrades durchgeleitet werden – sonst bricht die Blazor-Verbindung sofort ab:

```nginx
location / {
    proxy_pass http://127.0.0.1:8080;
    proxy_http_version 1.1;
    proxy_set_header Upgrade    $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_read_timeout 3600s;
    proxy_send_timeout 3600s;
    # ... weitere Standard-Header, siehe nginx.example.conf
}
```

Eine vollständige Beispiel-Konfiguration inkl. SSL liegt in `nginx.example.conf`.

## Versionen

Versionen werden aus den Docker Images gezogen (`DOCKER_IMAGE_TAG`).
Aktueller Stand: alle Images auf `:latest`.
