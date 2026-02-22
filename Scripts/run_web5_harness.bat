@echo off
REM Run only the WEB5 (STAR) API test harness. API should already be running (default: 5556).
REM Invokes run_web5_harness.ps1. Optional: run with --Web5BaseUrl=http://localhost:5556

cd /d "%~dp0"

if not exist "run_web5_harness.ps1" (
    echo Error: run_web5_harness.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "run_web5_harness.ps1" %*
set EXITCODE=%ERRORLEVEL%
echo.
echo Press any key to close...
pause >nul
exit /b %EXITCODE%
