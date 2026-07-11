@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%publish-crossplatform.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
echo Press any key to close...
pause >nul
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul

exit /b %EXITCODE%
