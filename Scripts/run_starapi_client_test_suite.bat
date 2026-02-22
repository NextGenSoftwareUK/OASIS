@echo off
REM Run STAR API client test suite. Invokes run_starapi_client_test_suite.ps1.

cd /d "%~dp0"

if not exist "run_starapi_client_test_suite.ps1" (
    echo Error: run_starapi_client_test_suite.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "run_starapi_client_test_suite.ps1" %*
set EXITCODE=%ERRORLEVEL%
echo.
echo Press any key to close...
pause >nul
exit /b %EXITCODE%
