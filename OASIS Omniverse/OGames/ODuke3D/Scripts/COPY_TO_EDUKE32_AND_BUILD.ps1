<#
.SYNOPSIS
    Installs the ODuke3D OASIS integration into EDuke32 and builds it.
.DESCRIPTION
    The integration (oduke3d_ogengine_integration.cpp) is built on OGLib/oglib_game.h,
    the ODOOM/OQuake pattern. It is opt-in in the EDuke32 project, so this builds
    platform\Windows\eduke32.sln with /p:OasisStarApi=true.
.PARAMETER BuildType
    Release (default) or Debug
#>
param(
    [string]$BuildType = "Release",
    [switch]$BatchMode
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$GameDir   = Split-Path -Parent $ScriptDir
$Omniverse = Split-Path -Parent (Split-Path -Parent $GameDir)
$OGLibDir  = Join-Path $Omniverse "OGLib"
$StarDir   = Join-Path $Omniverse "OGEngineClient"
$EDukeSrc  = if ($env:EDUKE32_SRC) { $env:EDUKE32_SRC } else { "C:\Source\ODuke3D" }
$Dest      = Join-Path $EDukeSrc "source\duke3d\src"
$OasisDst  = Join-Path $Dest "oasis"

Write-Host "`n=== ODuke3D - OASIS integration build ===" -ForegroundColor Cyan
if (-not (Test-Path (Join-Path $Dest "game.cpp"))) {
    throw "EDuke32 source not found at $EDukeSrc. Set EDUKE32_SRC or clone to that path."
}

Write-Host "[1/2] Installing integration files..." -ForegroundColor Yellow
Copy-Item (Join-Path $GameDir "oduke3d_ogengine_integration.h")   $Dest -Force
Copy-Item (Join-Path $GameDir "oduke3d_ogengine_integration.cpp") $Dest -Force
if (-not (Test-Path $OasisDst)) { New-Item -ItemType Directory -Path $OasisDst | Out-Null }
foreach ($f in @("ogengine.h", "ogengine_sync.h", "ogengine_sync.c")) { Copy-Item (Join-Path $StarDir $f) $OasisDst -Force }
foreach ($f in @("oglib_game.h", "oglib_config.h", "oglib_edge.h", "oglib_json.h", "oglib_str.h")) { Copy-Item (Join-Path $OGLibDir $f) $OasisDst -Force }

$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
if (-not $msbuild) { throw "MSBuild not found." }

Write-Host "[2/2] Building eduke32.sln ($BuildType x64, OasisStarApi=true)..." -ForegroundColor Yellow
& $msbuild (Join-Path $EDukeSrc "platform\Windows\eduke32.sln") /m /t:eduke32 "/p:Configuration=$BuildType" /p:Platform=x64 /p:OasisStarApi=true "/p:OgengineDir=$StarDir"
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$dll = Join-Path $StarDir "build\Release\ogengine.dll"
if (Test-Path $dll) { Copy-Item $dll $EDukeSrc -Force; Write-Host "  Deployed ogengine.dll" }
Write-Host "`n=== Build succeeded: $EDukeSrc\eduke32.exe ===" -ForegroundColor Green
Write-Host "In the game console (~): star beamin <user> <pass>"
