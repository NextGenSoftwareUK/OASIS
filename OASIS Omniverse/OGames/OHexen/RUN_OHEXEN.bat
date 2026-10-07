@echo off
setlocal
REM Launch OHexen: the ODOOM build with the Hexen IWAD (hexen.wad).
REM Put hexen.wad in OGames\ODOOM\build\ (or pass another path as the first argument).
set "HERE=%~dp0"
set "ODOOM_EXE=%HERE%..\ODOOM\build\ODOOM.exe"
set "IWAD=%~1"
if "%IWAD%"=="" set "IWAD=hexen.wad"
if not exist "%ODOOM_EXE%" (
    echo [OHexen] ODOOM not built yet. Building...
    call "%HERE%BUILD_OHEXEN.bat" batch
)
if not exist "%ODOOM_EXE%" (echo [OHexen] Build failed or ODOOM.exe missing. & pause & exit /b 1)
echo [OHexen] Launching ODOOM with %IWAD%...
pushd "%HERE%..\ODOOM\build"
start "" "%ODOOM_EXE%" -iwad "%IWAD%"
popd
exit /b 0
