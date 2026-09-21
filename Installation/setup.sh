#!/usr/bin/env bash
set -euo pipefail

# Gruppen-ID des Docker-Sockets für die Docker-Steuerung (Modulsteuerung, Restore) eintragen,
# falls in der .env noch keine gesetzt ist.
if [ -f .env ] && ! grep -qE '^DOCKER_GID=[0-9]+' .env; then
  sed -i '/^DOCKER_GID=/d' .env
  echo "DOCKER_GID=$(stat -c %g /var/run/docker.sock 2>/dev/null || echo 0)" >> .env
fi

docker compose up -d
