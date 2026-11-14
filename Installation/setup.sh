#!/usr/bin/env bash
set -euo pipefail

# Gruppen-ID des Docker-Sockets für die Docker-Steuerung (Modulsteuerung, Restore) eintragen,
# falls in der .env noch keine gesetzt ist.
if [ -f .env ] && ! grep -qE '^DOCKER_GID=[0-9]+' .env; then
  sed -i '/^DOCKER_GID=/d' .env
  echo "DOCKER_GID=$(stat -c %g /var/run/docker.sock 2>/dev/null || echo 0)" >> .env
fi

# JWT-Secret einmalig erzeugen, falls leer. Host und Module signieren und prüfen Anmeldungen damit.
if [ -f .env ] && ! grep -qE '^JWT_SECRET=.{32,}' .env; then
  sed -i '/^JWT_SECRET=/d' .env
  echo "JWT_SECRET=$(openssl rand -base64 32 2>/dev/null || head -c 32 /dev/urandom | base64)" >> .env
fi

docker compose up -d
