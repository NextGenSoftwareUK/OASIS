<#
.SYNOPSIS
  Copies OQuake + STAR files into vkQuake and patches host.c, pr_ext.c, sbar.c, gl_screen.c, and the build (like ODOOM: full automation).
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
$StarPublishDir = Join-Path $STARAPIClientRoot "bin\Release\net8.0\win-x64\publish"
if (-not $StarDll -and (Test-Path (Join-Path $StarPublishDir "star_api.dll"))) {
    $StarDll = Join-Path $StarPublishDir "star_api.dll"
    $StarLibDir = Join-Path $STARAPIClientRoot "bin\Release\net8.0\win-x64\native"
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
"@
            $content = $content -replace [regex]::Escape($Matches[1]) + "(\r?\n)", "$Matches[1]$add`$2"
            $prExtPatched = $true
            Write-Host "[OQuake] Patched pr_ext.c: added OQuake builtin externs" -ForegroundColor Green
        }
    }
    # Extension table: add four OQuake entries before the closing }; of extensionbuiltins
    if ($content -notmatch 'ex_OQuake_OnMonsterKilled') {
        $insert = @'

 {"ex_OQuake_OnKeyPickup", PF_OQuake_OnKeyPickup, PF_NoCSQC, 0, "void(string keyname)"},
 {"ex_OQuake_CheckDoorAccess", PF_OQuake_CheckDoorAccess, PF_NoCSQC, 0, "float(string doorname, string requiredkey)"},
 {"ex_OQuake_OnBossKilled", PF_OQuake_OnBossKilled, PF_NoCSQC, 0, "void(string bossname)"},
 {"ex_OQuake_OnMonsterKilled", PF_OQuake_OnMonsterKilled, PF_NoCSQC, 0, "void(string monster_classname)"},
'@
        if ($content -match '"ex_bot_followentity"') {
            $content = $content -replace '(\{\s*"ex_bot_followentity",\s*PF_Fixme,\s*PF_NoCSQC,\s*0,\s*"float\(entity bot, entity goal\)"\s*\},)(\r?\n)(\s*\}\s*;)', "`$1$insert`$2`$3"
            $prExtPatched = $true
            Write-Host "[OQuake] Patched pr_ext.c: added OQuake extension builtin entries" -ForegroundColor Green
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

# --- pr_cx.c: call OQuake_STAR_OnMonsterKilled when a monster entity is removed (so XP/NFT work without QuakeC changes) ---
$PrCxC = Join-Path $QuakeDir "pr_cx.c"
if (Test-Path $PrCxC) {
    $content = Get-Content $PrCxC -Raw
    $prCxPatched = $false
    if ($content -notmatch 'oquake_star_integration\.h') {
        $content = $content -replace '(\#include\s+"quakedef\.h")(\r?\n)', "`$1`$2`r`n#include `"oquake_star_integration.h`"`$2"
        $prCxPatched = $true
        Write-Host "[OQuake] Patched pr_cx.c: added #include oquake_star_integration.h" -ForegroundColor Green
    }
    # Before ED_Free(ed) in PF_remove, notify OQuake so monster kills grant XP and can mint NFTs (no QuakeC changes needed)
    if ($content -notmatch 'OQuake_STAR_OnMonsterKilled') {
        $hook = @'

	{
		const char *cls = PR_GetString(ed->v.classname);
		if (cls && strncmp(cls, "monster_", 8) == 0)
			OQuake_STAR_OnMonsterKilled(cls);
	}
'@
        if ($content -match '(\s+)(ED_Free\s*\(\s*ed\s*\)\s*;)') {
            $content = $content -replace '(\s+)(ED_Free\s*\(\s*ed\s*\)\s*;)', "$hook`r`n`$1`$2"
            $prCxPatched = $true
            Write-Host "[OQuake] Patched pr_cx.c: monster death hook before ED_Free (XP + NFT without QuakeC)" -ForegroundColor Green
        }
    }
    if ($prCxPatched) {
        Set-Content $PrCxC $content -NoNewline
        foreach ($dir in @((Join-Path $VkQuakeSrc "Windows\VisualStudio\Build-vkQuake"), (Join-Path $VkQuakeSrc "Windows\VisualStudio\x64"), (Join-Path $VkQuakeSrc "build"))) {
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir; Write-Host "[OQuake] Cleared build cache (pr_cx.c patched)" -ForegroundColor Yellow; break }
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
        $replacement = "`$1`$2	OQuake_STAR_DrawBeamedInStatus (cbx);`r`n	OQuake_STAR_DrawXpStatus (cbx);`r`n	OQuake_STAR_DrawVersionStatus (cbx);`r`n	OQuake_STAR_DrawInventoryOverlay (cbx);`r`n`$3"
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
    if ($vcxprojChanged) { Set-Content -Path $vcxproj -Value $projContent -NoNewline; break }
}
