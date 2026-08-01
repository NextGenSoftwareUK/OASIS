<#
.SYNOPSIS
    Copies ODOOM3-BFG integration files into the RBDOOM-3-BFG source tree,
    then runs CMake to build the game DLL.

.DESCRIPTION
    Run this after editing d3doom_star_integration.cpp/.h, OGLib, or star_api.h.
    The script copies deltas into C:\Source\ODOOM3-BFG\neo\d3xp\ and rebuilds.

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

$OmniverseRoot = Split-Path -Parent $PSScriptRoot   # ODOOM3-BFG folder
$OGLibSrc      = Join-Path (Split-Path -Parent $OmniverseRoot) "OGLib"
$STARSrc       = Join-Path (Split-Path -Parent $OmniverseRoot) "STARAPIClient"
$RBDoomRoot    = "C:\Source\ODOOM3-BFG"
$Dest          = Join-Path $RBDoomRoot "neo\d3xp"
$BuildDir      = Join-Path $RBDoomRoot "build-vs2019-win64"

Write-Host "[ODOOM3-BFG] Source root : $OmniverseRoot"
Write-Host "[ODOOM3-BFG] Destination : $Dest"
Write-Host "[ODOOM3-BFG] Build type  : $BuildType"

# Verify source exists
if (-not (Test-Path $RBDoomRoot)) {
    Write-Error "RBDOOM-3-BFG source not found at $RBDoomRoot. Clone it first."
}

# -----------------------------------------------------------------
# 1. Copy integration source files
# -----------------------------------------------------------------
Write-Host "`n[1/4] Copying integration source files..."

$IntegrationFiles = @(
    "d3doom_star_integration.h",
    "d3doom_star_integration.cpp"
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
# 2. Copy OGLib headers into d3xp/OGLib/
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

$STARFiles = @("star_api.h", "star_sync.h", "star_api.lib", "star_api.dll")
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
Write-Host "`n[4/4] Building RBDOOM-3-BFG ($BuildType)..."

if (-not (Test-Path $BuildDir)) {
    Write-Host "  Running CMake configuration..."
    & cmake -S $RBDoomRoot\neo -B $BuildDir -G "Visual Studio 16 2019" -A x64 `
            -DCMAKE_BUILD_TYPE=$BuildType `
            -DOASIS_STAR_SYNC_IN_CLIENT=1
    if ($LASTEXITCODE -ne 0) { Write-Error "CMake configuration failed." }
}

& cmake --build $BuildDir --config $BuildType --target d3game -- /m
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed." }

# Copy star_api.dll next to the output exe
$ExeDir = Join-Path $BuildDir $BuildType
$DllSrc = Join-Path $Dest "star_api.dll"
if ((Test-Path $DllSrc) -and (Test-Path $ExeDir)) {
    Copy-Item $DllSrc $ExeDir -Force
    Write-Host "  Deployed star_api.dll to $ExeDir"
}

# Copy oasisstar.json next to exe if not already there
$JsonSrc = Join-Path $OmniverseRoot "oasisstar.json"
$JsonDst = Join-Path $ExeDir "oasisstar.json"
if ((Test-Path $JsonSrc) -and -not (Test-Path $JsonDst)) {
    Copy-Item $JsonSrc $JsonDst -Force
    Write-Host "  Deployed oasisstar.json to $ExeDir"
}

Write-Host "`n[ODOOM3-BFG] Build complete."
