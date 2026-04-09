@echo off
REM Run the Demo Quest Seed to seed the STAR API with demo quests for ODOOM/OQuake.
REM Ensure STAR API (WEB5) and OASIS API (WEB4) are running. Optional: -NoBuild

cd /d "%~dp0"

if not exist "run_demo_quest_seed.ps1" (
    echo Error: run_demo_quest_seed.ps1 not found in %~dp0
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "run_demo_quest_seed.ps1" %*
set EXITCODE=%ERRORLEVEL%
echo.
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul
exit /b %EXITCODE%
