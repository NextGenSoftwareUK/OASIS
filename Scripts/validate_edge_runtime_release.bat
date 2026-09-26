@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0validate_edge_runtime_release.ps1" %*
exit /b %ERRORLEVEL%
