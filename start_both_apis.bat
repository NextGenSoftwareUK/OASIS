@echo off
REM Batch script to start both WEB4 and WEB5 APIs in serial
REM Calls the PowerShell script that starts both APIs sequentially

echo.
echo ========================================
echo Starting Both APIs (WEB4 and WEB5)
echo ========================================
echo.

REM Get the script directory
set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%OASIS Omniverse\STARAPIClient\start_local_web4_and_web5_apis.ps1"

REM Check if PowerShell script exists
if not exist "%PS_SCRIPT%" (
    echo Error: PowerShell script not found at:
    echo %PS_SCRIPT%
    pause
    exit /b 1
)

REM Run the PowerShell script
powershell -ExecutionPolicy Bypass -File "%PS_SCRIPT%"

pause






