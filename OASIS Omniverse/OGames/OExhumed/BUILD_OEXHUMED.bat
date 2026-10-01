@echo off
setlocal
REM OExhumed = Raze + OASIS running Exhumed / PowerSlave. One Raze integration covers Shadow Warrior,
REM Blood, Exhumed and Duke (game source chosen at runtime), so this builds the shared
REM Raze engine from OShadowWarrior. Usage: BUILD_OEXHUMED.bat [ batch ]
set "HERE=%~dp0"
call "%HERE%..\OShadowWarrior\BUILD_OSHADOWWARRIOR.bat" %*
exit /b %ERRORLEVEL%
