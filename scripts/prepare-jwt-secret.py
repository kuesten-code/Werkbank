#!/usr/bin/env python3
"""
Bereitet alle Werkbank-Stacks unterhalb eines Verzeichnisses auf host 2.18.0 vor: jeder Werkbank-Container
(Host und alle Module) bekommt Jwt__Secret=${JWT_SECRET}, und jede .env ein gültiges JWT_SECRET.

Die alten Images ignorieren Jwt__Secret in den Modulen, die Vorbereitung ist also vor dem Update gefahrlos.
Watchtower übernimmt beim Update die Umgebung der laufenden Container, deshalb muss nach dem Anpassen
"docker compose up -d" laufen (macht --apply automatisch).

Aufruf:
  ./prepare-jwt-secret.py /srv/werkbank              Probelauf: zeigt nur, was sich ändern würde
  ./prepare-jwt-secret.py /srv/werkbank --apply      schreibt, startet neu und prüft die Container
  ./prepare-jwt-secret.py /srv/werkbank --apply --no-up   schreibt nur, ohne Neustart

Vorgehen pro Stack (= Unterverzeichnis mit docker-compose.yml / compose.yml):
  - .env: vorhandenes JWT_SECRET bleibt, wenn es >= 32 Zeichen hat und kein bekannter Platzhalter ist
    (sonst werden alle Nutzer abgemeldet). Andernfalls wird ein neues Zufalls-Secret erzeugt.
  - Compose: in jedem Werkbank-Service wird Jwt__Secret ergänzt oder vereinheitlicht.
  - Sicherung als *.bak-<Zeitstempel>, Prüfung mit "docker compose config"; bei Fehler wird zurückgerollt.
"""

import argparse
import difflib
import re
import secrets
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path

WERKBANK_SERVICES = {"host", "faktura", "rapport", "offerte", "acta", "recepta", "saldo", "locatio"}
COMPOSE_NAMES = ("docker-compose.yml", "docker-compose.yaml", "compose.yml", "compose.yaml")
MIN_SECRET_LENGTH = 32
KNOWN_PLACEHOLDERS = {
    "change_me_use_a_long_random_string_here",
    "abcdefghijklmnopqrstuvwxyz123456",
    "dev-only-werkbank-jwt-secret-not-for-production",
}
ENV_VALUE = "${JWT_SECRET:?JWT_SECRET in .env setzen (openssl rand -base64 32)}"

SERVICE_RE = re.compile(r"^(\s+)([A-Za-z0-9_.-]+):\s*(#.*)?$")
KEY_RE = re.compile(r"^(\s+)([A-Za-z0-9_.-]+):(.*)$")


def compose_command() -> list[str]:
    if subprocess.run(["docker", "compose", "version"], capture_output=True).returncode == 0:
        return ["docker", "compose"]
    if shutil.which("docker-compose"):
        return ["docker-compose"]
    sys.exit("Weder 'docker compose' noch 'docker-compose' gefunden.")


def is_werkbank_image(image: str) -> bool:
    name = image.strip().strip("\"'").split("@")[0].rsplit("/", 1)[-1].split(":")[0]
    return name in WERKBANK_SERVICES


def indent_of(line: str) -> int:
    return len(line) - len(line.lstrip(" "))


def is_content(line: str) -> bool:
    stripped = line.strip()
    return bool(stripped) and not stripped.startswith("#")


def service_blocks(lines: list[str]):
    """Liefert (Name, Start, Ende) je Service unter dem Top-Level-Schlüssel services:."""
    try:
        start = next(i for i, l in enumerate(lines) if re.match(r"^services:\s*(#.*)?$", l))
    except StopIteration:
        return
    service_indent = None
    current = None
    for i in range(start + 1, len(lines) + 1):
        line = lines[i] if i < len(lines) else ""
        at_end = i == len(lines) or (is_content(line) and indent_of(line) == 0)
        if not at_end and not is_content(line):
            continue
        if not at_end and service_indent is None:
            service_indent = indent_of(line)
        is_header = not at_end and indent_of(line) == service_indent and SERVICE_RE.match(line)
        if at_end or is_header:
            if current:
                yield current[0], current[1], i
            if at_end:
                return
            current = (SERVICE_RE.match(line).group(2), i)


def add_jwt_secret(text: str) -> tuple[str, list[str]]:
    lines = text.split("\n")
    changed = []
    # Von hinten bearbeiten, damit eingefügte Zeilen die Indizes der übrigen Blöcke nicht verschieben.
    for name, start, end in reversed(list(service_blocks(lines))):
        body = [i for i in range(start + 1, end) if is_content(lines[i])]
        if not body:
            continue
        key_indent = indent_of(lines[body[0]])
        keys = {m.group(2): i for i in body if indent_of(lines[i]) == key_indent and (m := KEY_RE.match(lines[i]))}
        image = KEY_RE.match(lines[keys["image"]]).group(3) if "image" in keys else ""
        build_only = "image" not in keys and "build" in keys
        if not (is_werkbank_image(image) or (build_only and name in WERKBANK_SERVICES)):
            continue

        if "environment" not in keys:
            lines[start + 1:start + 1] = [" " * key_indent + "environment:",
                                          " " * (key_indent + 2) + f"- Jwt__Secret={ENV_VALUE}"]
            changed.append(name)
            continue

        env_line = keys["environment"]
        items = []
        for i in range(env_line + 1, end):
            if not is_content(lines[i]):
                continue
            if indent_of(lines[i]) <= key_indent:
                break
            items.append(i)
        list_style = not items or lines[items[0]].lstrip().startswith("-")
        item_indent = indent_of(lines[items[0]]) if items else key_indent + 2
        wanted = " " * item_indent + (f"- Jwt__Secret={ENV_VALUE}" if list_style else f"Jwt__Secret: {ENV_VALUE}")

        existing = next((i for i in items if re.match(r"""^\s*-?\s*["']?Jwt__Secret\s*[=:]""", lines[i])), None)
        if existing is not None:
            if lines[existing] != wanted:
                lines[existing] = wanted
                changed.append(name)
        else:
            lines.insert((items[-1] if items else env_line) + 1, wanted)
            changed.append(name)
    return "\n".join(lines), sorted(changed)


def read_env_secret(env_text: str) -> str | None:
    for line in env_text.splitlines():
        m = re.match(r"^\s*(?:export\s+)?JWT_SECRET\s*=\s*(.*?)\s*$", line)
        if m:
            return m.group(1).strip("\"'")
    return None


def secret_problem(secret: str | None) -> str | None:
    if not secret:
        return "fehlt"
    if secret in KNOWN_PLACEHOLDERS:
        return "ist ein bekannter Platzhalter"
    if len(secret) < MIN_SECRET_LENGTH:
        return f"ist kürzer als {MIN_SECRET_LENGTH} Zeichen"
    return None


def with_secret(env_text: str, secret: str) -> str:
    lines = [l for l in env_text.splitlines() if not re.match(r"^\s*(?:export\s+)?JWT_SECRET\s*=", l)]
    lines.append(f"JWT_SECRET={secret}")
    return "\n".join(lines) + "\n"


def read_text(path: Path) -> tuple[str, bool]:
    raw = path.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    return raw.decode("utf-8-sig"), bom


def write_text(path: Path, text: str, bom: bool) -> None:
    path.write_bytes((b"\xef\xbb\xbf" if bom else b"") + text.encode("utf-8"))


def show_diff(path: Path, old: str, new: str) -> None:
    sys.stdout.writelines(difflib.unified_diff(
        old.splitlines(keepends=True), new.splitlines(keepends=True), str(path), str(path) + " (neu)"))


def verify_containers(compose: list[str], stack: Path, services: list[str]) -> bool:
    ok = True
    for service in services:
        result = subprocess.run(compose + ["exec", "-T", service, "printenv", "Jwt__Secret"],
                                cwd=stack, capture_output=True, text=True)
        length = len(result.stdout.strip())
        good = result.returncode == 0 and length >= MIN_SECRET_LENGTH
        ok &= good
        print(f"    {service:<8} {'OK' if good else 'FEHLT'} ({length} Zeichen)")
    return ok


def process(stack: Path, compose_file: Path, compose: list[str], apply: bool, run_up: bool) -> bool:
    print(f"\n=== {stack.name}")
    env_path = stack / ".env"
    env_text, env_bom = read_text(env_path) if env_path.exists() else ("", False)
    compose_text, compose_bom = read_text(compose_file)

    problem = secret_problem(read_env_secret(env_text))
    new_env = with_secret(env_text, secrets.token_urlsafe(32)) if problem else env_text
    if problem:
        print(f"  .env: JWT_SECRET {problem} -> neues Zufalls-Secret (Nutzer werden einmal abgemeldet)")
    else:
        print("  .env: JWT_SECRET vorhanden und gültig, bleibt unverändert")

    new_compose, changed = add_jwt_secret(compose_text)
    all_services = sorted(n for n, s, e in service_blocks(compose_text.split("\n")) if n in WERKBANK_SERVICES)
    print(f"  Compose: Jwt__Secret ergänzt/vereinheitlicht in: {', '.join(changed) or '(nichts zu tun)'}")
    if changed:
        show_diff(compose_file, compose_text, new_compose)

    if not apply:
        return True

    stamp = datetime.now().strftime("%Y%m%d%H%M%S")
    backups = []
    created = []
    for path, old, new, bom in ((env_path, env_text, new_env, env_bom), (compose_file, compose_text, new_compose, compose_bom)):
        if old != new:
            if path.exists():
                backup = path.with_name(f"{path.name}.bak-{stamp}")
                shutil.copy2(path, backup)
                backups.append((path, backup))
            else:
                created.append(path)
            write_text(path, new, bom)
            if path == env_path:
                env_path.chmod(0o600)

    check = subprocess.run(compose + ["-f", compose_file.name, "config", "-q"], cwd=stack, capture_output=True, text=True)
    if check.returncode != 0:
        print(f"  FEHLER: docker compose config schlägt fehl, rolle zurück:\n{check.stderr}")
        for path, backup in backups:
            shutil.copy2(backup, path)
        for path in created:
            path.unlink()
        return False

    if not run_up:
        print("  geschrieben, Neustart übersprungen (--no-up). Vor dem Update 'docker compose up -d' ausführen!")
        return True

    up = subprocess.run(compose + ["-f", compose_file.name, "up", "-d"], cwd=stack, capture_output=True, text=True)
    if up.returncode != 0:
        print(f"  FEHLER bei 'up -d':\n{up.stderr}")
        return False
    print("  Container neu erstellt, prüfe Umgebung:")
    return verify_containers(compose + ["-f", compose_file.name], stack, all_services)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("root", type=Path, help="Verzeichnis, dessen Unterverzeichnisse je einen Stack enthalten")
    parser.add_argument("--apply", action="store_true", help="Änderungen schreiben (ohne: Probelauf)")
    parser.add_argument("--no-up", action="store_true", help="mit --apply: nicht neu starten")
    args = parser.parse_args()

    stacks = []
    for directory in sorted(p for p in args.root.iterdir() if p.is_dir()):
        compose_file = next((directory / n for n in COMPOSE_NAMES if (directory / n).exists()), None)
        if compose_file:
            stacks.append((directory, compose_file))
    if not stacks:
        sys.exit(f"Keine Stacks (Unterverzeichnis mit {COMPOSE_NAMES[0]}) unter {args.root} gefunden.")

    compose = compose_command() if args.apply else []
    if not args.apply:
        print("PROBELAUF - es wird nichts geschrieben. Mit --apply ausführen, um die Änderungen anzuwenden.")

    failed = [stack.name for stack, compose_file in stacks
              if not process(stack, compose_file, compose, args.apply, not args.no_up)]

    print(f"\n{len(stacks)} Stack(s) bearbeitet, {len(failed)} mit Fehler{': ' + ', '.join(failed) if failed else ''}.")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
