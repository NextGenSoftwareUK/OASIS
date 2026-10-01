@echo off
setlocal
REM ODoom64 - DOOM 64 EX+ + OASIS, the ODOOM/OQuake way via OGLib/oglib_game.h.
REM Installs the integration into src\engine and builds Windows\DOOM64.sln with
REM /p:OasisStarApi=true (the integration is opt-in in the project).
REM Usage: BUILD_ODOOM64.bat [ batch ]

set "HERE=%~dp0"
if not defined DOOM64_SRC set "DOOM64_SRC=C:\Source\ODOOM64"
set "OMNIVERSE=%HERE%..\.."
set "OGENGINECLIENT=%OMNIVERSE%\OGEngineClient"
set "OGLIB=%OMNIVERSE%\OGLib"

if exist "%OMNIVERSE%\run_oasis_header.bat" call "%OMNIVERSE%\run_oasis_header.bat" ODOOM64

if exist "%OMNIVERSE%\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%OMNIVERSE%\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [ODoom64] OGEngineClient build failed. & if not "%~1"=="batch" pause & exit /b 1)
)

if not exist "%DOOM64_SRC%\src\engine\d_main.c" (
    echo [ODoom64] Doom64 EX+ source not found at %DOOM64_SRC% ^(set DOOM64_SRC to override^)
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [ODoom64] Installing OASIS integration into %DOOM64_SRC%\src\engine ...
if not exist "%DOOM64_SRC%\src\engine\oasis" mkdir "%DOOM64_SRC%\src\engine\oasis"
copy /Y "%HERE%odoom64_ogengine_integration.c" "%DOOM64_SRC%\src\engine\" >nul
copy /Y "%HERE%odoom64_ogengine_integration.h" "%DOOM64_SRC%\src\engine\" >nul
for %%F in (ogengine.h ogengine_sync.h ogengine_sync.c) do copy /Y "%OGENGINECLIENT%\%%F" "%DOOM64_SRC%\src\engine\oasis\" >nul
for %%F in (oglib_game.h oglib_config.h oglib_edge.h oglib_json.h oglib_str.h) do copy /Y "%OGLIB%\%%F" "%DOOM64_SRC%\src\engine\oasis\" >nul

set "MSBUILD="
for /f "usebackq tokens=*" %%I in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%I"
if not defined MSBUILD (echo [ODoom64] MSBuild not found. & if not "%~1"=="batch" pause & exit /b 1)

echo [ODoom64] Building DOOM64.sln (Release x64, OasisStarApi=true)...
"%MSBUILD%" "%DOOM64_SRC%\Windows\DOOM64.sln" /m /p:Configuration=Release /p:Platform=x64 /p:OasisStarApi=true "/p:OgengineDir=%OGENGINECLIENT%"
if errorlevel 1 (echo [ODoom64] Build failed. & if not "%~1"=="batch" pause & exit /b 1)

echo.
echo [ODoom64] Done. In the game console: star beamin ^<user^> ^<pass^>
if not "%~1"=="batch" pause
exit /b 0
