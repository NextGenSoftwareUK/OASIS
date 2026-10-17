@echo off
setlocal
REM Launch OStrife: the ODOOM build with the Strife IWAD (strife1.wad).
REM Put strife1.wad in OGames\ODOOM\build\ (or pass another path as the first argument).
set "HERE=%~dp0"
set "ODOOM_EXE=%HERE%..\ODOOM\build\ODOOM.exe"
set "IWAD=%~1"
if "%IWAD%"=="" set "IWAD=strife1.wad"
if not exist "%ODOOM_EXE%" (
    echo [OStrife] ODOOM not built yet. Building...
    call "%HERE%BUILD_OSTRIFE.bat" batch
)
if not exist "%ODOOM_EXE%" (echo [OStrife] Build failed or ODOOM.exe missing. & pause & exit /b 1)
echo [OStrife] Launching ODOOM with %IWAD%...
pushd "%HERE%..\ODOOM\build"
start "" "%ODOOM_EXE%" -iwad "%IWAD%"
popd
exit /b 0
