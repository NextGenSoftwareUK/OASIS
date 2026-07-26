# Run WEB4 (ONODE) and WEB5 (STAR) API integration tests (dotnet test) in serial.
# Usage: .\Scripts\run_web4_web5_integration_tests.ps1 or run_web4_web5_integration_tests.bat

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
Push-Location $repoRoot | Out-Null

try {
    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  WEB4 & WEB5 API Integration Tests (Serial)   " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host ""

    $web4Project = "ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests\NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests.csproj"
    $web5Project = "STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI.IntegrationTests\NextGenSoftware.OASIS.STAR.WebAPI.IntegrationTests.csproj"

    # 1) WEB4 (ONODE) integration tests
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  WEB4 (ONODE) API Integration Tests          " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "Project: $web4Project" -ForegroundColor Gray
    Write-Host ""

    $web4Result = dotnet test $web4Project -m:1 -v n --logger "console;verbosity=minimal"
    $web4Exit = $LASTEXITCODE

    if ($web4Exit -ne 0) {
        Write-Host "WEB4 integration tests failed (exit code $web4Exit)." -ForegroundColor Red
    } else {
        Write-Host "WEB4 integration tests passed." -ForegroundColor Green
    }
    Write-Host ""

    # 2) WEB5 (STAR) integration tests
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  WEB5 (STAR) API Integration Tests           " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "Project: $web5Project" -ForegroundColor Gray
    Write-Host ""

    $web5Result = dotnet test $web5Project -m:1 -v n --logger "console;verbosity=minimal"
    $web5Exit = $LASTEXITCODE

    if ($web5Exit -ne 0) {
        Write-Host "WEB5 integration tests failed (exit code $web5Exit)." -ForegroundColor Red
    } else {
        Write-Host "WEB5 integration tests passed." -ForegroundColor Green
    }
    Write-Host ""

    # Summary
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  Summary                                     " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  WEB4 (ONODE): $(if ($web4Exit -eq 0) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(if ($web4Exit -eq 0) { 'Green' } else { 'Red' })
    Write-Host "  WEB5 (STAR):  $(if ($web5Exit -eq 0) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(if ($web5Exit -eq 0) { 'Green' } else { 'Red' })
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host ""

    $overallExit = if ($web4Exit -ne 0 -or $web5Exit -ne 0) { 1 } else { 0 }
    exit $overallExit
}
finally {
    Pop-Location | Out-Null
}
