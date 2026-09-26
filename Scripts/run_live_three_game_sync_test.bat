@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_live_three_game_sync_test.ps1" %*
exit /b %ERRORLEVEL%
