Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Gruppen-ID des Docker-Sockets für die Docker-Steuerung (Modulsteuerung, Restore): unter Docker
# Desktop reicht 0. Nur eintragen, falls in der .env noch keine gesetzt ist.
if ((Test-Path .env) -and -not (Select-String -Path .env -Pattern '^DOCKER_GID=\d+' -Quiet)) {
    $lines = @(Get-Content .env | Where-Object { $_ -notmatch '^DOCKER_GID=' })
    $lines += 'DOCKER_GID=0'
    Set-Content -Path .env -Value $lines
}

# JWT-Secret einmalig erzeugen, falls leer. Host und Module signieren und prüfen Anmeldungen damit.
if ((Test-Path .env) -and -not (Select-String -Path .env -Pattern '^JWT_SECRET=.{32,}' -Quiet)) {
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $lines = @(Get-Content .env | Where-Object { $_ -notmatch '^JWT_SECRET=' })
    $lines += 'JWT_SECRET=' + [Convert]::ToBase64String($bytes)
    Set-Content -Path .env -Value $lines
}

docker compose up -d
