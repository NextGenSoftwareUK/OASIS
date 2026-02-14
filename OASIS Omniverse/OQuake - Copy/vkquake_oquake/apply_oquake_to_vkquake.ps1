<#
.SYNOPSIS
  Copies OQuake + STAR files into vkQuake and patches host.c for console version string.
  Invoked by BUILD_OQUAKE.bat. Manual: .\apply_oquake_to_vkquake.ps1 -VkQuakeSrc "C:\Source\vkQuake"
#>
param([string]$VkQuakeSrc = $env:VKQUAKE_SRC)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OQuakeRoot = Split-Path -Parent $ScriptDir
$NativeWrapper = Join-Path (Split-Path -Parent $OQuakeRoot) "NativeWrapper"
$DoomFolder = Join-Path (Split-Path -Parent $OQuakeRoot) "Doom"

if (-not $VkQuakeSrc -or -not (Test-Path $VkQuakeSrc)) {
    Write-Host "Error: VkQuake source path required. Use -VkQuakeSrc or set VKQUAKE_SRC." -ForegroundColor Red
    exit 1
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

# Copy files
$files = @(
    @{ Src = Join-Path $OQuakeRoot "oquake_star_integration.c"; Dest = "oquake_star_integration.c" },
    @{ Src = Join-Path $OQuakeRoot "oquake_star_integration.h"; Dest = "oquake_star_integration.h" },
    @{ Src = Join-Path $OQuakeRoot "oquake_version.h"; Dest = "oquake_version.h" },
    @{ Src = Join-Path $ScriptDir "pr_ext_oquake.c"; Dest = "pr_ext_oquake.c" },
    @{ Src = Join-Path $NativeWrapper "star_api.h"; Dest = "star_api.h" }
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

# Patch sbar.c (OASIS inventory overlay + anorak face hook)
$SbarC = Join-Path $QuakeDir "sbar.c"
if (Test-Path $SbarC) {
    $sbar = Get-Content $SbarC -Raw
    $sbarPatched = $false

    if ($sbar -notmatch '#include "oquake_star_integration.h"') {
        $sbar = $sbar -replace '(\#include\s+"quakedef\.h")', "`$1`r`n#include `"oquake_star_integration.h`""
        $sbarPatched = $true
    }

    if ($sbar -notmatch 'static qpic_t \*sb_face_anorak;') {
        $sbar = $sbar -replace '(static qpic_t \*sb_face_invis_invuln;\r?\n)', "`$1static qpic_t *sb_face_anorak;`r`n"
        $sbarPatched = $true
    }

    if ($sbar -notmatch 'gfx/face_anorak\.png') {
        $sbar = $sbar -replace '(sb_face_quad = Draw_PicFromWad \("face_quad"\);\r?\n)', "`$1`t`tsb_face_anorak = Draw_TryCachePic (`"gfx/face_anorak.png`", TEXPREF_ALPHA | TEXPREF_PAD | TEXPREF_NOPICMIP);`r`n`t`tif (!sb_face_anorak)`r`n`t`t`tsb_face_anorak = Draw_TryCachePic (`"face_anorak.png`", TEXPREF_ALPHA | TEXPREF_PAD | TEXPREF_NOPICMIP);`r`n`t`tif (!sb_face_anorak)`r`n`t`t`tsb_face_anorak = Draw_TryCachePic (`"gfx/anorak_face.png`", TEXPREF_ALPHA | TEXPREF_PAD | TEXPREF_NOPICMIP);`r`n`t`tif (!sb_face_anorak)`r`n`t`t`tsb_face_anorak = Draw_TryCachePic (`"anorak_face.png`", TEXPREF_ALPHA | TEXPREF_PAD | TEXPREF_NOPICMIP);`r`n`t`tif (!sb_face_anorak)`r`n`t`t`tsb_face_anorak = Draw_PicFromWad (`"face_anorak`");`r`n"
        $sbarPatched = $true
    }

    if ($sbar -notmatch 'OQuake_STAR_ShouldUseAnorakFace \(\)') {
        $sbar = $sbar -replace '(int f, anim;\r?\n)', "`$1`textern qpic_t *pic_nul;`r`n"
        $sbar = $sbar -replace '(// PGM 01/19/97 - team color drawing\r?\n\r?\n)(\s*if \(\(cl\.items & \(IT_INVISIBILITY \| IT_INVULNERABILITY\)\) == \(IT_INVISIBILITY \| IT_INVULNERABILITY\)\))', "`$1`tif (OQuake_STAR_ShouldUseAnorakFace ())`r`n`t{`r`n`t`tif (sb_face_anorak && sb_face_anorak != pic_nul)`r`n`t`t`tSbar_DrawPic (cbx, x, y, sb_face_anorak);`r`n`t`telse`r`n`t`t`tSbar_DrawPic (cbx, x, y, sb_faces[4][0]);`r`n`t`treturn;`r`n`t}`r`n`r`n$2"
        $sbarPatched = $true
    }

    if ($sbar -notmatch 'Sbar_DrawCSCQ \(cbx\);\r?\n\s*GL_SetCanvas \(cbx, CANVAS_DEFAULT\);\r?\n\s*OQuake_STAR_DrawInventoryOverlay \(cbx\);') {
        $sbar = $sbar -replace '(Sbar_DrawCSCQ \(cbx\);\r?\n)(\s*return;)', "`$1`t`tGL_SetCanvas (cbx, CANVAS_DEFAULT);`r`n`t`tOQuake_STAR_DrawInventoryOverlay (cbx);`r`n$2"
        $sbarPatched = $true
    }

    if ($sbar -notmatch 'OQuake_STAR_DrawInventoryOverlay \(cbx\);\r?\n\s*\}') {
        $sbar = $sbar -replace '(if \(scr_style\.value < 2\.0f\)\r?\n\s*Sbar_DrawClassic \(cbx\);\r?\n\s*else\r?\n\s*Sbar_DrawModern \(cbx\);\r?\n)', "`$1`r`n`tGL_SetCanvas (cbx, CANVAS_DEFAULT);`r`n`tOQuake_STAR_DrawInventoryOverlay (cbx);`r`n"
        $sbarPatched = $true
    }

    if ($sbarPatched) {
        Set-Content $SbarC $sbar -NoNewline
    }
}

# Copy Anorak face image to vkQuake game directory
$AnorakFaceSrc = Join-Path $OQuakeRoot "build\id1\gfx\face_anorak.png"
$AnorakFaceDest = Join-Path $QuakeDir "gfx\face_anorak.png"
if (Test-Path $AnorakFaceSrc) {
    $GfxDir = Join-Path $QuakeDir "gfx"
    if (-not (Test-Path $GfxDir)) {
        New-Item -ItemType Directory -Path $GfxDir -Force | Out-Null
    }
    Copy-Item -Path $AnorakFaceSrc -Destination $AnorakFaceDest -Force
    Write-Host "Copied Anorak face to: $AnorakFaceDest"
}