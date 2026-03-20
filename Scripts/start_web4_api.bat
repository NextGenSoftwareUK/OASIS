@echo off
REM Batch script to start only WEB4 OASIS API.
REM Stops any existing process on port 7777 so the build (from dotnet run) can copy DLLs.

echo.
echo ========================================
echo Starting WEB4 OASIS API
echo ========================================
echo.

REM Get the script directory
set "SCRIPT_DIR=%~dp0"
set "WEB4_PROJECT=%SCRIPT_DIR%..\ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"

REM Check if project exists
if not exist "%WEB4_PROJECT%" (
    echo Error: WEB4 project not found at:
    echo %WEB4_PROJECT%
    pause
    exit /b 1
)

REM Stop any process already using port 7777 so dotnet run can build and copy DLLs
echo Checking for existing WEB4 OASIS API on port 7777...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-NetTCPConnection -LocalPort 7777 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue; Write-Host 'Stopped existing process on port 7777 (pid' $_.OwningProcess ')' }"
if %ERRORLEVEL% NEQ 0 (
    echo Port check completed (or PowerShell unavailable).
)

echo.
echo Starting WEB4 OASIS API on http://localhost:7777...
echo Press Ctrl+C to stop the API.
echo.

REM Change to project directory and run (dotnet run builds first; DLLs must not be locked)
cd /d "%SCRIPT_DIR%..\ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI"
dotnet run --no-launch-profile --project "%WEB4_PROJECT%" -c Release --urls "http://localhost:7777"
set EXITCODE=%ERRORLEVEL%
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
pause >nul
exit /b %EXITCODE%






