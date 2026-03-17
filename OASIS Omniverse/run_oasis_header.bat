@echo off
setlocal
set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
set "TITLE="
set "SUB="
if "%~1"=="ODOOM" set "TITLE=O A S I S   O D O O M"
if "%~1"=="OQUAKE" set "TITLE=O A S I S   O Q U A K E"
if "%~1"=="BUILD" set "TITLE=T H E   O A S I S   O M N I V E R S E" & set "SUB=STARAPIClient + ODOOM + OQuake"
if not exist "%ROOT%\show_oasis_header.ps1" exit /b 0
if defined SUB (powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\show_oasis_header.ps1" -Title "%TITLE%" -Subtitle "%SUB%") else (powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\show_oasis_header.ps1" -Title "%TITLE%")
