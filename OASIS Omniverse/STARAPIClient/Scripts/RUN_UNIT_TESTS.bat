@echo off
setlocal
cd /d "%~dp0"
echo Running STAR API Client unit tests [Release]...
dotnet test ../TestProjects\NextGenSoftware.OASIS.STARAPI.Client.UnitTests\NextGenSoftware.OASIS.STARAPI.Client.UnitTests.csproj -c Release --no-restore
set EXIT_CODE=%ERRORLEVEL%
if %EXIT_CODE% neq 0 echo Unit tests failed with exit code %EXIT_CODE%
echo.
pause
exit /b %EXIT_CODE%
