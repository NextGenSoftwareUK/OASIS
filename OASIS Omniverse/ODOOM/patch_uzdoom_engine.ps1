<#
.SYNOPSIS
  Patches UZDoom engine source for ODOOM: version.h, startscreen, status bar, p_interaction (boss kill), launcher, etc.
  Invoked by BUILD ODOOM.bat. Manual: .\patch_uzdoom_engine.ps1 -UZDOOM_SRC "C:\Source\UZDoom"
#>
param([Parameter(Mandatory=$true)][string]$UZDOOM_SRC)

$odoomRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = $UZDOOM_SRC.TrimEnd('\')
$versionH = "$src\src\version.h"
$startscreenCpp = "$src\src\common\startscreen\startscreen.cpp"
$sharedSbarCpp = "$src\src\g_statusbar\shared_sbar.cpp"
$dMainCpp = "$src\src\d_main.cpp"
$sbarMugshotCpp = "$src\src\g_statusbar\sbar_mugshot.cpp"
$zscriptTxt = "$src\wadsrc\static\zscript.txt"
$cvarinfoTxt = "$src\wadsrc\static\cvarinfo.txt"
$odoomCvarinfo = Join-Path $odoomRoot "odoom_cvarinfo.txt"
$doomItemsMapinfo = "$src\wadsrc\static\mapinfo\doomitems.txt"
$commonMapinfo = "$src\wadsrc\static\mapinfo\common.txt"
if (Test-Path (Join-Path $odoomRoot "generate_odoom_version.ps1")) {
    & (Join-Path $odoomRoot "generate_odoom_version.ps1") -Root $odoomRoot
}
$versionDisplay = "1.0 (Build 1)"
$versionDisplayPath = Join-Path $odoomRoot "version_display.txt"
if (Test-Path $versionDisplayPath) { $versionDisplay = (Get-Content $versionDisplayPath -Raw).Trim() }

function Test-PatchNeeded($path, $marker) {
    if (-not (Test-Path $path)) { return $false }
    return -not (Select-String -Path $path -Pattern $marker -Quiet)
}

$changes = @()

# 1. version.h
if (Test-PatchNeeded $versionH "odoom_branding") {
    $content = Get-Content $versionH -Raw
    $content = $content -replace '(\r?\n)(#endif //__VERSION_H__)', "`n#ifdef OASIS_STAR_API`n#include `"odoom_branding.h`"`n#endif`n`n`$1`$2"
    Set-Content $versionH $content -NoNewline
    $changes += "version.h"
}

# 2. startscreen.cpp
$startscreenChanged = $false
if (Test-Path $startscreenCpp) {
    $content = Get-Content $startscreenCpp -Raw
    $needInclude = -not ($content -match 'include "version\.h"')
    $needCreateHeader = -not ($content -match 'OASIS_STAR_API.*headerTitle')
    if ($needInclude) {
        $content = $content -replace '(\#include "texturemanager\.h")', "`$1`n#include `"version.h`""
        $startscreenChanged = $true
    }
    if ($needCreateHeader) {
        $old = 'ClearBlock\(HeaderBitmap, bcolor, 0, 0, HeaderBitmap\.GetWidth\(\), HeaderBitmap\.GetHeight\(\)\);\r?\n\tint textlen = SizeOfText\(GameStartupInfo\.Name\.GetChars\(\)\);\r?\n\tDrawString\(HeaderBitmap, \(HeaderBitmap\.GetWidth\(\) >> 4\) - \(textlen >> 1\), 0\.5, GameStartupInfo\.Name\.GetChars\(\), fcolor, bcolor\);'
        $new = @"
ClearBlock(HeaderBitmap, bcolor, 0, 0, HeaderBitmap.GetWidth(), HeaderBitmap.GetHeight());
#ifdef OASIS_STAR_API
	FString headerTitle = GAMENAME " " ODOOM_FULL_VERSION_STR;
	int textlen = SizeOfText(headerTitle.GetChars());
	DrawString(HeaderBitmap, (HeaderBitmap.GetWidth() >> 4) - (textlen >> 1), 0.5, headerTitle.GetChars(), fcolor, bcolor);
#else
	int textlen = SizeOfText(GameStartupInfo.Name.GetChars());
	DrawString(HeaderBitmap, (HeaderBitmap.GetWidth() >> 4) - (textlen >> 1), 0.5, GameStartupInfo.Name.GetChars(), fcolor, bcolor);
#endif
"@
        $content = $content -replace $old, $new
        $startscreenChanged = $true
    }
    if ($content -match 'OASIS_STAR_API.*headerTitle' -and $content -match 'headerTitle.*GetVersionString') {
        $content = $content -replace 'FString headerTitle = GAMENAME;\r?\n\s+headerTitle \+= " ";\r?\n\s+headerTitle \+= GetVersionString\(\);', 'FString headerTitle = GAMENAME " " ODOOM_FULL_VERSION_STR;'
        $startscreenChanged = $true
    }
    if ($startscreenChanged) { Set-Content $startscreenCpp $content -NoNewline; $changes += "startscreen" }
}

# 3. shared_sbar.cpp
$sbarChanged = $false
if (Test-Path $sharedSbarCpp) {
    $content = Get-Content $sharedSbarCpp -Raw
    $needsPatch = -not ($content -match 'OASIS_STAR_API.*verText')
    if ($needsPatch) {
        $old = '(\t\}\r?\n\r?\n\tif \(state != HUD_AltHud\))'
        $new = @"
	}

#ifdef OASIS_STAR_API
	ODOOM_InventoryInputCaptureFrame();
	{
		FString verText = GAMENAME " " ODOOM_FULL_VERSION_STR;
		double yVersion = twod->GetHeight() - 18;
		double xVersion = twod->GetWidth() - SmallFont->StringWidth(verText.GetChars()) * CleanXfac - 4;
		DrawText(twod, SmallFont, CR_TAN, xVersion, yVersion, verText.GetChars(), DTA_CleanNoMove, true, TAG_DONE);
		FBaseCVar *starUserVar = FindCVar("odoom_star_username", nullptr);
		const char *starUser = (starUserVar && starUserVar->GetRealType() == CVAR_String) ? starUserVar->GetGenericRep(CVAR_String).String : nullptr;
		FString beamedText = (starUser && *starUser) ? FString("Beamed In: ") + starUser : "Beamed In: None";
		DrawText(twod, SmallFont, CR_TAN, 4, 2, beamedText.GetChars(), DTA_CleanNoMove, true, TAG_DONE);
	}
#endif

	if (state != HUD_AltHud)
"@
        $content = $content -replace $old, $new
        $sbarChanged = $true
    }
    if ($content -match 'OASIS_STAR_API' -and $content -match 'verText' -and $content -notmatch 'Beamed In') {
        $content = $content -replace '(\t\tDrawText\(twod, SmallFont, CR_TAN, x, y, verText\.GetChars\(\), DTA_CleanNoMove, true, TAG_DONE\);\r?\n)(\t\})', @"
`$1		FBaseCVar *starUserVar = FindCVar("odoom_star_username", nullptr);
		const char *starUser = (starUserVar && starUserVar->GetRealType() == CVAR_String) ? starUserVar->GetGenericRep(CVAR_String).String : nullptr;
		FString beamedText = (starUser && *starUser) ? FString("Beamed In: ") + starUser : "Beamed In: None";
		DrawText(twod, SmallFont, CR_TAN, 4, 2, beamedText.GetChars(), DTA_CleanNoMove, true, TAG_DONE);
`$2
"@
        $sbarChanged = $true
    }
    if ($content -match 'OASIS_STAR_API' -and $content -match 'verText.*GetVersionString') {
        $content = $content -replace '(FString verText = GAMENAME;)\r?\n\s+verText \+= " ";\r?\n\s+verText \+= GetVersionString\(\);', 'FString verText = GAMENAME " " ODOOM_FULL_VERSION_STR;'
        $sbarChanged = $true
    }
    if ($content -match 'ODOOM_InventoryInputCaptureFrame' -and $content -notmatch 'uzdoom_star_integration\.h') {
        $content = $content -replace '(#ifdef OASIS_STAR_API)', "#include `"uzdoom_star_integration.h`"`r`n`$1"
        $sbarChanged = $true
    }
    # Move version string from top-right (y=2) to just above HUD on the right (same x, only y changes)
    if ($content -match 'OASIS_STAR_API.*verText') {
        if ($content -match 'double y = 2;') {
            $content = $content -replace 'double y = 2;(\r?\n)(\s+)double xVersion', 'double yVersion = twod->GetHeight() - 18;$1$2double xVersion'
            $content = $content -replace 'DrawText\(twod, SmallFont, CR_TAN, xVersion, y, verText\.GetChars\(\)', 'DrawText(twod, SmallFont, CR_TAN, xVersion, yVersion, verText.GetChars()'
            $sbarChanged = $true
        }
        if ($content -match 'xVersion, y,' -and $content -notmatch 'xVersion, yVersion,') {
            $content = $content -replace 'xVersion, y, verText', 'xVersion, yVersion, verText'
            $sbarChanged = $true
        }
    }
    if ($sbarChanged) { Set-Content $sharedSbarCpp $content -NoNewline; $changes += "shared_sbar" }
}

# 3a1. d_main.cpp: run inventory key capture at start of every frame (before TryRunTics/input) so bindings are cleared and key CVars set before ticcmd is built
if (Test-Path $dMainCpp) {
    $dContent = Get-Content $dMainCpp -Raw
    if ($dContent -notmatch 'ODOOM_InventoryInputCaptureFrame') {
        $dChanged = $false
        if ($dContent -match '#include "d_net\.h"') {
            $dContent = $dContent -replace '(#include "d_net\.h")', "`$1`r`n#ifdef OASIS_STAR_API`r`n#include `"uzdoom_star_integration.h`"`r`n#endif"
            $dChanged = $true
        }
        # Insert at start of for(;;) loop in D_DoomLoop so capture runs before TryRunTics/G_BuildTiccmd
        if ($dContent -match '(for\s*\(\s*;;\s*\)\s*\r?\n\s*\{\s*\r?\n)(\s*try\s)') {
            $dContent = $dContent -replace '(for\s*\(\s*;;\s*\)\s*\r?\n\s*\{\s*\r?\n)(\s*try\s)', "`$1#ifdef OASIS_STAR_API`r`n	ODOOM_InventoryInputCaptureFrame();`r`n#endif`r`n`$2"
            $dChanged = $true
        }
        if ($dChanged) {
            Set-Content $dMainCpp $dContent -NoNewline
            $changes += "d_main (inventory capture)"
        }
    }
}

# 3a2. g_game.cpp: run inventory key capture at start of each tic (backup if d_main patch missed)
$gGameCpp = "$src\src\g_game.cpp"
if (Test-Path $gGameCpp) {
    $gContent = Get-Content $gGameCpp -Raw
    if ($gContent -notmatch 'ODOOM_InventoryInputCaptureFrame') {
        $gChanged = $false
        if ($gContent -match '#include "version\.h"') {
            $gContent = $gContent -replace '(#include "version\.h")', "`$1`r`n#ifdef OASIS_STAR_API`r`n#include `"uzdoom_star_integration.h`"`r`n#endif"
            $gChanged = $true
        }
        $runTicPatterns = @(
            '(\bvoid\s+RunTic\s*\(\s*\)\s*\r?\n\s*\{\s*\r?\n)',
            '(\bvoid\s+FLevelLocals::RunTic\s*\(\s*\)\s*\r?\n\s*\{\s*\r?\n)'
        )
        foreach ($pat in $runTicPatterns) {
            if ($gContent -match $pat) {
                $gContent = $gContent -replace $pat, "`$1#ifdef OASIS_STAR_API`r`n	ODOOM_InventoryInputCaptureFrame();`r`n#endif`r`n"
                $gChanged = $true
                break
            }
        }
        if ($gChanged) {
            Set-Content $gGameCpp $gContent -NoNewline
            $changes += "g_game (inventory capture)"
        }
    }
}

# 3b. sbar_mugshot.cpp: show OASIS face (OASFACE) only when anorak face mode is enabled (via UZDoom_STAR_GetShowAnorakFace)
if (Test-Path $sbarMugshotCpp) {
    $mugContent = Get-Content $sbarMugshotCpp -Raw
    if ($mugContent -match 'FMugShot::GetFace' -and $mugContent -notmatch 'UZDoom_STAR_GetShowAnorakFace|oasis_star_anorak_face.*OASFACE|OASFACE.*oasis_star_anorak_face') {
        $oldMug = '(FGameTexture \*FMugShot::GetFace\(player_t \*player, const char \*default_face, int accuracy, StateFlags stateflags\)\r?\n\{\r?\n)(\tint angle = UpdateState)'
        $newMug = @"
`$1
#ifdef OASIS_STAR_API
	if (UZDoom_STAR_GetShowAnorakFace() != 0)
	{
		FGameTexture *oasFace = TexMan.FindGameTexture("OASFACE", ETextureType::Any, FTextureManager::TEXMAN_TryAny|FTextureManager::TEXMAN_AllowSkins);
		if (!oasFace) oasFace = TexMan.GetGameTexture(TexMan.CheckForTexture("OASFACE", ETextureType::Any, FTextureManager::TEXMAN_TryAny|FTextureManager::TEXMAN_AllowSkins));
		if (oasFace)
			return oasFace;
	}
#endif
`$2
"@
        $mugContent = $mugContent -replace $oldMug, $newMug
        if ($mugContent -notmatch 'uzdoom_star_integration\.h') {
            $mugContent = $mugContent -replace '(#include "texturemanager\.h")', "`$1`r`n#ifdef OASIS_STAR_API`r`n#include `"uzdoom_star_integration.h`"`r`n#endif"
        }
        Set-Content $sbarMugshotCpp $mugContent -NoNewline
        $changes += "sbar_mugshot"
    }
    # If already patched with star-user trigger, convert to anorak-only trigger.
    if ($mugContent -match 'odoom_star_username.*OASFACE') {
        $mugContent = Get-Content $sbarMugshotCpp -Raw
        $mugContent = $mugContent -replace '\tFBaseCVar \*starUserVar = FindCVar\("odoom_star_username", nullptr\);\r?\n\tconst char \*starUser = \(starUserVar && starUserVar->GetRealType\(\) == CVAR_String\) \? starUserVar->GetGenericRep\(CVAR_String\)\.String : nullptr;\r?\n', ''
        if ($mugContent -notmatch 'oasis_star_anorak_face') {
            $mugContent = $mugContent -replace '(\tif \()', "`tFBaseCVar *anorakFaceVar = FindCVar(`"oasis_star_anorak_face`", nullptr);`r`n`tbool anorakFace = (anorakFaceVar && anorakFaceVar->GetRealType() == CVAR_Bool) && (anorakFaceVar->GetGenericRep(CVAR_Bool).Int != 0);`r`n`$1"
        }
        $mugContent = $mugContent -replace 'if \(\(starUser && \*starUser\) \|\| anorakFace\)', 'if (anorakFace)'
        $mugContent = $mugContent -replace 'if \(starUser && \*starUser\)', 'if (anorakFace)'
        Set-Content $sbarMugshotCpp $mugContent -NoNewline
        $changes += "sbar_mugshot(anorak)"
    }
    # Remove any previously-added OQKGI0 fallback so default Doom face is preserved unless anorak face is active and OASFACE exists.
    if ($mugContent -match 'OQKGI0') {
        $mugContent = Get-Content $sbarMugshotCpp -Raw
        $mugContent = $mugContent -replace '\r?\n\t\t// Fallback when odoom_face\.pk3 is not loaded\.\r?\n\t\tFGameTexture \*fallbackFace = TexMan\.FindGameTexture\("OQKGI0", ETextureType::Any, FTextureManager::TEXMAN_TryAny\|FTextureManager::TEXMAN_AllowSkins\);\r?\n\t\tif \(!fallbackFace\) fallbackFace = TexMan\.GetGameTexture\(TexMan\.CheckForTexture\("OQKGI0", ETextureType::Any, FTextureManager::TEXMAN_TryAny\|FTextureManager::TEXMAN_AllowSkins\)\);\r?\n\t\tif \(fallbackFace\)\r?\n\t\t\treturn fallbackFace;', ''
        $mugContent = $mugContent -replace '\r?\n\t\t/\* Fallback if odoom_face\.pk3 is not loaded: use a guaranteed ODOOM icon texture\. \*/\r?\n\t\tFGameTexture \*fallbackFace = TexMan\.FindGameTexture\("OQKGI0", ETextureType::Any, FTextureManager::TEXMAN_TryAny\|FTextureManager::TEXMAN_AllowSkins\);\r?\n\t\tif \(!fallbackFace\) fallbackFace = TexMan\.GetGameTexture\(TexMan\.CheckForTexture\("OQKGI0", ETextureType::Any, FTextureManager::TEXMAN_TryAny\|FTextureManager::TEXMAN_AllowSkins\)\);\r?\n\t\tif \(fallbackFace\)\r?\n\t\t\treturn fallbackFace;', ''
        Set-Content $sbarMugshotCpp $mugContent -NoNewline
        $changes += "sbar_mugshot(remove-fallback)"
    }
}

# 3c. p_interaction.cpp: call UZDoom_STAR_OnMonsterKilled when any monster dies (mint per monster from oasisstar.json mint_monsters); OnBossKilled still used for legacy boss NFT
$pInteractionCpp = "$src\src\playsim\p_interaction.cpp"
if (Test-Path $pInteractionCpp) {
    $piContent = Get-Content $pInteractionCpp -Raw
    $piChanged = $false
    if ($piContent -notmatch 'uzdoom_star_integration\.h') {
        $piContent = $piContent -replace '(#include "d_main\.h")', "`$1`r`n#ifdef OASIS_STAR_API`r`n#include `"uzdoom_star_integration.h`"`r`n#endif"
        $piChanged = $true
    }
    # Prefer OnMonsterKilled (mint + add to inventory per mint_monsters); fallback to OnBossKilled block if only that exists
    if ($piContent -notmatch 'UZDoom_STAR_OnMonsterKilled') {
        if ($piContent -match 'UZDoom_STAR_OnBossKilled') {
            $oldBoss = 'if \(player == nullptr && \(flags3 & MF3_ISMONSTER\)\)\r?\n\s*\{\r?\n\s*FName tn = GetClass\(\)->TypeName;\r?\n\s*if \(tn == NAME_Cyberdemon \|\| tn == NAME_SpiderMastermind \|\| tn == NAME_BaronOfHell\)\r?\n\s*UZDoom_STAR_OnBossKilled\(tn\.GetChars\(\)\);\r?\n\s*\}'
            $newMonster = @'
if (player == nullptr && (flags3 & MF3_ISMONSTER))
	{
		FName tn = GetClass()->TypeName;
		UZDoom_STAR_OnMonsterKilled(tn.GetChars());
	}
'@
            if ($piContent -match $oldBoss) {
                $piContent = $piContent -replace $oldBoss, $newMonster
                $piChanged = $true
            }
        } else {
            $oldDie = 'bool wasgibbed = \(health < GetGibHealth\(\)\);\r?\n\r?\n(\t// Check to see if unmorph[^\r\n]*)'
            $newDie = @"
bool wasgibbed = (health < GetGibHealth());

	// OASIS STAR: when any monster is killed, check mint_monsters in oasisstar.json; mint NFT and add to inventory (Monsters tab) if enabled
	if (player == nullptr && (flags3 & MF3_ISMONSTER))
		UZDoom_STAR_OnMonsterKilled(GetClass()->TypeName.GetChars());

	`$1
"@
            if ($piContent -match $oldDie) {
                $piContent = $piContent -replace $oldDie, $newDie
                $piChanged = $true
            }
        }
    }
    if ($piChanged) {
        Set-Content $pInteractionCpp $piContent -NoNewline
        $changes += "p_interaction (monster kill mint)"
    }
}

# 3c2. a_keys.cpp: call UZDoom_STAR_CheckDoorAccess only when the player actually tries to open a door (E key).
#      P_CheckKeys(..., quiet): quiet==true = probe (status bar, automap, map load); quiet==false = player use.
#      We must only invoke STAR when !quiet so we never touch inventory on map load or other probes.
$aKeysCpp = "$src\src\gamedata\a_keys.cpp"
if (Test-Path $aKeysCpp) {
    $akContent = Get-Content $aKeysCpp -Raw
    $akChanged = $false
    if ($akContent -notmatch 'uzdoom_star_integration\.h') {
        $akContent = $akContent -replace '(\#include "g_levellocals\.h")', "`$1`r`n#ifdef OASIS_STAR_API`r`n#include `"uzdoom_star_integration.h`"`r`n#endif"
        $akChanged = $true
    }
    # Insert STAR check: when quiet (status bar/HUD probe) use PlayerHasKey so keys from STAR show; when !quiet use CheckDoorAccess for door open + use.
    $starBlockOld = '\r?\n\s*#ifdef OASIS_STAR_API\r?\n\s*if \(!quiet && UZDoom_STAR_CheckDoorAccess\(owner, keynum, remote\)\) return true;\r?\n\s*#endif'
    $starBlockNew = @'

#ifdef OASIS_STAR_API
	if (quiet) {
		if (UZDoom_STAR_PlayerHasKey(keynum)) return true;
	} else {
		if (UZDoom_STAR_CheckDoorAccess(owner, keynum, remote)) return true;
	}
#endif
'@
    if ($akContent -notmatch 'UZDoom_STAR_PlayerHasKey') {
        # Replace existing STAR block with extended one (door + HUD), or insert if missing.
        if ($akContent -match $starBlockOld) {
            $akContent = $akContent -replace $starBlockOld, $starBlockNew
            $akChanged = $true
        } else {
            # STAR block must sit between "if (lock->check(owner)) return true;" and "if (quiet) return false;" so E on door runs CheckDoorAccess.
            # Try multiple patterns: with/without leading space (UZDoom uses one space before both lines), tabs, \r\n.
            $newBlockNoLead = @'
if (lock->check(owner)) return true;
#ifdef OASIS_STAR_API
	if (quiet) {
		if (UZDoom_STAR_PlayerHasKey(keynum)) return true;
	} else {
		if (UZDoom_STAR_CheckDoorAccess(owner, keynum, remote)) return true;
	}
#endif
	if (quiet) return false;
'@
            $newBlockWithLead = @'
 if (lock->check(owner)) return true;
#ifdef OASIS_STAR_API
	if (quiet) {
		if (UZDoom_STAR_PlayerHasKey(keynum)) return true;
	} else {
		if (UZDoom_STAR_CheckDoorAccess(owner, keynum, remote)) return true;
	}
#endif
	if (quiet) return false;
'@
            $patched = $false
            # Pattern 1: no leading space (GZDoom style)
            if ($akContent -match 'if \(lock->check\(owner\)\) return true;\r?\n(\s+)if \(quiet\) return false;') {
                $akContent = $akContent -replace 'if \(lock->check\(owner\)\) return true;\r?\n(\s+)if \(quiet\) return false;', $newBlockNoLead
                $akChanged = $true; $patched = $true
            }
            # Pattern 2: one space before both lines (UZDoom style: " if (lock->check..." and " if (quiet)...")
            if (-not $patched -and $akContent -match ' if \(lock->check\(owner\)\) return true;\r?\n if \(quiet\) return false;') {
                $akContent = $akContent -replace ' if \(lock->check\(owner\)\) return true;\r?\n if \(quiet\) return false;', $newBlockWithLead
                $akChanged = $true; $patched = $true
            }
            # Pattern 3: any whitespace before first line, then newline, then any whitespace before second line
            if (-not $patched -and $akContent -match '\s+if \(lock->check\(owner\)\) return true;[\r\n]+\s+if \(quiet\) return false;') {
                $akContent = $akContent -replace '\s+if \(lock->check\(owner\)\) return true;[\r\n]+\s+if \(quiet\) return false;', $newBlockWithLead
                $akChanged = $true; $patched = $true
            }
            # Fallback: single replace that captures indentation so we preserve it
            if (-not $patched -and $akContent -match '(\s*)if \(lock->check\(owner\)\) return true;(\r?\n)(\s*)if \(quiet\) return false;') {
                $akContent = $akContent -replace '(\s*)if \(lock->check\(owner\)\) return true;(\r?\n)(\s*)if \(quiet\) return false;', "`$1if (lock->check(owner)) return true;`$2#ifdef OASIS_STAR_API`r`n`tif (quiet) {`r`n`t`tif (UZDoom_STAR_PlayerHasKey(keynum)) return true;`r`n`t} else {`r`n`t`tif (UZDoom_STAR_CheckDoorAccess(owner, keynum, remote)) return true;`r`n`t}`r`n#endif`r`n`$3if (quiet) return false;"
                $akChanged = $true; $patched = $true
            }
            if (-not $patched -and $akContent -match 'lock->check\(owner\)' -and $akContent -notmatch 'UZDoom_STAR') {
                # Last resort: insert STAR block on line after lock->check (match any line endings)
                $akContent = $akContent -replace '(if \(lock->check\(owner\)\) return true;)(\r?\n)(\s*)(if \(quiet\) return false;)', "`$1`$2#ifdef OASIS_STAR_API`r`n`tif (quiet) {`r`n`t`tif (UZDoom_STAR_PlayerHasKey(keynum)) return true;`r`n`t} else {`r`n`t`tif (UZDoom_STAR_CheckDoorAccess(owner, keynum, remote)) return true;`r`n`t}`r`n#endif`r`n`$3`$4"
                $akChanged = $true
            }
            # Literal replace: shared STAR block used for both tab- and space-indent sources (define before flexible/literal use)
            $literalNew = @"
 if (lock->check(owner)) return true;
#ifdef OASIS_STAR_API
	if (quiet) {
		if (UZDoom_STAR_PlayerHasKey(keynum)) return true;
	} else {
		if (UZDoom_STAR_CheckDoorAccess(owner, keynum, remote)) return true;
	}
#endif
	if (quiet) return false;
"@
            # Flexible: any whitespace before both if lines (catches 2 tabs, 3 spaces, etc.)
            if (-not $patched -and $akContent -notmatch 'UZDoom_STAR_CheckDoorAccess') {
                if ($akContent -match '\s+if \(lock->check\(owner\)\) return true;\r?\n\s+if \(quiet\) return false;') {
                    $akContent = $akContent -replace '\s+if \(lock->check\(owner\)\) return true;\r?\n\s+if \(quiet\) return false;', $literalNew
                    $akChanged = $true
                    $patched = $true
                }
            }
            # Literal replace: GZDoom uses one or two tabs before "if"
            if (-not $patched -and $akContent -notmatch 'UZDoom_STAR_CheckDoorAccess') {
                $tab1Lf = "`tif (lock->check(owner)) return true;`n`tif (quiet) return false;"
                $tab1CrLf = "`tif (lock->check(owner)) return true;`r`n`tif (quiet) return false;"
                $tab2Lf = "`t`tif (lock->check(owner)) return true;`n`t`tif (quiet) return false;"
                $tab2CrLf = "`t`tif (lock->check(owner)) return true;`r`n`t`tif (quiet) return false;"
                foreach ($pair in @(
                    @($tab2Lf, $literalNew),
                    @($tab2CrLf, $literalNew),
                    @($tab1Lf, $literalNew),
                    @($tab1CrLf, $literalNew)
                )) {
                    if ($akContent.Contains($pair[0])) {
                        $akContent = $akContent.Replace($pair[0], $pair[1])
                        $akChanged = $true; $patched = $true
                        break
                    }
                }
            }
            # Literal replace (UZDoom trunk exact): one space before both lines
            if (-not $patched -and $akContent -notmatch 'UZDoom_STAR_CheckDoorAccess') {
                $literalOld = " if (lock->check(owner)) return true;`n if (quiet) return false;"
                $literalOldCrLf = " if (lock->check(owner)) return true;`r`n if (quiet) return false;"
                if ($akContent.Contains($literalOld)) {
                    $akContent = $akContent.Replace($literalOld, $literalNew)
                    $akChanged = $true; $patched = $true
                } elseif ($akContent.Contains($literalOldCrLf)) {
                    $akContent = $akContent.Replace($literalOldCrLf, $literalNew)
                    $akChanged = $true; $patched = $true
                }
            }
        }
    }
    if ($akChanged) {
        Set-Content $aKeysCpp $akContent -NoNewline
        $changes += "a_keys (STAR door check only on player use)"
    }
    # Warn if a_keys still has no STAR block (patch patterns did not match)
    if (Test-Path $aKeysCpp) {
        $akFinal = Get-Content $aKeysCpp -Raw
        if ($akFinal -notmatch 'UZDoom_STAR_CheckDoorAccess') {
            Write-Host "[ODOOM] WARNING: a_keys.cpp has no STAR block (E on door will not check STAR). Re-run patch after confirming P_CheckKeys in a_keys.cpp contains: if (lock->check(owner)) return true; then if (quiet) return false;"
        }
    }
}

# 3c3. a_doors.cpp: diagnostic log when EV_DoDoor is about to check keys (confirms door path is reached when pressing E).
#      Classic Doom locked doors use EV_DoDoor(..., lock=arg3); the key check is here, not in P_ActivateLine (line->locknumber is often 0).
$aDoorsCpp = "$src\src\playsim\mapthinkers\a_doors.cpp"
if (Test-Path $aDoorsCpp) {
    $adContent = Get-Content $aDoorsCpp -Raw
    $adChanged = $false
    if ($adContent -notmatch 'uzdoom_star_integration\.h') {
        $adContent = $adContent -replace '(\#include "a_keys\.h")', "`$1`r`n#ifdef OASIS_STAR_API`r`n#include `"uzdoom_star_integration.h`"`r`n#endif"
        $adChanged = $true
    }
    if ($adContent -notmatch 'ODOOM_STAR_LogEvDoDoorLock') {
        $patched = $false
        # Capture leading whitespace + "if (lock..." so we can prepend the diagnostic block
        if ($adContent -match '(\s+)(if \(lock != 0 && !P_CheckKeys \(thing, lock, tag != 0\))') {
            $adContent = $adContent -replace '(\s+)(if \(lock != 0 && !P_CheckKeys \(thing, lock, tag != 0\))', "#ifdef OASIS_STAR_API`r`n`tODOOM_STAR_LogEvDoDoorLock(lock);`r`n#endif`r`n`$1`$2"
            $adChanged = $true
            $patched = $true
        }
        if (-not $patched -and $adContent -match 'if \(lock != 0 && !P_CheckKeys \(thing, lock, tag != 0\)\)') {
            $adContent = $adContent -replace '(if \(lock != 0 && !P_CheckKeys \(thing, lock, tag != 0\))', "#ifdef OASIS_STAR_API`r`n`tODOOM_STAR_LogEvDoDoorLock(lock);`r`n#endif`r`n`t`$1"
            $adChanged = $true
        }
    }
    if ($adChanged) {
        Set-Content $aDoorsCpp $adContent -NoNewline
        $changes += "a_doors (STAR door diagnostic log)"
    }
}

# 3d. CVARINFO: add ODOOM inventory CVars (odoom_inventory_open, odoom_key_*) for ZScript/C++ coordination
if (Test-Path $odoomCvarinfo) {
    $cvarContent = Get-Content $odoomCvarinfo -Raw
    if (Test-Path $cvarinfoTxt) {
        $existing = Get-Content $cvarinfoTxt -Raw
        if ($existing -notmatch 'odoom_inventory_open') {
            Add-Content -Path $cvarinfoTxt -Value "`r`n// ODOOM inventory overlay (OQuake-style key capture)`r`n$cvarContent"
            $changes += "cvarinfo"
        } else {
            # Existing ODOOM block present: append any newly added ODOOM cvar lines that are missing.
            $missingLines = New-Object System.Collections.Generic.List[string]
            foreach ($line in ($cvarContent -split "`r?`n")) {
                $trim = $line.Trim()
                if ([string]::IsNullOrWhiteSpace($trim)) { continue }
                if ($trim.StartsWith("//")) { continue }
                if ($existing -notmatch [regex]::Escape($trim)) {
                    $missingLines.Add($trim)
                }
            }
            if ($missingLines.Count -gt 0) {
                Add-Content -Path $cvarinfoTxt -Value ("`r`n" + ($missingLines -join "`r`n"))
                $changes += "cvarinfo(update)"
            }
        }
    } else {
        Set-Content -Path $cvarinfoTxt -Value $cvarContent -NoNewline
        $changes += "cvarinfo"
    }
}

# 4. Launcher about.txt: fix Release notes (UZDoom entries) and prepend ODOOM entry from global version
$aboutPath = "$src\wadsrc\static\about.txt"
if (Test-Path $aboutPath) {
    $lines = Get-Content $aboutPath
    $releaseIdx = -1
    $nextSectionIdx = $lines.Count
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -eq "# Release notes" -or $lines[$i] -eq "# Version history") { $releaseIdx = $i; break }
    }
    if ($releaseIdx -ge 0) {
        for ($j = $releaseIdx + 1; $j -lt $lines.Count; $j++) {
            if ($lines[$j] -match '^# ') { $nextSectionIdx = $j; break }
        }
        $releaseBlock = ($lines[($releaseIdx + 1)..($nextSectionIdx - 1)] -join "`r`n")
        $releaseBlock = $releaseBlock -replace 'ODOOM version', 'UZDoom version'
        $odoomEntry = "ODOOM version $versionDisplay, released (OASIS build)`r`n`r`nFirst release of ODOOM. UZDoom-based port with OASIS STAR API integration for cross-game features in the OASIS Omniverse. Supports keycard sharing with OQuake, STAR inventory, and interoperable games. By NextGen World Ltd.`r`n`r`n---`r`n`r`n"
        $newReleaseBlock = $odoomEntry + $releaseBlock
        $before = $lines[0..$releaseIdx] -join "`r`n"
        $after = if ($nextSectionIdx -lt $lines.Count) { "`r`n" + ($lines[$nextSectionIdx..($lines.Count - 1)] -join "`r`n") } else { "" }
        $aboutContent = $before + "`r`n" + $newReleaseBlock + $after
        Set-Content -Path $aboutPath -Value $aboutContent -NoNewline
    }
} else {
    if (Test-Path (Join-Path $odoomRoot "about.txt")) {
        Copy-Item -Path (Join-Path $odoomRoot "about.txt") -Destination $aboutPath -Force
    }
}

# 4b. CMake: add star_sync.c to build when OASIS_STAR_API is used (same list as uzdoom_star_integration.cpp)
$cmakeFiles = @()
if (Test-Path "$src\CMakeLists.txt") { $cmakeFiles += "$src\CMakeLists.txt" }
if (Test-Path "$src\src\CMakeLists.txt") { $cmakeFiles += "$src\src\CMakeLists.txt" }
foreach ($cmakePath in $cmakeFiles) {
    if (-not (Test-Path $cmakePath)) { continue }
    $cmakeContent = Get-Content $cmakePath -Raw
    if ($cmakeContent -match 'uzdoom_star_integration\.cpp' -and $cmakeContent -notmatch 'star_sync\.c') {
        $cmakeContent = $cmakeContent -replace '(\buzdoom_star_integration\.cpp\b)', "`$1`r`n    star_sync.c"
        Set-Content -Path $cmakePath -Value $cmakeContent -NoNewline
        $changes += "cmake(star_sync.c)"
    }
}

# 5. Register ODOOM OQUAKE actors in ZScript compile list
if (Test-Path $zscriptTxt) {
    $content = Get-Content $zscriptTxt -Raw
    if (-not ($content -match 'zscript/actors/doom/odoom_oquake_keys\.zs')) {
        $content = $content -replace '(#include "zscript/actors/doom/doomkeys\.zs")', "`$1`r`n#include `"zscript/actors/doom/odoom_oquake_keys.zs`""
    }
    if (-not ($content -match 'zscript/actors/doom/odoom_oquake_items\.zs')) {
        $content = $content -replace '(#include "zscript/actors/doom/odoom_oquake_keys\.zs")', "`$1`r`n#include `"zscript/actors/doom/odoom_oquake_items.zs`""
    }
    if (-not ($content -match 'zscript/ui/statusbar/odoom_inventory_popup\.zs')) {
        $content = $content -replace '(#include "zscript/ui/statusbar/doom_sbar\.zs")', "`$1`r`n#include `"zscript/ui/statusbar/odoom_inventory_popup.zs`""
    }
    Set-Content -Path $zscriptTxt -Value $content -NoNewline
}

# 6. Register ODOOM inventory event handler in MAPINFO gameinfo (safe mode: no UI input interception)
if (Test-Path $commonMapinfo) {
    $content = Get-Content $commonMapinfo -Raw
    if (-not ($content -match 'AddEventHandlers\s*=\s*"OASISInventoryOverlayHandler"')) {
        $content = $content -replace '(Gameinfo\s*\{[\s\S]*?)(\r?\n\})', "`$1`r`n`tAddEventHandlers = `"OASISInventoryOverlayHandler`"`$2"
    }
    Set-Content -Path $commonMapinfo -Value $content -NoNewline
}

# 7. Register DoomEdNums for OQUAKE thing ids in Doom mapinfo
if (Test-Path $doomItemsMapinfo) {
    $content = Get-Content $doomItemsMapinfo -Raw
    if (-not ($content -match '5005\s*=\s*OQGoldKey')) {
        $content = $content -replace '(\r?\n\}\s*)$', "`r`n  5005 = OQGoldKey`r`n  5013 = OQSilverKey`$1"
    }
    if (-not ($content -match '5201\s*=\s*OQShotgun')) {
        $content = $content -replace '(\r?\n\}\s*)$', "`r`n  5201 = OQShotgun`r`n  5202 = OQSuperShotgun`r`n  5203 = OQNailgun`r`n  5204 = OQSuperNailgun`r`n  5205 = OQGrenadeLauncher`r`n  5206 = OQRocketLauncher`r`n  5207 = OQThunderbolt`r`n  5208 = OQNails`r`n  5209 = OQShells`r`n  5210 = OQRockets`r`n  5211 = OQCells`r`n  5212 = OQHealth`r`n  5213 = OQHealthSmall`r`n  5214 = OQArmorGreen`r`n  5215 = OQArmorYellow`r`n  5216 = OQArmorMega`r`n  3010 = OQMonsterDog`r`n  3011 = OQMonsterZombie`r`n  5302 = OQMonsterDemon`r`n  5303 = OQMonsterShambler`r`n  5304 = OQMonsterGrunt`r`n  5305 = OQMonsterFish`r`n  5309 = OQMonsterOgre`r`n  5366 = OQMonsterEnforcer`r`n  5368 = OQMonsterSpawn`r`n  5369 = OQMonsterKnight`$1"
    }
    Set-Content -Path $doomItemsMapinfo -Value $content -NoNewline
}

# 8. Editor button: add centre button between Play and Exit in launcher button bar
$widgetsDest = "$src\src\widgets"
$lbbH = "$widgetsDest\launcherbuttonbar.h"
$lbbCpp = "$widgetsDest\launcherbuttonbar.cpp"
$lwH = "$src\src\widgets\launcherwindow.h"
$lwCpp = "$src\src\widgets\launcherwindow.cpp"

# Restore launcher widget files from git so we always patch from a clean state (fixes any prior broken patch).
$widgetsToRestore = @("$widgetsDest\launcherwindow.cpp", "$widgetsDest\launcherwindow.h", "$widgetsDest\launcherbuttonbar.cpp", "$widgetsDest\launcherbuttonbar.h")
$anyExist = $false
foreach ($f in $widgetsToRestore) { if (Test-Path $f) { $anyExist = $true; break } }
if ($anyExist) {
    $prevDir = Get-Location
    try {
        Set-Location -LiteralPath $src
        $gitOk = $false
        try { $null = git rev-parse --is-inside-work-tree 2>$null; $gitOk = $true } catch {}
        if ($gitOk) {
            $srcRoot = $src.TrimEnd("\", "/")
            foreach ($f in $widgetsToRestore) {
                if (Test-Path -LiteralPath $f) {
                    $rel = $f.Replace($srcRoot, "").Replace("\", "/").TrimStart("/")
                    $null = git checkout -- $rel 2>$null
                }
            }
        }
    } finally { Set-Location -LiteralPath $prevDir.Path }
}

if ((Test-Path $lbbH) -and (Test-Path $lbbCpp)) {
    if (-not (Select-String -Path $lbbH -Pattern "EditorButton" -Quiet)) {
        $hContent = Get-Content $lbbH -Raw
        $hContent = $hContent -replace '(void OnExitButtonClicked\(\);)', "`$1`r`n void OnEditorButtonClicked();"
        $hContent = $hContent -replace '(PushButton\* ExitButton = nullptr;)', "`$1`r`n PushButton* EditorButton = nullptr;"
        Set-Content -Path $lbbH -Value $hContent -NoNewline
    }
    if (-not (Select-String -Path $lbbCpp -Pattern "EditorButton" -Quiet)) {
        $cppContent = Get-Content $lbbCpp -Raw
        $cppContent = $cppContent -replace '(ExitButton = new PushButton\(this\);)', "`$1`r`n EditorButton = new PushButton(this);"
        $cppContent = $cppContent -replace '(ExitButton->OnClick = \[=\]\(\) \{ OnExitButtonClicked\(\); \};)', "`$1`r`n EditorButton->OnClick = [=]() { OnEditorButtonClicked(); };"
        $cppContent = $cppContent -replace '(ExitButton->SetText\(GStrings\.GetString\("PICKER_EXIT"\)\);)', "`$1`r`n EditorButton->SetText(`"Editor`");"
        $cppContent = $cppContent -replace '(return 20\.0 \+ std::max\(PlayButton->GetPreferredHeight\(\), ExitButton->GetPreferredHeight\(\)\);)', "return 20.0 + std::max(PlayButton->GetPreferredHeight(), std::max(ExitButton->GetPreferredHeight(), EditorButton->GetPreferredHeight()));"
        $cppContent = $cppContent -replace '(ExitButton->SetFrameGeometry\(GetWidth\(\) - 20\.0 - 120\.0, 10\.0, 120\.0, PlayButton->GetPreferredHeight\(\)\);)', "ExitButton->SetFrameGeometry(GetWidth() - 20.0 - 120.0, 10.0, 120.0, ExitButton->GetPreferredHeight());`r`n EditorButton->SetFrameGeometry((GetWidth() - 120.0) * 0.5, 10.0, 120.0, EditorButton->GetPreferredHeight());"
        $cppContent = $cppContent -replace '(void LauncherButtonbar::OnExitButtonClicked\(\)\s+\{\s+GetLauncher\(\)->Exit\(\);\s+\})', "`$1`r`n`r`nvoid LauncherButtonbar::OnEditorButtonClicked()`r`n{`r`n GetLauncher()->OnEditorButtonClicked();`r`n}"
        Set-Content -Path $lbbCpp -Value $cppContent -NoNewline
    }
}

if ((Test-Path $lwH) -and (Test-Path $lwCpp)) {
    # Ensure NOMINMAX is defined before windows.h so std::max (e.g. SetFrameGeometry) is not broken
    $lwRaw = [System.IO.File]::ReadAllText($lwCpp)
    if ($lwRaw -match '#include\s+<windows\.h>' -and $lwRaw -notmatch 'NOMINMAX') {
        $lwRaw = $lwRaw -replace '(#ifdef _WIN32\s*\r?\n)\s*#include\s+<windows\.h>', "`$1#define NOMINMAX`r`n#include <windows.h>"
        [System.IO.File]::WriteAllText($lwCpp, $lwRaw)
    }
    if (-not (Select-String -Path $lwH -Pattern "OnEditorButtonClicked" -Quiet)) {
        $hContent = Get-Content $lwH -Raw
        $hContent = $hContent -replace '(void Exit\(\);)', "`$1`r`n void OnEditorButtonClicked();"
        Set-Content -Path $lwH -Value $hContent -NoNewline
    }
    $editorLaunchLines = @(
        '',
        'void LauncherWindow::OnEditorButtonClicked()',
        '{',
        '#ifdef _WIN32',
        '	wchar_t path[MAX_PATH];',
        '	if (GetModuleFileNameW(NULL, path, MAX_PATH) == 0) return;',
        '	std::wstring exePath(path);',
        '	size_t lastSlash = exePath.find_last_of(L"\\/");',
        '	if (lastSlash == std::wstring::npos) return;',
        '	std::wstring dir = exePath.substr(0, lastSlash + 1);',
        '	std::wstring builder = dir + L"Editor\\Builder.exe";',
        '	ShellExecuteW(NULL, L"open", builder.c_str(), NULL, dir.c_str(), SW_SHOW);',
        '#else',
        '	(void)0;',
        '#endif',
        '}',
        ''
    )
    $hasLaunchCode = Select-String -Path $lwCpp -Pattern "GetModuleFileNameW|Editor\\\\Builder" -Quiet
    $hasEditorButton = Select-String -Path $lwCpp -Pattern "OnEditorButtonClicked" -Quiet
    $hasPlaceholder = Select-String -Path $lwCpp -Pattern "placeholder for map" -Quiet
    if (-not $hasEditorButton) {
        $raw = [System.IO.File]::ReadAllText($lwCpp)
        $hasWindowsInclude = $raw -match "#include\s+<windows\.h>"
        $block = "`r`nvoid LauncherWindow::OnEditorButtonClicked()`r`n{`r`n#ifdef _WIN32`r`n`twchar_t path[MAX_PATH];`r`n`tif (GetModuleFileNameW(NULL, path, MAX_PATH) == 0) return;`r`n`tstd::wstring exePath(path);`r`n`tsize_t lastSlash = exePath.find_last_of(L`"\\\\/`");`r`n`tif (lastSlash == std::wstring::npos) return;`r`n`tstd::wstring dir = exePath.substr(0, lastSlash + 1);`r`n`tstd::wstring builder = dir + L`"Editor\\\\Builder.exe`";`r`n`tShellExecuteW(NULL, L`"open`", builder.c_str(), NULL, dir.c_str(), SW_SHOW);`r`n#else`r`n`t(void)0;`r`n#endif`r`n}`r`n`r`n"
        if (-not $hasLaunchCode -and -not $hasWindowsInclude) {
            $raw = $raw -replace '(#include\s+"version\.h")', "`$1`r`n#ifdef _WIN32`r`n#define NOMINMAX`r`n#include <windows.h>`r`n#include <string>`r`n#endif"
        }
        $marker = "void LauncherWindow::UpdateLanguage()"
        $idx = $raw.IndexOf($marker)
        if ($idx -ge 0) {
            $raw = $raw.Insert($idx, $block)
            [System.IO.File]::WriteAllText($lwCpp, $raw)
        }
    } elseif ($hasPlaceholder -and -not $hasLaunchCode) {
        $cppContent = Get-Content $lwCpp -Raw
        $cppContent = $cppContent -replace 'void LauncherWindow::OnEditorButtonClicked\(\)\s*\{\s*// Editor button: placeholder[^}]*\}', ($editorLaunchLines -join "`r`n")
        $cppContent = $cppContent -replace '(#include "version\.h")', "`$1`r`n#ifdef _WIN32`r`n#define NOMINMAX`r`n#include <windows.h>`r`n#include <string>`r`n#endif"
        Set-Content -Path $lwCpp -Value $cppContent -NoNewline
    }
}

