@echo off
setlocal
REM OHeretic = ODOOM (UZDoom + OASIS) running Heretic.
REM The shared ODOOM integration detects Heretic and reports game source OHERETIC,
REM so there is no separate OHeretic integration to build. Usage: BUILD_OHERETIC.bat [ batch ]
set "HERE=%~dp0"
call "%HERE%..\ODOOM\BUILD ODOOM.bat" %*
exit /b %ERRORLEVEL%
