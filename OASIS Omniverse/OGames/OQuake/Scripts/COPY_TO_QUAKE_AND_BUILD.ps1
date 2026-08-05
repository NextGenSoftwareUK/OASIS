# Copy OQuake integration to Quake source and optionally trigger build.
# Usage: .\COPY_TO_QUAKE_AND_BUILD.ps1 [-QuakeSrc "C:\Source\quake-rerelease-qc"] [-VkQuakeSrc "C:\Source\vkQuake"]
# Or set env vars QUAKE_SRC / VKQUAKE_SRC.

param(
    [string] $QuakeSrc = $env:QUAKE_SRC,
    [string] $VkQuakeSrc = $env:VKQUAKE_SRC
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OQuakeRoot = Split-Path -Parent $ScriptDir
$OQuakeCode = Join-Path $OQuakeRoot "Code"
$OQuakeDocs = Join-Path $OQuakeRoot "Docs"
$OGEngineClientRoot = Join-Path (Split-Path -Parent $OQuakeRoot) "OGEngineClient"

if (-not $QuakeSrc -or -not (Test-Path $QuakeSrc)) {
    Write-Host "Set QUAKE_SRC or pass -QuakeSrc (e.g. C:\Source\quake-rerelease-qc)"
    exit 1
}

# STAR DLL/LIB (prefer OQuake Code, then OGEngineClient publish)
$StarDll = $null
$StarLib = $null
if (Test-Path (Join-Path $OQuakeCode "ogengine.dll")) {
    $StarDll = Join-Path $OQuakeCode "ogengine.dll"
    $StarLib = Join-Path $OQuakeCode "ogengine.lib"
}
$StarPublish = Join-Path $OGEngineClientRoot "bin\Release\net8.0\win-x64\publish"
if (-not $StarDll -and (Test-Path (Join-Path $StarPublish "ogengine.dll"))) {
    $StarDll = Join-Path $StarPublish "ogengine.dll"
    $StarNative = Join-Path $OGEngineClientRoot "bin\Release\net8.0\win-x64\native"
    if (Test-Path (Join-Path $StarNative "ogengine.lib")) { $StarLib = Join-Path $StarNative "ogengine.lib" }
}

$files = @(
    @{ Src = Join-Path $OQuakeCode "oquake_ogengine_integration.c"; Dest = "oquake_ogengine_integration.c" },
    @{ Src = Join-Path $OQuakeCode "oquake_ogengine_integration.h"; Dest = "oquake_ogengine_integration.h" },
    @{ Src = Join-Path $OQuakeCode "oquake_version.h"; Dest = "oquake_version.h" },
    @{ Src = Join-Path $OQuakeCode "engine_oquake_hooks.c.example"; Dest = "engine_oquake_hooks.c.example" },
    @{ Src = Join-Path $OQuakeDocs "WINDOWS_INTEGRATION.md"; Dest = "WINDOWS_INTEGRATION.md" },
    @{ Src = Join-Path $OGEngineClientRoot "ogengine.h"; Dest = "ogengine.h" }
)

Write-Host "Copying OQuake integration to $QuakeSrc"
foreach ($f in $files) {
    if (Test-Path $f.Src) {
        Copy-Item -Path $f.Src -Destination (Join-Path $QuakeSrc $f.Dest) -Force
        Write-Host "  $($f.Dest)"
    } else {
        Write-Warning "  Missing: $($f.Src)"
    }
}
if ($StarDll) {
    Copy-Item -Path $StarDll -Destination (Join-Path $QuakeSrc "ogengine.dll") -Force
    Write-Host "  ogengine.dll"
}
if ($StarLib) {
    Copy-Item -Path $StarLib -Destination (Join-Path $QuakeSrc "ogengine.lib") -Force
    Write-Host "  ogengine.lib"
}

Write-Host ""
Write-Host "Done. To build vkQuake, set VKQUAKE_SRC and run BUILD_OQUAKE.bat, or run apply_oquake_to_vkquake.ps1 in vkquake_oquake."
