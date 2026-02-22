@echo off
REM Deploy OASIS contracts to all supported chains. Invokes deploy-all-contracts.ps1.

cd /d "%~dp0"

if not exist "deploy-all-contracts.ps1" (
    echo Error: deploy-all-contracts.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "deploy-all-contracts.ps1" %*
exit /b %ERRORLEVEL%
