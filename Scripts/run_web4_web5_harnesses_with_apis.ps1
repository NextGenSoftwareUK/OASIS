# Start WEB4 and WEB5 APIs in serial (WEB4 first, then WEB5), then run both API test harnesses (WEB5 then WEB4). APIs are stopped at the end.
# Use run_web4_web5_harnesses.ps1 if APIs are already running.
# APIs are started on: WEB4 http://localhost:5003, WEB5 http://localhost:5055 (harnesses use these URLs).

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
$web4BaseUrl = "http://localhost:5003"
$web5BaseUrl = "http://localhost:5055"

Push-Location $repoRoot | Out-Null

try {
    Write-Host "Building APIs..." -ForegroundColor Cyan
    dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj" -c Release
    dotnet build "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release

    Write-Host "Starting WEB4 OASIS API (serial: first)..." -ForegroundColor Cyan
    $web4Process = Start-Process -FilePath "dotnet" -ArgumentList "run --no-launch-profile --project `"ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj`" -c Release --urls `"$web4BaseUrl`"" -PassThru -WindowStyle Hidden

    Write-Host "Waiting for WEB4 to be ready..." -ForegroundColor Yellow
    $web4Ready = $false
    for ($i = 0; $i -lt 120; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "$web4BaseUrl/api/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
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

    Write-Host "Starting WEB5 STAR API (serial: second)..." -ForegroundColor Cyan
    # STAR forwards auth to WEB4 and uses test data when live data not available (for harness 200s)
    $env:WEB4_OASIS_API_BASE_URL = $web4BaseUrl
    $env:USE_TEST_DATA_WHEN_LIVE_DATA_NOT_AVAILABLE = "true"
    $web5Process = Start-Process -FilePath "dotnet" -ArgumentList "run --no-launch-profile --project `"STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj`" -c Release --urls `"$web5BaseUrl`"" -PassThru -WindowStyle Hidden

    Write-Host "Waiting for WEB5 to be ready..." -ForegroundColor Yellow
    $web5Ready = $false
    for ($i = 0; $i -lt 120; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "$web5BaseUrl/api/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
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

    # Harness base URLs must match the APIs we started
    $env:STAR_WEBAPI_BASE_URL = $web5BaseUrl
    $env:STARAPI_WEB5_BASE_URL = $web5BaseUrl
    $env:STARAPI_WEB4_BASE_URL = $web4BaseUrl
    $env:OASIS_WEBAPI_BASE_URL = $web4BaseUrl

    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "Running WEB5 STAR API Test Harness" -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Push-Location "STAR ODK/TestProjects/NextGenSoftware.OASIS.STAR.WebAPI.TestHarness" | Out-Null
    $web5Result = dotnet run --configuration Release 2>&1
    $web5Exit = $LASTEXITCODE
    Pop-Location | Out-Null
    $web5Result | Select-String -Pattern "(=>|failures|passed|Failed|Passed|Total|200|400|500)" | ForEach-Object { Write-Host $_ }

    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "Running WEB4 OASIS API Test Harness" -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Push-Location (Join-Path $repoRoot "ONODE\TestProjects\NextGenSoftware.OASIS.API.ONODE.WebAPI.TestHarness") | Out-Null
    $web4Result = dotnet run --configuration Release 2>&1
    $web4Exit = $LASTEXITCODE
    Pop-Location | Out-Null
    $web4Result | Select-String -Pattern "(=>|failures|passed|Failed|Passed|Total|200|400|500)" | ForEach-Object { Write-Host $_ }

    Write-Host ""
    Write-Host "Stopping APIs..." -ForegroundColor Yellow
    Stop-Process -Id $web4Process.Id -Force -ErrorAction SilentlyContinue
    Stop-Process -Id $web5Process.Id -Force -ErrorAction SilentlyContinue

    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  Summary (harnesses: 0 = pass, non‑0 = fail)   " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  WEB5 (STAR):  exit $web5Exit" -ForegroundColor $(if ($web5Exit -eq 0) { 'Green' } else { 'Red' })
    Write-Host "  WEB4 (ONODE): exit $web4Exit" -ForegroundColor $(if ($web4Exit -eq 0) { 'Green' } else { 'Red' })
    Write-Host "==============================================" -ForegroundColor Cyan
    if ($web4Exit -ne 0 -or $web5Exit -ne 0) { exit 1 }
    Write-Host "Test run complete!" -ForegroundColor Green
}
finally {
    Pop-Location | Out-Null
}
