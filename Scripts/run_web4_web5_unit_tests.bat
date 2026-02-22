@echo off
REM Run WEB4 (ONODE) and WEB5 (STAR) API unit tests in serial.
REM Invokes the PowerShell script in this folder.

cd /d "%~dp0"

if not exist "run_web4_web5_unit_tests.ps1" (
    echo Error: run_web4_web5_unit_tests.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "run_web4_web5_unit_tests.ps1"
exit /b %ERRORLEVEL%
