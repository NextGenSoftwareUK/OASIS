@echo off
setlocal
REM Launch OHeretic: the ODOOM build with the Heretic IWAD (heretic.wad).
REM Put heretic.wad in OGames\ODOOM\build\ (or pass another path as the first argument).
set "HERE=%~dp0"
set "ODOOM_EXE=%HERE%..\ODOOM\build\ODOOM.exe"
set "IWAD=%~1"
if "%IWAD%"=="" set "IWAD=heretic.wad"
if not exist "%ODOOM_EXE%" (
    echo [OHeretic] ODOOM not built yet. Building...
    call "%HERE%BUILD_OHERETIC.bat" batch
)
if not exist "%ODOOM_EXE%" (echo [OHeretic] Build failed or ODOOM.exe missing. & pause & exit /b 1)
echo [OHeretic] Launching ODOOM with %IWAD%...
pushd "%HERE%..\ODOOM\build"
start "" "%ODOOM_EXE%" -iwad "%IWAD%"
popd
exit /b 0
