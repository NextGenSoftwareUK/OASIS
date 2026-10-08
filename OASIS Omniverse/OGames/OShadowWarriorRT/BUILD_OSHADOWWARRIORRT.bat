@echo off
setlocal
REM OShadowWarriorRT - Raze (OShadowWarrior-RT) + OASIS STAR API, the ODOOM/OQuake way via OGLib/oglib_game.h.
REM One Raze integration covers Shadow Warrior, Blood, Exhumed and Duke; the game source is
REM chosen at runtime. Usage: BUILD_OSHADOWWARRIORRT.bat [ batch ]

set "HERE=%~dp0"
if not defined RAZE_SRC set "RAZE_SRC=C:\Source\OShadowWarrior-RT"
set "OMNIVERSE=%HERE%..\.."
set "OGENGINECLIENT=%OMNIVERSE%\OGEngineClient"
set "OGLIB=%OMNIVERSE%\OGLib"
REM BUILD_AND_DEPLOY_STAR_CLIENT.bat publishes the native Edge profile here.
set "STAR_PUBLISH=%OMNIVERSE%\..\artifacts\native-games\native\Edge\win-x64\publish"
set "INTEGRATION=%HERE%..\OShadowWarrior"

if exist "%OMNIVERSE%\run_oasis_header.bat" call "%OMNIVERSE%\run_oasis_header.bat" OSHADOWWARRIORRT

if exist "%OMNIVERSE%\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%OMNIVERSE%\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [OShadowWarriorRT] OGEngineClient build failed. & (if not "%~1"=="batch" pause) & exit /b 1)
)

if not exist "%RAZE_SRC%\source\core\gamecontrol.cpp" (
    echo [OShadowWarriorRT] Raze source not found at %RAZE_SRC% ^(set RAZE_SRC to override^)
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [OShadowWarriorRT] Installing OASIS integration into %RAZE_SRC%\source\core ...
if not exist "%RAZE_SRC%\source\core\oasis" mkdir "%RAZE_SRC%\source\core\oasis"
copy /Y "%INTEGRATION%\raze_ogengine_integration.cpp" "%RAZE_SRC%\source\core\" >nul
copy /Y "%INTEGRATION%\raze_ogengine_integration.h"   "%RAZE_SRC%\source\core\" >nul
for %%F in (ogengine.h ogengine_sync.h ogengine_sync.c) do copy /Y "%OGENGINECLIENT%\%%F" "%RAZE_SRC%\source\core\oasis\" >nul
for %%F in (oglib_game.h oglib_config.h oglib_edge.h oglib_json.h oglib_str.h) do copy /Y "%OGLIB%\%%F" "%RAZE_SRC%\source\core\oasis\" >nul

echo [OShadowWarriorRT] Building Raze with OASIS_STAR_API=ON...
if not exist "%RAZE_SRC%\build-vs" mkdir "%RAZE_SRC%\build-vs"
cmake -S "%RAZE_SRC%" -B "%RAZE_SRC%\build-vs" -A x64 -DOASIS_STAR_API=ON "-DOGENGINE_LIB_DIR=%STAR_PUBLISH%"
if errorlevel 1 (echo [OShadowWarriorRT] CMake configure failed. & (if not "%~1"=="batch" pause) & exit /b 1)
cmake --build "%RAZE_SRC%\build-vs" --config Release
if errorlevel 1 (echo [OShadowWarriorRT] Build failed. & (if not "%~1"=="batch" pause) & exit /b 1)
for %%F in (ogengine.dll e_sqlite3.dll) do copy /Y "%STAR_PUBLISH%\%%F" "%RAZE_SRC%\build-vs\Release\" >nul

echo.
echo [OShadowWarriorRT] Done: %RAZE_SRC%\build-vs\Release\raze.exe
if not "%~1"=="batch" pause
exit /b 0
