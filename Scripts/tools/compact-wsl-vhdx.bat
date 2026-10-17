@echo off
net session >nul 2>&1
if %errorLevel% == 0 (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0compact-wsl-vhdx.ps1" %*
) else (
    powershell -NoProfile -Command "Start-Process powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"%~dp0compact-wsl-vhdx.ps1\"' -Verb RunAs -Wait"
)

