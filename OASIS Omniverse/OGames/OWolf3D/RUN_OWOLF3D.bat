@echo off
setlocal EnableDelayedExpansion

set "ROOT=%~dp0"
set "WOLF3D_DATA=%WOLF3D_DATA%"
if "%WOLF3D_DATA%"=="" set "WOLF3D_DATA=C:\Wolf3D"

set "ECWOLF_SRC=%OWOLF3D_SRC%"
if "%ECWOLF_SRC%"=="" set "ECWOLF_SRC=C:\Source\OWolf3D"

set "EXE=%ECWOLF_SRC%\build-vs2019-win64\Release\ecwolf.exe"

if not exist "%EXE%" (
    echo ecwolf.exe not found. Building first...
    call "%ROOT%BUILD_OWOLF3D.bat" batch
    if errorlevel 1 (
        echo Build failed. Cannot run OWolf3D.
        pause
        exit /b 1
    )
)

echo Starting OWolf3D...
echo Gamedata: %WOLF3D_DATA%

"%EXE%" --data "%WOLF3D_DATA%"
endlocal
