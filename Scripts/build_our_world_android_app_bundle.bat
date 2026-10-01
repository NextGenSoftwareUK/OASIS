@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_our_world_android_app_bundle.ps1" %*
exit /b %ERRORLEVEL%
