@echo off
REM Run STAR CLI (published star.exe or dotnet run). Always pauses so file-manager runs show output.
setlocal
set "EXITCODE=0"
set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\.."
set "PROJECT_DIR=%REPO_ROOT%\STAR ODK\NextGenSoftware.OASIS.STAR.CLI"
set "PUBLISH_DIR=%PROJECT_DIR%\publish"

if exist "%PUBLISH_DIR%\win-x64\star.exe" (
  "%PUBLISH_DIR%\win-x64\star.exe" %*
  set "EXITCODE=%ERRORLEVEL%"
  goto :pause_exit
)
if exist "%PUBLISH_DIR%\win-arm64\star.exe" (
  "%PUBLISH_DIR%\win-arm64\star.exe" %*
  set "EXITCODE=%ERRORLEVEL%"
  goto :pause_exit
)

cd /d "%PROJECT_DIR%"
dotnet run -c Release -- %*
set "EXITCODE=%ERRORLEVEL%"

:pause_exit
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
pause >nul
exit /b %EXITCODE%
