@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%publish-crossplatform.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
echo Press any key to close...
pause >nul
exit /b %EXITCODE%
