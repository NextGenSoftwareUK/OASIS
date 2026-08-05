<#
.SYNOPSIS
    Copies ODOOM3 integration files into the dhewm3 source tree,
    then runs CMake to build the game DLL (base.dll).

.DESCRIPTION
    Run this after editing d3doom3_ogengine_integration.cpp/.h, OGLib, or ogengine.h.
    The script copies deltas into C:\Source\ODOOM3\neo\game\ and rebuilds.

.PARAMETER BatchMode
    If set, suppresses interactive prompts (used by BUILD EVERYTHING.bat).

.PARAMETER BuildType
    Release (default) or Debug.
#>
param(
    [switch]$BatchMode,
    [string]$BuildType = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$OmniverseRoot = Split-Path -Parent $PSScriptRoot   # ODOOM3 folder
$OGamesRoot    = Split-Path -Parent $OmniverseRoot  # OGames folder
$OGLibSrc      = Join-Path (Split-Path -Parent $OGamesRoot) "OGLib"
$STARSrc       = Join-Path (Split-Path -Parent $OGamesRoot) "OGEngineClient"
$DHewm3Root    = "C:\Source\ODOOM3"
$Dest          = Join-Path $DHewm3Root "neo\game"
$BuildDir      = Join-Path $DHewm3Root "build-vs2019-win64"

Write-Host "[ODOOM3] Source root : $OmniverseRoot"
Write-Host "[ODOOM3] Destination : $Dest"
Write-Host "[ODOOM3] Build type  : $BuildType"

# Verify source exists
if (-not (Test-Path $DHewm3Root)) {
    Write-Error "dhewm3 source not found at $DHewm3Root. Clone it first."
}

# -----------------------------------------------------------------
# 1. Copy integration source files
# -----------------------------------------------------------------
Write-Host "`n[1/4] Copying integration source files..."

$IntegrationFiles = @(
    "d3doom3_ogengine_integration.h",
    "d3doom3_ogengine_integration.cpp"
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

# -----------------------------------------------------------------
# 2. Copy OGLib headers into game/OGLib/
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

$STARFiles = @("ogengine.h", "star_sync.h", "ogengine.lib", "ogengine.dll")
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
# 4. Configure and build with CMake (VS 2019)
# -----------------------------------------------------------------
Write-Host "`n[4/4] Building dhewm3 base.dll ($BuildType)..."

if (-not (Test-Path $BuildDir)) {
    Write-Host "  Running CMake configuration..."
    & cmake -S "$DHewm3Root\neo" -B $BuildDir -G "Visual Studio 16 2019" -A x64 `
            -DCMAKE_BUILD_TYPE=$BuildType `
            -DOASIS_STAR_SYNC_IN_CLIENT=1
    if ($LASTEXITCODE -ne 0) { Write-Error "CMake configuration failed." }
}

& cmake --build $BuildDir --config $BuildType --target base -- /m
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed." }

# Deploy ogengine.dll next to the dhewm3 executable
$ExeDir = Join-Path $BuildDir $BuildType
$DllSrc = Join-Path $Dest "ogengine.dll"
if ((Test-Path $DllSrc) -and (Test-Path $ExeDir)) {
    Copy-Item $DllSrc $ExeDir -Force
    Write-Host "  Deployed ogengine.dll to $ExeDir"
}

# Copy oasisstar.json next to exe if not already there
$JsonSrc = Join-Path $OmniverseRoot "oasisstar.json"
$JsonDst = Join-Path $ExeDir "oasisstar.json"
if ((Test-Path $JsonSrc) -and -not (Test-Path $JsonDst)) {
    Copy-Item $JsonSrc $JsonDst -Force
    Write-Host "  Deployed oasisstar.json to $ExeDir"
}

Write-Host "`n[ODOOM3] Build complete."
