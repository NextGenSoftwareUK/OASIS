@echo off
setlocal
REM ODuke3D-RT - Raze (ODuke3D-RT) + OASIS STAR API, the ODOOM/OQuake way via OGLib/oglib_game.h.
REM One Raze integration covers Shadow Warrior, Blood, Exhumed and Duke; the game source is
REM chosen at runtime. Usage: BUILD_ODUKE3DRT.bat [ batch ]

set "HERE=%~dp0"
if not defined RAZE_SRC set "RAZE_SRC=C:\Source\ODuke3D-RT"
set "OMNIVERSE=%HERE%..\.."
set "OGENGINECLIENT=%OMNIVERSE%\OGEngineClient"
set "OGLIB=%OMNIVERSE%\OGLib"
REM BUILD_AND_DEPLOY_STAR_CLIENT.bat publishes the native Edge profile here.
set "STAR_PUBLISH=%OMNIVERSE%\..\artifacts\native-games\native\Edge\win-x64\publish"
set "INTEGRATION=%HERE%..\OShadowWarrior"

if exist "%OMNIVERSE%\run_oasis_header.bat" call "%OMNIVERSE%\run_oasis_header.bat" ODUKE3DRT

if exist "%OMNIVERSE%\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%OMNIVERSE%\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [ODuke3D-RT] OGEngineClient build failed. & (if not "%~1"=="batch" pause) & exit /b 1)
)

if not exist "%RAZE_SRC%\source\core\gamecontrol.cpp" (
    echo [ODuke3D-RT] Raze source not found at %RAZE_SRC% ^(set RAZE_SRC to override^)
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [ODuke3D-RT] Installing OASIS integration into %RAZE_SRC%\source\core ...
if not exist "%RAZE_SRC%\source\core\oasis" mkdir "%RAZE_SRC%\source\core\oasis"
copy /Y "%INTEGRATION%\raze_ogengine_integration.cpp" "%RAZE_SRC%\source\core\" >nul
copy /Y "%INTEGRATION%\raze_ogengine_integration.h"   "%RAZE_SRC%\source\core\" >nul
for %%F in (ogengine.h ogengine_sync.h ogengine_sync.c) do copy /Y "%OGENGINECLIENT%\%%F" "%RAZE_SRC%\source\core\oasis\" >nul
for %%F in (oglib_game.h oglib_config.h oglib_edge.h oglib_json.h oglib_str.h) do copy /Y "%OGLIB%\%%F" "%RAZE_SRC%\source\core\oasis\" >nul

echo [ODuke3D-RT] Building Raze with OASIS_STAR_API=ON...
if not exist "%RAZE_SRC%\build-vs" mkdir "%RAZE_SRC%\build-vs"
REM RT renderer: build the NRI vendored in the engine (matches its headers). This generates
REM NRIAgilitySDK.h, fetches the DirectX Agility SDK and produces NRI.dll for Raze to stage.
set "NRI_SRC=%RAZE_SRC%\libraries\NRIFramework\External\NRI"
cmake -S "%NRI_SRC%" -B "%NRI_SRC%\_Build" -A x64
if errorlevel 1 (echo [ODuke3D-RT] NRI configure failed. & (if not "%~1"=="batch" pause) & exit /b 1)
cmake --build "%NRI_SRC%\_Build" --config Release
if errorlevel 1 (echo [ODuke3D-RT] NRI build failed. & (if not "%~1"=="batch" pause) & exit /b 1)
REM NRD: Raze compiles NRD's C++ directly but needs NRD's precompiled shader headers
REM (libraries\NRD\_Shaders). Build them with settings identical to Raze's NRD_* definitions.
set "NRD_SRC=%RAZE_SRC%\libraries\NRD"
cmake -S "%NRD_SRC%" -B "%NRD_SRC%\_Build" -A x64 -DNRD_NORMAL_ENCODING=2 -DNRD_ROUGHNESS_ENCODING=1 -DNRD_SUPPORTS_VIEWPORT_OFFSET=OFF -DNRD_SUPPORTS_CHECKERBOARD=OFF -DNRD_SUPPORTS_HISTORY_CONFIDENCE=OFF -DNRD_SUPPORTS_DISOCCLUSION_THRESHOLD_MIX=OFF -DNRD_SUPPORTS_BASECOLOR_METALNESS=ON -DNRD_SUPPORTS_ANTIFIREFLY=OFF -DREBLUR_PERFORMANCE_MODE=OFF
if errorlevel 1 (echo [ODuke3D-RT] NRD configure failed. & (if not "%~1"=="batch" pause) & exit /b 1)
cmake --build "%NRD_SRC%\_Build" --config Release
if errorlevel 1 (echo [ODuke3D-RT] NRD build failed. & (if not "%~1"=="batch" pause) & exit /b 1)
cmake -S "%RAZE_SRC%" -B "%RAZE_SRC%\build-vs" -A x64 -DOASIS_STAR_API=ON "-DOGENGINE_LIB_DIR=%STAR_PUBLISH%" "-DRAZE_NRI_RUNTIME_DIR=%NRI_SRC%\_Bin\Release"
if errorlevel 1 (echo [ODuke3D-RT] CMake configure failed. & (if not "%~1"=="batch" pause) & exit /b 1)
cmake --build "%RAZE_SRC%\build-vs" --config Release
if errorlevel 1 (echo [ODuke3D-RT] Build failed. & (if not "%~1"=="batch" pause) & exit /b 1)
for %%F in (ogengine.dll e_sqlite3.dll) do copy /Y "%STAR_PUBLISH%\%%F" "%RAZE_SRC%\build-vs\Release\" >nul

echo.
echo [ODuke3D-RT] Done: %RAZE_SRC%\build-vs\Release\raze.exe
if not "%~1"=="batch" pause
exit /b 0
