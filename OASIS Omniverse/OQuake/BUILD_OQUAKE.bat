@echo off
setlocal EnableDelayedExpansion
REM OQuake - vkQuake + OASIS STAR API. Credit: Novum/vkQuake (GPL-2.0). See CREDITS_AND_LICENSE.md.
REM Usage: BUILD_OQUAKE.bat [ run | batch ]
REM   (none) = prompt clean/incremental, then copy, patch, build
REM   run    = incremental build then launch (no prompts)
REM   batch  = incremental build, no prompts, do not launch (for BUILD EVERYTHING.bat)

set "QUAKE_SRC=C:\Source\quake-rerelease-qc"
set "VKQUAKE_SRC=C:\Source\vkQuake"
set "QUAKE_ENGINE_EXE="
set "HERE=%~dp0"
set "DOOM_FOLDER=%HERE%..\Doom"
set "STARAPICLIENT=%HERE%..\STARAPIClient"
set "OQUAKE_INTEGRATION=%HERE%"

REM Generate OQuake version from OQuake/oquake_version.txt
if exist "%OQUAKE_INTEGRATION%generate_oquake_version.ps1" powershell -NoProfile -ExecutionPolicy Bypass -File "%OQUAKE_INTEGRATION%generate_oquake_version.ps1" -Root "%OQUAKE_INTEGRATION%"
set "VERSION_DISPLAY=1.0 (Build 1)"
if exist "%OQUAKE_INTEGRATION%version_display.txt" for /f "usebackq delims=" %%a in ("%OQUAKE_INTEGRATION%version_display.txt") do set "VERSION_DISPLAY=%%a"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$v=$env:VERSION_DISPLAY; if(-not $v){$v='1.0 (Build 1)'}; $w=60; function c($s){$p=[math]::Max(0,[int](($w-$s.Length)/2)); '  '+(' '*$p)+$s}; Write-Host ''; Write-Host ('  '+('='*$w)) -ForegroundColor DarkCyan; Write-Host (c('O A S I S   O Q U A K E  v'+$v)) -ForegroundColor Cyan; Write-Host (c('By NextGen World Ltd')) -ForegroundColor DarkGray; Write-Host ('  '+('='*$w)) -ForegroundColor DarkCyan; Write-Host (c('Enabling full interoperable games across the OASIS Omniverse!')) -ForegroundColor DarkMagenta; Write-Host ''"

if /i not "%~1"=="run" if /i not "%~1"=="batch" (
    echo.
    set /p "DEPLOY_STAR=  Build and deploy STARAPIClient first? [y/N]: "
    if /i "!DEPLOY_STAR!"=="Y" (
        call "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
        if errorlevel 1 (echo [OQuake] STARAPIClient build/deploy failed. & pause & exit /b 1)
    )
    if /i "!DEPLOY_STAR!"=="YES" (
        call "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
        if errorlevel 1 (echo [OQuake] STARAPIClient build/deploy failed. & pause & exit /b 1)
    )
)

set "DO_FULL_CLEAN=0"
if /i not "%~1"=="run" if /i not "%~1"=="batch" (
    echo.
    set /p "BUILD_CHOICE=  Full clean/rebuild (C) or incremental build (I)? [I]: "
)
if not defined BUILD_CHOICE set "BUILD_CHOICE=I"
if /i "%BUILD_CHOICE%"=="C" set "DO_FULL_CLEAN=1"

REM --- STAR API ---
set "STAR_DLL="
set "STAR_LIB="
if exist "%DOOM_FOLDER%\star_api.dll" set "STAR_DLL=%DOOM_FOLDER%\star_api.dll" & set "STAR_LIB=%DOOM_FOLDER%\star_api.lib"
set "STAR_PUBLISH=%STARAPICLIENT%\bin\Release\net8.0\win-x64\publish"
set "STAR_NATIVE=%STARAPICLIENT%\bin\Release\net8.0\win-x64\native"
if not defined STAR_DLL if exist "%STAR_PUBLISH%\star_api.dll" set "STAR_DLL=%STAR_PUBLISH%\star_api.dll" & if exist "%STAR_NATIVE%\star_api.lib" (set "STAR_LIB=%STAR_NATIVE%\star_api.lib") else (set "STAR_LIB=")

if not defined STAR_DLL (
    echo [STAR API] Building STARAPIClient...
    if not exist "%STARAPICLIENT%\STARAPIClient.csproj" (echo STARAPIClient not found: %STARAPICLIENT% & pause & exit /b 1)
    cd /d "%HERE%.."
    dotnet publish "STARAPIClient\STARAPIClient.csproj" -c Release -r win-x64 -p:PublishAot=true -p:SelfContained=true -p:NoWarn=NU1605
    if errorlevel 1 (echo STARAPIClient build failed. & pause & exit /b 1)
    cd /d "%~dp0"
    if exist "%STAR_PUBLISH%\star_api.dll" set "STAR_DLL=%STAR_PUBLISH%\star_api.dll"
    if exist "%STAR_NATIVE%\star_api.lib" set "STAR_LIB=%STAR_NATIVE%\star_api.lib"
    if not exist "%STAR_DLL%" (echo star_api.dll missing after build. & pause & exit /b 1)
    if defined STAR_LIB copy /Y "%STAR_LIB%" "%DOOM_FOLDER%\star_api.lib" >nul
    copy /Y "%STAR_DLL%" "%DOOM_FOLDER%\star_api.dll" >nul
    echo [STAR API] Ready.
)

if not exist "%STARAPICLIENT%\star_api.h" (echo star_api.h not found: %STARAPICLIENT% & pause & exit /b 1)

REM --- QuakeC tree ---
if not exist "%QUAKE_SRC%" (echo Quake source not found: %QUAKE_SRC% & echo Edit QUAKE_SRC at top of script. & pause & exit /b 1)

REM --- star_sync (generic async layer from STARAPIClient). Copy only when OQuake has none, so local edits are not overwritten every build. ---
set "STARAPICLIENT=%HERE%..\STARAPIClient"
if not exist "%OQUAKE_INTEGRATION%star_sync.c" if exist "%STARAPICLIENT%\star_sync.c" (
    copy /Y "%STARAPICLIENT%\star_sync.c" "%OQUAKE_INTEGRATION%\" >nul
    copy /Y "%STARAPICLIENT%\star_sync.h" "%OQUAKE_INTEGRATION%\" >nul
)

echo.
echo [OQuake] Installing...
copy /Y "%OQUAKE_INTEGRATION%oquake_star_integration.c" "%QUAKE_SRC%\" >nul
copy /Y "%OQUAKE_INTEGRATION%oquake_star_integration.h" "%QUAKE_SRC%\" >nul
copy /Y "%OQUAKE_INTEGRATION%oquake_version.h" "%QUAKE_SRC%\" >nul
copy /Y "%OQUAKE_INTEGRATION%WINDOWS_INTEGRATION.md" "%QUAKE_SRC%\" >nul
copy /Y "%OQUAKE_INTEGRATION%engine_oquake_hooks.c.example" "%QUAKE_SRC%\" >nul
copy /Y "%STARAPICLIENT%\star_api.h" "%QUAKE_SRC%\" >nul
if exist "%OQUAKE_INTEGRATION%star_sync.c" copy /Y "%OQUAKE_INTEGRATION%star_sync.c" "%QUAKE_SRC%\" >nul
if exist "%OQUAKE_INTEGRATION%star_sync.h" copy /Y "%OQUAKE_INTEGRATION%star_sync.h" "%QUAKE_SRC%\" >nul
copy /Y "%STAR_DLL%" "%QUAKE_SRC%\star_api.dll" >nul
if defined STAR_LIB copy /Y "%STAR_LIB%" "%QUAKE_SRC%\star_api.lib" >nul
echo   %QUAKE_SRC%

REM --- vkQuake: apply + build ---
if not defined VKQUAKE_SRC goto :done
if not exist "%VKQUAKE_SRC%\Quake\pr_ext.c" goto :done

echo.
echo [OQuake] Patching vkQuake source...
set "APPLY_PS1=%OQUAKE_INTEGRATION%vkquake_oquake\apply_oquake_to_vkquake.ps1"
if exist "%APPLY_PS1%" powershell -NoProfile -ExecutionPolicy Bypass -File "%APPLY_PS1%" -VkQuakeSrc "%VKQUAKE_SRC%"
if exist "%OQUAKE_INTEGRATION%star_sync.c" copy /Y "%OQUAKE_INTEGRATION%star_sync.c" "%VKQUAKE_SRC%\Quake\" >nul
if exist "%OQUAKE_INTEGRATION%star_sync.h" copy /Y "%OQUAKE_INTEGRATION%star_sync.h" "%VKQUAKE_SRC%\Quake\" >nul
copy /Y "%STAR_DLL%" "%VKQUAKE_SRC%\Quake\star_api.dll" >nul
if defined STAR_LIB copy /Y "%STAR_LIB%" "%VKQUAKE_SRC%\Quake\star_api.lib" >nul

echo.
if "%DO_FULL_CLEAN%"=="1" if defined VKQUAKE_SRC (
    echo [OQuake] Full clean...
    if exist "%VKQUAKE_SRC%\Windows\VisualStudio\Build-vkQuake" rmdir /s /q "%VKQUAKE_SRC%\Windows\VisualStudio\Build-vkQuake" & echo   Build-vkQuake removed
    if exist "%VKQUAKE_SRC%\Windows\VisualStudio\x64" rmdir /s /q "%VKQUAKE_SRC%\Windows\VisualStudio\x64" & echo   x64 removed
    if exist "%VKQUAKE_SRC%\build" rmdir /s /q "%VKQUAKE_SRC%\build" & echo   build removed
)
echo [OQuake] Building engine...
if not defined VULKAN_SDK (
    if exist "C:\VulkanSDK\" for /f "delims=" %%D in ('dir /b /ad /o-n "C:\VulkanSDK\*" 2^>nul') do (
        if exist "C:\VulkanSDK\%%D\Include\vulkan\vulkan.h" set "VULKAN_SDK=C:\VulkanSDK\%%D" & goto :vulkan_ok
    )
    if exist "C:\VulkanSDK\1.3.296.0\Include\vulkan\vulkan.h" set "VULKAN_SDK=C:\VulkanSDK\1.3.296.0"
    if exist "C:\VulkanSDK\1.3.250.0\Include\vulkan\vulkan.h" set "VULKAN_SDK=C:\VulkanSDK\1.3.250.0"
)
:vulkan_ok
if not defined VULKAN_SDK (
    echo Vulkan SDK not found. Install from https://vulkan.lunarg.com/sdk/home and restart this script.
    pause
    exit /b 1
)

set "VKQUAKE_EXE="
if not exist "%VKQUAKE_SRC%\Windows\VisualStudio\vkquake.sln" goto :meson
set "VSDEVCMD="
where msbuild >nul 2>nul || (
    if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat" set "VSDEVCMD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
    if not defined VSDEVCMD if exist "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat" set "VSDEVCMD=%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat"
    if not defined VSDEVCMD if exist "%ProgramFiles%\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat" set "VSDEVCMD=%ProgramFiles%\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat"
)
if defined VSDEVCMD call "!VSDEVCMD!" -arch=amd64
where msbuild >nul 2>nul || (
    echo MSBuild not in PATH. Open "Developer Command Prompt for VS" or "x64 Native Tools Command Prompt" and run this script again.
    pause
    exit /b 1
)
msbuild "%VKQUAKE_SRC%\Windows\VisualStudio\vkquake.sln" /p:Configuration=Release /p:Platform=x64 /v:m
if errorlevel 1 (
    echo Build failed. Run from Developer Command Prompt for details.
    pause
    exit /b 1
)
if exist "%VKQUAKE_SRC%\Windows\VisualStudio\Build-vkQuake\x64\Release\vkquake.exe" set "VKQUAKE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\Build-vkQuake\x64\Release\vkquake.exe"
if not defined VKQUAKE_EXE if exist "%VKQUAKE_SRC%\Windows\VisualStudio\x64\Release\vkquake.exe" set "VKQUAKE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\x64\Release\vkquake.exe"
if not defined VKQUAKE_EXE if exist "%VKQUAKE_SRC%\Windows\VisualStudio\Release\vkquake.exe" set "VKQUAKE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\Release\vkquake.exe"
if not defined VKQUAKE_EXE if exist "%VKQUAKE_SRC%\build\Release\vkquake.exe" set "VKQUAKE_EXE=%VKQUAKE_SRC%\build\Release\vkquake.exe"
if not defined VKQUAKE_EXE (
    echo [WARNING] Build succeeded but vkquake.exe not found in expected locations.
    echo Searching for vkquake.exe...
    for /r "%VKQUAKE_SRC%" %%F in (vkquake.exe) do (
        if exist "%%F" (
            echo Found: %%F
            set "VKQUAKE_EXE=%%F"
            goto exe_found
        )
    )
)
:exe_found
goto :copy_out

:meson
if not exist "%VKQUAKE_SRC%\meson.build" goto :done
where meson >nul 2>nul
if errorlevel 1 goto :done
cd /d "%VKQUAKE_SRC%"
if not exist build mkdir build
meson setup build --buildtype=release
if not errorlevel 1 ninja -C build
if not errorlevel 1 if exist "%VKQUAKE_SRC%\build\vkquake.exe" set "VKQUAKE_EXE=%VKQUAKE_SRC%\build\vkquake.exe"
cd /d "%~dp0"
goto :copy_out

:copy_out
if not defined VKQUAKE_EXE goto copy_done
if not exist "%VKQUAKE_EXE%" (
    echo [ERROR] VKQUAKE_EXE points to non-existent file: %VKQUAKE_EXE%
    goto copy_done
)
echo [OQuake] Copying files to build folder...
for %%A in ("%VKQUAKE_EXE%") do set "EXE_DIR=%%~dpA"
copy /Y "%STAR_DLL%" "!EXE_DIR!" >nul
set "QUAKE_ENGINE_EXE=%VKQUAKE_EXE%"
if not exist "%OQUAKE_INTEGRATION%\build" mkdir "%OQUAKE_INTEGRATION%\build"
copy /Y "%VKQUAKE_EXE%" "%OQUAKE_INTEGRATION%\build\OQUAKE.exe"
if errorlevel 1 (
    echo [ERROR] Failed to copy exe to build folder
    goto copy_done
)
echo   Copied: %OQUAKE_INTEGRATION%\build\OQUAKE.exe
copy /Y "%STAR_DLL%" "%OQUAKE_INTEGRATION%\build\star_api.dll" >nul
for %%D in ("!EXE_DIR!*.dll") do (
    if exist "%%D" copy /Y "%%D" "%OQUAKE_INTEGRATION%\build\" >nul
)
REM Config file should be in build folder (already there)
if exist "%OQUAKE_INTEGRATION%\build\config.cfg" (
    echo   Config: %OQUAKE_INTEGRATION%\build\config.cfg
)
echo   Output: %OQUAKE_INTEGRATION%\build\OQUAKE.exe
:copy_done

goto :done

:done
echo.
echo ---
if defined QUAKE_ENGINE_EXE (
    echo OQuake ready. Use "BUILD_OQUAKE.bat run" to launch.
    echo Game data: id1 with pak0.pak, pak1.pak, gfx.wad in exe folder or -basedir.
) else if not "%~1"=="clean" (
    echo To build engine: set VKQUAKE_SRC at top ^(e.g. C:\Source\vkQuake^) and run again.
)
echo Cross-game keys: set STAR_USERNAME / STAR_PASSWORD or STAR_API_KEY / STAR_AVATAR_ID
echo ---

if /i "%~1"=="run" (
    if defined QUAKE_ENGINE_EXE if exist "%QUAKE_ENGINE_EXE%" (
        echo Launching OQuake...
        start "" "%QUAKE_ENGINE_EXE%"
    ) else (
        echo Set QUAKE_ENGINE_EXE or build first. Opening Quake folder.
        start "" "%QUAKE_SRC%"
    )
)
if /i not "%~1"=="batch" pause
