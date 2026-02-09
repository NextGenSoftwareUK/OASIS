@echo off
REM Single script: build star_api (if needed), copy OQuake integration to quake-rerelease-qc.
REM Same style as BUILD_ODOOM.bat / BUILD_UZDOOM_STAR.bat.
REM
REM   BUILD_OQUAKE.bat       = copy integration + star_api to Quake tree
REM   BUILD_OQUAKE.bat run   = same, then launch OQuake engine (if QUAKE_ENGINE_EXE set)
REM
REM Edit QUAKE_SRC if your QuakeC source is not at C:\Source\quake-rerelease-qc
REM Edit QUAKE_ENGINE_EXE to point to your OQuake-built engine exe to use "run".

set "QUAKE_SRC=C:\Source\quake-rerelease-qc"
set "QUAKE_ENGINE_EXE="
REM Optional: set to vkQuake source to clone and build (recommended engine - see ENGINE_RECOMMENDATION.md)
set "VKQUAKE_SRC=C:\Source\vkQuake"

set "HERE=%~dp0"
set "DOOM_FOLDER=%HERE%..\Doom"
set "NATIVEWRAPPER=%HERE%..\NativeWrapper"
set "OQUAKE_INTEGRATION=%HERE%"
set "NW_BUILD=%NATIVEWRAPPER%\build"
set "NW_RELEASE=%NW_BUILD%\Release"

REM ---------- 1. Ensure star_api is available (Doom folder or NativeWrapper build) ----------
set "STAR_DLL="
set "STAR_LIB="
if exist "%DOOM_FOLDER%\star_api.dll" (
    set "STAR_DLL=%DOOM_FOLDER%\star_api.dll"
    set "STAR_LIB=%DOOM_FOLDER%\star_api.lib"
)
if not defined STAR_DLL if exist "%NW_RELEASE%\star_api.dll" (
    set "STAR_DLL=%NW_RELEASE%\star_api.dll"
    set "STAR_LIB=%NW_RELEASE%\star_api.lib"
)

if not defined STAR_DLL (
    echo star_api not found. Building NativeWrapper...
    if not exist "%NATIVEWRAPPER%\star_api.h" (
        echo NativeWrapper not found at: %NATIVEWRAPPER%
        pause
        exit /b 1
    )
    if not exist "%NW_BUILD%" mkdir "%NW_BUILD%"
    cd /d "%NW_BUILD%"
    cmake .. -G "Visual Studio 17 2022" -A x64
    if errorlevel 1 (
        echo NativeWrapper CMake failed. Try "Visual Studio 16 2019" if you don't have VS 2022.
        pause
        exit /b 1
    )
    cmake --build . --config Release
    if errorlevel 1 (
        echo NativeWrapper build failed.
        pause
        exit /b 1
    )
    cd /d "%~dp0"
    set "STAR_DLL=%NW_RELEASE%\star_api.dll"
    set "STAR_LIB=%NW_RELEASE%\star_api.lib"
    if not exist "%STAR_DLL%" (
        echo star_api.dll still missing after build.
        pause
        exit /b 1
    )
    echo Copying star_api to Doom folder for ODOOM/OQuake...
    copy /Y "%STAR_DLL%" "%DOOM_FOLDER%\star_api.dll"
    copy /Y "%STAR_LIB%" "%DOOM_FOLDER%\star_api.lib"
)

if not exist "%NATIVEWRAPPER%\star_api.h" (
    echo star_api.h not found at: %NATIVEWRAPPER%
    pause
    exit /b 1
)

REM ---------- 2. Copy OQuake integration to Quake tree ----------
if not exist "%QUAKE_SRC%" (
    echo Quake source not found at: %QUAKE_SRC%
    echo Edit QUAKE_SRC at the top of this script.
    pause
    exit /b 1
)

echo === Copying OQuake integration to %QUAKE_SRC% ===
copy /Y "%OQUAKE_INTEGRATION%oquake_star_integration.c" "%QUAKE_SRC%\"
copy /Y "%OQUAKE_INTEGRATION%oquake_star_integration.h" "%QUAKE_SRC%\"
copy /Y "%OQUAKE_INTEGRATION%oquake_version.h" "%QUAKE_SRC%\"
copy /Y "%OQUAKE_INTEGRATION%WINDOWS_INTEGRATION.md" "%QUAKE_SRC%\"
copy /Y "%OQUAKE_INTEGRATION%engine_oquake_hooks.c.example" "%QUAKE_SRC%\"
copy /Y "%NATIVEWRAPPER%\star_api.h" "%QUAKE_SRC%\"
copy /Y "%STAR_DLL%" "%QUAKE_SRC%\star_api.dll"
copy /Y "%STAR_LIB%" "%QUAKE_SRC%\star_api.lib"

REM ---------- 3. Optional: clone and build vkQuake (recommended engine) ----------
if defined VKQUAKE_SRC call :do_vkquake
goto :done_vkquake

:do_vkquake
if not exist "%VKQUAKE_SRC%" (
    echo vkQuake source not found - cloning from GitHub
    git clone --depth 1 https://github.com/Novum/vkQuake.git "%VKQUAKE_SRC%"
    if errorlevel 1 echo Git clone failed - clone vkQuake manually to %VKQUAKE_SRC%
)
if not exist "%VKQUAKE_SRC%\Quake\pr_ext.c" goto :eof
echo/
echo === Ensuring OQuake/STAR files in vkQuake Quake folder ===
set "APPLY_PS1=%OQUAKE_INTEGRATION%vkquake_oquake\apply_oquake_to_vkquake.ps1"
if exist "%APPLY_PS1%" powershell -NoProfile -ExecutionPolicy Bypass -File "%APPLY_PS1%" -VkQuakeSrc "%VKQUAKE_SRC%"
copy /Y "%STAR_DLL%" "%VKQUAKE_SRC%\Quake\star_api.dll"
copy /Y "%STAR_LIB%" "%VKQUAKE_SRC%\Quake\star_api.lib"
echo/
echo === Building OQuake (vkQuake + STAR API) ===
REM Vulkan SDK: installer sets VULKAN_SDK; if not set, try default path (current cmd may not have new env after install)
if not defined VULKAN_SDK (
    if exist "C:\VulkanSDK\" (
        for /f "delims=" %%D in ('dir /b /ad /o-n "C:\VulkanSDK\*" 2^>nul') do (
            if exist "C:\VulkanSDK\%%D\Include\vulkan\vulkan.h" (
                set "VULKAN_SDK=C:\VulkanSDK\%%D"
                goto :vulkan_found
            )
        )
    )
    if exist "C:\VulkanSDK\1.3.296.0\Include\vulkan\vulkan.h" set "VULKAN_SDK=C:\VulkanSDK\1.3.296.0"
    if exist "C:\VulkanSDK\1.3.250.0\Include\vulkan\vulkan.h" set "VULKAN_SDK=C:\VulkanSDK\1.3.250.0"
)
:vulkan_found
if defined VULKAN_SDK (
    echo Vulkan SDK: %VULKAN_SDK%
) else (
    echo WARNING: VULKAN_SDK is not set and no Vulkan SDK found in C:\VulkanSDK\
    echo After installing Vulkan SDK, close this window and open a NEW command prompt, then run BUILD_OQUAKE.bat again.
    echo Or set VULKAN_SDK manually, e.g. set VULKAN_SDK=C:\VulkanSDK\1.3.296.0
    echo Download: https://vulkan.lunarg.com/sdk/home
)
set "VKQUAKE_EXE="
if exist "%VKQUAKE_SRC%\Windows\VisualStudio\vkquake.sln" (
    where msbuild >nul 2>nul
    if errorlevel 1 (
        echo MSBuild not in PATH. Open "Developer Command Prompt for VS 2022" and run BUILD_OQUAKE.bat from there.
    )
    if not errorlevel 1 (
        msbuild "%VKQUAKE_SRC%\Windows\VisualStudio\vkquake.sln" /p:Configuration=Release /p:Platform=x64 /v:m
        if not errorlevel 1 (
            if exist "%VKQUAKE_SRC%\Windows\VisualStudio\Build-vkQuake\x64\Release\vkquake.exe" set "VKQUAKE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\Build-vkQuake\x64\Release\vkquake.exe"
            if not defined VKQUAKE_EXE if exist "%VKQUAKE_SRC%\Windows\VisualStudio\x64\Release\vkquake.exe" set "VKQUAKE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\x64\Release\vkquake.exe"
            if not defined VKQUAKE_EXE if exist "%VKQUAKE_SRC%\Windows\VisualStudio\Release\vkquake.exe" set "VKQUAKE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\Release\vkquake.exe"
            if not defined VKQUAKE_EXE if exist "%VKQUAKE_SRC%\build\Release\vkquake.exe" set "VKQUAKE_EXE=%VKQUAKE_SRC%\build\Release\vkquake.exe"
        ) else (
            echo MSBuild failed. Run this in a NEW prompt to see full errors:
            echo   Use "Developer Command Prompt for VS" or "x64 Native Tools Command Prompt"
            echo   cd /d "%VKQUAKE_SRC%\Windows\VisualStudio"
            echo   msbuild vkquake.sln /p:Configuration=Release /p:Platform=x64
            echo Ensure VULKAN_SDK is set in that prompt, e.g. set VULKAN_SDK=C:\VulkanSDK\1.3.296.0
        )
    ) else (
        echo MSBuild not in PATH. Open "Developer Command Prompt for VS 2022" and run BUILD_OQUAKE.bat from there.
    )
)
if not defined VKQUAKE_EXE if exist "%VKQUAKE_SRC%\meson.build" (
    where meson >nul 2>nul
    if not errorlevel 1 (
        cd /d "%VKQUAKE_SRC%"
        if not exist build mkdir build
        meson setup build --buildtype=release
        if not errorlevel 1 (
            ninja -C build
            if not errorlevel 1 if exist "%VKQUAKE_SRC%\build\vkquake.exe" set "VKQUAKE_EXE=%VKQUAKE_SRC%\build\vkquake.exe"
        )
        cd /d "%~dp0"
    )
)
if defined VKQUAKE_EXE (
    for %%A in ("%VKQUAKE_EXE%") do copy /Y "%STAR_DLL%" "%%~dpA"
    set "QUAKE_ENGINE_EXE=%VKQUAKE_EXE%"
    echo OQuake built - %VKQUAKE_EXE%
    echo star_api.dll next to exe - use -game to point to quake-rerelease-qc progs for cross-game keys
    if not exist "%OQUAKE_INTEGRATION%\build" mkdir "%OQUAKE_INTEGRATION%\build"
    copy /Y "%VKQUAKE_EXE%" "%OQUAKE_INTEGRATION%\build\OQUAKE.exe"
    copy /Y "%STAR_DLL%" "%OQUAKE_INTEGRATION%\build\star_api.dll"
    echo Copied OQUAKE.exe and star_api.dll to %OQUAKE_INTEGRATION%\build\
) else (
    echo OQuake/vkQuake build failed.
    echo - If you just installed Vulkan SDK: close this window, open a NEW command prompt, run BUILD_OQUAKE.bat again.
    echo - Build from "Developer Command Prompt for VS 2022" so MSBuild and Vulkan are found.
    echo - To see the real error: cd "%VKQUAKE_SRC%\Windows\VisualStudio" then run: msbuild vkquake.sln /p:Configuration=Release /p:Platform=x64
)
goto :eof

:done_vkquake
echo/
echo === Done ===
echo   OQuake files and star_api are in: %QUAKE_SRC%
if defined QUAKE_ENGINE_EXE (
    echo   Engine built - use %~nx0 run to launch %QUAKE_ENGINE_EXE%
    echo   Game data: put id1 with pak0.pak and pak1.pak and gfx.wad in exe folder, or use -basedir path
) else (
    echo   To build vkQuake automatically, set VKQUAKE_SRC=C:\Source\vkQuake and run again.
    echo   See ENGINE_RECOMMENDATION.md for why vkQuake is recommended
)
echo   OQuake at VKQUAKE_SRC has STAR builtins - use quake-rerelease-qc progs for cross-game keys with ODOOM
echo   Set STAR_USERNAME/STAR_PASSWORD or STAR_API_KEY/STAR_AVATAR_ID for cross-game keys
echo/

if /i "%~1"=="run" (
    if defined QUAKE_ENGINE_EXE if exist "%QUAKE_ENGINE_EXE%" (
        echo Launching OQuake - %QUAKE_ENGINE_EXE%
        start "" "%QUAKE_ENGINE_EXE%"
    ) else (
        echo To auto-launch, set QUAKE_ENGINE_EXE at the top of this script to your engine exe
        echo Opening Quake folder
        start "" "%QUAKE_SRC%"
    )
) else (
    echo To copy and run when QUAKE_ENGINE_EXE is set, use %~nx0 run
)
pause
