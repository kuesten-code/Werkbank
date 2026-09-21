Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Gruppen-ID des Docker-Sockets für die Docker-Steuerung (Modulsteuerung, Restore): unter Docker
# Desktop reicht 0. Nur eintragen, falls in der .env noch keine gesetzt ist.
if ((Test-Path .env) -and -not (Select-String -Path .env -Pattern '^DOCKER_GID=\d+' -Quiet)) {
    $lines = @(Get-Content .env | Where-Object { $_ -notmatch '^DOCKER_GID=' })
    $lines += 'DOCKER_GID=0'
    Set-Content -Path .env -Value $lines
}

docker compose up -d
