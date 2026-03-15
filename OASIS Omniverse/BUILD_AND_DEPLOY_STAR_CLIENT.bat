@echo off
REM Build STARAPIClient and deploy star_api.dll, star_api.lib, star_api.h to game folders.
REM Build is skipped if star_api.dll is up to date (no C#/csproj changes). Use -ForceBuild to always rebuild.
REM Optional: -RunSmokeTest to compile and run the C smoke test after deploy.
REM
REM Deploy targets: ODOOM, OQuake, and (if present) UZDoom\src, vkQuake\Quake.
REM Load VS environment so dumpbin/lib are in PATH and star_api.lib can be generated from the DLL.

setlocal EnableDelayedExpansion
cd /d "%~dp0"

REM Prefer VS with C++ tools; fallback to any VS so vcvars64 sets PATH for dumpbin/lib (script also finds tools via vswhere if PATH not set)
for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath 2^>nul`) do set "VSINSTALL=%%i"
if not defined VSINSTALL for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath 2^>nul`) do set "VSINSTALL=%%i"
if defined VSINSTALL if exist "!VSINSTALL!\VC\Auxiliary\Build\vcvars64.bat" (
    call "!VSINSTALL!\VC\Auxiliary\Build\vcvars64.bat" >nul 2>&1
)

echo ========================================
echo OASIS STAR API - Build and Deploy
echo ========================================
echo.

powershell -ExecutionPolicy Bypass -File "STARAPIClient\Scripts\publish_and_deploy_star_api.ps1" %*
set "EXIT_CODE=%ERRORLEVEL%"

endlocal
exit /b %EXIT_CODE%
