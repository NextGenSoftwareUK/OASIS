@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0validate_our_world_edge_integration.ps1" %*
exit /b %ERRORLEVEL%
