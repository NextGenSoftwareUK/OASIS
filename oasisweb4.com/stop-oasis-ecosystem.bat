@echo off
REM OASIS Ecosystem Shutdown Script
REM Stops all OASIS services

echo.
echo 🛑 Shutting down OASIS Ecosystem...
echo =================================
echo.

echo Stopping all dotnet processes...
taskkill /f /im dotnet.exe >nul 2>&1

echo Stopping all node processes...
taskkill /f /im node.exe >nul 2>&1

echo Stopping all npm processes...
taskkill /f /im npm.exe >nul 2>&1

echo.
echo ✅ All OASIS services have been stopped.
echo.
pause

REM OASIS: Explorer pause (OASIS_BAT_NO_PAUSE=1 skips)
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul
