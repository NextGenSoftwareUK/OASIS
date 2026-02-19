# Script to run test harnesses against already running APIs
param(
    [string]$Web4BaseUrl = "http://localhost:5555",
    [string]$Web5BaseUrl = "http://localhost:5556"
)

$ErrorActionPreference = "Stop"

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Running Tests Against Running APIs" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "WEB4 OASIS API: $Web4BaseUrl" -ForegroundColor Yellow
Write-Host "WEB5 STAR API:  $Web5BaseUrl" -ForegroundColor Yellow
Write-Host ""

# Verify APIs are running
Write-Host "Verifying APIs are running..." -ForegroundColor Cyan
try {
    $web4Health = Invoke-WebRequest -Uri "$Web4BaseUrl/api/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    if ($web4Health.StatusCode -eq 200) {
        Write-Host "✓ WEB4 API is running" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ WEB4 API is not responding at $Web4BaseUrl/api/health" -ForegroundColor Red
    exit 1
}

try {
    $web5Health = Invoke-WebRequest -Uri "$Web5BaseUrl/api/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    if ($web5Health.StatusCode -eq 200) {
        Write-Host "✓ WEB5 API is running" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ WEB5 API is not responding at $Web5BaseUrl/api/health" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Running WEB5 STAR API Test Harness" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# Set environment variables for STAR API test harness
$env:STAR_WEBAPI_BASE_URL = $Web5BaseUrl
$env:STARAPI_WEB5_BASE_URL = $Web5BaseUrl
$env:STARAPI_WEB4_BASE_URL = $Web4BaseUrl
$env:STARAPI_HARNESS_MODE = "real-local"
$env:STARAPI_HARNESS_USE_FAKE_SERVER = "false"

Write-Host "Environment variables set:" -ForegroundColor Yellow
Write-Host "  STAR_WEBAPI_BASE_URL = $env:STAR_WEBAPI_BASE_URL" -ForegroundColor Gray

cd "STAR ODK/TestProjects/NextGenSoftware.OASIS.STAR.WebAPI.TestHarness"

# Use Start-Process to ensure environment variables are passed
$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = "dotnet"
$processInfo.Arguments = "run --configuration Release"
$processInfo.WorkingDirectory = Get-Location
$processInfo.UseShellExecute = $false
$processInfo.RedirectStandardOutput = $true
$processInfo.RedirectStandardError = $true
$processInfo.EnvironmentVariables["STAR_WEBAPI_BASE_URL"] = $Web5BaseUrl
$processInfo.EnvironmentVariables["STARAPI_WEB5_BASE_URL"] = $Web5BaseUrl
$processInfo.EnvironmentVariables["STARAPI_WEB4_BASE_URL"] = $Web4BaseUrl

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $processInfo
$process.Start() | Out-Null
$web5Result = $process.StandardOutput.ReadToEnd() + $process.StandardError.ReadToEnd()
$process.WaitForExit()
$web5Result | Select-String -Pattern "(=>|failures|passed|Failed|Passed|Total|200|400|500)" | ForEach-Object { Write-Host $_ }

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Running WEB4 OASIS API Test Harness" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# Set environment variable for WEB4 test harness
$env:OASIS_WEBAPI_BASE_URL = $Web4BaseUrl

Write-Host "Environment variables set:" -ForegroundColor Yellow
Write-Host "  OASIS_WEBAPI_BASE_URL = $env:OASIS_WEBAPI_BASE_URL" -ForegroundColor Gray

cd "../../../ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.WebAPI.TestHarness"

# Use Start-Process to ensure environment variables are passed
$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = "dotnet"
$processInfo.Arguments = "run --configuration Release"
$processInfo.WorkingDirectory = Get-Location
$processInfo.UseShellExecute = $false
$processInfo.RedirectStandardOutput = $true
$processInfo.RedirectStandardError = $true
$processInfo.EnvironmentVariables["OASIS_WEBAPI_BASE_URL"] = $Web4BaseUrl

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $processInfo
$process.Start() | Out-Null
$web4Result = $process.StandardOutput.ReadToEnd() + $process.StandardError.ReadToEnd()
$process.WaitForExit()
$web4Result | Select-String -Pattern "(=>|failures|passed|Failed|Passed|Total|200|400|500)" | ForEach-Object { Write-Host $_ }

Write-Host ""
Write-Host "Test run complete!" -ForegroundColor Green

