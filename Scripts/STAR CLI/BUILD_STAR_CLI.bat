@echo off
REM Build STAR CLI. Pauses at end for file-manager launches.
setlocal
set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\.."
set "PROJECT_DIR=%REPO_ROOT%\STAR ODK\NextGenSoftware.OASIS.STAR.CLI"
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Release"

echo Building STAR CLI (%CONFIG%) in %PROJECT_DIR%
dotnet build "%PROJECT_DIR%\NextGenSoftware.OASIS.STAR.CLI.csproj" -c %CONFIG%
set "EXITCODE=%ERRORLEVEL%"
if %EXITCODE% equ 0 (
  echo Build complete. Run with: Scripts\STAR CLI\RUN_STAR_CLI.bat
) else (
  echo Build failed with exit code %EXITCODE%
)
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
pause >nul
exit /b %EXITCODE%
