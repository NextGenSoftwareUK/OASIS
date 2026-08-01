@echo off
setlocal EnableDelayedExpansion
REM OQuake3 - Quake3e + OASIS STAR API. Credit: ec- and Quake3e contributors (GPL-2.0).
REM Base: Quake3e (https://github.com/ec-/Quake3e) — open-source Quake III Arena engine.
REM OASIS thing type range: 7000-7899 (Quake III Arena)
REM Usage: BUILD_OQUAKE3.bat [ run | batch ]

set "Q3E_SRC=C:\Source\Quake3e"
set "Q3E_ENGINE_EXE="
set "HERE=%~dp0"
set "OGENGINECLIENT=%HERE%..\OGEngineClient"
set "OQUAKE3_INTEGRATION=%HERE%"
set "OQUAKE3_CODE=%HERE%Code\"
set "BUILD_STAR_CLIENT=0"

if exist "%HERE%..\run_oasis_header.bat" (
    call "%HERE%..\run_oasis_header.bat" OQUAKE3
) else (
    echo [OQuake3] run_oasis_header.bat not found in parent folder - skipping.
)

if exist "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    echo [OQuake3] Checking OGEngineClient - build if changed, deploy...
    if "%BUILD_STAR_CLIENT%"=="1" (
        call "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" -ForceBuild
    ) else (
        call "%HERE%..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    )
    if errorlevel 1 (echo [OQuake3] OGEngineClient build/deploy failed. & pause & exit /b 1)
) else (
    echo [OQuake3] BUILD_AND_DEPLOY_STAR_CLIENT.bat not found - using existing ogengine.dll/lib if present.
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
if not defined STAR_DLL if exist "%OQUAKE3_INTEGRATION%\ogengine.dll" set "STAR_DLL=%OQUAKE3_INTEGRATION%\ogengine.dll" & set "STAR_LIB=%OQUAKE3_INTEGRATION%\ogengine.lib"
if not defined STAR_DLL (
    echo ogengine.dll missing after deploy. Check OGEngineClient build.
    pause & exit /b 1
)

if not exist "%OGENGINECLIENT%\ogengine.h" (echo ogengine.h not found: %OGENGINECLIENT% & pause & exit /b 1)

REM --- Copy shared headers ---
if not exist "%OQUAKE3_CODE%" mkdir "%OQUAKE3_CODE%"
copy /Y "%OGENGINECLIENT%\ogengine.h" "%OQUAKE3_CODE%" >nul
if exist "%OGENGINECLIENT%\ogengine_sync.h" copy /Y "%OGENGINECLIENT%\ogengine_sync.h" "%OQUAKE3_CODE%" >nul

REM --- Require Quake3e source ---
if not exist "%Q3E_SRC%\code\game\g_main.c" (
    echo Quake3e source not found: %Q3E_SRC%
    echo Download from: https://github.com/ec-/Quake3e
    goto :done
)

echo.
echo [OQuake3] Copying integration files into Quake3e source...
if not exist "%Q3E_SRC%\code\game" mkdir "%Q3E_SRC%\code\game"
copy /Y "%OQUAKE3_CODE%oquake3_ogengine_integration.c" "%Q3E_SRC%\code\game\" >nul
copy /Y "%OQUAKE3_CODE%oquake3_ogengine_integration.h" "%Q3E_SRC%\code\game\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h" "%Q3E_SRC%\code\game\" >nul
if exist "%OQUAKE3_CODE%ogengine_sync.h" copy /Y "%OQUAKE3_CODE%ogengine_sync.h" "%Q3E_SRC%\code\game\" >nul
copy /Y "%STAR_DLL%" "%Q3E_SRC%\ogengine.dll" >nul
if defined STAR_LIB copy /Y "%STAR_LIB%" "%Q3E_SRC%\ogengine.lib" >nul
echo   Copied to: %Q3E_SRC%\code\game\

echo.
if "%DO_FULL_CLEAN%"=="1" (
    echo [OQuake3] Full clean...
    if exist "%Q3E_SRC%\build" rmdir /s /q "%Q3E_SRC%\build" & echo   build removed
)
echo [OQuake3] Building Quake3e...
if not exist "%Q3E_SRC%\CMakeLists.txt" goto :try_make
where cmake >nul 2>nul
if errorlevel 1 goto :try_make
if not exist "%Q3E_SRC%\build" mkdir "%Q3E_SRC%\build"
cd /d "%Q3E_SRC%\build"
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
cd /d "%HERE%"
if exist "%Q3E_SRC%\build\quake3e.exe" set "Q3E_ENGINE_EXE=%Q3E_SRC%\build\quake3e.exe"
if exist "%Q3E_SRC%\build\Release\quake3e.exe" set "Q3E_ENGINE_EXE=%Q3E_SRC%\build\Release\quake3e.exe"
goto :copy_out

:try_make
where nmake >nul 2>nul
if errorlevel 1 goto :copy_out
cd /d "%Q3E_SRC%"
nmake
cd /d "%HERE%"
if exist "%Q3E_SRC%\quake3e.x64.exe" set "Q3E_ENGINE_EXE=%Q3E_SRC%\quake3e.x64.exe"
if exist "%Q3E_SRC%\quake3e.exe" set "Q3E_ENGINE_EXE=%Q3E_SRC%\quake3e.exe"

:copy_out
if not defined Q3E_ENGINE_EXE goto copy_done
if not exist "%Q3E_ENGINE_EXE%" goto copy_done
echo [OQuake3] Copying files to build folder...
if not exist "%OQUAKE3_INTEGRATION%\build" mkdir "%OQUAKE3_INTEGRATION%\build"
copy /Y "%Q3E_ENGINE_EXE%" "%OQUAKE3_INTEGRATION%\build\OQUAKE3.exe" >nul
copy /Y "%STAR_DLL%" "%OQUAKE3_INTEGRATION%\build\ogengine.dll" >nul
echo   Output: %OQUAKE3_INTEGRATION%\build\OQUAKE3.exe
:copy_done

:done
echo.
echo ---
if defined Q3E_ENGINE_EXE (
    echo OQuake3 ready. Use "BUILD_OQUAKE3.bat run" to launch.
    echo Game data: baseq3 with pak0.pk3 in exe folder or -fs_basepath.
) else (
    echo To build engine: set Q3E_SRC at top ^(e.g. C:\Source\Quake3e^) and run again.
    echo See Docs\INTEGRATION_INSTRUCTIONS.md for setup steps.
)
echo OASIS thing type range: 7000-7899 (Quake III Arena). Portal: 5900.
echo Cross-game keys: set STAR_USERNAME / STAR_PASSWORD or OGENGINE_KEY / STAR_AVATAR_ID
echo ---

if /i "%~1"=="run" (
    if defined Q3E_ENGINE_EXE if exist "%Q3E_ENGINE_EXE%" (
        echo Launching OQuake3...
        start "" "%Q3E_ENGINE_EXE%"
    )
)
if /i not "%~1"=="batch" pause
