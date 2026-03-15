<#
.SYNOPSIS
  Copies OQuake + STAR files into vkQuake and patches host.c, pr_ext.c, sbar.c, gl_screen.c, pr_edict.c/pr_cx.c (monster kill hook), and the build (like ODOOM: full automation).
  Invoked by BUILD_OQUAKE.bat. Manual: .\apply_oquake_to_vkquake.ps1 -VkQuakeSrc "C:\Source\vkQuake"
  Use -RevertMonsterHook to remove the ED_Free monster hook (no XP/NFT on kill; use if you get host/program errors).
#>
param(
    [string]$VkQuakeSrc = $env:VKQUAKE_SRC,
    [string]$QuakeInstallDir = "C:\Program Files (x86)\Steam\steamapps\common\Quake",
    [switch]$SkipQuakeInstallPrompt,
    [switch]$RevertMonsterHook
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OQuakeRoot = Split-Path -Parent $ScriptDir
$OasisRoot = Split-Path -Parent $OQuakeRoot
$STARAPIClientRoot = Join-Path $OasisRoot "STARAPIClient"

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

$OQuakeScripts = Join-Path $OQuakeRoot "Scripts"
$OQuakeCode = Join-Path $OQuakeRoot "Code"
$OQuakeVersion = Join-Path $OQuakeRoot "Version"
$OQuakeImages = Join-Path $OQuakeRoot "Images"
if (Test-Path (Join-Path $OQuakeScripts "generate_oquake_version.ps1")) {
    & (Join-Path $OQuakeScripts "generate_oquake_version.ps1") -Root $OQuakeRoot
}
$versionDisplay = "1.0 (Build 1)"
$versionDisplayPath = Join-Path $OQuakeVersion "version_display.txt"
if (Test-Path $versionDisplayPath) { $versionDisplay = (Get-Content $versionDisplayPath -Raw).Trim() }

# STAR DLL/LIB (prefer OQuake Code, then STARAPIClient publish)
$StarDll = $null
$StarLib = $null
if (Test-Path (Join-Path $OQuakeCode "star_api.dll")) {
    $StarDll = Join-Path $OQuakeCode "star_api.dll"
    $StarLib = Join-Path $OQuakeCode "star_api.lib"
    if (-not (Test-Path $StarLib)) { $StarLib = $null }
}
$StarPublishDir = Join-Path $STARAPIClientRoot "bin\Release\net9.0\win-x64\publish"
if (-not $StarDll -and (Test-Path (Join-Path $StarPublishDir "star_api.dll"))) {
    $StarDll = Join-Path $StarPublishDir "star_api.dll"
    $StarLibDir = Join-Path $STARAPIClientRoot "bin\Release\net9.0\win-x64\native"
    $StarLib = Join-Path $StarLibDir "star_api.lib"
    if (-not (Test-Path $StarLib)) { $StarLib = $null }
}

# star_sync: always use OQuake Code copy so OQuake-specific sync behaviour is used. Fallback to STARAPIClient only if missing.
$starSyncRoot = $OQuakeCode
if (-not (Test-Path (Join-Path $OQuakeCode "star_sync.c"))) {
    $starSyncRoot = Join-Path (Split-Path -Parent $OQuakeRoot) "STARAPIClient"
    Write-Warning "[OQuake] OQuake\Code\star_sync.c not found; using STARAPIClient\star_sync.c. Copy star_sync.c into OQuake\Code to avoid overwriting on next apply."
}
$files = @(
    @{ Src = Join-Path $OQuakeCode "oquake_star_integration.c"; Dest = "oquake_star_integration.c" },
    @{ Src = Join-Path $OQuakeCode "oquake_star_integration.h"; Dest = "oquake_star_integration.h" },
    @{ Src = Join-Path $OQuakeCode "oquake_version.h"; Dest = "oquake_version.h" },
    @{ Src = Join-Path $ScriptDir "pr_ext_oquake.c"; Dest = "pr_ext_oquake.c" },
    @{ Src = Join-Path $STARAPIClientRoot "star_api.h"; Dest = "star_api.h" },
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
    $copied++
    if ($StarLib -and (Test-Path $StarLib)) {
        Copy-Item -Path $StarLib -Destination (Join-Path $QuakeDir "star_api.lib") -Force
        $copied++
    }
}

# Copy custom face image into Quake install dir so HUD can load gfx/face_anorak.
# For anorak face when beamed in, inventory overlay (I key), and Send to Avatar/Clan popups,
# you must patch vkQuake's sbar.c and gl_screen.c (or equivalent) as described in VKQUAKE_OQUAKE_INTEGRATION.md section 9.
$faceSource = Join-Path $OQuakeImages "face_anorak.png"
if (-not (Test-Path $faceSource)) {
    $altFaceSource = Join-Path $OQuakeRoot "face_anorak.png"
    if (Test-Path $altFaceSource) { $faceSource = $altFaceSource }
    elseif (Test-Path (Join-Path $OQuakeRoot "gfx\face_anorak.png")) { $faceSource = Join-Path $OQuakeRoot "gfx\face_anorak.png" }
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
    Write-Warning "[OQuake] face_anorak.png not found. Expected: OQuake\Images\face_anorak.png or OQuake\face_anorak.png"
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
    if ($content -notmatch 'oquake_star_integration\.h') {
        $content = $content -replace '(\#include\s+"oquake_version\.h")', "`$1`r`n#include `"oquake_star_integration.h`""
        $patched = $true
        Write-Host "[OQuake] Patched host.c: added #include oquake_star_integration.h" -ForegroundColor Green
    }
    # OQuake STAR init at startup (registers 'star' command and prints welcome message)
    if ($content -notmatch 'OQuake_STAR_Init') {
        if ($content -match '(\s+PR_Init\s*\(\)\s*;)') {
            $content = $content -replace '(\s+PR_Init\s*\(\)\s*;)', "`$1`r`n	OQuake_STAR_Init ();"
            $patched = $true
            Write-Host "[OQuake] Patched host.c: added OQuake_STAR_Init()" -ForegroundColor Green
        }
    }
    # OQuake STAR cleanup at shutdown
    if ($content -notmatch 'OQuake_STAR_Cleanup') {
        if ($content -match 'Host_WriteConfiguration\s*\(\)\s*;') {
            $content = $content -replace '(Host_WriteConfiguration\s*\(\)\s*;\s*\r?\n)(\s*\r?\n)(\s+NET_Shutdown)', "`$1`$2	OQuake_STAR_Cleanup ();`$2`$3"
            $patched = $true
            Write-Host "[OQuake] Patched host.c: added OQuake_STAR_Cleanup()" -ForegroundColor Green
        }
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

# --- pr_ext.c: OQuake builtins (externs + extension table) ---
$PrExtC = Join-Path $QuakeDir "pr_ext.c"
if (Test-Path $PrExtC) {
    $content = Get-Content $PrExtC -Raw
    $prExtPatched = $false
    # Externs: add after PF_sv_localsound (or last PF_ extern before static)
    if ($content -notmatch 'PF_OQuake_OnKeyPickup') {
        if ($content -match '(extern void PF_sv_localsound \(void\);)(\r?\n)') {
            $add = @"

extern void PF_OQuake_OnKeyPickup (void);
extern void PF_OQuake_CheckDoorAccess (void);
extern void PF_OQuake_OnBossKilled (void);
extern void PF_OQuake_OnMonsterKilled (void);
extern void PF_OQuake_OnPickupLeftOnFloor (void);
"@
            $content = $content -replace [regex]::Escape($Matches[1]) + "(\r?\n)", "$Matches[1]$add`$2"
            $prExtPatched = $true
            Write-Host "[OQuake] Patched pr_ext.c: added OQuake builtin externs" -ForegroundColor Green
        }
    } elseif ($content -notmatch 'PF_OQuake_OnPickupLeftOnFloor') {
        $newExtern = "extern void PF_OQuake_OnPickupLeftOnFloor (void);"
        if ($content -match [regex]::Escape("extern void PF_OQuake_OnMonsterKilled (void);") -and $content -notmatch [regex]::Escape($newExtern)) {
            $content = $content -replace '(extern void PF_OQuake_OnMonsterKilled \(void\);)(\r?\n)', "`$1`$2$newExtern`$2"
            $prExtPatched = $true
            Write-Host "[OQuake] Patched pr_ext.c: added PF_OQuake_OnPickupLeftOnFloor extern" -ForegroundColor Green
        }
    }
    # Extension table: add five OQuake entries before the closing }; of extensionbuiltins
    if ($content -notmatch 'ex_OQuake_OnMonsterKilled') {
        $insert = @'

 {"ex_OQuake_OnKeyPickup", PF_OQuake_OnKeyPickup, PF_NoCSQC, 0, "void(string keyname)"},
 {"ex_OQuake_CheckDoorAccess", PF_OQuake_CheckDoorAccess, PF_NoCSQC, 0, "float(string doorname, string requiredkey)"},
 {"ex_OQuake_OnBossKilled", PF_OQuake_OnBossKilled, PF_NoCSQC, 0, "void(string bossname)"},
 {"ex_OQuake_OnMonsterKilled", PF_OQuake_OnMonsterKilled, PF_NoCSQC, 0, "void(string monster_classname)"},
 {"ex_OQuake_OnPickupLeftOnFloor", PF_OQuake_OnPickupLeftOnFloor, PF_NoCSQC, 0, "void(string item_name, string item_type, float quantity)"},
'@
        if ($content -match '"ex_bot_followentity"') {
            $content = $content -replace '(\{\s*"ex_bot_followentity",\s*PF_Fixme,\s*PF_NoCSQC,\s*0,\s*"float\(entity bot, entity goal\)"\s*\},)(\r?\n)(\s*\}\s*;)', "`$1$insert`$2`$3"
            $prExtPatched = $true
            Write-Host "[OQuake] Patched pr_ext.c: added OQuake extension builtin entries" -ForegroundColor Green
        }
    } elseif ($content -notmatch 'ex_OQuake_OnPickupLeftOnFloor') {
        # Already had four OQuake builtins; add the fifth (OnPickupLeftOnFloor)
        if ($content -match '(\{\s*"ex_OQuake_OnMonsterKilled",[^\}]+\},)(\r?\n)(\s*\}\s*;)') {
            $insert = @'

 {"ex_OQuake_OnPickupLeftOnFloor", PF_OQuake_OnPickupLeftOnFloor, PF_NoCSQC, 0, "void(string item_name, string item_type, float quantity)"},
'@
            $content = $content -replace '(\{\s*"ex_OQuake_OnMonsterKilled",[^\}]+\},)(\r?\n)(\s*\}\s*;)', "`$1$insert`$2`$3"
            $prExtPatched = $true
            Write-Host "[OQuake] Patched pr_ext.c: added OQuake_OnPickupLeftOnFloor builtin" -ForegroundColor Green
        }
    }
    if ($prExtPatched) {
        Set-Content $PrExtC $content -NoNewline
        $buildDirs = @(
            (Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"),
            (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"),
            (Join-Path $VkQuakeSrc "build")
        )
        foreach ($dir in $buildDirs) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache (pr_ext.c patched)" -ForegroundColor Yellow; break }
        }
    }
}

# --- sbar.c: anorak face + OQuake include ---
$SbarC = Join-Path $QuakeDir "sbar.c"
if (Test-Path $SbarC) {
    $content = Get-Content $SbarC -Raw
    $sbarPatched = $false
    if ($content -notmatch 'oquake_star_integration\.h') {
        $content = $content -replace '(\#include\s+"quakedef\.h")(\r?\n)', "`$1`$2`r`n#include `"oquake_star_integration.h`"`$2"
        $sbarPatched = $true
        Write-Host "[OQuake] Patched sbar.c: added #include oquake_star_integration.h" -ForegroundColor Green
    }
    if ($content -notmatch 'sb_face_anorak') {
        $content = $content -replace '(static qpic_t \*sb_face_invis_invuln\;)(\r?\n)', "`$1`$2static qpic_t *sb_face_anorak;`$2"
        $sbarPatched = $true
        Write-Host "[OQuake] Patched sbar.c: added sb_face_anorak" -ForegroundColor Green
    }
    if ($content -notmatch 'sb_face_anorak\s*=') {
        $content = $content -replace '(sb_face_quad = Draw_PicFromWad \("face_quad"\);)(\r?\n)', "`$1`$2	sb_face_anorak = Sbar_CheckPicFromWad (`"face_anorak`");`$2"
        $sbarPatched = $true
        Write-Host "[OQuake] Patched sbar.c: load face_anorak in Sbar_LoadPics" -ForegroundColor Green
    }
    if ($content -notmatch 'OQuake_STAR_ShouldUseAnorakFace') {
        # Insert anorak check before "Sbar_DrawPic (cbx, x, y, sb_faces[f][anim]);"
        $content = $content -replace '(\s+)(Sbar_DrawPic \(cbx, x, y, sb_faces\[f\]\[anim\]\);)', "`$1if (OQuake_STAR_ShouldUseAnorakFace () && sb_face_anorak)`r`n`$1{`r`n`$1	Sbar_DrawPic (cbx, x, y, sb_face_anorak);`r`n`$1	return;`r`n`$1}`r`n`$1`$2"
        $sbarPatched = $true
        Write-Host "[OQuake] Patched sbar.c: anorak face when beamed in" -ForegroundColor Green
    }
    if ($sbarPatched) {
        Set-Content $SbarC $content -NoNewline
        foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache (sbar.c patched)" -ForegroundColor Yellow; break }
        }
    }
}

# --- cl_input.c: block movement when quest popup is open (so keys work after close) ---
$ClInputC = Join-Path $QuakeDir "cl_input.c"
if (Test-Path $ClInputC) {
    $content = Get-Content $ClInputC -Raw
    $clInputPatched = $false
    if ($content -notmatch 'oquake_star_integration\.h') {
        $content = $content -replace '(\#include\s+"quakedef\.h")(\r?\n)', "`$1`$2`r`n#include `"oquake_star_integration.h`"`$2"
        $clInputPatched = $true
        Write-Host "[OQuake] Patched cl_input.c: added #include oquake_star_integration.h" -ForegroundColor Green
    }
    # Block movement when quest popup (Q) or inventory popup (I) is open (same as arrow/Home/PgDn: engine just does not use keys). Tab/scoreboard is blocked in keys.c.
    if ($content -notmatch 'OQuake_STAR_IsQuestPopupOpen') {
        if ($content -match 'VectorCopy\s*\(\s*cl\.viewangles\s*,\s*cmd->viewangles\s*\)\s*;') {
            $content = $content -replace '(VectorCopy\s*\(\s*cl\.viewangles\s*,\s*cmd->viewangles\s*\)\s*;\s*\r?\n)(\r?\n)(\s+)(if\s*\(\s*cls\.signon\s*!=\s*SIGNONS\s*\))', "`$1`$2`tif (OQuake_STAR_IsQuestPopupOpen () || OQuake_STAR_IsInventoryPopupOpen ())`r`n`t`treturn;`r`n`$2`$3`$4"
            $clInputPatched = $true
            Write-Host "[OQuake] Patched cl_input.c: block movement when quest or inventory popup open" -ForegroundColor Green
        }
    } elseif ($content -match 'OQuake_STAR_ClearTabKeyIfPopupOpen|keydown\[K_TAB\]\s*=\s*false') {
        # Revert old Tab-clear approach: leave only early return (Tab is now blocked in keys.c)
        $content = $content -replace 'if\s*\(\s*OQuake_STAR_IsQuestPopupOpen\s*\(\s*\)\s*\|\|\s*OQuake_STAR_IsInventoryPopupOpen\s*\(\s*\)\s*\)\s*\{\s*OQuake_STAR_ClearTabKeyIfPopupOpen\s*\(\s*\)\s*;\s*return\s*;\s*\}', 'if (OQuake_STAR_IsQuestPopupOpen () || OQuake_STAR_IsInventoryPopupOpen ()) return;'
        $content = $content -replace 'if\s*\(\s*OQuake_STAR_IsQuestPopupOpen\s*\(\s*\)\s*\|\|\s*OQuake_STAR_IsInventoryPopupOpen\s*\(\s*\)\s*\)\s*\{\s*keydown\[K_TAB\]\s*=\s*false\s*;\s*return\s*;\s*\}', 'if (OQuake_STAR_IsQuestPopupOpen () || OQuake_STAR_IsInventoryPopupOpen ()) return;'
        $clInputPatched = $true
        Write-Host "[OQuake] Patched cl_input.c: removed Tab clear (Tab handled in keys.c)" -ForegroundColor Green
    } elseif ($content -notmatch 'OQuake_STAR_IsInventoryPopupOpen') {
        # Already had quest check; add inventory check so both popups block movement
        $content = $content -replace 'if\s*\(\s*OQuake_STAR_IsQuestPopupOpen\s*\(\s*\)\s*\)', 'if (OQuake_STAR_IsQuestPopupOpen () || OQuake_STAR_IsInventoryPopupOpen ())'
        $clInputPatched = $true
        Write-Host "[OQuake] Patched cl_input.c: added inventory popup to movement block" -ForegroundColor Green
    }
    if ($content -notmatch 'CL_AdjustAngles[\s\S]{0,120}OQuake_STAR_IsQuestPopupOpen') {
        if ($content -match 'void CL_AdjustAngles\s*\(\s*void\s*\)\s*\r?\n\s*\{') {
            $content = $content -replace '(void CL_AdjustAngles\s*\(\s*void\s*\)\s*\r?\n\s*\{\s*\r?\n)(\s+)(float\s+speed;)', "`$1`tif (OQuake_STAR_IsQuestPopupOpen () || OQuake_STAR_IsInventoryPopupOpen ())`r`n`t`treturn;`r`n`r`n`$2`$3"
            $clInputPatched = $true
            Write-Host "[OQuake] Patched cl_input.c: block view angles when quest or inventory popup open" -ForegroundColor Green
        }
    }
    if ($clInputPatched) {
        Set-Content $ClInputC $content -NoNewline
        foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache (cl_input.c patched)" -ForegroundColor Yellow; break }
        }
    }
}

# --- keys.c: skip Tab binding when quest/inventory popup open (same technique as arrow keys in cl_input: do not use key) ---
$KeysC = Join-Path $QuakeDir "keys.c"
if (Test-Path $KeysC) {
    $content = Get-Content $KeysC -Raw
    $keysPatched = $false
    if ($content -notmatch 'oquake_star_integration\.h') {
        $content = $content -replace '(\#include\s+"quakedef\.h")(\r?\n)', "`$1`$2`r`n#include `"oquake_star_integration.h`"`$2"
        $keysPatched = $true
        Write-Host "[OQuake] Patched keys.c: added #include oquake_star_integration.h" -ForegroundColor Green
    }
    if ($content -notmatch 'K_TAB.*OQuake_STAR_IsQuestPopupOpen|OQuake_STAR_IsQuestPopupOpen.*K_TAB') {
        # In Key_Event, when about to run binding for key_game etc., skip Tab so scoreboard does not open (same as not using arrow keys in CL_BaseMove)
        if ($content -match '!consolekeys\[key\]\)\)\)') {
            $orig = $content
            $content = $content -replace '(\)\)\)\s*\r?\n\s*\{\s*\r?\n)(\s*)(kb\s*=\s*keybindings\[key\];)', "`$1`$2if (key == K_TAB && (OQuake_STAR_IsQuestPopupOpen () || OQuake_STAR_IsInventoryPopupOpen ()))`r`n`$2 return;`r`n`$2`$3"
            if ($content -ne $orig) {
                $keysPatched = $true
                Write-Host "[OQuake] Patched keys.c: skip Tab binding when quest or inventory popup open" -ForegroundColor Green
            }
        }
    }
    if ($keysPatched) {
        Set-Content $KeysC $content -NoNewline
        foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache (keys.c patched)" -ForegroundColor Yellow; break }
        }
    }
}

# --- Monster kill hook: hook inside ED_Free() in pr_edict.c so EVERY caller is caught (unconditional, no #ifdef). ---
function Add-MonsterHookInsideEDFree {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_edict.c') { return $false }
    $content = Get-Content $FilePath -Raw
    if ($content -match 'void ED_Free\s*\([^)]*\)\s*\{\s*[\r\n]+\s*OQuake_STAR_OnEntityFreed\s*\(\s*ed\s*\)') { return $false }
    if ($content -match 'void ED_Free\s*\([^)]*\)\s*\{\s*[\r\n]+\s*#ifdef OASIS_STAR_API') {
        $content = $content -replace '(void ED_Free\s*\(\s*edict_t\s*\*\s*ed\s*\)\s*\{\s*)\r?\n\s*#ifdef OASIS_STAR_API\r?\n\s*OQuake_STAR_OnEntityFreed\s*\(\s*ed\s*\)\s*;\r?\n\s*#endif', "`$1`r`n`tOQuake_STAR_OnEntityFreed(ed);"
        Set-Content $FilePath $content -NoNewline
        Write-Host "[OQuake] pr_edict.c: ED_Free hook now unconditional (no #ifdef)" -ForegroundColor Green
        return $true
    }
    if ($content -notmatch 'void ED_Free\s*\(\s*edict_t\s*\*\s*ed\s*\)') { return $false }
    if ($content -notmatch 'oquake_star_integration\.h') {
        $content = $content -replace '(\#include\s+"pr_edict\.h")(\r?\n)', "`$1`$2`r`n#include `"oquake_star_integration.h`"`$2"
        if ($content -notmatch 'oquake_star_integration\.h') {
            $content = $content -replace '(\#include\s+"quakedef\.h")(\r?\n)', "`$1`$2`r`n#include `"oquake_star_integration.h`"`$2"
        }
    }
    $orig = $content
    $content = $content -replace '(void ED_Free\s*\(\s*edict_t\s*\*\s*ed\s*\)\s*\{)(\r?\n)(\s*if\s*\(\s*ed->free\s*)', "`$1`$2`tOQuake_STAR_OnEntityFreed(ed);`r`n`$2`$3"
    if ($content -eq $orig) { return $false }
    Set-Content $FilePath $content -NoNewline
    Write-Host "[OQuake] Patched pr_edict.c: unconditional hook inside ED_Free() (all callers)" -ForegroundColor Green
    return $true
}
function Remove-MonsterHookFromInsideEDFree {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_edict.c') { return $false }
    $content = Get-Content $FilePath -Raw
    if ($content -notmatch 'OQuake_STAR_OnEntityFreed\s*\(\s*ed\s*\)') { return $false }
    $orig = $content
    $content = $content -replace '(\{\s*)\r?\n\s*#ifdef OASIS_STAR_API\r?\n\s*OQuake_STAR_OnEntityFreed\s*\(\s*ed\s*\)\s*;\r?\n\s*#endif\r?\n(\s*if\s*\(\s*ed->free\s*)', "`$1`r`n`$2"
    if ($content -eq $orig) {
        $content = $content -replace '(\{\s*)\r?\n\s*OQuake_STAR_OnEntityFreed\s*\(\s*ed\s*\)\s*;\r?\n(\s*if\s*\(\s*ed->free\s*)', "`$1`r`n`$2"
    }
    if ($content -eq $orig) { return $false }
    Set-Content $FilePath $content -NoNewline
    Write-Host "[OQuake] Removed hook from inside ED_Free() in pr_edict.c" -ForegroundColor Green
    return $true
}

# Hook in pr_cmds.c at PF_sv_makestatic (makestatic). UNCONDITIONAL (no #ifdef) so it always runs regardless of OASIS_STAR_API define.
function Add-MonsterHookInPrCmds {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_cmds.c') { return $false }
    $content = Get-Content $FilePath -Raw
    if ($content -match 'OQuake_STAR_OnEntityFreed\s*\(\s*ent\s*\)') { return $false }
    if ($content -notmatch '// throw the entity away now\s*\r?\n\s*ED_Free\s*\(\s*ent\s*\)') { return $false }
    if ($content -notmatch 'oquake_star_integration\.h') {
        $content = $content -replace '(\#include\s+"quakedef\.h")(\r?\n)', "`$1`$2`r`n#include `"oquake_star_integration.h`"`$2"
    }
    $orig = $content
    $content = $content -replace '(// throw the entity away now\s*\r?\n)(\s*)(ED_Free\s*\(\s*ent\s*\)\s*;)', "`$1`$2OQuake_STAR_OnEntityFreed(ent);`r`n`$2`$3"
    if ($content -eq $orig) { return $false }
    Set-Content $FilePath $content -NoNewline
    Write-Host "[OQuake] Patched pr_cmds.c: unconditional monster hook in PF_sv_makestatic (before ED_Free)" -ForegroundColor Green
    return $true
}
function Remove-MonsterHookFromPrCmds {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_cmds.c') { return $false }
    $content = Get-Content $FilePath -Raw
    if ($content -notmatch 'OQuake_STAR_OnEntityFreed\s*\(\s*ent\s*\)') { return $false }
    $orig = $content
    # Remove unconditional hook (makestatic)
    $content = $content -replace '(// throw the entity away now\s*\r?\n)\s*OQuake_STAR_OnEntityFreed\s*\(\s*ent\s*\)\s*;\r?\n(\s*ED_Free\s*\(\s*ent\s*\)\s*;)', "`$1`$2"
    if ($content -eq $orig) {
        $content = $content -replace '(// throw the entity away now\s*\r?\n)\s*#ifdef OASIS_STAR_API\r?\n\s*OQuake_STAR_OnEntityFreed\s*\(\s*ent\s*\)\s*;\r?\n\s*#endif\r?\n(\s*ED_Free\s*\(\s*ent\s*\)\s*;)', "`$1`$2"
    }
    # Remove PF_Remove hook
    $content = $content -replace '(\s+ed\s*=\s*G_EDICT\s*\(\s*OFS_PARM0\s*\)\s*;\s*\r?\n)\s*OQuake_STAR_OnEntityFreed\s*\(\s*ed\s*\)\s*;[^\r\n]*\r?\n(\s*ED_Free\s*\(\s*ed\s*\)\s*;)', "`$1`$2"
    if ($content -eq $orig) { return $false }
    Set-Content $FilePath $content -NoNewline
    Write-Host "[OQuake] Removed monster hook from pr_cmds.c" -ForegroundColor Green
    return $true
}

# Hook in PF_Remove so we catch monster kills when progs call remove(self) instead of makestatic(self).
function Add-MonsterHookInPF_Remove {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_cmds.c') { return $false }
    $content = Get-Content $FilePath -Raw
    if ($content -match 'ed\s*=\s*G_EDICT\s*\(\s*OFS_PARM0\s*\)\s*;\s*\r?\n\s*OQuake_STAR_OnEntityFreed\s*\(\s*ed\s*\)') { return $false }
    if ($content -notmatch 'ed\s*=\s*G_EDICT\s*\(\s*OFS_PARM0\s*\)\s*;\s*\r?\n\s*ED_Free\s*\(\s*ed\s*\)') { return $false }
    $orig = $content
    $content = $content -replace '(ed\s*=\s*G_EDICT\s*\(\s*OFS_PARM0\s*\)\s*;\s*\r?\n)(\s*)(ED_Free\s*\(\s*ed\s*\)\s*;)', "`$1`$2OQuake_STAR_OnEntityFreed(ed);`r`n`$2`$3"
    if ($content -eq $orig) { return $false }
    Set-Content $FilePath $content -NoNewline
    Write-Host "[OQuake] Patched pr_cmds.c: monster hook in PF_Remove (remove builtin)" -ForegroundColor Green
    return $true
}
function Remove-MonsterHookFromPF_Remove {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_cmds.c') { return $false }
    $content = Get-Content $FilePath -Raw
    if ($content -notmatch 'OQuake_STAR_OnEntityFreed\s*\(\s*ed\s*\)\s*;[^\r\n]*\r?\n\s*ED_Free\s*\(\s*ed\s*\)') { return $false }
    $orig = $content
    $content = $content -replace '(\s+ed\s*=\s*G_EDICT\s*\(\s*OFS_PARM0\s*\)\s*;\s*\r?\n)\s*OQuake_STAR_OnEntityFreed\s*\(\s*ed\s*\)\s*;[^\r\n]*\r?\n(\s*ED_Free\s*\(\s*ed\s*\)\s*;)', "`$1`$2"
    if ($content -eq $orig) { return $false }
    Set-Content $FilePath $content -NoNewline
    Write-Host "[OQuake] Removed monster hook from PF_Remove in pr_cmds.c" -ForegroundColor Green
    return $true
}

# --- Touch pickup at max: intercept in SV_Impact (sv_phys.c) so when player at max health/armor touches a health/armor pickup we add to STAR and remove entity. ---
function Add-SVImpactTouchIntercept {
    $svPhysPaths = @(
        (Join-Path $QuakeDir "sv_phys.c"),
        (Join-Path $VkQuakeSrc "sv_phys.c"),
        (Join-Path $VkQuakeSrc "Quake\sv_phys.c")
    )
    $svPhys = $null
    foreach ($p in $svPhysPaths) {
        if (Test-Path $p) { $svPhys = $p; break }
    }
    if (-not $svPhys) {
        Write-Host "[OQuake] sv_phys.c not found (tried Quake/sv_phys.c under VkQuakeSrc). At-max health pickup to STAR will not work until SV_Impact is patched." -ForegroundColor Yellow
        return $false
    }
    $content = Get-Content $svPhys -Raw
    if ($content -match 'OQuake_STAR_InterceptTouchPickupAtMax') {
        Write-Host "[OQuake] sv_phys.c already has touch intercept (health/armor at max -> STAR), skipping." -ForegroundColor Cyan
        return $true
    }
    if ($content -notmatch 'SV_Impact|PR_ExecuteProgram\s*\(\s*\w+->v\.touch\s*\)') {
        Write-Host "[OQuake] sv_phys.c does not contain expected SV_Impact/touch pattern; skipping patch." -ForegroundColor Yellow
        return $false
    }
    if ($content -notmatch 'oquake_star_integration\.h') {
        $content = $content -replace '(\#include\s+"quakedef\.h")', "`$1`r`n#include `"oquake_star_integration.h`""
    }
    # Already has return-2 handling (detection earlier for at-max health when engine only calls (player,item))
    if ($content -match 'r == 2|oq_done:') {
        Write-Host "[OQuake] sv_phys.c already has touch intercept (return 2 / oq_done), skipping." -ForegroundColor Cyan
        return $true
    }
    $patched = $false
    # New pattern: return 1 = free e1, return 2 = free e2; when (player,item) we return 2 so item is freed and touch is skipped (detection earlier).
    $repl1 = @"
		{
			int r = OQuake_STAR_InterceptTouchPickupAtMax (e1, e2);
			if (r == 1) ED_Free (e1);
			else if (r == 2) { ED_Free (e2); goto oq_done; }
			else PR_ExecuteProgram (e1->v.touch);
		}
"@
    $repl2 = @"
		{
			int r = OQuake_STAR_InterceptTouchPickupAtMax (e2, e1);
			if (r == 1) ED_Free (e2);
			else if (r == 2) ED_Free (e1);
			else PR_ExecuteProgram (e2->v.touch);
		}
"@
    $doneLabel = "`r`noq_done:`r`n"
    # Block 1: replace "if (OQuake... (e1, e2)) ED_Free(e1); else PR_ExecuteProgram(e1->v.touch);" with 3-way
    $pat1 = 'if \(OQuake_STAR_InterceptTouchPickupAtMax \(e1, e2\)\)\r?\n\s*ED_Free \(e1\);\r?\n\s*else\r?\n\s*PR_ExecuteProgram \(e1->v\.touch\);'
    if ($content -match $pat1) {
        $content = $content -replace $pat1, $repl1.TrimEnd()
        $patched = $true
    }
    # Block 2: replace "if (OQuake... (e2, e1)) ED_Free(e2); else PR_ExecuteProgram(e2->v.touch);" with 3-way
    $pat2 = 'if \(OQuake_STAR_InterceptTouchPickupAtMax \(e2, e1\)\)\r?\n\s*ED_Free \(e2\);\r?\n\s*else\r?\n\s*PR_ExecuteProgram \(e2->v\.touch\);'
    if ($content -match $pat2) {
        $content = $content -replace $pat2, $repl2.TrimEnd()
        $patched = $true
    }
    # Add oq_done label before pr_global_struct->self = old_self (only if we patched and not already present)
    if ($patched -and $content -notmatch 'oq_done:') {
        $content = $content -replace '(\r?\n\s*)(pr_global_struct->self = old_self;)', "$doneLabel`$1`$2"
    }
    if ($patched -and $content -match 'OQuake_STAR_InterceptTouchPickupAtMax') {
        Set-Content $svPhys $content -NoNewline
        Write-Host "[OQuake] Patched sv_phys.c: SV_Impact touch intercept (return 1/2, at-max and use_X=0 -> STAR)" -ForegroundColor Green
        return $true
    }
    # Fallback: original pattern (no intercept yet) - replace PR_ExecuteProgram with intercept + new 3-way
    $repl1orig = " if (OQuake_STAR_InterceptTouchPickupAtMax (e1, e2))`r`n ED_Free (e1);`r`n else`r`n PR_ExecuteProgram (e1->v.touch);"
    $repl2orig = " if (OQuake_STAR_InterceptTouchPickupAtMax (e2, e1))`r`n ED_Free (e2);`r`n else`r`n PR_ExecuteProgram (e2->v.touch);"
    $pat1b = '(pr_global_struct->self\s*=\s*EDICT_TO_PROG\s*\(\s*e1\s*\)\s*;\s*\r?\n\s*pr_global_struct->other\s*=\s*EDICT_TO_PROG\s*\(\s*e2\s*\)\s*;\s*\r?\n\s*)PR_ExecuteProgram\s*\(\s*e1->v\.touch\s*\)\s*;'
    $pat2b = '(pr_global_struct->self\s*=\s*EDICT_TO_PROG\s*\(\s*e2\s*\)\s*;\s*\r?\n\s*pr_global_struct->other\s*=\s*EDICT_TO_PROG\s*\(\s*e1\s*\)\s*;\s*\r?\n\s*)PR_ExecuteProgram\s*\(\s*e2->v\.touch\s*\)\s*;'
    if ($content -match $pat1b -and $content -notmatch 'OQuake_STAR_InterceptTouchPickupAtMax') {
        $content = $content -replace $pat1b, "`$1$repl1"
        $content = $content -replace $repl1, $repl1.TrimEnd()
        $patched = $true
    }
    if ($content -match $pat2b -and $content -notmatch 'r == 2') {
        $content = $content -replace $pat2b, "`$1$repl2"
        $content = $content -replace '(\r?\n\s*)(pr_global_struct->self = old_self;)', "$doneLabel`$1`$2"
        $patched = $true
    }
    Write-Host "[OQuake] Could not apply SV_Impact patch (pattern mismatch). At-max health pickup to STAR may not work." -ForegroundColor Yellow
    return $false
}

# --- Touch pickup at max: before running the Touch builtin (fallback; real path is SV_Impact in sv_phys.c). ---
# Try two patterns: (1) two lines with G_EDICT(PARM0) then G_EDICT(PARM1); (2) same with optional comma/semicolon between.
function Add-TouchPickupInterceptInPrCmds {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_cmds.c') { return $false }
    $content = Get-Content $FilePath -Raw
    if ($content -match 'OQuake_STAR_InterceptTouchPickupAtMax') { return $false }
    # PF_Touch: two edict params, item = PARM0 (self), player = PARM1 (other). Free first (item) when we intercept.
    $pattern1 = '(edict_t\s*\*\s*(\w+)\s*=\s*G_EDICT\s*\(\s*OFS_PARM0\s*\)\s*;\s*\r?\n)(\s*edict_t\s*\*\s*(\w+)\s*=\s*G_EDICT\s*\(\s*OFS_PARM1\s*\)\s*;\s*)'
    $repl = "`$1`$3`r`n`tif (OQuake_STAR_InterceptTouchPickupAtMax(`$2, `$4)) { ED_Free(`$2); return; }"
    if ($content -match $pattern1) {
        $content = $content -replace $pattern1, $repl
        if ($content -match 'OQuake_STAR_InterceptTouchPickupAtMax') {
            Set-Content $FilePath $content -NoNewline
            Write-Host "[OQuake] Patched pr_cmds.c: touch pickup at max -> STAR (health/armor when player full)" -ForegroundColor Green
            return $true
        }
    }
    # Alternative: single line "edict_t *e1 = G_EDICT(OFS_PARM0); edict_t *e2 = G_EDICT(OFS_PARM1);" or with comma
    $pattern2 = '(edict_t\s*\*\s*(\w+)\s*=\s*G_EDICT\s*\(\s*OFS_PARM0\s*\)\s*;\s*)(\s*edict_t\s*\*\s*(\w+)\s*=\s*G_EDICT\s*\(\s*OFS_PARM1\s*\)\s*;\s*)'
    if ($content -match $pattern2) {
        $content = $content -replace $pattern2, "`$1`$3`r`n`tif (OQuake_STAR_InterceptTouchPickupAtMax(`$2, `$4)) { ED_Free(`$2); return; }"
        if ($content -match 'OQuake_STAR_InterceptTouchPickupAtMax') {
            Set-Content $FilePath $content -NoNewline
            Write-Host "[OQuake] Patched pr_cmds.c: touch pickup at max -> STAR (pattern 2)" -ForegroundColor Green
            return $true
        }
    }
    return $false
}

# Monster kill via SVC_KILLEDMONSTER: when QuakeC Killed() does WriteByte(MSG_ALL, SVC_KILLEDMONSTER), self is the dead monster. Primary path for XP/NFT.
function Add-MonsterHookInPF_sv_WriteByte {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_cmds.c') { return $false }
    $content = Get-Content $FilePath -Raw
    if ($content -match 'svc_killedmonster') { return $false }
    $pattern = '(static void PF_sv_WriteByte\s*\(\s*void\s*\)\s*\{\s*\r?\n)\s*MSG_WriteByte\s*\(\s*WriteDest\s*\(\s*\)\s*,\s*G_FLOAT\s*\(\s*OFS_PARM1\s*\)\s*\)\s*;\s*\r?\n(\})'
    $repl = "`$1	float byteval = G_FLOAT (OFS_PARM1);`r`n	/* When QuakeC Killed() does WriteByte(MSG_ALL, SVC_KILLEDMONSTER), self is the dead monster. */`r`n	if ((int)byteval == svc_killedmonster)`r`n	{`r`n		edict_t *dead = PROG_TO_EDICT (pr_global_struct->self);`r`n		const char *classname = dead ? PR_GetString (dead->v.classname) : NULL;`r`n		if (classname && classname[0])`r`n			OQuake_STAR_OnMonsterKilled (classname);`r`n	}`r`n	MSG_WriteByte (WriteDest (), byteval);`r`n`$2"
    if ($content -notmatch $pattern) { return $false }
    $content = $content -replace $pattern, $repl
    if ($content -notmatch 'svc_killedmonster') { return $false }
    Set-Content $FilePath $content -NoNewline
    Write-Host "[OQuake] Patched pr_cmds.c: monster kill hook in PF_sv_WriteByte (SVC_KILLEDMONSTER)" -ForegroundColor Green
    return $true
}
function Remove-MonsterHookFromPF_sv_WriteByte {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_cmds.c') { return $false }
    $content = Get-Content $FilePath -Raw
    if ($content -notmatch 'svc_killedmonster') { return $false }
    # Match from "float byteval" through "MSG_WriteByte (WriteDest (), byteval);" and replace with vanilla one-liner (. matches newline)
    $pattern = '(?s)(static void PF_sv_WriteByte\s*\(\s*void\s*\)\s*\{\s*\r?\n).*?MSG_WriteByte\s*\(\s*WriteDest\s*\(\s*\)\s*,\s*byteval\s*\)\s*;\s*\r?\n(\})'
    $repl = "`$1`tMSG_WriteByte (WriteDest (), G_FLOAT (OFS_PARM1));`r`n`$2"
    $content = $content -replace $pattern, $repl
    if ($content -match 'svc_killedmonster') { return $false }
    Set-Content $FilePath $content -NoNewline
    Write-Host "[OQuake] Removed monster hook from PF_sv_WriteByte in pr_cmds.c" -ForegroundColor Green
    return $true
}

# Repair: preprocessor must start in column 0 (MSVC C2014). Fix already-patched files that have indented #ifdef/#endif.
function Repair-MonsterHookPreprocessor {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_edict.c' -and $FileLabel -ne 'pr_cx.c') { return $false }
    $content = Get-Content $FilePath -Raw
    # Preprocessor must start in column 0 (MSVC C2014). Fix #ifdef/#endif that are indented or on same line as other code.
    if ($content -notmatch '\s+#ifdef\s+OASIS_STAR_API|OQuake_STAR_OnEntityFreed') { return $false }
    $orig = $content
    # Pass 1: ensure #ifdef OASIS_STAR_API is always at column 0 (fix "if (!init)\t\t#ifdef" or "\t#ifdef")
    $content = $content -replace '\s+#ifdef\s+OASIS_STAR_API', "`r`n#ifdef OASIS_STAR_API"
    # Pass 2: ensure #endif that follows OQuake_STAR_OnEntityFreed is at column 0
    $content = $content -replace '(OQuake_STAR_OnEntityFreed\s*\(\s*\w+\s*\)\s*;\s*)\r?\n\s+#endif', "`$1`r`n#endif"
    # Pass 3: fix full block when #ifdef was on its own indented line (original pattern)
    $content = $content -replace '(?m)^(\s+)#ifdef\s+OASIS_STAR_API\s*\r?\n\s+OQuake_STAR_OnEntityFreed\s*\(\s*(\w+)\s*\)\s*;\s*\r?\n\s+#endif', "`r`n#ifdef OASIS_STAR_API`r`n`tOQuake_STAR_OnEntityFreed(`$2);`r`n#endif"
    if ($content -eq $orig) { return $false }
    Set-Content $FilePath $content -NoNewline
    Write-Host "[OQuake] Repaired ${FileLabel}: preprocessor at column 0 (fix C2014/C1020)" -ForegroundColor Green
    return $true
}
function Add-MonsterHookToFile {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    if ($FileLabel -ne 'pr_edict.c' -and $FileLabel -ne 'pr_cx.c') { return $false }
    $content = Get-Content $FilePath -Raw
    # If hook present but with indented preprocessor, repair instead of skipping
    if ($content -match '\s+#ifdef\s+OASIS_STAR_API' -and $content -match 'OQuake_STAR_OnEntityFreed') {
        return Repair-MonsterHookPreprocessor -FilePath $FilePath -FileLabel $FileLabel
    }
    if ($content -match 'OQuake_STAR_OnEntityFreed|OQuake_STAR_OnMonsterKilled') { return $false }
    if ($content -notmatch 'ED_Free\s*\(\s*\w+\s*\)') { return $false }
    # Add include so OQuake_STAR_OnEntityFreed is declared
    if ($content -notmatch 'oquake_star_integration\.h') {
        $content = $content -replace '(\#include\s+"pr_edict\.h")(\r?\n)', "`$1`$2`r`n#include `"oquake_star_integration.h`"`$2"
        if ($content -notmatch 'oquake_star_integration\.h') {
            $content = $content -replace '(\#include\s+"quakedef\.h")(\r?\n)', "`$1`$2`r`n#include `"oquake_star_integration.h`"`$2"
        }
    }
    # Replace each ED_Free(ent); with single safe call + ED_Free. Preprocessor (#ifdef/#endif) must start in column 0 for MSVC (C2014).
    $repl = "`r`n#ifdef OASIS_STAR_API`r`n`$1OQuake_STAR_OnEntityFreed(`$3);`r`n#endif`r`n`$1`$2`$3`$4"
    $content = $content -replace '(\s*)(ED_Free\s*\(\s*)(\w+)(\s*\)\s*;)', $repl
    if ($content -notmatch 'OQuake_STAR_OnEntityFreed') { return $false }
    Set-Content $FilePath $content -NoNewline
    Write-Host "[OQuake] Patched ${FileLabel}: added ED_Free monster hook (safe: OQuake_STAR_OnEntityFreed, guards in integration)" -ForegroundColor Green
    return $true
}

function Remove-MonsterHookFromFile {
    param([string]$FilePath, [string]$FileLabel)
    if (-not (Test-Path $FilePath)) { return $false }
    # Only strip from engine files that have the ED_Free hook; never touch oquake_star_integration.c (it contains OQuake_STAR_On* in normal code).
    if ($FileLabel -ne 'pr_edict.c' -and $FileLabel -ne 'pr_cx.c') { return $false }
    $content = Get-Content $FilePath -Raw
    if ($content -notmatch 'OQuake_STAR_OnMonsterKilled|OQuake_STAR_OnEntityFreed') { return $false }
    $orig = $content
    # Strip patterns (avoid [^)] in regex - use [^\)]+ so .NET accepts). Run each in try/catch in case engine differs.
    $stripPatterns = @(
        @{ re = '(?ms)\s*#ifdef\s+OASIS_STAR_API\s*\r?\n\s*OQuake_STAR_OnEntityFreed\s*\(\s*\w+\s*\)\s*;\s*\r?\n\s*#endif\s*\r?\n(\s*ED_Free\s*\(\s*\w+\s*\)\s*;)'; repl = '$1' },
        @{ re = '(?ms)\s*\{\s*\r?\n\s*OQuake_STAR_OnEntityFreed\s*\(\s*\w+\s*\)\s*;\s*\r?\n\s*\}\s*\r?\n(\s*ED_Free\s*\(\s*\w+\s*\)\s*;)'; repl = '$1' },
        @{ re = '(?ms)\s*#ifdef\s+OASIS_STAR_API\s*\r?\n\s*\{\s*\r?\n\s*const\s+char\s+\*ed_classname\s*=\s*PR_GetString\s*\(\s*\w+->v\.classname\s*\)\s*;\s*\r?\n\s*if\s*\(\s*ed_classname\s*&&\s*strncmp\s*\(\s*ed_classname\s*,\s*"monster_"\s*,\s*8\s*\)\s*==\s*0\s*\)\s*\r?\n\s*OQuake_STAR_OnMonsterKilled\s*\(\s*ed_classname\s*\)\s*;\s*\r?\n\s*\}\s*\r?\n\s*#endif\s*\r?\n(\s*ED_Free\s*\(\s*\w+\s*\)\s*;)'; repl = '$1' },
        @{ re = '(?ms)\s*\{\s*\r?\n\s*const\s+char\s+\*cls\s*=\s*PR_GetString\s*\(\s*\w+->v\.classname\s*\)\s*;\s*\r?\n\s*if\s*\(\s*cls\s*&&\s*strncmp\s*\(\s*cls\s*,\s*"monster_"\s*,\s*8\s*\)\s*==\s*0\s*\)\s*\r?\n\s*OQuake_STAR_OnMonsterKilled\s*\(\s*cls\s*\)\s*;\s*\r?\n\s*\}\s*\r?\n(\s*ED_Free\s*\(\s*\w+\s*\)\s*;)'; repl = '$1' }
    )
    foreach ($p in $stripPatterns) {
        try {
            $content = $content -replace $p.re, $p.repl
        } catch {
            # Pattern invalid in this PowerShell/.NET version; fallback will run below
        }
    }
    # Fallback: remove block line-by-line (match lines that are only the hook and leave ED_Free)
    if ($content -match 'OQuake_STAR_OnMonsterKilled|OQuake_STAR_OnEntityFreed') {
        $lines = $content -split '\r?\n'
        $out = New-Object System.Collections.Generic.List[string]
        $i = 0
        while ($i -lt $lines.Count) {
            $line = $lines[$i]
            if ($line -match '^\s*\{\s*$' -and ($i + 1) -lt $lines.Count -and $lines[$i + 1] -match 'OQuake_STAR_On|PR_GetString.*classname') {
                $braceCount = 1
                $i++
                while ($i -lt $lines.Count -and $braceCount -gt 0) {
                    if ($lines[$i] -match '\{') { $braceCount++ }
                    if ($lines[$i] -match '\}') { $braceCount-- }
                    $i++
                }
                if ($i -lt $lines.Count -and $lines[$i] -match '^\s*ED_Free\s*\(') {
                    $out.Add($lines[$i])
                    $i++
                }
                continue
            }
            $out.Add($line)
            $i++
        }
        $content = $out -join "`r`n"
    }
    if ($content -ne $orig) {
        Set-Content $FilePath $content -NoNewline
        Write-Host "[OQuake] Removed call-site monster hooks from ${FileLabel} (using hook inside ED_Free)" -ForegroundColor Green
        return $true
    }
    Write-Host "[OQuake] WARNING: ${FileLabel} still contains OQuake_STAR monster hook - strip did not match. Restore from vkQuake repo: git checkout Quake/${FileLabel}" -ForegroundColor Yellow
    return $false
}

if ($RevertMonsterHook) {
    foreach ($cFile in @("pr_cx.c", "pr_edict.c")) {
        $path = Join-Path $QuakeDir $cFile
        if (Remove-MonsterHookFromFile -FilePath $path -FileLabel $cFile) {
            foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
                if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache" -ForegroundColor Yellow; break }
            }
        }
    }
    $path = Join-Path $QuakeDir "pr_edict.c"
    if (Remove-MonsterHookFromInsideEDFree -FilePath $path -FileLabel "pr_edict.c") {
        foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache" -ForegroundColor Yellow; break }
        }
    }
    $path = Join-Path $QuakeDir "pr_cmds.c"
    if (Remove-MonsterHookFromPF_Remove -FilePath $path -FileLabel "pr_cmds.c") {
        foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache" -ForegroundColor Yellow; break }
        }
    }
    if (Remove-MonsterHookFromPrCmds -FilePath $path -FileLabel "pr_cmds.c") {
        foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache" -ForegroundColor Yellow; break }
        }
    }
    if (Remove-MonsterHookFromPF_sv_WriteByte -FilePath $path -FileLabel "pr_cmds.c") {
        foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache" -ForegroundColor Yellow; break }
        }
    }
    Get-ChildItem -Path $QuakeDir -Filter "*.c" | ForEach-Object {
        if (Remove-MonsterHookFromFile -FilePath $_.FullName -FileLabel $_.Name) {
            foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
                if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache" -ForegroundColor Yellow; break }
            }
        }
    }
} else {
    # Repair preprocessor (column 0) in both files first
    foreach ($cFile in @("pr_cx.c", "pr_edict.c")) {
        $path = Join-Path $QuakeDir $cFile
        Repair-MonsterHookPreprocessor -FilePath $path -FileLabel $cFile | Out-Null
    }
    # Remove call-site hooks so we use a single hook inside ED_Free (catches pr_cmds.c, host_cmd.c, etc.)
    foreach ($cFile in @("pr_cx.c", "pr_edict.c")) {
        $path = Join-Path $QuakeDir $cFile
        Remove-MonsterHookFromFile -FilePath $path -FileLabel $cFile | Out-Null
    }
    $monsterHookAdded = $false
    # Primary: hook SVC_KILLEDMONSTER in PF_sv_WriteByte (when QuakeC Killed() runs, self = dead monster). No pr_edict.c hook (avoids C2082).
    $path = Join-Path $QuakeDir "pr_cmds.c"
    if (Add-MonsterHookInPF_sv_WriteByte -FilePath $path -FileLabel "pr_cmds.c") {
        $monsterHookAdded = $true
    }
    if (Add-MonsterHookInPrCmds -FilePath $path -FileLabel "pr_cmds.c") {
        $monsterHookAdded = $true
    }
    if (Add-MonsterHookInPF_Remove -FilePath $path -FileLabel "pr_cmds.c") {
        $monsterHookAdded = $true
    }
    # SV_Impact (sv_phys.c) is where touch is actually invoked for pickups; patch it first. pr_cmds Touch builtin is fallback.
    if (Add-SVImpactTouchIntercept) {
        $monsterHookAdded = $true
    }
    if (Add-TouchPickupInterceptInPrCmds -FilePath $path -FileLabel "pr_cmds.c") {
        $monsterHookAdded = $true
    }
    if ($monsterHookAdded) {
        foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache (monster hook inside ED_Free + pr_cmds makestatic)" -ForegroundColor Yellow; break }
        }
    }
}

# --- gl_screen.c: OQuake HUD (Beamed In, XP, version, inventory overlay) ---
$GlScreenC = Join-Path $QuakeDir "gl_screen.c"
if (Test-Path $GlScreenC) {
    $content = Get-Content $GlScreenC -Raw
    $glPatched = $false
    if ($content -notmatch 'oquake_star_integration\.h') {
        $content = $content -replace '(\#include\s+"quakedef\.h")(\r?\n)', "`$1`$2`r`n#include `"oquake_star_integration.h`"`$2"
        $glPatched = $true
        Write-Host "[OQuake] Patched gl_screen.c: added #include oquake_star_integration.h" -ForegroundColor Green
    }
    if ($content -notmatch 'OQuake_STAR_DrawBeamedInStatus') {
        # After SCR_DrawClock (cbx); in the normal game HUD block add the four OQuake draw calls
        $pattern = '(SCR_DrawClock \(cbx\);\s*(?://[^\r\n]*)?)(\r?\n)(\s+SCR_DrawConsole)'
        $replacement = "`$1`$2	OQuake_STAR_DrawBeamedInStatus (cbx);`r`n	OQuake_STAR_DrawXpStatus (cbx);`r`n	OQuake_STAR_DrawVersionStatus (cbx);`r`n	OQuake_STAR_DrawToast (cbx);`r`n	OQuake_STAR_DrawInventoryOverlay (cbx);`r`n`$3"
        if ($content -match $pattern) {
            $content = $content -replace $pattern, $replacement
            $glPatched = $true
            Write-Host "[OQuake] Patched gl_screen.c: OQuake HUD (Beamed In, XP, version, inventory)" -ForegroundColor Green
        }
    }
    if ($glPatched) {
        Set-Content $GlScreenC $content -NoNewline
        foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache (gl_screen.c patched)" -ForegroundColor Yellow; break }
        }
    }
}

# Patch vkQuake Visual Studio project: add OQuake sources (integration, pr_ext_oquake, star_sync) + star_api.lib
$vcxprojPaths = @(
    (Join-Path $VkQuakeSrc "Windows\VisualStudio\vkquake.vcxproj"),
    (Join-Path $VkQuakeSrc "Windows\VisualStudio\Quake\Quake.vcxproj")
)
foreach ($vcxproj in $vcxprojPaths) {
    if (-not (Test-Path $vcxproj)) { continue }
    $projContent = Get-Content $vcxproj -Raw
    # Find path prefix from pr_ext.c so we can add OQuake sources to the same ItemGroup
    $pathPrefix = $null
    if ($projContent -match '<ClCompile\s+Include="([^"]*?)pr_ext\.c"') { $pathPrefix = $Matches[1] }
    if (-not $pathPrefix -and $projContent -match '<ClCompile\s+Include="([^"]*?)oquake_star_integration\.c"') { $pathPrefix = $Matches[1] }
    if (-not $pathPrefix) { continue }

    $vcxprojChanged = $false
    $blockIntegration = @"
    <ClCompile Include="$pathPrefix`oquake_star_integration.c">
      <PrecompiledHeader>NotUsing</PrecompiledHeader>
    </ClCompile>
"@
    $blockPrExtOquake = @"
    <ClCompile Include="$pathPrefix`pr_ext_oquake.c">
      <PrecompiledHeader>NotUsing</PrecompiledHeader>
    </ClCompile>
"@
    $blockStarSync = @"
    <ClCompile Include="$pathPrefix`star_sync.c">
      <PrecompiledHeader>NotUsing</PrecompiledHeader>
    </ClCompile>
"@
    if ($projContent -notmatch 'oquake_star_integration\.c') {
        $projContent = $projContent -replace "(\r?\n)(\s*<ClCompile\s+Include=`"[^`"]*pr_ext\.c`"[^\r\n]*)(\r?\n)", "`$1`$2`$3$blockIntegration`r`n"
        $vcxprojChanged = $true
        Write-Host "[OQuake] Added oquake_star_integration.c to project $(Split-Path -Leaf $vcxproj)" -ForegroundColor Green
    }
    if ($projContent -notmatch 'pr_ext_oquake\.c') {
        $anchor = if ($projContent -match 'oquake_star_integration\.c') { 'oquake_star_integration\.c' } else { 'pr_ext\.c' }
        $projContent = $projContent -replace "(\r?\n)(\s*<ClCompile\s+Include=`"[^`"]*$anchor`"[^\r\n]*)(\r?\n)", "`$1`$2`$3$blockPrExtOquake`r`n"
        $vcxprojChanged = $true
        Write-Host "[OQuake] Added pr_ext_oquake.c to project $(Split-Path -Leaf $vcxproj)" -ForegroundColor Green
    }
    if ($projContent -notmatch 'star_sync\.c') {
        $anchor = if ($projContent -match 'oquake_star_integration\.c') { 'oquake_star_integration\.c' } else { 'pr_ext_oquake\.c' }
        if (-not $anchor) { $anchor = 'pr_ext\.c' }
        $projContent = $projContent -replace "(\r?\n)(\s*<ClCompile\s+Include=`"[^`"]*$anchor`"[^\r\n]*)(\r?\n)", "`$1`$2`$3$blockStarSync`r`n"
        $vcxprojChanged = $true
        Write-Host "[OQuake] Added star_sync.c to project $(Split-Path -Leaf $vcxproj) (PCH disabled)" -ForegroundColor Green
    }
    if ($projContent -notmatch 'star_api\.lib') {
        if ($projContent -match '<AdditionalDependencies>([^<]*)</AdditionalDependencies>') {
            $projContent = $projContent -replace '(<AdditionalDependencies>)([^<]*)(</AdditionalDependencies>)', "`$1`$2;star_api.lib`$3"
            $vcxprojChanged = $true
            Write-Host "[OQuake] Added star_api.lib to linker in $(Split-Path -Leaf $vcxproj)" -ForegroundColor Green
        }
    }
    # Client exports star_api_refresh_avatar_profile (see STARAPIClient obj/.../native/star_api.def). Linker must use the deployed lib in vkQuake\Quake.
    if ($projContent -match 'star_api\.lib' -and $projContent -notmatch 'AdditionalLibraryDirectories.*\.\.\\\.\.\\Quake') {
        $projContent = $projContent -replace '(<AdditionalDependencies>[^<]*</AdditionalDependencies>)([\s\S]*?)(</Link>)', "`$1`r`n      <AdditionalLibraryDirectories>..\..\Quake;%(AdditionalLibraryDirectories)</AdditionalLibraryDirectories>`r`n`$2`$3"
        $vcxprojChanged = $true
        Write-Host "[OQuake] Added AdditionalLibraryDirectories ..\..\Quake so linker uses deployed star_api.lib in $(Split-Path -Leaf $vcxproj)" -ForegroundColor Green
    }
    # Ensure Quake folder is on include path so compiler finds star_api.h (with STAR_API_OP_PROFILE_LOADED and star_api_set_operation_callback) copied by BUILD_OQUAKE.bat / apply script.
    if ($projContent -notmatch 'AdditionalIncludeDirectories.*\.\.\\\.\.\\Quake') {
        $projContent = $projContent -replace '(<ClCompile>\s*\r?\n)(\s*<(?:WarningLevel|PreprocessorDefinitions))', "`$1      <AdditionalIncludeDirectories>..\..\Quake;%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>`r`n`$2"
        $vcxprojChanged = $true
        Write-Host "[OQuake] Added AdditionalIncludeDirectories ..\..\Quake so compiler finds star_api.h in $(Split-Path -Leaf $vcxproj)" -ForegroundColor Green
    }
    # Define OASIS_STAR_API so #ifdef OASIS_STAR_API blocks in pr_edict.c, host.c, sbar.c, etc. are compiled (monster hook, init, HUD).
    if ($projContent -notmatch 'OASIS_STAR_API') {
        $projContent = $projContent -replace '(<PreprocessorDefinitions>)([^<]+)(</PreprocessorDefinitions>)', "`$1OASIS_STAR_API;`$2`$3"
        $vcxprojChanged = $true
        Write-Host "[OQuake] Added OASIS_STAR_API to PreprocessorDefinitions in $(Split-Path -Leaf $vcxproj)" -ForegroundColor Green
    }
    # Define OQUAKE_STAR_API_TRACKER_STUBS so linker finds stub implementations when star_api.lib is old and does not export quest tracker APIs. Remove this define after deploying a STAR client that exports star_api_get_quest_tracker_objectives_string and star_api_get_quest_tracker_active_objective_index.
    if ($projContent -notmatch 'OQUAKE_STAR_API_TRACKER_STUBS') {
        $projContent = $projContent -replace '(<PreprocessorDefinitions>)([^<]+)(</PreprocessorDefinitions>)', "`$1OQUAKE_STAR_API_TRACKER_STUBS;`$2`$3"
        $vcxprojChanged = $true
        Write-Host "[OQuake] Added OQUAKE_STAR_API_TRACKER_STUBS to PreprocessorDefinitions (use stub if star_api.lib is old)" -ForegroundColor Green
    }
    # Define OQUAKE_STAR_API_REFRESH_AVATAR_PROFILE_IMPL so oquake_star_integration.c provides star_api_refresh_avatar_profile (forwards to DLL at runtime). Fixes LNK2001 when the linked star_api.lib does not export this symbol.
    if ($projContent -notmatch 'OQUAKE_STAR_API_REFRESH_AVATAR_PROFILE_IMPL') {
        $projContent = $projContent -replace '(<PreprocessorDefinitions>)([^<]+)(</PreprocessorDefinitions>)', "`$1OQUAKE_STAR_API_REFRESH_AVATAR_PROFILE_IMPL;`$2`$3"
        $vcxprojChanged = $true
        Write-Host "[OQuake] Added OQUAKE_STAR_API_REFRESH_AVATAR_PROFILE_IMPL (provides star_api_refresh_avatar_profile when lib does not)" -ForegroundColor Green
    }
    if ($vcxprojChanged) { Set-Content -Path $vcxproj -Value $projContent -NoNewline; break }
}
