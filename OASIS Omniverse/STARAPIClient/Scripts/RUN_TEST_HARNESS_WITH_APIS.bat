@echo off
REM Start WEB4 and WEB5 APIs (root Scripts\start_web4_and_web5_apis), then run STARAPIClient test harness. APIs stopped at end.

cd /d "%~dp0"

if not exist "run_test_harness_with_apis.ps1" (
    echo Error: run_test_harness_with_apis.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "run_test_harness_with_apis.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul
exit /b %EXITCODE%
