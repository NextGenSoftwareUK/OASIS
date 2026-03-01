<#
.SYNOPSIS
  Verifies whether UZDoom src/gamedata/a_keys.cpp has the ODOOM STAR door patch (UZDoom_STAR_CheckDoorAccess).
  Run from ODOOM folder: .\Scripts\verify_a_keys_patch.ps1 -UZDOOM_SRC "C:\Source\UZDoom"
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$UZDOOM_SRC
)
$src = $UZDOOM_SRC.TrimEnd('\')
$aKeysCpp = Join-Path $src "src\gamedata\a_keys.cpp"
if (-not (Test-Path $aKeysCpp)) {
    Write-Host "ERROR: File not found: $aKeysCpp" -ForegroundColor Red
    exit 1
}
$content = Get-Content $aKeysCpp -Raw
$hasStar = $content -match 'UZDoom_STAR_CheckDoorAccess'
$hasInclude = $content -match 'uzdoom_star_integration\.h'
Write-Host "File: $aKeysCpp" -ForegroundColor Cyan
Write-Host "  STAR block (UZDoom_STAR_CheckDoorAccess): $(if ($hasStar) { 'YES - patched' } else { 'NO - not patched' })" -ForegroundColor $(if ($hasStar) { 'Green' } else { 'Red' })
Write-Host "  Include (uzdoom_star_integration.h):      $(if ($hasInclude) { 'YES' } else { 'NO' })" -ForegroundColor $(if ($hasInclude) { 'Green' } else { 'Yellow' })
if (-not $hasStar) {
    $hasLockCheck = $content -match 'lock->check\(owner\)'
    $hasQuiet = $content -match 'if \(quiet\) return false'
    Write-Host "  lock->check(owner) present: $(if ($hasLockCheck) { 'YES' } else { 'NO' })" -ForegroundColor $(if ($hasLockCheck) { 'Gray' } else { 'Red' })
    Write-Host "  if (quiet) return false present: $(if ($hasQuiet) { 'YES' } else { 'NO' })" -ForegroundColor $(if ($hasQuiet) { 'Gray' } else { 'Red' })
    $literalLf = " if (lock->check(owner)) return true;`n if (quiet) return false;"
    $literalCrLf = " if (lock->check(owner)) return true;`r`n if (quiet) return false;"
    $wouldMatchLiteralLf = $content.Contains($literalLf)
    $wouldMatchLiteralCrLf = $content.Contains($literalCrLf)
    Write-Host "  Literal match (LF):   $(if ($wouldMatchLiteralLf) { 'YES' } else { 'NO' })" -ForegroundColor Gray
    Write-Host "  Literal match (CRLF): $(if ($wouldMatchLiteralCrLf) { 'YES' } else { 'NO' })" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Run the full patch to apply: .\patch_uzdoom_engine.ps1 -UZDOOM_SRC `"$UZDOOM_SRC`"" -ForegroundColor Yellow
}
exit $(if ($hasStar) { 0 } else { 1 })
