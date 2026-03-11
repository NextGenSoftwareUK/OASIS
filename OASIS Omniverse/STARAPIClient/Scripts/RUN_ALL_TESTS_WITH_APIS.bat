@echo off
REM Start WEB4 and WEB5 APIs (root Scripts\start_web4_and_web5_apis), then run STARAPIClient unit + integration + harness. APIs stopped at end.

cd /d "%~dp0"

if not exist "run_all_tests_with_apis.ps1" (
    echo Error: run_all_tests_with_apis.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "run_all_tests_with_apis.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
pause
exit /b %EXITCODE%
