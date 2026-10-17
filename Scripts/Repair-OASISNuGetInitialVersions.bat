@echo off
setlocal
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0Repair-OASISNuGetInitialVersions.ps1" %*
exit /b %ERRORLEVEL%
