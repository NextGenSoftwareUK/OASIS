@echo off
REM Stop WEB4 and WEB5 API processes. Invokes stop_web4_and_web5_apis.ps1.

cd /d "%~dp0"

if not exist "stop_web4_and_web5_apis.ps1" (
    echo Error: stop_web4_and_web5_apis.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "stop_web4_and_web5_apis.ps1"
exit /b %ERRORLEVEL%
