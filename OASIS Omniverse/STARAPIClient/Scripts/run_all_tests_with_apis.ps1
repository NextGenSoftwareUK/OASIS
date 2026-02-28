# Start WEB4 and WEB5 APIs (root Scripts), run STARAPIClient unit + integration + harness against real APIs, then stop APIs.
# Same pattern as root Scripts/run_web4_web5_*_with_apis: start APIs first, run tests, stop APIs.

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

$env:STARAPI_INTEGRATION_USE_FAKE = "false"
$env:STARAPI_HARNESS_USE_FAKE_SERVER = "false"
$script:totalExit = 0

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
        Write-Host "========== 1/3 Unit tests ==========" -ForegroundColor Cyan
        dotnet test "TestProjects\NextGenSoftware.OASIS.STARAPI.Client.UnitTests\NextGenSoftware.OASIS.STARAPI.Client.UnitTests.csproj" -c $Configuration --no-restore
        $unitExit = $LASTEXITCODE

        Write-Host ""
        Write-Host "========== 2/3 Integration tests (real APIs :5556, :5555) ==========" -ForegroundColor Cyan
        dotnet test "TestProjects\NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests\NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests.csproj" -c $Configuration --no-restore
        $intExit = $LASTEXITCODE

        Write-Host ""
        Write-Host "========== 3/3 Test harness (real APIs :5556, :5555) ==========" -ForegroundColor Cyan
        dotnet run --project "TestProjects\NextGenSoftware.OASIS.STARAPI.Client.TestHarness\NextGenSoftware.OASIS.STARAPI.Client.TestHarness.csproj" -c $Configuration
        $harnessExit = $LASTEXITCODE

        $script:totalExit = if ($unitExit -ne 0 -or $intExit -ne 0 -or $harnessExit -ne 0) { 1 } else { 0 }
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
    if ($script:totalExit -eq 0) {
        Write-Host "All tests passed." -ForegroundColor Green
    } else {
        Write-Host "One or more test runs failed." -ForegroundColor Red
    }
    Write-Host ""
    exit $script:totalExit
}
