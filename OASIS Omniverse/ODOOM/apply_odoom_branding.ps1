<#
.SYNOPSIS
  Applies ODOOM branding to UZDoom source (version.h, startscreen, status bar).
  Invoked by BUILD ODOOM.bat. Manual: .\apply_odoom_branding.ps1 -UZDOOM_SRC "C:\Source\UZDoom"
#>
param([Parameter(Mandatory=$true)][string]$UZDOOM_SRC)

$src = $UZDOOM_SRC.TrimEnd('\')
$versionH = "$src\src\version.h"
$startscreenCpp = "$src\src\common\startscreen\startscreen.cpp"
$sharedSbarCpp = "$src\src\g_statusbar\shared_sbar.cpp"

$odoomRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
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
	{
		FString verText = GAMENAME " " ODOOM_FULL_VERSION_STR;
		double x = twod->GetWidth() - SmallFont->StringWidth(verText.GetChars()) * CleanXfac - 4;
		double y = twod->GetHeight() - SmallFont->GetHeight() * CleanYfac - 2;
		DrawText(twod, SmallFont, CR_TAN, x, y, verText.GetChars(), DTA_CleanNoMove, true, TAG_DONE);
	}
#endif

	if (state != HUD_AltHud)
"@
        $content = $content -replace $old, $new
        $sbarChanged = $true
    }
    if ($content -match 'OASIS_STAR_API' -and $content -match 'verText.*GetVersionString') {
        $content = $content -replace '(FString verText = GAMENAME;)\r?\n\s+verText \+= " ";\r?\n\s+verText \+= GetVersionString\(\);', 'FString verText = GAMENAME " " ODOOM_FULL_VERSION_STR;'
        $sbarChanged = $true
    }
    if ($sbarChanged) { Set-Content $sharedSbarCpp $content -NoNewline; $changes += "shared_sbar" }
}

