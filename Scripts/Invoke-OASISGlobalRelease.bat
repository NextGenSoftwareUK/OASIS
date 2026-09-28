@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-OASISGlobalRelease.ps1" %*
exit /b %ERRORLEVEL%
