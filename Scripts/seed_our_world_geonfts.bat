@echo off
REM Launch the Our World GeoNFT demo seed script; forward all arguments.
cd /d "%~dp0"

echo.
echo ========================================
echo   Our World GeoNFT demo seed
echo ========================================
echo Starting PowerShell...

if not exist "seed_our_world_geonfts.ps1" (
    echo Error: seed_our_world_geonfts.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0seed_our_world_geonfts.ps1" %*
set EXITCODE=%ERRORLEVEL%
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
pause >nul
exit /b %EXITCODE%
