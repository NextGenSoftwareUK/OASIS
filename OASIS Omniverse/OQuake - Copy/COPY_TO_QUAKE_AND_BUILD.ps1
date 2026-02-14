# Copy OQuake integration to Quake source and optionally trigger build.
# Usage: .\COPY_TO_QUAKE_AND_BUILD.ps1 [-QuakeSrc "C:\Source\quake-rerelease-qc"] [-VkQuakeSrc "C:\Source\vkQuake"]
# Or set env vars QUAKE_SRC / VKQUAKE_SRC.

param(
    [string] $QuakeSrc = $env:QUAKE_SRC,
    [string] $VkQuakeSrc = $env:VKQUAKE_SRC
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$NativeWrapper = Join-Path (Split-Path -Parent $ScriptDir) "NativeWrapper"
$DoomFolder = Join-Path (Split-Path -Parent $ScriptDir) "Doom"

if (-not $QuakeSrc -or -not (Test-Path $QuakeSrc)) {
    Write-Host "Set QUAKE_SRC or pass -QuakeSrc (e.g. C:\Source\quake-rerelease-qc)"
    exit 1
}

# STAR DLL/LIB
$StarDll = $null
$StarLib = $null
if (Test-Path (Join-Path $DoomFolder "star_api.dll")) {
    $StarDll = Join-Path $DoomFolder "star_api.dll"
    $StarLib = Join-Path $DoomFolder "star_api.lib"
}
$NWRelease = Join-Path $NativeWrapper "build\Release"
if (-not $StarDll -and (Test-Path (Join-Path $NWRelease "star_api.dll"))) {
    $StarDll = Join-Path $NWRelease "star_api.dll"
    $StarLib = Join-Path $NWRelease "star_api.lib"
}

$files = @(
    @{ Src = Join-Path $ScriptDir "oquake_star_integration.c"; Dest = "oquake_star_integration.c" },
    @{ Src = Join-Path $ScriptDir "oquake_star_integration.h"; Dest = "oquake_star_integration.h" },
    @{ Src = Join-Path $ScriptDir "oquake_version.h"; Dest = "oquake_version.h" },
    @{ Src = Join-Path $ScriptDir "engine_oquake_hooks.c.example"; Dest = "engine_oquake_hooks.c.example" },
    @{ Src = Join-Path $ScriptDir "WINDOWS_INTEGRATION.md"; Dest = "WINDOWS_INTEGRATION.md" },
    @{ Src = Join-Path $NativeWrapper "star_api.h"; Dest = "star_api.h" }
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
    Copy-Item -Path $StarDll -Destination (Join-Path $QuakeSrc "star_api.dll") -Force
    Write-Host "  star_api.dll"
}
if ($StarLib) {
    Copy-Item -Path $StarLib -Destination (Join-Path $QuakeSrc "star_api.lib") -Force
    Write-Host "  star_api.lib"
}

Write-Host ""
Write-Host "Done. To build vkQuake, set VKQUAKE_SRC and run BUILD_OQUAKE.bat, or run apply_oquake_to_vkquake.ps1 in vkquake_oquake."
