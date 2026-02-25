<#
.SYNOPSIS
    Kuestencode Werkbank - Update Script

.DESCRIPTION
    Prueft auf neue Docker Images und aktualisiert die Container

.PARAMETER Check
    Nur pruefen, nicht aktualisieren (pullt trotzdem um zu vergleichen)

.PARAMETER Force
    Update erzwingen, auch wenn keine Aenderungen erkannt wurden

.PARAMETER CheckNew
    Auch nach neuen Modulen suchen

.EXAMPLE
    .\Update-Werkbank.ps1
    Prueft und aktualisiert alle Module

.EXAMPLE
    .\Update-Werkbank.ps1 -Check
    Zeigt nur an, welche Updates verfuegbar sind

.EXAMPLE
    .\Update-Werkbank.ps1 -CheckNew
    Prueft auch auf neue Module
#>

param(
    [switch]$Check,
    [switch]$Force,
    [switch]$CheckNew,
    [string]$DockerUser = "kuestencode",
    [string]$ComposeFile = "docker-compose.yml"
)

# Colors
$Colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error   = "Red"
    Info    = "Cyan"
}

# Known modules
$KnownModules = @("host", "faktura", "rapport", "offerte", "acta", "recepta")


function Write-Header {
    Write-Host ""
    Write-Host "============================================" -ForegroundColor $Colors.Info
    Write-Host "    Kuestencode Werkbank Update Script      " -ForegroundColor $Colors.Info
    Write-Host "============================================" -ForegroundColor $Colors.Info
    Write-Host ""
}

function Get-LocalImageId {
    param([string]$Image)
    $result = docker images --format "{{.ID}}" $Image 2>$null | Select-Object -First 1
    return $result
}

function Test-ImageExists {
    param([string]$Image)
    $null = docker manifest inspect $Image 2>$null
    return $LASTEXITCODE -eq 0
}

# Main
Write-Header

Write-Host "Checking for updates..." -ForegroundColor $Colors.Warning
Write-Host ""

$updatesAvailable = @()
$pullResults = @{}

foreach ($module in $KnownModules) {
    $image = "$DockerUser/${module}:latest"

    Write-Host "  $($module.PadRight(12)) " -NoNewline

    # Check if image exists remotely
    if (-not (Test-ImageExists $image)) {
        Write-Host "not found on Docker Hub" -ForegroundColor $Colors.Error
        continue
    }

    # Get current local ID (before pull)
    $localIdBefore = Get-LocalImageId $image

    # Pull the image
    $pullOutput = docker pull $image 2>&1

    # Get new local ID (after pull)
    $localIdAfter = Get-LocalImageId $image

    # Check if image was updated
    if (-not $localIdBefore) {
        Write-Host "downloaded (new)" -ForegroundColor $Colors.Success
        $updatesAvailable += $module
        $pullResults[$module] = "new"
    }
    elseif ($localIdBefore -ne $localIdAfter) {
        Write-Host "updated" -ForegroundColor $Colors.Success
        $updatesAvailable += $module
        $pullResults[$module] = "updated"
    }
    else {
        Write-Host "up to date" -ForegroundColor "Gray"
        $pullResults[$module] = "current"
    }
}

Write-Host ""

# Check for new modules
if ($CheckNew) {
    Write-Host "Checking for new modules..." -ForegroundColor $Colors.Warning
    Write-Host ""

    $newModules = @()

    foreach ($module in $PotentialModules) {
        $image = "$DockerUser/${module}:latest"

        Write-Host "  $($module.PadRight(12)) " -NoNewline

        if (Test-ImageExists $image) {
            Write-Host "available (new!)" -ForegroundColor $Colors.Success
            $newModules += $module
        }
        else {
            Write-Host "not available" -ForegroundColor "Gray"
        }
    }

    Write-Host ""

    if ($newModules.Count -gt 0) {
        Write-Host "New modules found: $($newModules -join ', ')" -ForegroundColor $Colors.Success
        Write-Host "Add them to your docker-compose.yml to use them." -ForegroundColor $Colors.Warning
        Write-Host ""
    }
}

# Summary
if ($updatesAvailable.Count -eq 0) {
    Write-Host "All modules are up to date!" -ForegroundColor $Colors.Success
    Write-Host ""

    if (-not $Force) {
        exit 0
    }
    else {
        Write-Host "Force mode: restarting containers anyway..." -ForegroundColor $Colors.Warning
    }
}
else {
    Write-Host "Updates pulled for: $($updatesAvailable -join ', ')" -ForegroundColor $Colors.Warning
}

Write-Host ""

# Exit if check only
if ($Check) {
    Write-Host "Check complete. Images have been pulled but containers not restarted." -ForegroundColor $Colors.Info
    Write-Host "Run without -Check to restart containers with new images." -ForegroundColor $Colors.Info
    exit 0
}

# Check if compose file exists
if (-not (Test-Path $ComposeFile)) {
    Write-Host "Compose file not found: $ComposeFile" -ForegroundColor $Colors.Error
    Write-Host "Please restart your containers manually with:" -ForegroundColor $Colors.Warning
    Write-Host "  docker-compose up -d" -ForegroundColor "White"
    exit 1
}

Write-Host "Restarting containers..." -ForegroundColor $Colors.Warning

# Restart with docker-compose
docker-compose -f $ComposeFile up -d

Write-Host ""
Write-Host "============================================" -ForegroundColor $Colors.Success
Write-Host "           Update complete!                 " -ForegroundColor $Colors.Success
Write-Host "============================================" -ForegroundColor $Colors.Success
Write-Host ""

# Show running containers
Write-Host "Running containers:" -ForegroundColor $Colors.Info
docker-compose -f $ComposeFile ps

Write-Host ""
Write-Host "Tip: Check the logs with: docker-compose logs -f" -ForegroundColor $Colors.Warning
