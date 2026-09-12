@echo off
net session >nul 2>&1
if %errorLevel% == 0 (
    powershell -ExecutionPolicy Bypass -File "%~dp0cleanup-emergency-space.ps1" %*
) else (
    powershell -Command "Start-Process cmd -ArgumentList '/c \"%~f0\" %*' -Verb RunAs"
)
