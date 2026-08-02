@echo off
setlocal EnableDelayedExpansion
REM OQuake2-RTX - NVIDIA Q2 RTX + OASIS STAR API. Credit: NVIDIA / Q2 RTX team (GPL-2.0).
REM Base: Q2 RTX (https://github.com/NVIDIA/Q2RTX) — Yamagi Q2 + Vulkan RTX renderer.
REM OASIS thing type range: 6000-6899 (same as OQuake2 — shared game content).
REM Usage: BUILD_OQUAKE2RTX.bat [ run | batch ]

set "Q2RTX_SRC=C:\Source\OQuake2-RTX"
set "Q2RTX_ENGINE_EXE="
set "HERE=%~dp0"
set "OGENGINECLIENT=%HERE%..\OGEngineClient"
set "OQUAKE2RTX_INTEGRATION=%HERE%"
set "OQUAKE2RTX_CODE=%HERE%Code\"
set "BUILD_STAR_CLIENT=0"

if exist "%HERE%..\run_oasis_header.bat" (
    call "%HERE%..\run_oasis_header.bat" OQUAKE2-RTX
) else (
    echo [OQuake2-RTX] run_oasis_header.bat not found in parent folder - skipping.
)

if exist "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    echo [OQuake2-RTX] Checking OGEngineClient - build if changed, deploy...
    if "%BUILD_STAR_CLIENT%"=="1" (
        call "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" -ForceBuild
    ) else (
        call "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    )
    if errorlevel 1 (echo [OQuake2-RTX] OGEngineClient build/deploy failed. & pause & exit /b 1)
) else (
    echo [OQuake2-RTX] BUILD_AND_DEPLOY_STAR_CLIENT.bat not found - using existing ogengine.dll/lib if present.
)

set "DO_FULL_CLEAN=0"
if /i not "%~1"=="run" if /i not "%~1"=="batch" (
    echo.
    set /p "BUILD_CHOICE=  Full clean/rebuild [C] or incremental build [I]? [I]: "
)
if not defined BUILD_CHOICE set "BUILD_CHOICE=I"
if /i "%BUILD_CHOICE%"=="C" set "DO_FULL_CLEAN=1"

REM --- STAR API DLL ---
set "STAR_DLL="
set "STAR_LIB="
if exist "%OGENGINECLIENT%\bin\Release\net9.0\win-x64\publish\ogengine.dll" if exist "%OGENGINECLIENT%\bin\Release\net9.0\win-x64\native\ogengine.lib" (
    set "STAR_DLL=%OGENGINECLIENT%\bin\Release\net9.0\win-x64\publish\ogengine.dll"
    set "STAR_LIB=%OGENGINECLIENT%\bin\Release\net9.0\win-x64\native\ogengine.lib"
)
if not defined STAR_DLL if exist "%OQUAKE2RTX_INTEGRATION%\ogengine.dll" set "STAR_DLL=%OQUAKE2RTX_INTEGRATION%\ogengine.dll" & set "STAR_LIB=%OQUAKE2RTX_INTEGRATION%\ogengine.lib"
if not defined STAR_DLL (
    echo ogengine.dll missing after deploy. Check OGEngineClient build.
    pause & exit /b 1
)

if not exist "%OGENGINECLIENT%\ogengine.h" (echo ogengine.h not found: %OGENGINECLIENT% & pause & exit /b 1)

REM --- Copy shared headers ---
if not exist "%OQUAKE2RTX_CODE%" mkdir "%OQUAKE2RTX_CODE%"
copy /Y "%OGENGINECLIENT%\ogengine.h" "%OQUAKE2RTX_CODE%" >nul
if exist "%OGENGINECLIENT%\ogengine_sync.h" copy /Y "%OGENGINECLIENT%\ogengine_sync.h" "%OQUAKE2RTX_CODE%" >nul

REM --- Require Q2 RTX source ---
if not defined Q2RTX_SRC (echo Q2RTX_SRC not set. Set it at top of script. & goto :done)
if not exist "%Q2RTX_SRC%\src\client\cl_main.c" (echo Q2 RTX source not found: %Q2RTX_SRC% & goto :done)

echo.
echo [OQuake2-RTX] Copying integration files into Q2 RTX source...
if not exist "%Q2RTX_SRC%\src\game" mkdir "%Q2RTX_SRC%\src\game"
copy /Y "%OQUAKE2RTX_INTEGRATION%oquake2rtx_ogengine_integration.c" "%Q2RTX_SRC%\src\game\" >nul
copy /Y "%OQUAKE2RTX_INTEGRATION%oquake2rtx_ogengine_integration.h" "%Q2RTX_SRC%\src\game\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h" "%Q2RTX_SRC%\src\game\" >nul
if exist "%OQUAKE2RTX_CODE%ogengine_sync.h" copy /Y "%OQUAKE2RTX_CODE%ogengine_sync.h" "%Q2RTX_SRC%\src\game\" >nul
copy /Y "%STAR_DLL%" "%Q2RTX_SRC%\ogengine.dll" >nul
if defined STAR_LIB copy /Y "%STAR_LIB%" "%Q2RTX_SRC%\ogengine.lib" >nul
echo   Copied to: %Q2RTX_SRC%\src\game\

echo.
if "%DO_FULL_CLEAN%"=="1" (
    echo [OQuake2-RTX] Full clean...
    if exist "%Q2RTX_SRC%\build" rmdir /s /q "%Q2RTX_SRC%\build" & echo   build removed
)
echo [OQuake2-RTX] Building engine (Q2 RTX requires Vulkan SDK + NVIDIA RTX GPU or RTX fallback)...
if not exist "%Q2RTX_SRC%\CMakeLists.txt" goto :try_make
where cmake >nul 2>nul
if errorlevel 1 goto :try_make
if not exist "%Q2RTX_SRC%\build" mkdir "%Q2RTX_SRC%\build"
cd /d "%Q2RTX_SRC%\build"
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
cd /d "%HERE%"
if exist "%Q2RTX_SRC%\build\q2rtx.exe" set "Q2RTX_ENGINE_EXE=%Q2RTX_SRC%\build\q2rtx.exe"
if exist "%Q2RTX_SRC%\build\Release\q2rtx.exe" set "Q2RTX_ENGINE_EXE=%Q2RTX_SRC%\build\Release\q2rtx.exe"
goto :copy_out

:try_make
where nmake >nul 2>nul
if errorlevel 1 goto :copy_out
cd /d "%Q2RTX_SRC%"
nmake
cd /d "%HERE%"
if exist "%Q2RTX_SRC%\q2rtx.exe" set "Q2RTX_ENGINE_EXE=%Q2RTX_SRC%\q2rtx.exe"

:copy_out
if not defined Q2RTX_ENGINE_EXE goto copy_done
if not exist "%Q2RTX_ENGINE_EXE%" goto copy_done
echo [OQuake2-RTX] Copying files to build folder...
if not exist "%OQUAKE2RTX_INTEGRATION%\build" mkdir "%OQUAKE2RTX_INTEGRATION%\build"
copy /Y "%Q2RTX_ENGINE_EXE%" "%OQUAKE2RTX_INTEGRATION%\build\OQUAKE2RTX.exe" >nul
copy /Y "%STAR_DLL%" "%OQUAKE2RTX_INTEGRATION%\build\ogengine.dll" >nul
echo   Output: %OQUAKE2RTX_INTEGRATION%\build\OQUAKE2RTX.exe
:copy_done

:done
echo.
echo ---
if defined Q2RTX_ENGINE_EXE (
    echo OQuake2-RTX ready. Use "BUILD_OQUAKE2RTX.bat run" to launch.
    echo Game data: baseq2 with pak0.pak in exe folder or -datadir.
    echo Note: Q2 RTX requires Vulkan + RTX GPU (or software RTX fallback).
) else (
    echo To build engine: set Q2RTX_SRC at top ^(e.g. C:\Source\Q2RTX^) and run again.
    echo See Docs\INTEGRATION_INSTRUCTIONS.md for setup steps.
)
echo OASIS thing type range: 6000-6899 (same as OQuake2). Portal: 5900.
echo Cross-game keys: set STAR_USERNAME / STAR_PASSWORD or OGENGINE_KEY / STAR_AVATAR_ID
echo ---

if /i "%~1"=="run" (
    if defined Q2RTX_ENGINE_EXE if exist "%Q2RTX_ENGINE_EXE%" (
        echo Launching OQuake2-RTX...
        start "" "%Q2RTX_ENGINE_EXE%"
    )
)
if /i not "%~1"=="batch" pause
