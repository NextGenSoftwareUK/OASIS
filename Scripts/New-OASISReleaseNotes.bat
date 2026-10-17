@echo off
setlocal
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0New-OASISReleaseNotes.ps1" %*
exit /b %ERRORLEVEL%
