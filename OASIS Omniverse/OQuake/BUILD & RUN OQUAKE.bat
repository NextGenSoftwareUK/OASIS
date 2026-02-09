@echo off
REM Run OQuake. OQuake is based on vkQuake - credit: https://github.com/Novum/vkQuake (GPL-2.0). See CREDITS_AND_LICENSE.md.
REM If already built, just launches; otherwise builds first.
REM Edit OQUAKE_BASEDIR below if your Quake is installed elsewhere.
REM Edit VKQUAKE_SRC if vkQuake is not at C:\Source\vkQuake.

set "OQUAKE_BASEDIR=C:\Program Files (x86)\Steam\steamapps\common\Quake"
set "VKQUAKE_SRC=C:\Source\vkQuake"
set "HERE=%~dp0"

REM Prefer OQUAKE.exe in OQuake\build, then vkQuake source paths
set "QUAKE_ENGINE_EXE="
if exist "%HERE%build\OQUAKE.exe" set "QUAKE_ENGINE_EXE=%HERE%build\OQUAKE.exe"
if not defined QUAKE_ENGINE_EXE if exist "%VKQUAKE_SRC%\Windows\VisualStudio\Build-vkQuake\x64\Release\vkquake.exe" set "QUAKE_ENGINE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\Build-vkQuake\x64\Release\vkquake.exe"
if not defined QUAKE_ENGINE_EXE if exist "%VKQUAKE_SRC%\Windows\VisualStudio\x64\Release\vkquake.exe" set "QUAKE_ENGINE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\x64\Release\vkquake.exe"
if not defined QUAKE_ENGINE_EXE if exist "%VKQUAKE_SRC%\Windows\VisualStudio\Release\vkquake.exe" set "QUAKE_ENGINE_EXE=%VKQUAKE_SRC%\Windows\VisualStudio\Release\vkquake.exe"
if not defined QUAKE_ENGINE_EXE if exist "%VKQUAKE_SRC%\build\Release\vkquake.exe" set "QUAKE_ENGINE_EXE=%VKQUAKE_SRC%\build\Release\vkquake.exe"
if not defined QUAKE_ENGINE_EXE if exist "%VKQUAKE_SRC%\build\vkquake.exe" set "QUAKE_ENGINE_EXE=%VKQUAKE_SRC%\build\vkquake.exe"

if defined QUAKE_ENGINE_EXE goto :launch

REM Not built yet - run build script then launch
echo OQuake exe not found. Building...
call "%HERE%BUILD_OQUAKE.bat"
if not errorlevel 1 if exist "%HERE%build\OQUAKE.exe" set "QUAKE_ENGINE_EXE=%HERE%build\OQUAKE.exe"
if not errorlevel 1 if defined QUAKE_ENGINE_EXE if exist "%QUAKE_ENGINE_EXE%" goto :launch
if not defined QUAKE_ENGINE_EXE (
    echo Engine not built. Set VKQUAKE_SRC in BUILD_OQUAKE.bat and run again.
) else if not exist "%QUAKE_ENGINE_EXE%" (
    echo Engine exe not found. Build failed or path changed.
)
exit /b 0

:launch
echo Launching OQuake...
start "" "%QUAKE_ENGINE_EXE%" -basedir "%OQUAKE_BASEDIR%"
