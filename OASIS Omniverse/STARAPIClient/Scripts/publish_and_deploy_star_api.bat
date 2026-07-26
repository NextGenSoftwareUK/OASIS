@echo off
setlocal
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0publish_and_deploy_star_api.ps1" %*
endlocal

REM OASIS: Explorer pause (OASIS_BAT_NO_PAUSE=1 skips)
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul
