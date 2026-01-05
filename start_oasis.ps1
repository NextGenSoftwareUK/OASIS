# OASIS Startup Script - Uses docker/ folder structure
Write-Host "Starting OASIS Infrastructure..." -ForegroundColor Cyan

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

# 1. Create network if needed
Write-Host "`n1. Ensuring oasis-network exists..." -ForegroundColor Yellow
Invoke-Expression "$DOCKER network inspect oasis-network" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating network..." -ForegroundColor Green
    Invoke-Expression "$DOCKER network create oasis-network"
}

# 2. Build Base Image
Write-Host "`n2. Building Base Image..." -ForegroundColor Yellow
Invoke-Expression "$DOCKER build -f docker/base/Dockerfile -t oasis-monorepo-base:latest ."
if ($LASTEXITCODE -ne 0) { Write-Error "Base build failed"; exit 1 }

# 3. Build OASIS API Image
Write-Host "`n3. Building OASIS API..." -ForegroundColor Yellow
Invoke-Expression "$DOCKER build -f docker/oasis-api/Dockerfile -t oasis-api:latest ."
if ($LASTEXITCODE -ne 0) { Write-Error "OASIS API build failed"; exit 1 }

# 4. Build STAR API Image
Write-Host "`n4. Building STAR API..." -ForegroundColor Yellow
Invoke-Expression "$DOCKER build -f docker/star-api/Dockerfile -t star-api:latest ."
if ($LASTEXITCODE -ne 0) { Write-Error "STAR API build failed"; exit 1 }

# 5. Start Infrastructure (DBs)
Write-Host "`n5. Starting Infrastructure..." -ForegroundColor Yellow
Exec-Compose "docker/infrastructure/docker-compose.yml" "up" "-d"

# 6. Start APIs
Write-Host "`n6. Starting OASIS API..." -ForegroundColor Yellow
Exec-Compose "docker/oasis-api/docker-compose.yml" "up" "-d"

Write-Host "`n7. Starting STAR API..." -ForegroundColor Yellow
Exec-Compose "docker/star-api/docker-compose.yml" "up" "-d"

Write-Host "`n--- OASIS Started! ---" -ForegroundColor Green
Write-Host "OASIS API:     http://localhost:5002"
Write-Host "STAR API:      http://localhost:5003"
Write-Host "Mongo Express: http://localhost:8081"
