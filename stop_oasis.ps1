# OASIS Cleanup Script - Stops and removes containers/images
Write-Host "Stopping and removing OASIS Services..." -ForegroundColor Cyan

# Auto-detect Podman
$DOCKER = "docker"
$COMPOSE = "docker-compose"

if (Get-Command "podman" -ErrorAction SilentlyContinue) {
    if (-not (Get-Command "docker" -ErrorAction SilentlyContinue)) {
        Write-Host "Docker not found. Using Podman." -ForegroundColor Yellow
        $DOCKER = "podman"
    }
}
if (Get-Command "podman-compose" -ErrorAction SilentlyContinue) {
    if (-not (Get-Command "docker-compose" -ErrorAction SilentlyContinue)) {
        Write-Host "Docker-compose not found. Using Podman-compose." -ForegroundColor Yellow
        $COMPOSE = "podman-compose"
    }
}

# Helper function to execute command string
function Exec-Compose {
    param($File, $Cmd, $Args)
    if ($COMPOSE -eq "podman-compose") {
        podman-compose -f $File $Cmd $Args
    } else {
        docker-compose -f $File $Cmd $Args
    }
}

# Stop and remove containers
Write-Host "`n1. Stopping STAR API..." -ForegroundColor Yellow
Exec-Compose "docker/star-api/docker-compose.yml" "down" "--volumes"

Write-Host "`n2. Stopping OASIS API..." -ForegroundColor Yellow
Exec-Compose "docker/oasis-api/docker-compose.yml" "down" "--volumes"

Write-Host "`n3. Stopping Infrastructure..." -ForegroundColor Yellow
Exec-Compose "docker/infrastructure/docker-compose.yml" "down" "--volumes"

# Remove custom images
Write-Host "`n4. Removing OASIS images..." -ForegroundColor Yellow
Invoke-Expression "$DOCKER rmi oasis-api:latest star-api:latest oasis-monorepo-base:latest 2>$null"

# Optional: Remove network
Write-Host "`n5. Removing network..." -ForegroundColor Yellow
Invoke-Expression "$DOCKER network rm oasis-network 2>$null"

Write-Host "`n--- OASIS Cleaned Up! ---" -ForegroundColor Green
