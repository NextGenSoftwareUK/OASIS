@echo off
REM Build ODOOM if needed, then launch. Put WAD (e.g. doom2.wad) in ODOOM\build\.
echo   O A S I S   O D O O M   ^|   Build ^& Run
echo.
set "ODOOM_SRC=C:\Source\UZDoom"
set "HERE=%~dp0"
set "ODOOM_EXE="

if exist "%HERE%build\ODOOM.exe" set "ODOOM_EXE=%HERE%build\ODOOM.exe"
if not defined ODOOM_EXE if exist "%ODOOM_SRC%\build\Release\uzdoom.exe" set "ODOOM_EXE=%ODOOM_SRC%\build\Release\uzdoom.exe"

if defined ODOOM_EXE goto :launch
echo ODOOM not built. Building...
call "%HERE%BUILD ODOOM.bat"
if exist "%HERE%build\ODOOM.exe" set "ODOOM_EXE=%HERE%build\ODOOM.exe"
if not defined ODOOM_EXE if exist "%ODOOM_SRC%\build\Release\uzdoom.exe" set "ODOOM_EXE=%ODOOM_SRC%\build\Release\uzdoom.exe"
if not defined ODOOM_EXE (echo Build failed or path missing.) & pause & exit /b 1

:launch
echo Launching ODOOM...
start "" "%ODOOM_EXE%"
