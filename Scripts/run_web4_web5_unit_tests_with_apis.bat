@echo off
REM Start WEB4 and WEB5 APIs in serial, run both unit test projects, then stop the APIs.

cd /d "%~dp0"
if not exist "run_web4_web5_unit_tests_with_apis.ps1" (
    echo Error: run_web4_web5_unit_tests_with_apis.ps1 not found in %~dp0
    pause
    exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "run_web4_web5_unit_tests_with_apis.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
echo Press any key to close...
pause >nul
exit /b %EXITCODE%
