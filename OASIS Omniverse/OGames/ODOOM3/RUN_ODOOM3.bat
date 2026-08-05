@echo off
REM RUN_ODOOM3.bat — Launch dhewm3 with OASIS STAR integration
REM
REM Usage:
REM   RUN_ODOOM3.bat [additional dhewm3 args]
REM
REM dhewm3 loads base.dll from the same directory as dhewm3.exe,
REM or from the game data directory. Set DHEWM3_BUILD below to match
REM your CMake output path.

set DHEWM3_BUILD=C:\Source\ODOOM3\build-vs2019-win64\Release
set EXE=%DHEWM3_BUILD%\dhewm3.exe

if not exist "%EXE%" (
    echo [ERROR] Executable not found: %EXE%
    echo Run BUILD_ODOOM3.bat first.
    pause
    exit /b 1
)

REM Deploy latest oasisstar.json if it doesn't exist in build dir
if not exist "%DHEWM3_BUILD%\oasisstar.json" (
    copy /y "%~dp0oasisstar.json" "%DHEWM3_BUILD%\oasisstar.json"
)

REM Deploy latest ogengine.dll if updated
set STARDLL=%~dp0..\OGEngineClient\ogengine.dll
if exist "%STARDLL%" (
    xcopy /y /d "%STARDLL%" "%DHEWM3_BUILD%\" >nul
)

echo Starting ODOOM3 (dhewm3)...
start "" "%EXE%" %*
