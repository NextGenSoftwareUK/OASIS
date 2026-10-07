@echo off
setlocal
REM OStrife = ODOOM (UZDoom + OASIS) running Strife.
REM The shared ODOOM integration detects Strife and reports game source OSTRIFE,
REM so there is no separate OStrife integration to build. Usage: BUILD_OSTRIFE.bat [ batch ]
set "HERE=%~dp0"
call "%HERE%..\ODOOM\BUILD ODOOM.bat" %*
exit /b %ERRORLEVEL%
