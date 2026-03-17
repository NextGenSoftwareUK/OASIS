@echo off
REM Batch script to start only WEB5 STAR API.
REM Stops any existing process on port 5556 so the build (from dotnet run) can copy DLLs.

echo.
echo ========================================
echo Starting WEB5 STAR API
echo ========================================
echo.

REM Get the script directory
set "SCRIPT_DIR=%~dp0"
set "WEB5_PROJECT=%SCRIPT_DIR%..\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI\NextGenSoftware.OASIS.STAR.WebAPI.csproj"

REM Check if project exists
if not exist "%WEB5_PROJECT%" (
    echo Error: WEB5 project not found at:
    echo %WEB5_PROJECT%
    pause
    exit /b 1
)

REM Stop any process already using port 5556 so dotnet run can build and copy DLLs
echo Checking for existing WEB5 STAR API on port 5556...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-NetTCPConnection -LocalPort 5556 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue; Write-Host 'Stopped existing process on port 5556 (pid' $_.OwningProcess ')' }"
if %ERRORLEVEL% NEQ 0 (
    echo Port check completed (or PowerShell unavailable).
)

echo.
echo Starting WEB5 STAR API on http://localhost:5556...
echo Press Ctrl+C to stop the API.
echo.

REM Change to project directory and run (dotnet run builds first; DLLs must not be locked)
cd /d "%SCRIPT_DIR%..\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI"
dotnet run --no-launch-profile --project "%WEB5_PROJECT%" -c Release --urls "http://localhost:5556"

pause






