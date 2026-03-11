@echo off
REM Build STARAPIClient and deploy star_api.dll, star_api.lib, star_api.h to game folders.
REM Build is skipped if star_api.dll is up to date (no C#/csproj changes). Use -ForceBuild to always rebuild.
REM Optional: -RunSmokeTest to compile and run the C smoke test after deploy.
REM
REM Deploy targets: ODOOM, OQuake, and (if present) UZDoom\src, vkQuake\Quake.

setlocal
cd /d "%~dp0"

echo ========================================
echo OASIS STAR API - Build and Deploy
echo ========================================
echo.

powershell -ExecutionPolicy Bypass -File "STARAPIClient\Scripts\publish_and_deploy_star_api.ps1" %*
set "EXIT_CODE=%ERRORLEVEL%"

endlocal
exit /b %EXIT_CODE%
