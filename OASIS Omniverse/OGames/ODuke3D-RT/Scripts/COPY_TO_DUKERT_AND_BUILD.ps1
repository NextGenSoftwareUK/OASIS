<#
.SYNOPSIS
    Copies ODuke3D-RT integration files into the Duke-RT source tree,
    then runs CMake to build the game executable.

.DESCRIPTION
    Run this after editing oduke3drt_ogengine_integration.c/.h, OGLib, or ogengine.h.
    The script copies deltas into C:\Source\ODuke3D-RT\source\duke3d\src\ and rebuilds.
    ODuke3D-RT is a fork of Duke-RT (https://github.com/fgsfdsfgs/duke-rt),
    a Vulkan ray-tracing modification of EDuke32 (GPL-2.0).

    Invoked by BUILD_ODUKE3DRT.bat.
    Manual: .\COPY_TO_DUKERT_AND_BUILD.ps1 -DukeRTSrc "C:\Source\ODuke3D-RT"

.PARAMETER BatchMode
    If set, suppresses interactive prompts (used by BUILD EVERYTHING.bat).

.PARAMETER BuildType
    Release (default) or Debug.

.PARAMETER DukeRTSrc
    Path to the ODuke3D-RT (Duke-RT fork) source checkout.
    Defaults to C:\Source\ODuke3D-RT, or $env:DUKERT_SRC if set.
#>
param(
    [switch]$BatchMode,
    [string]$BuildType  = "Release",
    [string]$DukeRTSrc = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$OmniverseRoot = Split-Path -Parent $PSScriptRoot   # ODuke3D-RT folder
$OGamesRoot    = Split-Path -Parent $OmniverseRoot  # OGames folder
$OGLibSrc      = Join-Path (Split-Path -Parent $OGamesRoot) "OGLib"
$STARSrc       = Join-Path (Split-Path -Parent $OGamesRoot) "OGEngineClient"

if ($DukeRTSrc -eq "") {
    $DukeRTSrc = if ($env:DUKERT_SRC) { $env:DUKERT_SRC } else { "C:\Source\ODuke3D-RT" }
}

$Dest     = Join-Path $DukeRTSrc "source\duke3d\src"
$BuildDir = Join-Path $DukeRTSrc "build-vs2019-win64"

Write-Host "[ODuke3D-RT] Source root  : $OmniverseRoot"
Write-Host "[ODuke3D-RT] Duke-RT src  : $DukeRTSrc"
Write-Host "[ODuke3D-RT] Destination  : $Dest"
Write-Host "[ODuke3D-RT] Build type   : $BuildType"

if (-not (Test-Path $DukeRTSrc)) {
    Write-Error "ODuke3D-RT source (Duke-RT fork) not found at $DukeRTSrc.`nClone it first or set DUKERT_SRC (e.g. C:\Source\ODuke3D-RT)."
}
if (-not (Test-Path $Dest)) {
    Write-Error "Duke-RT destination path not found: $Dest`nEnsure $DukeRTSrc is a valid Duke-RT / EDuke32 source checkout."
}

# -----------------------------------------------------------------
# 1. Copy integration source files
# -----------------------------------------------------------------
Write-Host "`n[1/4] Copying integration source files..."

foreach ($f in @("oduke3drt_ogengine_integration.h", "oduke3drt_ogengine_integration.c")) {
    $src = Join-Path $OmniverseRoot $f
    $dst = Join-Path $Dest $f
    if (Test-Path $src) { Copy-Item $src $dst -Force; Write-Host "  Copied: $f" }
    else { Write-Warning "  Missing: $src" }
}

$JsonSrc = Join-Path $OmniverseRoot "oasisstar.json"

# -----------------------------------------------------------------
# 2. Copy OGLib headers
# -----------------------------------------------------------------
Write-Host "`n[2/4] Copying OGLib headers..."

$OGLibDest = Join-Path $Dest "OGLib"
if (-not (Test-Path $OGLibDest)) { New-Item -ItemType Directory -Path $OGLibDest | Out-Null }

$OGLibFiles = @("oglib.h","oglib_str.h","oglib_json.h","oglib_crossgame.h",
                "oglib_monster.h","oglib_session.h","oglib_config.h","oglib_beamin.h")
foreach ($f in $OGLibFiles) {
    $src = Join-Path $OGLibSrc $f
    $dst = Join-Path $OGLibDest $f
    if (Test-Path $src) { Copy-Item $src $dst -Force; Write-Host "  Copied: OGLib\$f" }
    else { Write-Warning "  Missing: $src" }
}

# -----------------------------------------------------------------
# 3. Copy STAR API headers and library
# -----------------------------------------------------------------
Write-Host "`n[3/4] Copying STAR API files..."

foreach ($f in @("ogengine.h","star_sync.h","star_sync.c","ogengine.lib","ogengine.dll")) {
    $src = Join-Path $STARSrc $f
    $dst = Join-Path $Dest $f
    if (Test-Path $src) { Copy-Item $src $dst -Force; Write-Host "  Copied: $f" }
    else { Write-Warning "  Missing (may be ok if not yet built): $f" }
}

# -----------------------------------------------------------------
# 4. Configure and build with CMake (Duke-RT uses CMake)
# -----------------------------------------------------------------
Write-Host "`n[4/4] Building ODuke3D-RT ($BuildType)..."

if (-not (Test-Path $BuildDir)) {
    Write-Host "  Running CMake configuration..."
    & cmake -S $DukeRTSrc -B $BuildDir -G "Visual Studio 16 2019" -A x64 `
            -DCMAKE_BUILD_TYPE=$BuildType `
            -DOASIS_STAR_SYNC_IN_CLIENT=1
    if ($LASTEXITCODE -ne 0) { Write-Error "CMake configuration failed." }
}

& cmake --build $BuildDir --config $BuildType -- /m
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed." }

# Deploy ogengine.dll next to exe
$ExeDir = Join-Path $BuildDir $BuildType
$DllSrc = Join-Path $Dest "ogengine.dll"
if ((Test-Path $DllSrc) -and (Test-Path $ExeDir)) {
    Copy-Item $DllSrc $ExeDir -Force
    Write-Host "  Deployed ogengine.dll to $ExeDir"
}

# Deploy oasisstar.json next to exe
$JsonDst = Join-Path $ExeDir "oasisstar.json"
if ((Test-Path $JsonSrc) -and -not (Test-Path $JsonDst)) {
    Copy-Item $JsonSrc $JsonDst -Force
    Write-Host "  Deployed oasisstar.json to $ExeDir"
}

Write-Host "`n[ODuke3D-RT] Build complete."
