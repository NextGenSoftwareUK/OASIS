# Copy OQuake + STAR files into vkQuake Quake folder and patch host.c for OQuake version string.
# pr_ext.c still needs manual edit; see VKQUAKE_OQUAKE_INTEGRATION.md.
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
    @{ Src = Join-Path $OQuakeRoot "oquake_version.h"; Dest = "oquake_version.h" },
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

# Patch host.c so bottom-right and version show "OQuake 1.0 (Build 1) (vkQuake x.x.x)"
$HostC = Join-Path $QuakeDir "host.c"
if (Test-Path $HostC) {
    $content = Get-Content $HostC -Raw
    $patched = $false

    # 1. Add #include "oquake_version.h" if not present (after quakedef.h)
    if ($content -notmatch 'oquake_version\.h') {
        $content = $content -replace '(\#include\s+"quakedef\.h")', "`$1`r`n#include `"oquake_version.h`""
        $patched = $true
    }

    # 2. Replace pr_engine line so display shows OQuake + vkQuake version (try several patterns)
    $prEnginePatched = $false
    if ($content -match 'OQUAKE_VERSION_STR.*ENGINE_NAME_AND_VER') {
        $prEnginePatched = $true  # already patched
    } elseif ($content -match 'static\s+cvar_t\s+pr_engine\s*=\s*\{\s*"pr_engine"\s*,\s*ENGINE_NAME_AND_VER\s*,\s*CVAR_NONE\s*\}\s*;') {
        $content = $content -replace 'static\s+cvar_t\s+pr_engine\s*=\s*\{\s*"pr_engine"\s*,\s*ENGINE_NAME_AND_VER\s*,\s*CVAR_NONE\s*\}\s*;', 'static cvar_t pr_engine = {"pr_engine", OQUAKE_VERSION_STR " (" ENGINE_NAME_AND_VER ")", CVAR_NONE};'
        $prEnginePatched = $true
        $patched = $true
    } elseif ($content -match '"pr_engine",\s*ENGINE_NAME_AND_VER,\s*CVAR_NONE') {
        $content = $content -replace '"pr_engine",\s*ENGINE_NAME_AND_VER,\s*CVAR_NONE', '"pr_engine", OQUAKE_VERSION_STR " (" ENGINE_NAME_AND_VER ")", CVAR_NONE'
        $prEnginePatched = $true
        $patched = $true
    } elseif ($content -match 'pr_engine\s*=\s*\{\s*"pr_engine"\s*,\s*ENGINE_NAME_AND_VER') {
        $content = $content -replace '(\bpr_engine\s*=\s*\{\s*"pr_engine"\s*,\s*)ENGINE_NAME_AND_VER(\s*,\s*CVAR_NONE\s*\})', '${1}OQUAKE_VERSION_STR " (" ENGINE_NAME_AND_VER ")"${2}'
        $prEnginePatched = $true
        $patched = $true
    }

    if ($patched) {
        Set-Content $HostC $content -NoNewline
        Write-Host ""
        Write-Host "Patched host.c: OQuake version in bottom-right (OQuake 1.0 (Build 1) (vkQuake ...))."
        if ($prEnginePatched) {
            Write-Host "  pr_engine updated - do a full Rebuild (Clean then Build) so the exe shows OQuake."
        }
    }
    if (-not $prEnginePatched -and $content -match 'pr_engine') {
        Write-Host ""
        Write-Host "WARNING: host.c pr_engine line not found or already changed. Bottom-right may still show vkQuake."
        Write-Host "  Patch manually - see VKQUAKE_OQUAKE_INTEGRATION.md section 6."
    }
} else {
    Write-Host ""
    Write-Host "host.c not found at $HostC - skip version patch. Apply manually; see VKQUAKE_OQUAKE_INTEGRATION.md."
}

Write-Host ""
Write-Host "Next: edit pr_ext.c (add OQuake builtins), add sources and link star_api.lib. See VKQUAKE_OQUAKE_INTEGRATION.md"
