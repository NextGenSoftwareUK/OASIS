@echo off
setlocal
REM Build STARAPIClient, ODOOM and OQuake with no prompts and no launch.
REM Use RUN ODOOM.bat / RUN OQUAKE.bat to launch after a successful build.

set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
cd /d "%ROOT%"

call "%ROOT%\run_oasis_header.bat" BUILD

echo [1/3] Building and deploying STARAPIClient...
call "%ROOT%\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
if errorlevel 1 (
    echo [BUILD EVERYTHING] STARAPIClient failed.
    exit /b 1
)
echo.

echo [2/3] Building ODOOM (batch, no prompts)...
call "%ROOT%\ODOOM\BUILD ODOOM.bat" batch nosprites
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM build failed.
    exit /b 1
)
echo.

echo [3/3] Building OQuake (batch, no prompts)...
call "%ROOT%\OQuake\BUILD_OQUAKE.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake build failed.
    exit /b 1
)

if exist "%ROOT%\show_oasis_header.ps1" powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\show_oasis_header.ps1" -Success -Message "B U I L D   E V E R Y T H I N G   c o m p l e t e d   s u c c e s s f u l l y" -Message2 "Run RUN ODOOM.bat or RUN OQUAKE.bat to launch."
exit /b 0
