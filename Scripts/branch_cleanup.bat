@echo off
rem Windows launcher for Scripts\branch_cleanup.sh (runs it with Git Bash).
rem   branch_cleanup.bat           dry run: lists what is safe to delete and what needs review (pauses at the end)
rem   branch_cleanup.bat --apply   deletes the safe branches
rem See Docs\Devs\BRANCH_CLEANUP_RUNBOOK.md.
setlocal

set "GITBASH="
if exist "%ProgramFiles%\Git\bin\bash.exe" set "GITBASH=%ProgramFiles%\Git\bin\bash.exe"
if not defined GITBASH if exist "%ProgramFiles(x86)%\Git\bin\bash.exe" set "GITBASH=%ProgramFiles(x86)%\Git\bin\bash.exe"
if not defined GITBASH if exist "%LocalAppData%\Programs\Git\bin\bash.exe" set "GITBASH=%LocalAppData%\Programs\Git\bin\bash.exe"
if not defined GITBASH (
    echo Git Bash was not found. Install Git for Windows from https://git-scm.com/download/win
    set "RC=1"
    goto :done
)

rem Run from the repository root so the script finds the parent repo and its submodules.
pushd "%~dp0.."
"%GITBASH%" "Scripts/branch_cleanup.sh" %*
set "RC=%ERRORLEVEL%"
popd

if "%RC%"=="3" echo.& echo Some branches need review: see the list above and Docs\Devs\BRANCH_CLEANUP_RUNBOOK.md.

:done
rem Started with no arguments (for example by double-click): keep the window open to read the result.
if "%~1"=="" pause
exit /b %RC%
