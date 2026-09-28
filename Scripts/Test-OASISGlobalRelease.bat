@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Test-OASISGlobalRelease.ps1" %*
exit /b %ERRORLEVEL%
