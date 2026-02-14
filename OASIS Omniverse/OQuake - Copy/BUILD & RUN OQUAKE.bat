@echo off
REM Build OQuake if needed, then launch. Edit paths below if required.
echo   O A S I S   O Q U A K E   ^|   Build ^& Run
echo.
set "OQUAKE_BASEDIR=C:\Program Files (x86)\Steam\steamapps\common\Quake"
set "VKQUAKE_SRC=C:\Source\vkQuake"
set "HERE=%~dp0"
set "QUAKE_ENGINE_EXE="

if exist "%HERE%build\OQUAKE.exe" set "QUAKE_ENGINE_EXE=%HERE%build\OQUAKE.exe"
if not defined QUAKE_ENGINE_EXE if exist "%VKQUAKE_SRC%\Windows\VisualStudio\Build-vkQuake\x64\Release\vkquake.exe" set "QUAKE_ENGINE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\Build-vkQuake\x64\Release\vkquake.exe"
if not defined QUAKE_ENGINE_EXE if exist "%VKQUAKE_SRC%\Windows\VisualStudio\x64\Release\vkquake.exe" set "QUAKE_ENGINE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\x64\Release\vkquake.exe"
if not defined QUAKE_ENGINE_EXE if exist "%VKQUAKE_SRC%\build\vkquake.exe" set "QUAKE_ENGINE_EXE=%VKQUAKE_SRC%\build\vkquake.exe"

if defined QUAKE_ENGINE_EXE goto :launch
echo OQuake not built. Building...
call "%HERE%BUILD_OQUAKE.bat"
if exist "%HERE%build\OQUAKE.exe" set "QUAKE_ENGINE_EXE=%HERE%build\OQUAKE.exe"
if not defined QUAKE_ENGINE_EXE (echo Build failed or path missing. Set VKQUAKE_SRC in BUILD_OQUAKE.bat.) & pause & exit /b 1

:launch
echo Launching OQuake...
start "" "%QUAKE_ENGINE_EXE%" -basedir "%OQUAKE_BASEDIR%"
