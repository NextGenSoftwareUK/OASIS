@echo off
REM Run ODOOM. ODOOM is a fork of UZDoom - credit: https://github.com/UZDoom/UZDoom (GPL-3.0). See CREDITS_AND_LICENSE.md.
REM If already built, just launches; otherwise builds first.
REM Edit ODOOM_SRC below if your ODOOM (UZDoom) source is not at C:\Source\UZDoom.
REM Put your WAD (e.g. doom2.wad) in the build folder or same folder as ODOOM.exe.

set "ODOOM_SRC=C:\Source\UZDoom"
set "HERE=%~dp0"

REM Prefer ODOOM.exe in ODOOM\build, then ODOOM source build output
set "ODOOM_EXE="
if exist "%HERE%build\ODOOM.exe" set "ODOOM_EXE=%HERE%build\ODOOM.exe"
if not defined ODOOM_EXE if exist "%ODOOM_SRC%\build\Release\ODOOM.exe" set "ODOOM_EXE=%ODOOM_SRC%\build\Release\ODOOM.exe"
if not defined ODOOM_EXE if exist "%ODOOM_SRC%\build\Release\uzdoom.exe" set "ODOOM_EXE=%ODOOM_SRC%\build\Release\uzdoom.exe"

if defined ODOOM_EXE goto :launch

REM Not built yet - run build script then launch
echo ODOOM exe not found. Building...
call "%HERE%BUILD ODOOM.bat"
if not errorlevel 1 if exist "%HERE%build\ODOOM.exe" set "ODOOM_EXE=%HERE%build\ODOOM.exe"
if not errorlevel 1 if not defined ODOOM_EXE if exist "%ODOOM_SRC%\build\Release\ODOOM.exe" set "ODOOM_EXE=%ODOOM_SRC%\build\Release\ODOOM.exe"
if not errorlevel 1 if not defined ODOOM_EXE if exist "%ODOOM_SRC%\build\Release\uzdoom.exe" set "ODOOM_EXE=%ODOOM_SRC%\build\Release\uzdoom.exe"
if not defined ODOOM_EXE (
    echo ODOOM not built. Set ODOOM_SRC in BUILD ODOOM.bat and run again.
) else if not exist "%ODOOM_EXE%" (
    echo ODOOM exe not found. Build failed or path changed.
)
pause
exit /b 0

:launch
echo Launching ODOOM...
start "" "%ODOOM_EXE%"
pause
