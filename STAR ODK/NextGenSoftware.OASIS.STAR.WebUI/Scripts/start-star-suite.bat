@echo off
setlocal
rem ---------------------------------------------
rem Configure default ports/URLs here if needed
rem ---------------------------------------------
set "OASIS_API_URL=http://localhost:50563"
set "STAR_API_URL=http://localhost:50564"
set "STAR_UI_PORT=3000"

rem Resolve path to PowerShell script (same folder)
set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%start-star-suite.ps1"

if not exist "%PS_SCRIPT%" (
    echo PowerShell script not found: %PS_SCRIPT%
    pause
    exit /b 1
)

rem Launch PowerShell to run the suite
powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" -OasisApiUrl "%OASIS_API_URL%" -StarApiUrl "%STAR_API_URL%" -StarUiPort "%STAR_UI_PORT%"

if errorlevel 1 (
    echo.
    echo There was an error launching one or more services.
    pause
)

exit /b 0
