@echo off
REM Publish STAR CLI so RUN_STAR_CLI finds star.exe + DNA\OASIS_DNA.json (dotnet build alone does not refresh publish\).
setlocal EnableDelayedExpansion
set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\.."
set "PROJECT_DIR=%REPO_ROOT%\STAR ODK\NextGenSoftware.OASIS.STAR.CLI"
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Release"
set "OUT=%PROJECT_DIR%\publish\win-x64"

if /i "%STAR_CLI_QUICK_PUBLISH%"=="1" (
  echo Publishing STAR CLI quick: framework-dependent to !OUT!
  dotnet publish "!PROJECT_DIR!\NextGenSoftware.OASIS.STAR.CLI.csproj" -c !CONFIG! -r win-x64 -o "!OUT!" --self-contained false -p:PublishSingleFile=false
) else (
  echo Publishing STAR CLI single-file self-contained to !OUT!
  dotnet publish "!PROJECT_DIR!\NextGenSoftware.OASIS.STAR.CLI.csproj" -c !CONFIG! -r win-x64 -o "!OUT!" -p:PublishSingleFile=true -p:SelfContained=true
)
set "EXITCODE=%ERRORLEVEL%"
if not %EXITCODE% equ 0 goto :done

if not exist "!OUT!\DNA\OASIS_DNA.json" (
  echo ERROR: DNA\OASIS_DNA.json missing after publish in !OUT!
  set "EXITCODE=1"
  goto :done
)
echo OK: !OUT!\star.exe and DNA\OASIS_DNA.json
echo Run: Scripts\STAR CLI\RUN_STAR_CLI.bat

:done
if %EXITCODE% equ 0 (
  echo Publish complete.
) else (
  echo Publish failed with exit code %EXITCODE%
)
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
pause >nul
exit /b %EXITCODE%
