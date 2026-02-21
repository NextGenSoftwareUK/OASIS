<#
.SYNOPSIS
  Generates oquake_version.h from oquake_version.txt (OQuake's single version source).
#>
param([string]$Root)

$ErrorActionPreference = "Stop"
if (-not $Root -or $Root.Trim() -eq '') { $Root = $PSScriptRoot }
$Root = ($Root -replace '[\r\n\t]', '').Replace('"', '').Trim().TrimEnd('\', '/')
$versionTxt = Join-Path $Root "oquake_version.txt"
if (-not (Test-Path -LiteralPath $versionTxt)) {
    Set-Content -Path $versionTxt -Value "1.0`n1"
}
$lines = Get-Content $versionTxt | Where-Object { $_ -notmatch '^\s*#' -and $_.Trim() -ne '' }
$version = if ($lines.Count -gt 0) { ($lines[0] -replace '\s+$', '').Trim() } else { "1.0" }
$build   = if ($lines.Count -gt 1) { ($lines[1] -replace '\s+$', '').Trim() } else { "1" }
if (-not $version) { $version = "1.0" }
if (-not $build)   { $build   = "1" }

$hPath = Join-Path $Root "oquake_version.h"
$content = @"
/**
 * OQuake version - generated from oquake_version.txt. Do not edit.
 */
#ifndef OQUAKE_VERSION_H
#define OQUAKE_VERSION_H

#define OQUAKE_VERSION "$version"
#define OQUAKE_BUILD   "$build"
#define OQUAKE_VERSION_STR "OQuake " OQUAKE_VERSION " (Build " OQUAKE_BUILD ")"

#endif
"@
Set-Content -Path $hPath -Value $content.TrimEnd() -NoNewline
Set-Content -Path (Join-Path $Root "version_display.txt") -Value "$version (Build $build)" -NoNewline
