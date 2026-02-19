@echo off
REM Batch script to start only WEB4 OASIS API

echo.
echo ========================================
echo Starting WEB4 OASIS API
echo ========================================
echo.

REM Get the script directory
set "SCRIPT_DIR=%~dp0"
set "WEB4_PROJECT=%SCRIPT_DIR%ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"

REM Check if project exists
if not exist "%WEB4_PROJECT%" (
    echo Error: WEB4 project not found at:
    echo %WEB4_PROJECT%
    pause
    exit /b 1
)

echo Starting WEB4 OASIS API on http://localhost:5555...
echo Press Ctrl+C to stop the API.
echo.

REM Change to project directory and run
cd /d "%SCRIPT_DIR%ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI"
dotnet run --no-launch-profile --project "%WEB4_PROJECT%" -c Release --urls "http://localhost:5555"

pause






