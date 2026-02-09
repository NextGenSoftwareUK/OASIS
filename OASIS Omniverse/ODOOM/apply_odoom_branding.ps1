# Apply ODOOM branding patches to UZDoom source (version.h, startscreen.cpp, shared_sbar.cpp).
# Run from BUILD ODOOM.bat with: powershell -ExecutionPolicy Bypass -File "%~dp0apply_odoom_branding.ps1" -UZDOOM_SRC "C:\Source\UZDoom"
param([Parameter(Mandatory=$true)][string]$UZDOOM_SRC)

$src = $UZDOOM_SRC.TrimEnd('\')
$versionH = "$src\src\version.h"
$startscreenCpp = "$src\src\common\startscreen\startscreen.cpp"
$sharedSbarCpp = "$src\src\g_statusbar\shared_sbar.cpp"

function Test-PatchNeeded($path, $marker) {
    if (-not (Test-Path $path)) { return $false }
    return -not (Select-String -Path $path -Pattern $marker -Quiet)
}

# 1. version.h: add #include "odoom_branding.h" when OASIS_STAR_API (before final #endif)
if (Test-PatchNeeded $versionH "odoom_branding") {
    $content = Get-Content $versionH -Raw
    $content = $content -replace '(\r?\n)(#endif //__VERSION_H__)', "`n#ifdef OASIS_STAR_API`n#include `"odoom_branding.h`"`n#endif`n`n`$1`$2"
    Set-Content $versionH $content -NoNewline
    Write-Host "Patched version.h for ODOOM branding."
}

# 2. startscreen.cpp: add version.h include and OASIS_STAR_API header text in CreateHeader
if (Test-Path $startscreenCpp) {
    $content = Get-Content $startscreenCpp -Raw
    $needInclude = -not ($content -match 'include "version\.h"')
    $needCreateHeader = -not ($content -match 'OASIS_STAR_API.*headerTitle')
    if ($needInclude) {
        $content = $content -replace '(\#include "texturemanager\.h")', "`$1`n#include `"version.h`""
        Write-Host "Added version.h include to startscreen.cpp."
    }
    if ($needCreateHeader) {
        $old = 'ClearBlock\(HeaderBitmap, bcolor, 0, 0, HeaderBitmap\.GetWidth\(\), HeaderBitmap\.GetHeight\(\)\);\r?\n\tint textlen = SizeOfText\(GameStartupInfo\.Name\.GetChars\(\)\);\r?\n\tDrawString\(HeaderBitmap, \(HeaderBitmap\.GetWidth\(\) >> 4\) - \(textlen >> 1\), 0\.5, GameStartupInfo\.Name\.GetChars\(\), fcolor, bcolor\);'
        $new = @"
ClearBlock(HeaderBitmap, bcolor, 0, 0, HeaderBitmap.GetWidth(), HeaderBitmap.GetHeight());
#ifdef OASIS_STAR_API
	FString headerTitle = GAMENAME;
	headerTitle += " ";
	headerTitle += GetVersionString();
	int textlen = SizeOfText(headerTitle.GetChars());
	DrawString(HeaderBitmap, (HeaderBitmap.GetWidth() >> 4) - (textlen >> 1), 0.5, headerTitle.GetChars(), fcolor, bcolor);
#else
	int textlen = SizeOfText(GameStartupInfo.Name.GetChars());
	DrawString(HeaderBitmap, (HeaderBitmap.GetWidth() >> 4) - (textlen >> 1), 0.5, GameStartupInfo.Name.GetChars(), fcolor, bcolor);
#endif
"@
        $content = $content -replace $old, $new
        Write-Host "Patched startscreen.cpp CreateHeader for ODOOM."
    }
    if ($needInclude -or $needCreateHeader) { Set-Content $startscreenCpp $content -NoNewline }
}

# 3. shared_sbar.cpp: draw ODOOM + version on HUD when OASIS_STAR_API
if (Test-Path $sharedSbarCpp) {
    $content = Get-Content $sharedSbarCpp -Raw
    if (-not ($content -match 'OASIS_STAR_API.*verText')) {
        $old = '(\t\}\r?\n\r?\n\tif \(state != HUD_AltHud\))'
        $new = @"
	}

#ifdef OASIS_STAR_API
	{
		FString verText = GAMENAME;
		verText += " ";
		verText += GetVersionString();
		double x = twod->GetWidth() - SmallFont->StringWidth(verText.GetChars()) * CleanXfac - 4;
		double y = twod->GetHeight() - SmallFont->GetHeight() * CleanYfac - 2;
		DrawText(twod, SmallFont, CR_TAN, x, y, verText.GetChars(), DTA_CleanNoMove, true, TAG_DONE);
	}
#endif

	if (state != HUD_AltHud)
"@
        $content = $content -replace $old, $new
        Set-Content $sharedSbarCpp $content -NoNewline
        Write-Host "Patched shared_sbar.cpp for ODOOM HUD."
    }
}
