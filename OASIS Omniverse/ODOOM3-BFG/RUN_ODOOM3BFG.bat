@echo off
REM RUN_ODOOM3BFG.bat — Launch RBDOOM-3-BFG with OASIS STAR integration
REM
REM Usage:
REM   RUN_ODOOM3BFG.bat [additional doom3 args]

set RBDOOM_BUILD=C:\Source\ODOOM3-BFG\build-vs2019-win64\Release
set EXE=%RBDOOM_BUILD%\RBDOOM-3-BFG.exe

if not exist "%EXE%" (
    echo [ERROR] Executable not found: %EXE%
    echo Run BUILD_ODOOM3BFG.bat first.
    pause
    exit /b 1
)

REM Deploy latest oasisstar.json if it doesn't exist in build dir
if not exist "%RBDOOM_BUILD%\oasisstar.json" (
    copy /y "%~dp0oasisstar.json" "%RBDOOM_BUILD%\oasisstar.json"
)

REM Deploy latest star_api.dll if updated
set STARDLL=%~dp0..\STARAPIClient\star_api.dll
if exist "%STARDLL%" (
    xcopy /y /d "%STARDLL%" "%RBDOOM_BUILD%\" >nul
)

echo Starting ODOOM3-BFG...
start "" "%EXE%" +set fs_game d3xp %*
