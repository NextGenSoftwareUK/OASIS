@echo off
setlocal EnableExtensions
REM ODOOM - UZDoom + OASIS STAR API. Credit: UZDoom (GPL-3.0). See CREDITS_AND_LICENSE.md.
REM Usage: BUILD ODOOM.bat [ run ] [ nosprites ]
REM   (none) = prompt clean/incremental, then copy, branding, build
REM   run    = incremental build then launch (no prompt)
REM   nosprites = skip sprite/icon regeneration for faster builds

if /i "%~1"=="__logrun" (
    shift
    goto :main
)

REM Keep interactive mode fully visible/responsive.
REM Auto-log wrapping is enabled for non-interactive "run" mode.
if /i not "%~1"=="run" goto :main

set "ODOOM_LOG_DIR=%~dp0build_logs"
if not exist "%ODOOM_LOG_DIR%" mkdir "%ODOOM_LOG_DIR%"
for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set "ODOOM_LOG_TS=%%I"
set "ODOOM_BUILD_LOG=%ODOOM_LOG_DIR%\odoom_build_%ODOOM_LOG_TS%.log"
echo [ODOOM][INFO] Writing build log to: "%ODOOM_BUILD_LOG%"
call "%~f0" __logrun %* > "%ODOOM_BUILD_LOG%" 2>&1
set "ODOOM_BUILD_EXIT=%ERRORLEVEL%"
type "%ODOOM_BUILD_LOG%"
echo [ODOOM][INFO] Build log saved: "%ODOOM_BUILD_LOG%"
exit /b %ODOOM_BUILD_EXIT%

:main

set "UZDOOM_SRC=C:\Source\UZDoom"
set "HERE=%~dp0"
set "STARAPICLIENT=%HERE%..\STARAPIClient"
set "ODOOM_INTEGRATION=%HERE%"
set "DOOM_FOLDER=%ODOOM_INTEGRATION%"
set "ULTIMATE_DOOM_BUILDER_BUILD=C:\Source\UltimateDoomBuilder\Build"
set "ULTIMATE_DOOM_BUILDER_ASSETS=C:\Source\UltimateDoomBuilder\Assets\Common\UDBScript\Scripts\OASIS\Sprites"
set "OASIS_SPRITES_SRC=%ULTIMATE_DOOM_BUILDER_BUILD%\UDBScript\Scripts\OASIS\Sprites"
if exist "%ULTIMATE_DOOM_BUILDER_ASSETS%\5005.png" set "OASIS_SPRITES_SRC=%ULTIMATE_DOOM_BUILDER_ASSETS%"

REM Generate ODOOM version from ODOOM/odoom_version.txt
if exist "%ODOOM_INTEGRATION%generate_odoom_version.ps1" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%generate_odoom_version.ps1" -Root "%ODOOM_INTEGRATION%"
set "VERSION_DISPLAY=1.0 (Build 1)"
if exist "%ODOOM_INTEGRATION%version_display.txt" for /f "usebackq delims=" %%a in ("%ODOOM_INTEGRATION%version_display.txt") do set "VERSION_DISPLAY=%%a"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$v=$env:VERSION_DISPLAY; if(-not $v){$v='1.0 (Build 1)'}; $w=60; function c($s){$p=[math]::Max(0,[int](($w-$s.Length)/2)); '  '+(' '*$p)+$s}; Write-Host ''; Write-Host ('  '+('='*$w)) -ForegroundColor DarkCyan; Write-Host (c('O A S I S   O D O O M  v'+$v)) -ForegroundColor Cyan; Write-Host (c('By NextGen World Ltd')) -ForegroundColor DarkGray; Write-Host ('  '+('='*$w)) -ForegroundColor DarkCyan; Write-Host (c('Enabling full interoperable games across the OASIS Omniverse!')) -ForegroundColor DarkMagenta; Write-Host ''"

set "DO_FULL_CLEAN=0"
set "DO_SPRITE_REGEN=1"
set "SKIP_SPRITE_PROMPT=0"
set "OQ_MONSTER_PAD=0"
set "OQ_ITEM_PAD=0"
set "QUAKE_PAK0=C:\Program Files (x86)\Steam\steamapps\common\Quake\id1\PAK0.PAK"
set "QUAKE_PAK1=C:\Program Files (x86)\Steam\steamapps\common\Quake\id1\PAK1.PAK"
if /i not "%~1"=="run" (
    echo.
    set /p "BUILD_CHOICE=  Full clean/rebuild (C) or incremental build (I)? [I]: "
)
if not defined BUILD_CHOICE set "BUILD_CHOICE=I"
if /i "%BUILD_CHOICE%"=="C" set "DO_FULL_CLEAN=1"
if /i "%~1"=="nosprites" set "DO_SPRITE_REGEN=0" & set "SKIP_SPRITE_PROMPT=1"
if /i "%~2"=="nosprites" set "DO_SPRITE_REGEN=0" & set "SKIP_SPRITE_PROMPT=1"
if "%SKIP_SPRITE_PROMPT%"=="0" if /i not "%~1"=="run" (
    echo.
    set /p "SPRITE_CHOICE=  Regenerate sprites/icons this build (Y/N)? [Y]: "
    if not defined SPRITE_CHOICE set "SPRITE_CHOICE=Y"
    if /i "%SPRITE_CHOICE%"=="N" set "DO_SPRITE_REGEN=0"
    if /i "%SPRITE_CHOICE%"=="NO" set "DO_SPRITE_REGEN=0"
)
if "%DO_SPRITE_REGEN%"=="0" (
    if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKGA0.png" set "DO_SPRITE_REGEN=1"
    if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQW1A0.png" set "DO_SPRITE_REGEN=1"
    if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM1A0.png" set "DO_SPRITE_REGEN=1"
    if "%DO_SPRITE_REGEN%"=="1" echo [ODOOM][NOTE] Required OQ runtime sprites missing in UZDoom; enabling sprite/icon regeneration automatically.
)

REM --- Prerequisites ---
if not exist "%UZDOOM_SRC%\src\d_main.cpp" (
    echo UZDoom source not found: %UZDOOM_SRC%
    echo Edit UZDOOM_SRC at top of script.
    pause
    exit /b 1
)
if not exist "%ODOOM_INTEGRATION%\star_api.dll" (
    if exist "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
        echo [ODOOM] star_api not found. Building and deploying STARAPIClient...
        call "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
        if errorlevel 1 (echo [ODOOM] BUILD_AND_DEPLOY_STAR_CLIENT.bat failed. & pause & exit /b 1)
    )
)
if not exist "%ODOOM_INTEGRATION%\star_api.dll" (
    echo star_api not found: %ODOOM_INTEGRATION%
    echo Run BUILD_AND_DEPLOY_STAR_CLIENT.bat from OASIS Omniverse, or copy star_api.dll and star_api.lib into the ODOOM folder.
    pause
    exit /b 1
)
if not exist "%ODOOM_INTEGRATION%\star_api.lib" (
    echo star_api.lib not found: %ODOOM_INTEGRATION%
    pause
    exit /b 1
)
if not exist "%STARAPICLIENT%\star_api.h" (
    echo star_api.h not found: %STARAPICLIENT%
    pause
    exit /b 1
)

set "PYTHON3_EXE="
for /f "tokens=*" %%i in ('python -c "import sys; print(sys.executable)" 2^>nul') do set "PYTHON3_EXE=%%i"
if not defined PYTHON3_EXE for /f "tokens=*" %%i in ('py -3 -c "import sys; print(sys.executable)" 2^>nul') do set "PYTHON3_EXE=%%i"
if not defined PYTHON3_EXE if exist "%LocalAppData%\Programs\Python\Python312\python.exe" set "PYTHON3_EXE=%LocalAppData%\Programs\Python\Python312\python.exe"
if not defined PYTHON3_EXE if exist "%LocalAppData%\Programs\Python\Python311\python.exe" set "PYTHON3_EXE=%LocalAppData%\Programs\Python\Python311\python.exe"
if not defined PYTHON3_EXE if exist "%ProgramFiles%\Python312\python.exe" set "PYTHON3_EXE=%ProgramFiles%\Python312\python.exe"
if not defined PYTHON3_EXE if exist "%ProgramFiles%\Python311\python.exe" set "PYTHON3_EXE=%ProgramFiles%\Python311\python.exe"
if not defined PYTHON3_EXE (
    echo Python 3 required. Install from https://www.python.org/downloads/ and add to PATH.
    pause
    exit /b 1
)
REM --- star_sync (generic async layer from STARAPIClient) ---
set "STARAPICLIENT=%HERE%..\STARAPIClient"
if exist "%STARAPICLIENT%\star_sync.c" (
    copy /Y "%STARAPICLIENT%\star_sync.c" "%ODOOM_INTEGRATION%\" >nul
    copy /Y "%STARAPICLIENT%\star_sync.h" "%ODOOM_INTEGRATION%\" >nul
)
echo.
echo [ODOOM][STEP] Installing integration files...
echo [ODOOM][INFO] OASIS sprite source: %OASIS_SPRITES_SRC%
copy /Y "%ODOOM_INTEGRATION%uzdoom_star_integration.cpp" "%UZDOOM_SRC%\src\uzdoom_star_integration.cpp" >nul
copy /Y "%ODOOM_INTEGRATION%uzdoom_star_integration.h" "%UZDOOM_SRC%\src\uzdoom_star_integration.h" >nul
if exist "%ODOOM_INTEGRATION%star_sync.c" copy /Y "%ODOOM_INTEGRATION%star_sync.c" "%UZDOOM_SRC%\src\star_sync.c" >nul
if exist "%ODOOM_INTEGRATION%star_sync.h" copy /Y "%ODOOM_INTEGRATION%star_sync.h" "%UZDOOM_SRC%\src\star_sync.h" >nul
copy /Y "%ODOOM_INTEGRATION%odoom_branding.h" "%UZDOOM_SRC%\src\odoom_branding.h" >nul
copy /Y "%ODOOM_INTEGRATION%odoom_oquake_keys.zs" "%UZDOOM_SRC%\wadsrc\static\zscript\actors\doom\odoom_oquake_keys.zs" >nul
copy /Y "%ODOOM_INTEGRATION%odoom_oquake_items.zs" "%UZDOOM_SRC%\wadsrc\static\zscript\actors\doom\odoom_oquake_items.zs" >nul
copy /Y "%ODOOM_INTEGRATION%odoom_inventory_popup.zs" "%UZDOOM_SRC%\wadsrc\static\zscript\ui\statusbar\odoom_inventory_popup.zs" >nul
if not exist "%UZDOOM_SRC%\wadsrc\static\textures" mkdir "%UZDOOM_SRC%\wadsrc\static\textures"
if exist "%ODOOM_INTEGRATION%face_anorak.png" (
    echo [ODOOM][STEP] Preparing anorak HUD face from face_anorak.png ^(target 34x30^)...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_face_texture.ps1" -SourcePath "%ODOOM_INTEGRATION%face_anorak.png" -DestPath "%ODOOM_INTEGRATION%textures\OASFACE.png" -Width 34 -Height 30 -OffsetX 1 -OffsetY 1
    if errorlevel 1 echo [ODOOM][WARN] face_anorak.png processing failed; using existing OASFACE.png if present.
) else (
    echo [ODOOM][NOTE] face_anorak.png not found in ODOOM root; using existing textures\OASFACE.png.
)
if exist "%ODOOM_INTEGRATION%textures\OASFACE.png" copy /Y "%ODOOM_INTEGRATION%textures\OASFACE.png" "%UZDOOM_SRC%\wadsrc\static\textures\OASFACE.png" >nul
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites" mkdir "%UZDOOM_SRC%\wadsrc\static\sprites"
if not exist "%UZDOOM_SRC%\wadsrc\static\graphics" mkdir "%UZDOOM_SRC%\wadsrc\static\graphics"
if "%DO_SPRITE_REGEN%"=="1" goto :regen_sprites
echo [ODOOM][NOTE] Sprite/icon regeneration disabled - nosprites mode.
goto :after_sprites

:regen_sprites
REM Gold key: only generate if output missing
if exist "%OASIS_SPRITES_SRC%\5005.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKGA0.png" (
    echo [ODOOM][STEP] Regenerating OQ gold key sprites...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5005.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQKGA0.png" -MaxWidth 18 -MaxHeight 24
    echo [ODOOM][DONE] OQ gold key sprite generation complete.
)
if exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKGA0.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKGB0.png" copy /Y "%UZDOOM_SRC%\wadsrc\static\sprites\OQKGA0.png" "%UZDOOM_SRC%\wadsrc\static\sprites\OQKGB0.png" >nul
if exist "%OASIS_SPRITES_SRC%\5005.png" if exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKGA0.png" echo [ODOOM][SKIP] OQ gold key sprites already generated.
if not exist "%OASIS_SPRITES_SRC%\5005.png" echo [ODOOM][NOTE] Sprite step skipped: gold key source 5005.png not found in %OASIS_SPRITES_SRC%.

REM Silver key: only generate if output missing
if exist "%OASIS_SPRITES_SRC%\5013.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKSA0.png" (
    echo [ODOOM][STEP] Regenerating OQ silver key sprites...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5013.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQKSA0.png" -MaxWidth 18 -MaxHeight 24
    echo [ODOOM][DONE] OQ silver key sprite generation complete.
)
if exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKSA0.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKSB0.png" copy /Y "%UZDOOM_SRC%\wadsrc\static\sprites\OQKSA0.png" "%UZDOOM_SRC%\wadsrc\static\sprites\OQKSB0.png" >nul
if exist "%OASIS_SPRITES_SRC%\5013.png" if exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKSA0.png" echo [ODOOM][SKIP] OQ silver key sprites already generated.
if not exist "%OASIS_SPRITES_SRC%\5013.png" echo [ODOOM][NOTE] Sprite step skipped: silver key source 5013.png not found in %OASIS_SPRITES_SRC%.

REM Non-key OQUAKE sprites - only generate if output missing
if exist "%OASIS_SPRITES_SRC%\5201.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQW1A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5201.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQW1A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5202.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQW2A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5202.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQW2A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5203.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQW3A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5203.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQW3A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5204.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQW4A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5204.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQW4A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5205.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQW5A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5205.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQW5A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5206.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQW6A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5206.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQW6A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5207.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQW7A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5207.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQW7A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%

if exist "%OASIS_SPRITES_SRC%\5208.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQA1A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5208.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQA1A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5209.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQA2A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5209.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQA2A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5210.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQA3A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5210.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQA3A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5211.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQA4A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5211.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQA4A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%

if exist "%OASIS_SPRITES_SRC%\5212.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQH1A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5212.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQH1A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5213.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQH2A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5213.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQH2A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5214.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQH3A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5214.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQH3A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5215.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQH4A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5215.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQH4A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%
if exist "%OASIS_SPRITES_SRC%\5216.png" if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQH5A0.png" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%prepare_odoom_key_sprite.ps1" -SourcePath "%OASIS_SPRITES_SRC%\5216.png" -DestPath "%UZDOOM_SRC%\wadsrc\static\sprites\OQH5A0.png" -MaxWidth 24 -MaxHeight 24 -PadBottom %OQ_ITEM_PAD%

if not exist "%QUAKE_PAK0%" goto :missing_quake_pak0
if not exist "%QUAKE_PAK1%" goto :missing_quake_pak1
REM OQ monster sprites from Quake MDL - script outputs A1 etc. Skip if first file exists.
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM1A1.png" (
    echo [ODOOM][STEP] Generating OQ monster sprites: dog OQM1...
    "%PYTHON3_EXE%" "%ODOOM_INTEGRATION%generate_oquake_mdl_sprites.py" --mdl-pak "%QUAKE_PAK0%" --mdl-entry "progs/dog.mdl" --palette-pak "%QUAKE_PAK0%" --palette-entry "gfx/palette.lmp" --out-dir "%UZDOOM_SRC%\wadsrc\static\sprites" --sprite-prefix OQM1 --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest
    if errorlevel 1 goto :mdlgen_failed
) else echo [ODOOM][SKIP] OQ monster OQM1 already generated.
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM2A1.png" (
    echo [ODOOM][STEP] Generating OQ monster sprites: zombie OQM2...
    "%PYTHON3_EXE%" "%ODOOM_INTEGRATION%generate_oquake_mdl_sprites.py" --mdl-pak "%QUAKE_PAK0%" --mdl-entry "progs/zombie.mdl" --palette-pak "%QUAKE_PAK0%" --palette-entry "gfx/palette.lmp" --out-dir "%UZDOOM_SRC%\wadsrc\static\sprites" --sprite-prefix OQM2 --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest
    if errorlevel 1 goto :mdlgen_failed
) else echo [ODOOM][SKIP] OQ monster OQM2 already generated.
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM3A1.png" (
    "%PYTHON3_EXE%" "%ODOOM_INTEGRATION%generate_oquake_mdl_sprites.py" --mdl-pak "%QUAKE_PAK0%" --mdl-entry "progs/demon.mdl" --palette-pak "%QUAKE_PAK0%" --palette-entry "gfx/palette.lmp" --out-dir "%UZDOOM_SRC%\wadsrc\static\sprites" --sprite-prefix OQM3 --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest
    if errorlevel 1 goto :mdlgen_failed
) else echo [ODOOM][SKIP] OQ monster OQM3 already generated.
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM4A1.png" (
    "%PYTHON3_EXE%" "%ODOOM_INTEGRATION%generate_oquake_mdl_sprites.py" --mdl-pak "%QUAKE_PAK0%" --mdl-entry "progs/shambler.mdl" --palette-pak "%QUAKE_PAK0%" --palette-entry "gfx/palette.lmp" --out-dir "%UZDOOM_SRC%\wadsrc\static\sprites" --sprite-prefix OQM4 --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest
    if errorlevel 1 goto :mdlgen_failed
) else echo [ODOOM][SKIP] OQ monster OQM4 already generated.
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM5A1.png" (
    "%PYTHON3_EXE%" "%ODOOM_INTEGRATION%generate_oquake_mdl_sprites.py" --mdl-pak "%QUAKE_PAK0%" --mdl-entry "progs/soldier.mdl" --palette-pak "%QUAKE_PAK0%" --palette-entry "gfx/palette.lmp" --out-dir "%UZDOOM_SRC%\wadsrc\static\sprites" --sprite-prefix OQM5 --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest
    if errorlevel 1 goto :mdlgen_failed
) else echo [ODOOM][SKIP] OQ monster OQM5 already generated.
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM6A1.png" (
    "%PYTHON3_EXE%" "%ODOOM_INTEGRATION%generate_oquake_mdl_sprites.py" --mdl-pak "%QUAKE_PAK1%" --mdl-entry "progs/fish.mdl" --palette-pak "%QUAKE_PAK0%" --palette-entry "gfx/palette.lmp" --out-dir "%UZDOOM_SRC%\wadsrc\static\sprites" --sprite-prefix OQM6 --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest
    if errorlevel 1 goto :mdlgen_failed
) else echo [ODOOM][SKIP] OQ monster OQM6 already generated.
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM7A1.png" (
    "%PYTHON3_EXE%" "%ODOOM_INTEGRATION%generate_oquake_mdl_sprites.py" --mdl-pak "%QUAKE_PAK0%" --mdl-entry "progs/ogre.mdl" --palette-pak "%QUAKE_PAK0%" --palette-entry "gfx/palette.lmp" --out-dir "%UZDOOM_SRC%\wadsrc\static\sprites" --sprite-prefix OQM7 --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest
    if errorlevel 1 goto :mdlgen_failed
) else echo [ODOOM][SKIP] OQ monster OQM7 already generated.
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM8A1.png" (
    "%PYTHON3_EXE%" "%ODOOM_INTEGRATION%generate_oquake_mdl_sprites.py" --mdl-pak "%QUAKE_PAK1%" --mdl-entry "progs/enforcer.mdl" --palette-pak "%QUAKE_PAK0%" --palette-entry "gfx/palette.lmp" --out-dir "%UZDOOM_SRC%\wadsrc\static\sprites" --sprite-prefix OQM8 --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest
    if errorlevel 1 goto :mdlgen_failed
) else echo [ODOOM][SKIP] OQ monster OQM8 already generated.
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM9A1.png" (
    "%PYTHON3_EXE%" "%ODOOM_INTEGRATION%generate_oquake_mdl_sprites.py" --mdl-pak "%QUAKE_PAK1%" --mdl-entry "progs/tarbaby.mdl" --palette-pak "%QUAKE_PAK0%" --palette-entry "gfx/palette.lmp" --out-dir "%UZDOOM_SRC%\wadsrc\static\sprites" --sprite-prefix OQM9 --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest
    if errorlevel 1 goto :mdlgen_failed
) else echo [ODOOM][SKIP] OQ monster OQM9 already generated.
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQMAA1.png" (
    "%PYTHON3_EXE%" "%ODOOM_INTEGRATION%generate_oquake_mdl_sprites.py" --mdl-pak "%QUAKE_PAK1%" --mdl-entry "progs/hknight.mdl" --palette-pak "%QUAKE_PAK0%" --palette-entry "gfx/palette.lmp" --out-dir "%UZDOOM_SRC%\wadsrc\static\sprites" --sprite-prefix OQMA --width 160 --height 160 --angles 8 --profile zombieman --yaw-offset-deg 90 --no-manifest
    if errorlevel 1 goto :mdlgen_failed
) else echo [ODOOM][SKIP] OQ monster OQMA already generated.
goto :after_quake_monsters

:missing_quake_pak0
echo [ODOOM][ERROR] Quake pak0 not found for MDL sprite generation.
echo [ODOOM][ERROR] Expected path: "%QUAKE_PAK0%"
pause
exit /b 1

:missing_quake_pak1
echo [ODOOM][ERROR] Quake pak1 not found for MDL sprite generation.
echo [ODOOM][ERROR] Expected path: "%QUAKE_PAK1%"
pause
exit /b 1

:mdlgen_failed
echo [ODOOM][ERROR] Failed to generate one or more Doom-profile OQ monster sprite sets.
pause
exit /b 1

:after_quake_monsters

REM OQ HUD key icons - only generate if output missing
if not exist "%UZDOOM_SRC%\wadsrc\static\graphics\OQKGI0.png" (
    echo [ODOOM][STEP] Generating OQ HUD key icons...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%generate_odoom_hud_key_icons.ps1" -OutputDir "%UZDOOM_SRC%\wadsrc\static\graphics"
    echo [ODOOM][DONE] OQ HUD key icons generated.
) else echo [ODOOM][SKIP] OQ HUD key icons already generated.

:after_sprites
echo [ODOOM][STEP] Verifying required OQ runtime sprites...
set "REQ_OQ_MISSING=0"
set "OQW_COUNT=0"
set "OQH_COUNT=0"
set "OQM_COUNT=0"
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKGA0.png" echo [ODOOM][ERROR] Missing required sprite: OQKGA0.png & set "REQ_OQ_MISSING=1"
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQKSA0.png" echo [ODOOM][ERROR] Missing required sprite: OQKSA0.png & set "REQ_OQ_MISSING=1"
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQW1A0.png" echo [ODOOM][ERROR] Missing required sprite: OQW1A0.png & set "REQ_OQ_MISSING=1"
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQH1A0.png" echo [ODOOM][ERROR] Missing required sprite: OQH1A0.png & set "REQ_OQ_MISSING=1"
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM1A1.png" echo [ODOOM][ERROR] Missing required sprite: OQM1A1.png & set "REQ_OQ_MISSING=1"
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQM2A1.png" echo [ODOOM][ERROR] Missing required sprite: OQM2A1.png & set "REQ_OQ_MISSING=1"
if not exist "%UZDOOM_SRC%\wadsrc\static\sprites\OQMAA1.png" echo [ODOOM][ERROR] Missing required sprite: OQMAA1.png & set "REQ_OQ_MISSING=1"
for %%I in ("%UZDOOM_SRC%\wadsrc\static\sprites\OQW*A0.png") do set /a OQW_COUNT+=1
for %%I in ("%UZDOOM_SRC%\wadsrc\static\sprites\OQH*A0.png") do set /a OQH_COUNT+=1
for %%I in ("%UZDOOM_SRC%\wadsrc\static\sprites\OQM*.png") do set /a OQM_COUNT+=1
if not defined OQW_COUNT set "OQW_COUNT=0"
if not defined OQH_COUNT set "OQH_COUNT=0"
if not defined OQM_COUNT set "OQM_COUNT=0"
echo [ODOOM][INFO] Sprite counts: OQW=%OQW_COUNT% OQH=%OQH_COUNT% OQM=%OQM_COUNT%
if "%REQ_OQ_MISSING%"=="1" (
    echo [ODOOM][ERROR] Required OQ sprites are missing. Re-run with sprite regeneration enabled.
    pause
    exit /b 1
)
echo [ODOOM][DONE] OQ runtime sprite verification passed.
if exist "%ODOOM_INTEGRATION%odoom_version_generated.h" copy /Y "%ODOOM_INTEGRATION%odoom_version_generated.h" "%UZDOOM_SRC%\src\odoom_version_generated.h" >nul
powershell -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%patch_uzdoom_engine.ps1" -UZDOOM_SRC "%UZDOOM_SRC%"
if exist "%ODOOM_INTEGRATION%oasis_banner.png" (
    copy /Y "%ODOOM_INTEGRATION%oasis_banner.png" "%UZDOOM_SRC%\wadsrc\static\ui\banner-dark.png" >nul
    copy /Y "%ODOOM_INTEGRATION%oasis_banner.png" "%UZDOOM_SRC%\wadsrc\static\ui\banner-light.png" >nul
    copy /Y "%ODOOM_INTEGRATION%oasis_banner.png" "%UZDOOM_SRC%\wadsrc\static\graphics\bootlogo.png" >nul
)

echo.
if "%DO_FULL_CLEAN%"=="1" (
    echo [ODOOM][STEP] Full clean...
    if exist "%UZDOOM_SRC%\build" rmdir /s /q "%UZDOOM_SRC%\build" & echo   build removed
)
echo [ODOOM][STEP] Configuring CMake and STAR API...
cd /d "%UZDOOM_SRC%"
if not exist build mkdir build
cd build
set "STAR_API_DIR=%STARAPICLIENT%"
set "STAR_API_LIB_DIR=%DOOM_FOLDER%"
echo [ODOOM][INFO] CMake STAR_API_DIR="%STAR_API_DIR%"
echo [ODOOM][INFO] CMake STAR_API_LIB_DIR="%STAR_API_LIB_DIR%"
echo [ODOOM][INFO] CMake Python3_EXECUTABLE="%PYTHON3_EXE%"
cmake .. -G "Visual Studio 17 2022" -A x64 -DOASIS_STAR_API=ON -DSTAR_API_DIR:PATH="%STAR_API_DIR%" -DSTAR_API_LIB_DIR:PATH="%STAR_API_LIB_DIR%" -DPython3_EXECUTABLE:FILEPATH="%PYTHON3_EXE%"
if errorlevel 1 (echo [ODOOM][ERROR] CMake failed. & pause & exit /b 1)

echo.
echo [ODOOM][STEP] Building Release...
cmake --build . --config Release
if errorlevel 1 (echo [ODOOM][ERROR] Build failed. & pause & exit /b 1)

echo.
echo [ODOOM][STEP] Packaging output...
REM Package current OASFACE texture into odoom_face.pk3 for standalone distribution
echo [ODOOM][STEP] Packaging OASIS beamed-in face (OASFACE)...
"%PYTHON3_EXE%" "%ODOOM_INTEGRATION%create_odoom_face_pk3.py"
if errorlevel 1 echo [ODOOM][WARN] OASFACE pk3 generation failed - beamed-in face may be missing.

copy /Y "%DOOM_FOLDER%\star_api.dll" "%UZDOOM_SRC%\build\Release\star_api.dll" >nul
if not exist "%ODOOM_INTEGRATION%build" mkdir "%ODOOM_INTEGRATION%build"
xcopy "%UZDOOM_SRC%\build\Release\*" "%ODOOM_INTEGRATION%build" /Y /I /Q /E >nul
copy /Y "%UZDOOM_SRC%\build\Release\uzdoom.exe" "%ODOOM_INTEGRATION%build\ODOOM.exe" >nul
copy /Y "%DOOM_FOLDER%\star_api.dll" "%ODOOM_INTEGRATION%build\star_api.dll" >nul
if exist "%ODOOM_INTEGRATION%odoom_face.pk3" copy /Y "%ODOOM_INTEGRATION%odoom_face.pk3" "%ODOOM_INTEGRATION%build\odoom_face.pk3" >nul
if exist "%ODOOM_INTEGRATION%build\uzdoom.exe" del "%ODOOM_INTEGRATION%build\uzdoom.exe"
if exist "%ODOOM_INTEGRATION%soft_oal.dll" copy /Y "%ODOOM_INTEGRATION%soft_oal.dll" "%ODOOM_INTEGRATION%build\soft_oal.dll" >nul

if not exist "%ODOOM_INTEGRATION%build\Editor" mkdir "%ODOOM_INTEGRATION%build\Editor"
if not exist "%ODOOM_INTEGRATION%build\Editor\Builder.exe" (
    if exist "%ULTIMATE_DOOM_BUILDER_BUILD%\Builder.exe" (
        echo [ODOOM][STEP] Copying Ultimate Doom Builder into build\Editor...
        xcopy "%ULTIMATE_DOOM_BUILDER_BUILD%\*" "%ODOOM_INTEGRATION%build\Editor" /Y /I /Q /E >nul
    ) else (
        echo [ODOOM][NOTE] Editor folder empty and %ULTIMATE_DOOM_BUILDER_BUILD% not found. Put Builder.exe in build\Editor for Editor button.
    )
)
if exist "%ODOOM_INTEGRATION%copy_builder_native.ps1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%copy_builder_native.ps1" -EditorDir "%ODOOM_INTEGRATION%build\Editor" -UltimateDoomBuilderRoot "C:\Source\UltimateDoomBuilder"
) else if exist "%ODOOM_INTEGRATION%build\Editor\Builder.exe" if not exist "%ODOOM_INTEGRATION%build\Editor\BuilderNative.dll" (
    if exist "%ULTIMATE_DOOM_BUILDER_BUILD%\BuilderNative.dll" copy /Y "%ULTIMATE_DOOM_BUILDER_BUILD%\BuilderNative.dll" "%ODOOM_INTEGRATION%build\Editor\" >nul
    if exist "%ULTIMATE_DOOM_BUILDER_BUILD%\x64\BuilderNative.dll" copy /Y "%ULTIMATE_DOOM_BUILDER_BUILD%\x64\BuilderNative.dll" "%ODOOM_INTEGRATION%build\Editor\" >nul
)

echo.
echo ---
echo [ODOOM][DONE] ODOOM ready: %ODOOM_INTEGRATION%build\ODOOM.exe
echo [ODOOM][INFO] Put doom2.wad in build folder. odoom_face.pk3 is included for beamed-in status bar face.
echo [ODOOM][INFO] Use "BUILD & RUN ODOOM.bat" to launch.
echo ---

if /i "%~1"=="run" (
    if exist "%ODOOM_INTEGRATION%build\ODOOM.exe" (
        echo Launching ODOOM...
        start "" "%ODOOM_INTEGRATION%build\ODOOM.exe"
    ) else (
        start "" "%UZDOOM_SRC%\build\Release\uzdoom.exe"
    )
)
pause
