@echo off
REM Build the STAR CLI (dotnet build).
REM Run from repo root: Scripts\STAR CLI\BUILD_STAR_CLI.bat   or   Scripts\STAR CLI\BUILD_STAR_CLI.bat Debug
setlocal
set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\.."
set "PROJECT_DIR=%REPO_ROOT%\STAR ODK\NextGenSoftware.OASIS.STAR.CLI"
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Release"

echo Building STAR CLI (%CONFIG%) in %PROJECT_DIR%
dotnet build "%PROJECT_DIR%\NextGenSoftware.OASIS.STAR.CLI.csproj" -c %CONFIG%
if errorlevel 1 exit /b 1
echo Build complete. Run with: Scripts\STAR CLI\RUN_STAR_CLI.bat
