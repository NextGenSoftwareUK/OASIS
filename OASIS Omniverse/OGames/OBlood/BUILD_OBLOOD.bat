@echo off
setlocal
REM OBlood = Raze + OASIS running Blood. One Raze integration covers Shadow Warrior,
REM Blood, Exhumed and Duke (game source chosen at runtime), so this builds the shared
REM Raze engine from OShadowWarrior. Usage: BUILD_OBLOOD.bat [ batch ]
set "HERE=%~dp0"
call "%HERE%..\OShadowWarrior\BUILD_OSHADOWWARRIOR.bat" %*
exit /b %ERRORLEVEL%
