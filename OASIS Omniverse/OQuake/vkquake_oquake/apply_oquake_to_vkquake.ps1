# Copy OQuake + STAR files into vkQuake Quake folder so you can build OQuake.
# Does NOT patch host.c or pr_ext.c; see VKQUAKE_OQUAKE_INTEGRATION.md for manual steps.
#
# Usage: .\apply_oquake_to_vkquake.ps1 -VkQuakeSrc "C:\Source\vkQuake"
# Or set $VkQuakeSrc and run without args.

param(
    [string] $VkQuakeSrc = $env:VKQUAKE_SRC
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OQuakeRoot = Split-Path -Parent $ScriptDir
$NativeWrapper = Split-Path -Parent $OQuakeRoot
$NativeWrapper = Join-Path $NativeWrapper "NativeWrapper"
$DoomFolder = Join-Path (Split-Path -Parent $OQuakeRoot) "Doom"

if (-not $VkQuakeSrc -or -not (Test-Path $VkQuakeSrc)) {
    Write-Host "Usage: .\apply_oquake_to_vkquake.ps1 -VkQuakeSrc C:\Source\vkQuake"
    Write-Host "Or set VKQUAKE_SRC and run again."
    exit 1
}

$QuakeDir = Join-Path $VkQuakeSrc "Quake"
if (-not (Test-Path $QuakeDir)) {
    Write-Host "Quake folder not found: $QuakeDir"
    exit 1
}

# STAR DLL/LIB: prefer Doom folder, then NativeWrapper build
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
    @{ Src = Join-Path $OQuakeRoot "oquake_star_integration.c"; Dest = "oquake_star_integration.c" },
    @{ Src = Join-Path $OQuakeRoot "oquake_star_integration.h"; Dest = "oquake_star_integration.h" },
    @{ Src = Join-Path $ScriptDir "pr_ext_oquake.c"; Dest = "pr_ext_oquake.c" },
    @{ Src = Join-Path $NativeWrapper "star_api.h"; Dest = "star_api.h" }
)

Write-Host "Copying OQuake integration into $QuakeDir"
foreach ($f in $files) {
    if (Test-Path $f.Src) {
        Copy-Item -Path $f.Src -Destination (Join-Path $QuakeDir $f.Dest) -Force
        Write-Host "  $($f.Dest)"
    } else {
        Write-Warning "  Missing: $($f.Src)"
    }
}

if ($StarDll) {
    Copy-Item -Path $StarDll -Destination (Join-Path $QuakeDir "star_api.dll") -Force
    Write-Host "  star_api.dll"
}
if ($StarLib) {
    Copy-Item -Path $StarLib -Destination (Join-Path $QuakeDir "star_api.lib") -Force
    Write-Host "  star_api.lib"
} else {
    Write-Host "  star_api.lib not copied (build NativeWrapper or copy from Doom folder)"
}

Write-Host ""
Write-Host "Next: edit host.c and pr_ext.c, add sources and link star_api.lib. See VKQUAKE_OQUAKE_INTEGRATION.md"
