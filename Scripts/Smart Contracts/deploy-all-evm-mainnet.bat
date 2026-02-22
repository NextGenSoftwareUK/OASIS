@echo off
REM Deploy OASIS contracts to EVM mainnet. Invokes deploy-all-evm-mainnet.ps1.

cd /d "%~dp0"

if not exist "deploy-all-evm-mainnet.ps1" (
    echo Error: deploy-all-evm-mainnet.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "deploy-all-evm-mainnet.ps1" %*
set EXITCODE=%ERRORLEVEL%
echo.
echo Press any key to close...
pause >nul
exit /b %EXITCODE%
