@echo off
setlocal EnableDelayedExpansion
REM OQuake2 - Yamagi Quake II + OASIS STAR API. Credit: Yamagi Quake II team (GPL-2.0).
REM Usage: BUILD_OQUAKE2.bat [ run | batch ]
REM   (none) = prompt clean/incremental, then copy, patch, build
REM   run    = incremental build then launch (no prompts)
REM   batch  = incremental build, no prompts, do not launch (for BUILD EVERYTHING.bat)

set "YQUAKE2_SRC=C:\Source\yquake2"
set "QUAKE2_ENGINE_EXE="
set "HERE=%~dp0"
set "OGENGINECLIENT=%HERE%..\OGEngineClient"
set "OQUAKE2_INTEGRATION=%HERE%"
set "OQUAKE2_CODE=%HERE%Code\"
REM Set to 1 to always build and deploy OGEngineClient before building.
set "BUILD_STAR_CLIENT=0"

if exist "%HERE%..\run_oasis_header.bat" (
    call "%HERE%..\run_oasis_header.bat" OQUAKE2
) else (
    echo [OQuake2] run_oasis_header.bat not found in parent folder - skipping.
)

REM Always check OGEngineClient (build if source changed, then deploy).
if exist "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    echo [OQuake2] Checking OGEngineClient - build if changed, deploy...
    if "%BUILD_STAR_CLIENT%"=="1" (
        call "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" -ForceBuild
    ) else (
        call "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    )
    if errorlevel 1 (echo [OQuake2] OGEngineClient build/deploy failed. & pause & exit /b 1)
) else (
    echo [OQuake2] BUILD_AND_DEPLOY_STAR_CLIENT.bat not found - using existing ogengine.dll/lib if present.
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
if not defined STAR_DLL if exist "%OQUAKE2_INTEGRATION%\ogengine.dll" set "STAR_DLL=%OQUAKE2_INTEGRATION%\ogengine.dll" & set "STAR_LIB=%OQUAKE2_INTEGRATION%\ogengine.lib"
if not defined STAR_DLL (
    echo ogengine.dll missing after deploy. Check OGEngineClient build.
    pause
    exit /b 1
)

if not exist "%OGENGINECLIENT%\ogengine.h" (echo ogengine.h not found: %OGENGINECLIENT% & pause & exit /b 1)

REM --- Copy shared headers into integration folder ---
if not exist "%OQUAKE2_CODE%" mkdir "%OQUAKE2_CODE%"
copy /Y "%OGENGINECLIENT%\ogengine.h" "%OQUAKE2_CODE%" >nul
if exist "%OGENGINECLIENT%\ogengine_sync.h" copy /Y "%OGENGINECLIENT%\ogengine_sync.h" "%OQUAKE2_CODE%" >nul

REM --- Require Yamagi Q2 source ---
if not defined YQUAKE2_SRC (echo YQUAKE2_SRC not set. Set it at top of script. & goto :done)
if not exist "%YQUAKE2_SRC%\src\client\cl_main.c" (echo Yamagi Q2 source not found: %YQUAKE2_SRC% & goto :done)

echo.
echo [OQuake2] Copying integration files into Yamagi Q2 source...
if not exist "%YQUAKE2_SRC%\src\game" mkdir "%YQUAKE2_SRC%\src\game"
copy /Y "%OQUAKE2_CODE%oquake2_ogengine_integration.c" "%YQUAKE2_SRC%\src\game\" >nul
copy /Y "%OQUAKE2_CODE%oquake2_ogengine_integration.h" "%YQUAKE2_SRC%\src\game\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h" "%YQUAKE2_SRC%\src\game\" >nul
if exist "%OQUAKE2_CODE%ogengine_sync.h" copy /Y "%OQUAKE2_CODE%ogengine_sync.h" "%YQUAKE2_SRC%\src\game\" >nul
copy /Y "%STAR_DLL%" "%YQUAKE2_SRC%\ogengine.dll" >nul
if defined STAR_LIB copy /Y "%STAR_LIB%" "%YQUAKE2_SRC%\ogengine.lib" >nul
echo   Copied to: %YQUAKE2_SRC%\src\game\

echo.
if "%DO_FULL_CLEAN%"=="1" if defined YQUAKE2_SRC (
    echo [OQuake2] Full clean...
    if exist "%YQUAKE2_SRC%\build" rmdir /s /q "%YQUAKE2_SRC%\build" & echo   build removed
)
echo [OQuake2] Building engine...
if not exist "%YQUAKE2_SRC%\CMakeLists.txt" goto :try_make
where cmake >nul 2>nul
if errorlevel 1 goto :try_make
if not exist "%YQUAKE2_SRC%\build" mkdir "%YQUAKE2_SRC%\build"
cd /d "%YQUAKE2_SRC%\build"
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
cd /d "%HERE%"
if exist "%YQUAKE2_SRC%\build\quake2.exe" set "QUAKE2_ENGINE_EXE=%YQUAKE2_SRC%\build\quake2.exe"
if exist "%YQUAKE2_SRC%\build\Release\quake2.exe" set "QUAKE2_ENGINE_EXE=%YQUAKE2_SRC%\build\Release\quake2.exe"
goto :copy_out

:try_make
where nmake >nul 2>nul
if errorlevel 1 goto :copy_out
cd /d "%YQUAKE2_SRC%"
nmake
cd /d "%HERE%"
if exist "%YQUAKE2_SRC%\quake2.exe" set "QUAKE2_ENGINE_EXE=%YQUAKE2_SRC%\quake2.exe"

:copy_out
if not defined QUAKE2_ENGINE_EXE goto copy_done
if not exist "%QUAKE2_ENGINE_EXE%" goto copy_done
echo [OQuake2] Copying files to build folder...
if not exist "%OQUAKE2_INTEGRATION%\build" mkdir "%OQUAKE2_INTEGRATION%\build"
copy /Y "%QUAKE2_ENGINE_EXE%" "%OQUAKE2_INTEGRATION%\build\OQUAKE2.exe" >nul
copy /Y "%STAR_DLL%" "%OQUAKE2_INTEGRATION%\build\ogengine.dll" >nul
echo   Output: %OQUAKE2_INTEGRATION%\build\OQUAKE2.exe
:copy_done

:done
echo.
echo ---
if defined QUAKE2_ENGINE_EXE (
    echo OQuake2 ready. Use "BUILD_OQUAKE2.bat run" to launch.
    echo Game data: baseq2 with pak0.pak in exe folder or -datadir.
) else (
    echo To build engine: set YQUAKE2_SRC at top ^(e.g. C:\Source\yquake2^) and run again.
    echo See Docs\INTEGRATION_INSTRUCTIONS.md for setup steps.
)
echo OASIS thing type range: 6000-6899. Portal: 5900.
echo Cross-game keys: set STAR_USERNAME / STAR_PASSWORD or OGENGINE_KEY / STAR_AVATAR_ID
echo ---

if /i "%~1"=="run" (
    if defined QUAKE2_ENGINE_EXE if exist "%QUAKE2_ENGINE_EXE%" (
        echo Launching OQuake2...
        start "" "%QUAKE2_ENGINE_EXE%"
    )
)
if /i not "%~1"=="batch" pause
