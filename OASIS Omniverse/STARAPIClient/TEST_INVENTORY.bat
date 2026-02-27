@echo off
REM Inventory test: init, auth, get inventory, has_item, add_item, sync, send-to-avatar, send-to-clan.
REM Test runs with: BaseUrl http://localhost:5556, Username dellams. Avatar ID = whatever the API returns for that login (see test output ">>> TEST IS USING AVATAR ID: ... <<<").
REM Optional: add -SendAvatarTarget "username" -SendClanName "ClanName" to test real send targets.
echo.
echo Test will use: BaseUrl=http://localhost:5556  Username=dellams  (avatar from API - see output below)
echo.
set /p REBUILD="Rebuild STAR API client first? [Y/n]: "
if /i "%REBUILD%"=="n" (
    powershell -ExecutionPolicy Bypass -Command "& '%~dp0compile_and_test_inventory.ps1' -BaseUrl 'http://localhost:5556' -Username 'dellams' -Password 'test!' -RebuildClient:$false"
) else (
    powershell -ExecutionPolicy Bypass -Command "& '%~dp0compile_and_test_inventory.ps1' -BaseUrl 'http://localhost:5556' -Username 'dellams' -Password 'test!' -RebuildClient:$true"
)
