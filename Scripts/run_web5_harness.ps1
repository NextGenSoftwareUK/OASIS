# Run only the WEB5 (STAR) API test harness. Optionally pass -Web5BaseUrl (default http://localhost:5556).
# WEB5 API should be running, or use run_web4_web5_harnesses_with_apis.ps1 to start APIs and run both harnesses.

param(
    [string]$Web5BaseUrl = "http://localhost:5556"
)

$ErrorActionPreference = "Continue"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
$harnessPath = Join-Path $repoRoot "STAR ODK\TestProjects\NextGenSoftware.OASIS.STAR.WebAPI.TestHarness"
$resultsDir = Join-Path $repoRoot "STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI\Test Results"

Push-Location $harnessPath | Out-Null
try {
    $output = dotnet run --configuration Release -- --Web5BaseUrl="$Web5BaseUrl" 2>&1
    New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
    $output | Out-File -FilePath (Join-Path $resultsDir "test_results_web5.txt") -Encoding utf8
    Write-Host "Tests completed. Results saved to Test Results directory."
} finally {
    Pop-Location | Out-Null
}
