@echo off
setlocal
REM Launch OShadowWarriorRT: Raze (OShadowWarrior-RT) + OASIS. Put the Shadow Warrior (RT) game data (sw.grp) where Raze
REM finds it; Raze's startup picker selects the game, and OASIS reports it as that game.
set "HERE=%~dp0"
if not defined RAZE_SRC set "RAZE_SRC=C:\Source\OShadowWarrior-RT"
set "RAZE_EXE=%RAZE_SRC%\build-vs\Release\raze.exe"
if not exist "%RAZE_EXE%" (
    echo [OShadowWarriorRT] Raze not built yet. Building...
    call "%HERE%BUILD_OSHADOWWARRIORRT.bat" batch
)
if not exist "%RAZE_EXE%" (echo [OShadowWarriorRT] Build failed or raze.exe missing. & pause & exit /b 1)
pushd "%RAZE_SRC%\build-vs\Release"
start "" "%RAZE_EXE%" %*
popd
exit /b 0
