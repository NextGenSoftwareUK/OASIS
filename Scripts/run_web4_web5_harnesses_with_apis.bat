@echo off
REM Start WEB4 and WEB5 APIs then run both test harnesses. APIs are stopped at the end.
REM Invokes run_web4_web5_harnesses_with_apis.ps1.

cd /d "%~dp0"

if not exist "run_web4_web5_harnesses_with_apis.ps1" (
    echo Error: run_web4_web5_harnesses_with_apis.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "run_web4_web5_harnesses_with_apis.ps1"
exit /b %ERRORLEVEL%
