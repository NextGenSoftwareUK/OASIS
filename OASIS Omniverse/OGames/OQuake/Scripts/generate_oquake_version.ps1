<#
.SYNOPSIS
  Generates oquake_version.h from oquake_version.txt (OQuake's single version source).
  Automatically increments the build number on each run so every build has a unique build number.
#>
param([string]$Root)

$ErrorActionPreference = "Stop"
if (-not $Root -or $Root.Trim() -eq '') { $Root = $PSScriptRoot }
$Root = ($Root -replace '[\r\n\t]', '').Replace('"', '').Trim().TrimEnd('\', '/')
$versionDir = Join-Path $Root "Version"
$codeDir = Join-Path $Root "Code"
$versionTxt = Join-Path $versionDir "oquake_version.txt"
if (-not (Test-Path -LiteralPath $versionDir)) { New-Item -Path $versionDir -ItemType Directory -Force | Out-Null }
if (-not (Test-Path -LiteralPath $versionTxt)) {
    Set-Content -Path $versionTxt -Value "1.0`n1"
}
$rawLines = Get-Content $versionTxt
$commentLines = $rawLines | Where-Object { $_ -match '^\s*#' }
$lines = $rawLines | Where-Object { $_ -notmatch '^\s*#' -and $_.Trim() -ne '' }
$version = if ($lines.Count -gt 0) { ($lines[0] -replace '\s+$', '').Trim() } else { "1.0" }
$build   = if ($lines.Count -gt 1) { ($lines[1] -replace '\s+$', '').Trim() } else { "1" }
if (-not $version) { $version = "1.0" }
if (-not $build)   { $build   = "1" }
# Auto-increment build number on each run
$buildNum = 1
if ($build -match '^\d+$') { $buildNum = [int]$build + 1 }
$build = [string]$buildNum
# Write back version file so next build gets next number
$versionFileContent = if ($commentLines.Count -gt 0) { ($commentLines -join "`r`n") + "`r`n" + $version + "`r`n" + $build } else { $version + "`r`n" + $build }
Set-Content -Path $versionTxt -Value $versionFileContent -NoNewline

if (-not (Test-Path -LiteralPath $codeDir)) { New-Item -Path $codeDir -ItemType Directory -Force | Out-Null }
$hPath = Join-Path $codeDir "oquake_version.h"
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
Set-Content -Path (Join-Path $versionDir "version_display.txt") -Value "$version (Build $build)" -NoNewline
