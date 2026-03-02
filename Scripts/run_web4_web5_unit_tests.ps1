# Run WEB4 (ONODE) and WEB5 (STAR) API unit tests in serial.
# Usage: run from repo root, or .\Scripts\run_web4_web5_unit_tests.ps1

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
Push-Location $repoRoot | Out-Null

try {
    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  WEB4 & WEB5 API Unit Tests (Serial)         " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host ""

    $web4Project = "ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests\NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj"
    $web5Project = "STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI.UnitTests\NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.csproj"

    # 1) WEB4 (ONODE) unit tests
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  WEB4 (ONODE) API Unit Tests                 " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "Project: $web4Project" -ForegroundColor Gray
    Write-Host ""

    $null = dotnet test $web4Project -m:1 -v n --logger "console;verbosity=minimal"
    $web4Exit = $LASTEXITCODE

    if ($web4Exit -ne 0) {
        Write-Host "WEB4 unit tests failed (exit code $web4Exit)." -ForegroundColor Red
    } else {
        Write-Host "WEB4 unit tests passed." -ForegroundColor Green
    }
    Write-Host ""

    # 2) WEB5 (STAR) unit tests
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "  WEB5 (STAR) API Unit Tests                  " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "Project: $web5Project" -ForegroundColor Gray
    Write-Host ""

    $null = dotnet test $web5Project -m:1 -v n --logger "console;verbosity=minimal"
    $web5Exit = $LASTEXITCODE

    if ($web5Exit -ne 0) {
        Write-Host "WEB5 unit tests failed (exit code $web5Exit)." -ForegroundColor Red
    } else {
        Write-Host "WEB5 unit tests passed." -ForegroundColor Green
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
