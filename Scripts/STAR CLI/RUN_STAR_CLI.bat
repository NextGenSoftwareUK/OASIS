@echo off
REM Run the STAR CLI. Uses published binary if present (publish\win-x64\star.exe),
REM otherwise runs via dotnet run.
REM Run from repo root: Scripts\STAR CLI\RUN_STAR_CLI.bat   or   Scripts\STAR CLI\RUN_STAR_CLI.bat -- version
setlocal
set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\.."
set "PROJECT_DIR=%REPO_ROOT%\STAR ODK\NextGenSoftware.OASIS.STAR.CLI"
set "PUBLISH_DIR=%PROJECT_DIR%\publish"

if exist "%PUBLISH_DIR%\win-x64\star.exe" (
  "%PUBLISH_DIR%\win-x64\star.exe" %*
  exit /b
)
if exist "%PUBLISH_DIR%\win-arm64\star.exe" (
  "%PUBLISH_DIR%\win-arm64\star.exe" %*
  exit /b
)

cd /d "%PROJECT_DIR%"
dotnet run -c Release -- %*
