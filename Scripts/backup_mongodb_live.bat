@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0backup_mongodb_live.ps1" %*
exit /b %ERRORLEVEL%
