# Script to start both APIs and run test harnesses
$ErrorActionPreference = "Stop"

Write-Host "Building APIs..." -ForegroundColor Cyan
dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj" -c Release
dotnet build "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release

Write-Host "Starting WEB4 OASIS API..." -ForegroundColor Cyan
$web4Process = Start-Process -FilePath "dotnet" -ArgumentList "run --no-launch-profile --project `"ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj`" -c Release --urls `"http://localhost:5003`"" -PassThru -WindowStyle Hidden

Write-Host "Waiting for WEB4 to be ready..." -ForegroundColor Yellow
$web4Ready = $false
for ($i = 0; $i -lt 120; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5003/health" -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $web4Ready = $true
            Write-Host "WEB4 is ready!" -ForegroundColor Green
            break
        }
    } catch {
        Start-Sleep -Seconds 1
    }
}

if (-not $web4Ready) {
    Write-Host "WEB4 failed to start" -ForegroundColor Red
    Stop-Process -Id $web4Process.Id -Force -ErrorAction SilentlyContinue
    exit 1
}

Write-Host "Starting WEB5 STAR API..." -ForegroundColor Cyan
$web5Process = Start-Process -FilePath "dotnet" -ArgumentList "run --no-launch-profile --project `"STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj`" -c Release --urls `"http://localhost:5055`"" -PassThru -WindowStyle Hidden

Write-Host "Waiting for WEB5 to be ready..." -ForegroundColor Yellow
$web5Ready = $false
for ($i = 0; $i -lt 120; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5055/health" -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $web5Ready = $true
            Write-Host "WEB5 is ready!" -ForegroundColor Green
            break
        }
    } catch {
        Start-Sleep -Seconds 1
    }
}

if (-not $web5Ready) {
    Write-Host "WEB5 failed to start" -ForegroundColor Red
    Stop-Process -Id $web4Process.Id -Force -ErrorAction SilentlyContinue
    Stop-Process -Id $web5Process.Id -Force -ErrorAction SilentlyContinue
    exit 1
}

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Running WEB5 STAR API Test Harness" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
cd "STAR ODK/TestProjects/NextGenSoftware.OASIS.STAR.WebAPI.TestHarness"
$web5Result = dotnet run --configuration Release 2>&1
$web5Result | Select-String -Pattern "(=>|failures|passed|Failed|Passed|Total)" | ForEach-Object { Write-Host $_ }

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Running WEB4 OASIS API Test Harness" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
cd "../../../ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.WebAPI.TestHarness"
$web4Result = dotnet run --configuration Release 2>&1
$web4Result | Select-String -Pattern "(=>|failures|passed|Failed|Passed|Total)" | ForEach-Object { Write-Host $_ }

Write-Host ""
Write-Host "Stopping APIs..." -ForegroundColor Yellow
Stop-Process -Id $web4Process.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $web5Process.Id -Force -ErrorAction SilentlyContinue

Write-Host "Test run complete!" -ForegroundColor Green



