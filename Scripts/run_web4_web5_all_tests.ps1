# Run all WEB4 and WEB5 tests in order: unit tests, integration tests, then test harnesses (APIs started for harnesses only).
# Usage: .\Scripts\run_web4_web5_all_tests.ps1 or run_web4_web5_all_tests.bat

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path

function Invoke-Phase {
    param(
        [string]$Name,
        [string]$ScriptPath
    )
    $fullPath = Join-Path $scriptDir $ScriptPath
    if (-not (Test-Path $fullPath)) {
        Write-Host "Error: script not found: $fullPath" -ForegroundColor Red
        return -1
    }
    $p = Start-Process -FilePath "powershell.exe" -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $fullPath -WorkingDirectory $repoRoot -Wait -NoNewWindow -PassThru
    return $p.ExitCode
}

Write-Host ""
Write-Host "##################################################################" -ForegroundColor Cyan
Write-Host "  WEB4 & WEB5 – All tests (unit, integration, harnesses)         " -ForegroundColor Cyan
Write-Host "##################################################################" -ForegroundColor Cyan
Write-Host ""

$unitExit = Invoke-Phase -Name "Unit tests" -ScriptPath "run_web4_web5_unit_tests.ps1"
$unitPass = ($unitExit -eq 0)

Write-Host ""
$integrationExit = Invoke-Phase -Name "Integration tests" -ScriptPath "run_web4_web5_integration_tests.ps1"
$integrationPass = ($integrationExit -eq 0)

Write-Host ""
$harnessesExit = Invoke-Phase -Name "Test harnesses (APIs started then stopped)" -ScriptPath "run_web4_web5_harnesses_with_apis.ps1"
$harnessesPass = ($harnessesExit -eq 0)

Write-Host ""
Write-Host "##################################################################" -ForegroundColor Cyan
Write-Host "  Final summary                                                " -ForegroundColor Cyan
Write-Host "##################################################################" -ForegroundColor Cyan
Write-Host "  Unit tests:        $(if ($unitPass) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(if ($unitPass) { 'Green' } else { 'Red' })
Write-Host "  Integration tests: $(if ($integrationPass) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(if ($integrationPass) { 'Green' } else { 'Red' })
Write-Host "  Test harnesses:    $(if ($harnessesPass) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(if ($harnessesPass) { 'Green' } else { 'Red' })
Write-Host "##################################################################" -ForegroundColor Cyan
Write-Host ""

$overallExit = if ($unitPass -and $integrationPass -and $harnessesPass) { 0 } else { 1 }
exit $overallExit
