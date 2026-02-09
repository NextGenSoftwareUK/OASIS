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

# 5. Editor button: add centre button between Play and Exit in launcher button bar
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

