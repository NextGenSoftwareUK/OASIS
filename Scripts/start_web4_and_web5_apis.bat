@echo off
REM Start WEB4 (ONODE) and WEB5 (STAR) APIs in serial. Calls start_web4_and_web5_apis.ps1.

echo.
echo ========================================
echo Starting WEB4 and WEB5 APIs
echo ========================================
echo.

cd /d "%~dp0"

if not exist "start_web4_and_web5_apis.ps1" (
    echo Error: start_web4_and_web5_apis.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -ExecutionPolicy Bypass -File "start_web4_and_web5_apis.ps1"

pause
