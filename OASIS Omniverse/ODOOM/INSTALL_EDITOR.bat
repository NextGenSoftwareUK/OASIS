@echo off
REM ODOOM - Install/copy only the Editor (Ultimate Doom Builder) into build\Editor.
REM Use this when you only want to refresh the Editor without running the full build.
REM Requires: Ultimate Doom Builder at C:\Source\UltimateDoomBuilder (or set ULTIMATE_DOOM_BUILDER_BUILD below).

set "HERE=%~dp0"
set "ODOOM_INTEGRATION=%HERE%"
set "ULTIMATE_DOOM_BUILDER_BUILD=C:\Source\UltimateDoomBuilder\Build"

echo.
echo [ODOOM] Editor-only install: copying Ultimate Doom Builder into build\Editor...
echo.

if not exist "%ODOOM_INTEGRATION%build" mkdir "%ODOOM_INTEGRATION%build"
if not exist "%ODOOM_INTEGRATION%build\Editor" mkdir "%ODOOM_INTEGRATION%build\Editor"

if not exist "%ODOOM_INTEGRATION%build\Editor\Builder.exe" (
    if exist "%ULTIMATE_DOOM_BUILDER_BUILD%\Builder.exe" (
        echo [ODOOM] Copying Ultimate Doom Builder into build\Editor...
        xcopy "%ULTIMATE_DOOM_BUILDER_BUILD%\*" "%ODOOM_INTEGRATION%build\Editor" /Y /I /Q /E >nul
    ) else (
        echo [ODOOM] ERROR: Builder.exe not found at %ULTIMATE_DOOM_BUILDER_BUILD%
        echo         Edit ULTIMATE_DOOM_BUILDER_BUILD in this script or put Builder.exe in build\Editor manually.
        pause
        exit /b 1
    )
) else (
    echo [ODOOM] build\Editor\Builder.exe already present.
)

if exist "%ODOOM_INTEGRATION%copy_builder_native.ps1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%copy_builder_native.ps1" -EditorDir "%ODOOM_INTEGRATION%build\Editor" -UltimateDoomBuilderRoot "C:\Source\UltimateDoomBuilder"
) else if exist "%ODOOM_INTEGRATION%build\Editor\Builder.exe" if not exist "%ODOOM_INTEGRATION%build\Editor\BuilderNative.dll" (
    if exist "%ULTIMATE_DOOM_BUILDER_BUILD%\BuilderNative.dll" copy /Y "%ULTIMATE_DOOM_BUILDER_BUILD%\BuilderNative.dll" "%ODOOM_INTEGRATION%build\Editor\" >nul
    if exist "%ULTIMATE_DOOM_BUILDER_BUILD%\x64\BuilderNative.dll" copy /Y "%ULTIMATE_DOOM_BUILDER_BUILD%\x64\BuilderNative.dll" "%ODOOM_INTEGRATION%build\Editor\" >nul
)

echo.
echo [ODOOM] Editor install done: %ODOOM_INTEGRATION%build\Editor
echo.
pause
