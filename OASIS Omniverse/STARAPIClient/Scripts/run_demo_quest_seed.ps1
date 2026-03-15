# Run the Demo Quest Seed program to seed the STAR API with demo quests for ODOOM/OQuake.
# Ensure the STAR API (WEB5) and OASIS API (WEB4) are running and set env vars or use defaults:
#   STARAPI_WEB5_BASE_URL (default from StarApiTestDefaults), STARAPI_WEB4_BASE_URL,
#   STARAPI_USERNAME, STARAPI_PASSWORD

param(
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$starApiClientRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
$demoQuestSeedDir = Join-Path $starApiClientRoot "TestProjects\DemoQuestSeed"
$projectPath = Join-Path $demoQuestSeedDir "DemoQuestSeed.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Error "DemoQuestSeed project not found at: $projectPath"
    exit 1
}

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host '  OASIS STAR API - Demo Quest Seed           ' -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

Push-Location $starApiClientRoot | Out-Null
try {
    if (-not $NoBuild) {
        Write-Host "Building DemoQuestSeed ($Configuration)..." -ForegroundColor Yellow
        dotnet build "TestProjects\DemoQuestSeed\DemoQuestSeed.csproj" -c $Configuration --nologo -v q
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed."
            exit $LASTEXITCODE
        }
        Write-Host ""
    }

    Write-Host "Running Demo Quest Seed..." -ForegroundColor Green
    dotnet run --project "TestProjects\DemoQuestSeed\DemoQuestSeed.csproj" -c $Configuration --no-build
    $exitCode = $LASTEXITCODE
    Write-Host ''
    Write-Host 'Press any key to exit...' -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
    exit $exitCode
}
finally {
    Pop-Location | Out-Null
}
