@echo off
REM Batch script to start only WEB5 STAR API

echo.
echo ========================================
echo Starting WEB5 STAR API
echo ========================================
echo.

REM Get the script directory
set "SCRIPT_DIR=%~dp0"
set "WEB5_PROJECT=%SCRIPT_DIR%STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI\NextGenSoftware.OASIS.STAR.WebAPI.csproj"

REM Check if project exists
if not exist "%WEB5_PROJECT%" (
    echo Error: WEB5 project not found at:
    echo %WEB5_PROJECT%
    pause
    exit /b 1
)

echo Starting WEB5 STAR API on http://localhost:5556...
echo Press Ctrl+C to stop the API.
echo.

REM Change to project directory and run
cd /d "%SCRIPT_DIR%STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI"
dotnet run --no-launch-profile --project "%WEB5_PROJECT%" -c Release --urls "http://localhost:5556"

pause


