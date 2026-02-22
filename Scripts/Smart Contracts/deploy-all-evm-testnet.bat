@echo off
REM Deploy OASIS contracts to EVM testnet. Invokes deploy-all-evm-testnet.ps1.

cd /d "%~dp0"

if not exist "deploy-all-evm-testnet.ps1" (
    echo Error: deploy-all-evm-testnet.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "deploy-all-evm-testnet.ps1" %*
set EXITCODE=%ERRORLEVEL%
echo.
echo Press any key to close...
pause >nul
exit /b %EXITCODE%
