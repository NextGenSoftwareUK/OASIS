@echo off
setlocal
REM OHexen = ODOOM (UZDoom + OASIS) running Hexen.
REM The shared ODOOM integration detects Hexen and reports game source OHEXEN,
REM so there is no separate OHexen integration to build. Usage: BUILD_OHEXEN.bat [ batch ]
set "HERE=%~dp0"
call "%HERE%..\ODOOM\BUILD ODOOM.bat" %*
exit /b %ERRORLEVEL%
