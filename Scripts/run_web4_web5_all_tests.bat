@echo off
REM Run all WEB4 and WEB5 tests: unit tests, integration tests, and test harnesses (APIs started for harnesses).

cd /d "%~dp0"

if not exist "run_web4_web5_all_tests.ps1" (
    echo Error: run_web4_web5_all_tests.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "run_web4_web5_all_tests.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
echo Press any key to close...
pause >nul
exit /b %EXITCODE%
