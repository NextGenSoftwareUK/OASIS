@echo off
REM Run WEB4 and WEB5 API test harnesses against already-running APIs (default: 5555, 5556).
REM Invokes run_web4_web5_harnesses.ps1. Start APIs first with start_web4_and_web5_apis.bat or start_web4_and_web5_apis.ps1.

cd /d "%~dp0"

if not exist "run_web4_web5_harnesses.ps1" (
    echo Error: run_web4_web5_harnesses.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "run_web4_web5_harnesses.ps1" %*
exit /b %ERRORLEVEL%
