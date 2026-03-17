@echo off
REM Deploy master contracts. Invokes deploy-master.ps1.

cd /d "%~dp0"

if not exist "deploy-master.ps1" (
    echo Error: deploy-master.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "deploy-master.ps1" %*
set EXITCODE=%ERRORLEVEL%
echo.
echo Press any key to close...
pause >nul
exit /b %EXITCODE%
