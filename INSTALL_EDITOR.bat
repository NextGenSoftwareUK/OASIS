@echo off
REM Install/copy only the ODOOM Editor (Ultimate Doom Builder) into ODOOM\build\Editor.
REM Runs the Editor-only install script in OASIS Omniverse\ODOOM (no full build).

set "SCRIPT_DIR=%~dp0"
set "ODOOM_INSTALL=%SCRIPT_DIR%OASIS Omniverse\ODOOM\INSTALL_EDITOR.bat"

if not exist "%ODOOM_INSTALL%" (
    echo ERROR: INSTALL_EDITOR.bat not found at %ODOOM_INSTALL%
    pause
    exit /b 1
)

call "%ODOOM_INSTALL%"
