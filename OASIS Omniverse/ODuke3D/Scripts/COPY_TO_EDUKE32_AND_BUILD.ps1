<#
.SYNOPSIS
    Copies ODuke3D integration files into the EDuke32 source tree,
    then runs Make (MinGW) or MSBuild to build the game executable.

.DESCRIPTION
    Run this after editing oduke3d_star_integration.c/.h, OGLib, or star_api.h.
    The script copies deltas into C:\Source\ODuke3D\source\duke3d\src\ and rebuilds.
    ODuke3D is a fork of EDuke32 (https://eduke32.com).

    Invoked by BUILD_ODUKE3D.bat.
    Manual: .\COPY_TO_EDUKE32_AND_BUILD.ps1 -EDuke32Src "C:\Source\ODuke3D"

.PARAMETER BatchMode
    If set, suppresses interactive prompts (used by BUILD EVERYTHING.bat).

.PARAMETER BuildType
    Release (default) or Debug.

.PARAMETER EDuke32Src
    Path to the ODuke3D (EDuke32 fork) source checkout.
    Defaults to C:\Source\ODuke3D, or $env:EDUKE32_SRC if set.
#>
param(
    [switch]$BatchMode,
    [string]$BuildType   = "Release",
    [string]$EDuke32Src  = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$OmniverseRoot = Split-Path -Parent $PSScriptRoot   # ODuke3D folder
$OGLibSrc      = Join-Path (Split-Path -Parent $OmniverseRoot) "OGLib"
$STARSrc       = Join-Path (Split-Path -Parent $OmniverseRoot) "STARAPIClient"

if ($EDuke32Src -eq "") {
    $EDuke32Src = if ($env:EDUKE32_SRC) { $env:EDUKE32_SRC } else { "C:\Source\ODuke3D" }
}

$Dest = Join-Path $EDuke32Src "source\duke3d\src"

Write-Host "[ODuke3D] Source root : $OmniverseRoot"
Write-Host "[ODuke3D] EDuke32 src : $EDuke32Src"
Write-Host "[ODuke3D] Destination : $Dest"
Write-Host "[ODuke3D] Build type  : $BuildType"

# Verify source exists
if (-not (Test-Path $EDuke32Src)) {
    Write-Error "ODuke3D source (EDuke32 fork) not found at $EDuke32Src.`nClone it first or set EDUKE32_SRC (e.g. C:\Source\ODuke3D)."
}

if (-not (Test-Path $Dest)) {
    Write-Error "EDuke32 destination path not found: $Dest`nEnsure $EDuke32Src is a valid EDuke32 source checkout."
}

# -----------------------------------------------------------------
# 1. Copy integration source files
# -----------------------------------------------------------------
Write-Host "`n[1/4] Copying integration source files..."

$IntegrationFiles = @(
    "oduke3d_star_integration.h",
    "oduke3d_star_integration.c"
)
foreach ($f in $IntegrationFiles) {
    $src = Join-Path $OmniverseRoot $f
    $dst = Join-Path $Dest $f
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-Host "  Copied: $f"
    } else {
        Write-Warning "  Missing: $src"
    }
}

# Also copy oasisstar.json next to the eventual exe
$JsonSrc = Join-Path $OmniverseRoot "oasisstar.json"

# -----------------------------------------------------------------
# 2. Copy OGLib headers into src/OGLib/
# -----------------------------------------------------------------
Write-Host "`n[2/4] Copying OGLib headers..."

$OGLibDest = Join-Path $Dest "OGLib"
if (-not (Test-Path $OGLibDest)) { New-Item -ItemType Directory -Path $OGLibDest | Out-Null }

$OGLibFiles = @(
    "oglib.h",
    "oglib_str.h",
    "oglib_json.h",
    "oglib_crossgame.h",
    "oglib_monster.h",
    "oglib_session.h",
    "oglib_config.h",
    "oglib_beamin.h"
)
foreach ($f in $OGLibFiles) {
    $src = Join-Path $OGLibSrc $f
    $dst = Join-Path $OGLibDest $f
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-Host "  Copied: OGLib\$f"
    } else {
        Write-Warning "  Missing: $src"
    }
}

# -----------------------------------------------------------------
# 3. Copy STAR API headers and library
# -----------------------------------------------------------------
Write-Host "`n[3/4] Copying STAR API files..."

$STARFiles = @("star_api.h", "star_sync.h", "star_sync.c", "star_api.lib", "star_api.dll")
foreach ($f in $STARFiles) {
    $src = Join-Path $STARSrc $f
    $dst = Join-Path $Dest $f
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-Host "  Copied: $f"
    } else {
        Write-Warning "  Missing (may be ok if not yet built): $f"
    }
}

# -----------------------------------------------------------------
# 4. Build with GNU Make (MinGW) — EDuke32's standard build system
# -----------------------------------------------------------------
Write-Host "`n[4/4] Building ODuke3D (EDuke32 + OASIS STAR)..."

$MakePath = (Get-Command "mingw32-make" -ErrorAction SilentlyContinue)?.Source
if (-not $MakePath) {
    $MakePath = (Get-Command "make" -ErrorAction SilentlyContinue)?.Source
}

if ($MakePath) {
    Write-Host "  Using Make: $MakePath"
    Push-Location (Join-Path $EDuke32Src "source\duke3d")
    try {
        & $MakePath EDUKE32_STANDALONE=1 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Error "Make build failed." }
    } finally {
        Pop-Location
    }
} else {
    Write-Warning "GNU Make (MinGW) not found in PATH."
    Write-Warning "To build manually:"
    Write-Warning "  cd `"$EDuke32Src\source\duke3d`""
    Write-Warning "  mingw32-make EDUKE32_STANDALONE=1"
    Write-Warning "Or install MinGW-w64 and add it to PATH."
}

# Deploy oasisstar.json next to exe
$ExeDir  = $EDuke32Src   # EDuke32 puts the exe in the root of the source
$JsonDst = Join-Path $ExeDir "oasisstar.json"
if ((Test-Path $JsonSrc) -and -not (Test-Path $JsonDst)) {
    Copy-Item $JsonSrc $JsonDst -Force
    Write-Host "  Deployed oasisstar.json to $ExeDir"
}

Write-Host "`n[ODuke3D] Build complete."
