# Start WEB4 and WEB5 APIs (root Scripts), run STARAPIClient test harness against real APIs, then stop APIs.
# Same pattern as root Scripts: start APIs first, run harness, stop APIs.

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$starApiClientRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..\..\..")).Path
$rootScripts = Join-Path $repoRoot "Scripts"
$startPs1 = Join-Path $rootScripts "start_web4_and_web5_apis.ps1"
$stopPs1 = Join-Path $rootScripts "stop_web4_and_web5_apis.ps1"

if (-not (Test-Path $startPs1)) {
    Write-Error "start_web4_and_web5_apis.ps1 not found at: $startPs1"
    exit 1
}
if (-not (Test-Path $stopPs1)) {
    Write-Error "stop_web4_and_web5_apis.ps1 not found at: $stopPs1"
    exit 1
}

$env:STARAPI_HARNESS_USE_FAKE_SERVER = "false"
$exitCode = 0

try {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Starting WEB4 and WEB5 APIs (root)    " -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    & $startPs1 -NoWait -Web4OasisApiBaseUrl "http://localhost:5555" -Web5StarApiBaseUrl "http://localhost:5556"

    Write-Host ""
    Write-Host "Waiting a few seconds for APIs to be ready..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5

    Push-Location $starApiClientRoot | Out-Null

    try {
        Write-Host ""
        Write-Host "Running STAR API Client test harness against real APIs (WEB5 :5556, WEB4 :5555)..." -ForegroundColor Cyan
        dotnet run --project "TestProjects\NextGenSoftware.OASIS.STARAPI.Client.TestHarness\NextGenSoftware.OASIS.STARAPI.Client.TestHarness.csproj" -c $Configuration
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location | Out-Null
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Stopping WEB4 and WEB5 APIs            " -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    & $stopPs1
}
finally {
    if ($exitCode -ne 0) { Write-Host "Test harness failed with exit code $exitCode" -ForegroundColor Red }
    Write-Host ""
    exit $exitCode
}
