@echo off
setlocal

REM BUILD_OMINECRAFT.bat — Install OMineCraft OASIS mod into Minetest
REM
REM Usage:
REM   BUILD_OMINECRAFT.bat          — interactive install
REM   BUILD_OMINECRAFT.bat batch    — non-interactive
REM
REM No compilation required — Minetest loads Lua mods directly.
REM This script copies the mod to the Minetest mods directory.
REM
REM Prerequisites:
REM   - Minetest 5.6+ installed at C:\Program Files\Minetest (or set MINETEST_DIR)

set BATCH=%1
set SCRIPT_DIR=%~dp0
set MINETEST_DIR=%MINETEST_DIR%
if "%MINETEST_DIR%"=="" set MINETEST_DIR=C:\Program Files\Minetest
set MOD_DEST=%MINETEST_DIR%\mods\oasis

echo.
echo =======================================================
echo  OMineCraft - OASIS STAR API Mod Install (Minetest)
echo =======================================================
echo.

if not exist "%MINETEST_DIR%\minetest.exe" (
    echo [ERROR] Minetest not found at %MINETEST_DIR%
    echo         Set MINETEST_DIR to your Minetest installation path.
    if "%BATCH%"=="" pause
    exit /b 1
)

echo [OMineCraft] Installing mod to: %MOD_DEST%
if not exist "%MOD_DEST%" mkdir "%MOD_DEST%"

for %%F in (init.lua api.lua portals.lua hud.lua mod.conf oasisstar.json) do (
    copy /Y "%SCRIPT_DIR%%%F" "%MOD_DEST%\%%F" > nul
)

echo.
echo [OMineCraft] Mod installed.
echo.
echo Next steps:
echo   1. Open Minetest and select your world.
echo   2. Enable the 'oasis' mod in world settings.
echo   3. Add to minetest.conf:
echo        secure.http_mods = oasis
echo        oasis_star_url = https://star-api.oasisplatform.world/api
echo   4. In-game: /oasis login ^<username^> ^<password^>
echo.
if "%BATCH%"=="" pause
exit /b 0
