# Run WEB4 and WEB5 API test harnesses against already-running APIs.
# WEB4 and WEB5 must be running (e.g. start_web4_and_web5_apis.ps1). Uses default URLs unless -Web4BaseUrl / -Web5BaseUrl are set.

param(
    [string]$Web4BaseUrl = "http://localhost:5555",
    [string]$Web5BaseUrl = "http://localhost:5556"
)

$ErrorActionPreference = "Stop"

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Running WEB4 & WEB5 Test Harnesses" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "WEB4 OASIS API: $Web4BaseUrl" -ForegroundColor Yellow
Write-Host "WEB5 STAR API:  $Web5BaseUrl" -ForegroundColor Yellow
Write-Host ""

# Verify APIs are running
Write-Host "Verifying APIs are running..." -ForegroundColor Cyan
try {
    $web4Health = Invoke-WebRequest -Uri "$Web4BaseUrl/api/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    if ($web4Health.StatusCode -eq 200) {
        Write-Host "[OK] WEB4 API is running" -ForegroundColor Green
    }
} catch {
    Write-Host "[FAIL] WEB4 API is not responding at $Web4BaseUrl/api/health" -ForegroundColor Red
    exit 1
}

try {
    $web5Health = Invoke-WebRequest -Uri "$Web5BaseUrl/api/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    if ($web5Health.StatusCode -eq 200) {
        Write-Host "[OK] WEB5 API is running" -ForegroundColor Green
    }
} catch {
    Write-Host "[FAIL] WEB5 API is not responding at $Web5BaseUrl/api/health" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Running WEB5 STAR API Test Harness" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

$env:STAR_WEBAPI_BASE_URL = $Web5BaseUrl
$env:STARAPI_WEB5_BASE_URL = $Web5BaseUrl
$env:STARAPI_WEB4_BASE_URL = $Web4BaseUrl
$env:STARAPI_HARNESS_MODE = "real-local"
$env:STARAPI_HARNESS_USE_FAKE_SERVER = "false"

Write-Host "Environment variables set:" -ForegroundColor Yellow
Write-Host "  STAR_WEBAPI_BASE_URL = $env:STAR_WEBAPI_BASE_URL" -ForegroundColor Gray

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
$testHarnessPath = Join-Path $repoRoot "STAR ODK\TestProjects\NextGenSoftware.OASIS.STAR.WebAPI.TestHarness"
$testResultsDir = Join-Path $repoRoot "STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI\Test Results"
New-Item -ItemType Directory -Path $testResultsDir -Force | Out-Null
$testResultsFile = Join-Path $testResultsDir "test_results_web5.txt"

Push-Location $testHarnessPath | Out-Null
$web5Result = & dotnet run --configuration Release 2>&1 | ForEach-Object { Write-Host $_; $_ }
Pop-Location | Out-Null

$web5Result | Out-File -FilePath $testResultsFile -Encoding utf8
Write-Host "Full test results saved to: $testResultsFile" -ForegroundColor Gray
$web5Result | Select-String -Pattern "(=>|failures|passed|Failed|Passed|Total|200|400|500)" | ForEach-Object { Write-Host $_ }

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Running WEB4 OASIS API Test Harness" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

$env:OASIS_WEBAPI_BASE_URL = $Web4BaseUrl
Write-Host "Environment variables set:" -ForegroundColor Yellow
Write-Host "  OASIS_WEBAPI_BASE_URL = $env:OASIS_WEBAPI_BASE_URL" -ForegroundColor Gray
Write-Host "Web4 harness may take several minutes (hitting all endpoints)..." -ForegroundColor Gray

$web4HarnessPath = Join-Path $repoRoot "ONODE\TestProjects\NextGenSoftware.OASIS.API.ONODE.WebAPI.TestHarness"
$testResultsDirWeb4 = Join-Path $repoRoot "ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\Test Results"
New-Item -ItemType Directory -Path $testResultsDirWeb4 -Force | Out-Null
$testResultsFileWeb4 = Join-Path $testResultsDirWeb4 "test_results_web4.txt"

Push-Location $web4HarnessPath | Out-Null
$web4Result = & dotnet run --configuration Release 2>&1 | ForEach-Object { Write-Host $_; $_ }
Pop-Location | Out-Null

$web4Result | Out-File -FilePath $testResultsFileWeb4 -Encoding utf8
Write-Host "Full test results saved to: $testResultsFileWeb4" -ForegroundColor Gray
$web4Result | Select-String -Pattern "(=>|failures|passed|Failed|Passed|Total|200|400|500)" | ForEach-Object { Write-Host $_ }

Write-Host ""
Write-Host "Test run complete!" -ForegroundColor Green
