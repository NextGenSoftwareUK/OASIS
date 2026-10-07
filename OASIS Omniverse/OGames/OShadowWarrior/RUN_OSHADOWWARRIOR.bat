@echo off
setlocal
REM Launch OShadowWarrior: Raze (OShadowWarrior) + OASIS. Put the Shadow Warrior game data (sw.grp) where Raze
REM finds it; Raze's startup picker selects the game, and OASIS reports it as that game.
set "HERE=%~dp0"
if not defined RAZE_SRC set "RAZE_SRC=C:\Source\OShadowWarrior"
set "RAZE_EXE=%RAZE_SRC%\build-vs\Release\raze.exe"
if not exist "%RAZE_EXE%" (
    echo [OShadowWarrior] Raze not built yet. Building...
    call "%HERE%BUILD_OSHADOWWARRIOR.bat" batch
)
if not exist "%RAZE_EXE%" (echo [OShadowWarrior] Build failed or raze.exe missing. & pause & exit /b 1)
pushd "%RAZE_SRC%\build-vs\Release"
start "" "%RAZE_EXE%" %*
popd
exit /b 0
