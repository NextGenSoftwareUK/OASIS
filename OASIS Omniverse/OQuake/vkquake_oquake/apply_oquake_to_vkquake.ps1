<#
.SYNOPSIS
  Copies OQuake + STAR files into vkQuake and patches host.c for console version string.
  Invoked by BUILD_OQUAKE.bat. Manual: .\apply_oquake_to_vkquake.ps1 -VkQuakeSrc "C:\Source\vkQuake"
#>
param(
    [string]$VkQuakeSrc = $env:VKQUAKE_SRC,
    [string]$QuakeInstallDir = "C:\Program Files (x86)\Steam\steamapps\common\Quake",
    [switch]$SkipQuakeInstallPrompt
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OQuakeRoot = Split-Path -Parent $ScriptDir
$NativeWrapper = Join-Path (Split-Path -Parent $OQuakeRoot) "NativeWrapper"
$DoomFolder = Join-Path (Split-Path -Parent $OQuakeRoot) "Doom"

if (-not $VkQuakeSrc -or -not (Test-Path $VkQuakeSrc)) {
    Write-Host "Error: VkQuake source path required. Use -VkQuakeSrc or set VKQUAKE_SRC." -ForegroundColor Red
    exit 1
}

if (-not $SkipQuakeInstallPrompt) {
    Write-Host ""
    Write-Host "[OQuake] Quake install directory:"
    Write-Host ""
    Write-Host "  $QuakeInstallDir"
    Write-Host ""
    $useDefault = Read-Host "Use this path? [Y]"
    Write-Host ""
    if ($useDefault -match '^(n|no)$') {
        $overridePath = Read-Host "Enter Quake install directory path"
        if ($overridePath -and $overridePath.Trim().Length -gt 0) {
            $QuakeInstallDir = $overridePath.Trim()
        }
    }
}

$QuakeDir = Join-Path $VkQuakeSrc "Quake"
if (-not (Test-Path $QuakeDir)) {
    Write-Host "Error: Quake folder not found: $QuakeDir" -ForegroundColor Red
    exit 1
}

if (Test-Path (Join-Path $OQuakeRoot "generate_oquake_version.ps1")) {
    & (Join-Path $OQuakeRoot "generate_oquake_version.ps1") -Root $OQuakeRoot
}
$versionDisplay = "1.0 (Build 1)"
$versionDisplayPath = Join-Path $OQuakeRoot "version_display.txt"
if (Test-Path $versionDisplayPath) { $versionDisplay = (Get-Content $versionDisplayPath -Raw).Trim() }

# STAR DLL/LIB
$StarDll = $null
$StarLib = $null
if (Test-Path (Join-Path $DoomFolder "star_api.dll")) {
    $StarDll = Join-Path $DoomFolder "star_api.dll"
    $StarLib = Join-Path $DoomFolder "star_api.lib"
}
if (-not $StarDll -and (Test-Path (Join-Path $NativeWrapper "build\Release\star_api.dll"))) {
    $StarDll = Join-Path $NativeWrapper "build\Release\star_api.dll"
    $StarLib = Join-Path $NativeWrapper "build\Release\star_api.lib"
}

$starSyncRoot = $OQuakeRoot
if (-not (Test-Path (Join-Path $OQuakeRoot "star_sync.c"))) {
    $starSyncRoot = Join-Path (Split-Path -Parent $OQuakeRoot) "STARAPIClient"
}
$files = @(
    @{ Src = Join-Path $OQuakeRoot "oquake_star_integration.c"; Dest = "oquake_star_integration.c" },
    @{ Src = Join-Path $OQuakeRoot "oquake_star_integration.h"; Dest = "oquake_star_integration.h" },
    @{ Src = Join-Path $OQuakeRoot "oquake_version.h"; Dest = "oquake_version.h" },
    @{ Src = Join-Path $ScriptDir "pr_ext_oquake.c"; Dest = "pr_ext_oquake.c" },
    @{ Src = Join-Path $NativeWrapper "star_api.h"; Dest = "star_api.h" },
    @{ Src = Join-Path $starSyncRoot "star_sync.c"; Dest = "star_sync.c" },
    @{ Src = Join-Path $starSyncRoot "star_sync.h"; Dest = "star_sync.h" }
)
$copied = 0
foreach ($f in $files) {
    if (Test-Path $f.Src) {
        Copy-Item -Path $f.Src -Destination (Join-Path $QuakeDir $f.Dest) -Force
        $copied++
    }
}
if ($StarDll) {
    Copy-Item -Path $StarDll -Destination (Join-Path $QuakeDir "star_api.dll") -Force
    Copy-Item -Path $StarLib -Destination (Join-Path $QuakeDir "star_api.lib") -Force
    $copied += 2
}

# Copy custom face image into Quake install dir so HUD can load gfx/face_anorak.
$faceSource = Join-Path $OQuakeRoot "face_anorak.png"
if (-not (Test-Path $faceSource)) {
    $altFaceSource = Join-Path $OQuakeRoot "gfx\face_anorak.png"
    if (Test-Path $altFaceSource) {
        $faceSource = $altFaceSource
    }
}
if (Test-Path $faceSource) {
    try {
        $faceDestDir = Join-Path $QuakeInstallDir "id1\gfx"
        New-Item -Path $faceDestDir -ItemType Directory -Force | Out-Null
        Copy-Item -Path $faceSource -Destination (Join-Path $faceDestDir "face_anorak.png") -Force
        Write-Host "[OQuake] Copied face_anorak.png -> $faceDestDir"
    } catch {
        Write-Warning "[OQuake] Failed to copy face_anorak.png to '$QuakeInstallDir\id1\gfx': $($_.Exception.Message)"
    }
} else {
    Write-Warning "[OQuake] face_anorak.png not found in OQuake root. Expected: $faceSource"
}

# Patch host.c (OQuake version in bottom-right)
$HostC = Join-Path $QuakeDir "host.c"
$patched = $false
$prEnginePatched = $false

if (Test-Path $HostC) {
    $content = Get-Content $HostC -Raw

    if ($content -notmatch 'oquake_version\.h') {
        $content = $content -replace '(\#include\s+"quakedef\.h")', "`$1`r`n#include `"oquake_version.h`""
        $patched = $true
    }
    # Call OQuake STAR item poll every frame so pickups are reported even if sbar isn't drawn
    if ($content -notmatch 'OQuake_STAR_PollItems') {
        if ($content -match '(\s+CL_ReadFromServer\s*\(\)\s*;)') {
            $content = $content -replace '(\s+CL_ReadFromServer\s*\(\)\s*;)', "`$1`r`n		OQuake_STAR_PollItems ();"
            $patched = $true
            Write-Host "[OQuake] Patched host.c: added OQuake_STAR_PollItems() call" -ForegroundColor Green
        }
    }

    if ($content -match 'OQUAKE_VERSION_STR.*ENGINE_NAME_AND_VER') {
        $prEnginePatched = $true
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
        if ($prEnginePatched) {
            $buildDirs = @(
                (Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"),
                (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"),
                (Join-Path $VkQuakeSrc "build")
            )
            foreach ($dir in $buildDirs) {
                if (Test-Path $dir) {
                    Remove-Item -Recurse -Force $dir
                    break
                }
            }
        }
    }
}

# Patch vkQuake Visual Studio project to include star_sync.c (fixes LNK2001 unresolved star_sync_*)
$vcxprojPaths = @(
    (Join-Path $VkQuakeSrc "Windows\VisualStudio\vkquake.vcxproj"),
    (Join-Path $VkQuakeSrc "Windows\VisualStudio\Quake\Quake.vcxproj")
)
foreach ($vcxproj in $vcxprojPaths) {
    if (-not (Test-Path $vcxproj)) { continue }
    $projContent = Get-Content $vcxproj -Raw
    if ($projContent -notmatch 'oquake_star_integration\.c') { continue }
    if ($projContent -match 'star_sync\.c') { continue }
    # Add star_sync.c: same path style as oquake_star_integration.c, with PCH disabled (star_sync.c has no quakedef.h)
    if ($projContent -match '<ClCompile\s+Include="([^"]*?)oquake_star_integration\.c"') {
        $pathPrefix = $Matches[1]   # e.g. "..\..\Quake\" or "Quake\"
        $newBlock = @"
    <ClCompile Include="$pathPrefix`star_sync.c">
      <PrecompiledHeader>NotUsing</PrecompiledHeader>
    </ClCompile>
"@
        $projContent = $projContent -replace '(<ClCompile\s+Include="[^"]*oquake_star_integration\.c"\s*/>)', "`$1`r`n$newBlock"
        Set-Content -Path $vcxproj -Value $projContent -NoNewline
        Write-Host "[OQuake] Added star_sync.c to project $(Split-Path -Leaf $vcxproj) (PCH disabled)" -ForegroundColor Green
        break
    }
}
