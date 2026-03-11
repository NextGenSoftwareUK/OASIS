@echo off
setlocal
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0publish_and_deploy_star_api.ps1" %*
endlocal
