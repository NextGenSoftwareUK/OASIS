@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0validate_our_world_android_build.ps1" %*
exit /b %ERRORLEVEL%
