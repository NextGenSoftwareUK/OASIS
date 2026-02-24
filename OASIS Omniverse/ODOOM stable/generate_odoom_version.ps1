<#
.SYNOPSIS
  Generates from odoom_version.txt (ODOOM's single version source):
  odoom_version_generated.h, version_display.txt, and about.txt (launcher About/Release notes).
#>
param([string]$Root)

$ErrorActionPreference = "Stop"
if (-not $Root -or $Root.Trim() -eq '') { $Root = $PSScriptRoot }
$Root = ($Root -replace '[\r\n\t]', '').Replace('"', '').Trim().TrimEnd('\', '/')
$versionTxt = Join-Path $Root "odoom_version.txt"
if (-not (Test-Path -LiteralPath $versionTxt)) {
    Set-Content -Path $versionTxt -Value "1.0`n1"
}
$lines = Get-Content $versionTxt | Where-Object { $_ -notmatch '^\s*#' -and $_.Trim() -ne '' }
$version = if ($lines.Count -gt 0) { ($lines[0] -replace '\s+$', '').Trim() } else { "1.0" }
$build   = if ($lines.Count -gt 1) { ($lines[1] -replace '\s+$', '').Trim() } else { "1" }
if (-not $version) { $version = "1.0" }
if (-not $build)   { $build   = "1" }

$hPath = Join-Path $Root "odoom_version_generated.h"
$content = @"
/**
 * ODOOM version - generated from odoom_version.txt. Do not edit.
 */
#ifndef ODOOM_VERSION_GENERATED_H
#define ODOOM_VERSION_GENERATED_H

#define ODOOM_VERSION "$version"
#define ODOOM_BUILD   "$build"
#define ODOOM_VERSION_STR ODOOM_VERSION " (Build " ODOOM_BUILD ")"

#endif
"@
Set-Content -Path $hPath -Value ($content.TrimEnd() + "`n") -NoNewline
Set-Content -Path (Join-Path $Root "version_display.txt") -Value "$version (Build $build)" -NoNewline

# Launcher about.txt (version from global odoom_version.txt)
$versionDisplay = "$version (Build $build)"
$aboutPath = Join-Path $Root "about.txt"
$aboutContent = @"
# ODOOM

ODOOM is a fork of UZDoom with the OASIS STAR API integrated for cross-game features in the OASIS Omniverse. It uses a native Windows/SDL2 stack with proper sound, music, and mouse handling.

ODOOM adds STAR API integration so keycard pickups are reported to the STAR API, door and lock checks can use cross-game inventory, and keys from other STAR-integrated games (including OQuake) can open doors. Use BUILD ODOOM.bat to build (output: ODOOM.exe in ODOOM\build\) or BUILD & RUN ODOOM.bat to build and launch.

Full credit for the underlying engine goes to the UZDoom project. ODOOM is by NextGen World Ltd. See CREDITS_AND_LICENSE.md in this folder for details.

# Release notes

ODOOM $versionDisplay — First release of ODOOM. UZDoom-based port with OASIS STAR API integration for cross-game features in the OASIS Omniverse. Supports keycard sharing with OQuake, STAR inventory, and interoperable games. By NextGen World Ltd.

UZDoom — The base engine. UZDoom is a modern, feature-rich source port for the classic game DOOM. A continuation of ZDoom and GZDoom. See https://github.com/UZDoom/UZDoom

# Acknowledgments

- id Software for creating the original DOOM and releasing its source code.
- UZDoom project (https://github.com/UZDoom/UZDoom) for the engine ODOOM is based on.
- Marisa Heit for her foundational work on ZDoom, and Christoph Oelckers for his work on GZDoom.
- The DOOM community and all contributors.

# Licensing

ODOOM is a fork of UZDoom. UZDoom is licensed under the GNU General Public License v3.0 (GPL-3.0). When you build or distribute ODOOM, you must comply with UZDoom's license and give appropriate credit. See https://www.gnu.org/licenses/

# Contributors
"@
Set-Content -Path $aboutPath -Value $aboutContent.TrimEnd() -NoNewline
