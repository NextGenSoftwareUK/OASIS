@echo off
setlocal
REM Launch ODuke3D-RT: Raze (ODuke3D-RT) + OASIS. Put the Duke Nukem 3D (RT) game data (duke3d.grp) where Raze
REM finds it; Raze's startup picker selects the game, and OASIS reports it as that game.
set "HERE=%~dp0"
if not defined RAZE_SRC set "RAZE_SRC=C:\Source\ODuke3D-RT"
set "RAZE_EXE=%RAZE_SRC%\build-vs\Release\raze.exe"
if not exist "%RAZE_EXE%" (
    echo [ODuke3D-RT] Raze not built yet. Building...
    call "%HERE%BUILD_ODUKE3DRT.bat" batch
)
if not exist "%RAZE_EXE%" (echo [ODuke3D-RT] Build failed or raze.exe missing. & pause & exit /b 1)
pushd "%RAZE_SRC%\build-vs\Release"
start "" "%RAZE_EXE%" %*
popd
exit /b 0
