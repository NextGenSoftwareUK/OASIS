@echo off
REM Inventory test: init, auth, get inventory, has_item, add_item, sync, send-to-avatar, send-to-clan.
REM Test runs with: BaseUrl https://dev.api.starnet.oasisomniverse.one, Username dellams. Avatar ID = whatever the API returns for that login (see test output ">>> TEST IS USING AVATAR ID: ... <<<").
REM Optional: add -SendAvatarTarget "username" -SendClanName "ClanName" to test real send targets.
REM REM Old localhost URLs (commented out):
REM REM   -BaseUrl 'http://localhost:5556' (no rebuild)
REM REM   -BaseUrl 'http://localhost:8888' (rebuild)
echo.
echo Test will use: BaseUrl=https://dev.api.starnet.oasisomniverse.one  Username=dellams  (avatar from API - see output below)
echo.
set /p REBUILD="Rebuild STAR API client first? [Y/n]: "
if /i "%REBUILD%"=="n" (
    powershell -ExecutionPolicy Bypass -Command "& '%~dp0compile_and_test_inventory.ps1' -BaseUrl 'https://dev.api.starnet.oasisomniverse.one' -Username 'dellams' -Password 'test12345678' -RebuildClient:$false"
) else (
    powershell -ExecutionPolicy Bypass -Command "& '%~dp0compile_and_test_inventory.ps1' -BaseUrl 'https://dev.api.starnet.oasisomniverse.one' -Username 'dellams' -Password 'test12345678' -RebuildClient:$true"
)

REM OASIS: Explorer pause (OASIS_BAT_NO_PAUSE=1 skips)
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul
