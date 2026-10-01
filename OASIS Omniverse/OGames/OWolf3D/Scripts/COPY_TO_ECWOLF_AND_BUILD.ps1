<#
.SYNOPSIS
    Installs the OWolf3D OASIS integration into the ECWolf source tree and builds it.
.DESCRIPTION
    The integration (owolf3d_ogengine_integration.cpp) is built on OGLib/oglib_game.h,
    the ODOOM/OQuake pattern. ECWolf's CMake has an OASIS_STAR_API option (off by default);
    this script turns it on and points OGENGINE_DIR at OGEngineClient.
.PARAMETER BuildType
    Release (default) or Debug
.PARAMETER BatchMode
    Suppress interactive prompts
#>
param(
    [string]$BuildType = "Release",
    [switch]$BatchMode
)

$ErrorActionPreference = "Stop"

$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$GameDir    = Split-Path -Parent $ScriptDir
$Omniverse  = Split-Path -Parent (Split-Path -Parent $GameDir)
$OGLibDir   = Join-Path $Omniverse "OGLib"
$StarDir    = Join-Path $Omniverse "OGEngineClient"
$ECWolfSrc  = if ($env:OWOLF3D_SRC) { $env:OWOLF3D_SRC } else { "C:\Source\OWolf3D" }
$BuildDir   = Join-Path $ECWolfSrc "build-vs"
$SrcDst     = Join-Path $ECWolfSrc "src"
$OasisDst   = Join-Path $SrcDst "oasis"

Write-Host "`n=== OWolf3D - OASIS integration build ===" -ForegroundColor Cyan
Write-Host "ECWolf source : $ECWolfSrc"
Write-Host "Build type    : $BuildType"

if (-not (Test-Path (Join-Path $SrcDst "wl_main.cpp"))) {
    throw "ECWolf source not found at $ECWolfSrc. Set OWOLF3D_SRC or clone to that path."
}

Write-Host "`n[1/3] Installing integration files..." -ForegroundColor Yellow
Copy-Item (Join-Path $GameDir "owolf3d_ogengine_integration.h")   $SrcDst -Force
Copy-Item (Join-Path $GameDir "owolf3d_ogengine_integration.cpp") $SrcDst -Force
if (-not (Test-Path $OasisDst)) { New-Item -ItemType Directory -Path $OasisDst | Out-Null }
foreach ($f in @("ogengine.h", "ogengine_sync.h", "ogengine_sync.c")) {
    Copy-Item (Join-Path $StarDir $f) $OasisDst -Force
}
foreach ($f in @("oglib_game.h", "oglib_config.h", "oglib_edge.h", "oglib_json.h", "oglib_str.h")) {
    Copy-Item (Join-Path $OGLibDir $f) $OasisDst -Force
}

Write-Host "`n[2/3] Configuring CMake (OASIS_STAR_API=ON)..." -ForegroundColor Yellow
& cmake -S $ECWolfSrc -B $BuildDir -A x64 -DGPL=ON -DOASIS_STAR_API=ON "-DOGENGINE_DIR=$StarDir"
if ($LASTEXITCODE -ne 0) { throw "CMake configure failed." }

Write-Host "`n[3/3] Building ($BuildType)..." -ForegroundColor Yellow
& cmake --build $BuildDir --config $BuildType
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$OutDir = Join-Path $BuildDir $BuildType
$dll = Join-Path $StarDir "build\Release\ogengine.dll"
if (Test-Path $dll) { Copy-Item $dll $OutDir -Force; Write-Host "  Deployed ogengine.dll" }
$cfg = Join-Path $GameDir "oasisstar.json"
if ((Test-Path $cfg) -and -not (Test-Path (Join-Path $OutDir "oasisstar.json"))) {
    Copy-Item $cfg $OutDir -Force
    Write-Host "  Deployed default oasisstar.json"
}

Write-Host "`n=== Build succeeded: $OutDir\ecwolf.exe ===" -ForegroundColor Green
Write-Host "Beam in once with:  ecwolf.exe --star `"beamin <user> <pass>`"  (session is then remembered)"
