@echo off
setlocal
cd /d "%~dp0"
echo Running STAR API Client test harness [Release]...
echo Default: real APIs [WEB5 localhost:5556, WEB4 localhost:5555]. Set STARAPI_HARNESS_USE_FAKE_SERVER=true or STARAPI_HARNESS_MODE=fake for fake servers.
dotnet run --project TestProjects\NextGenSoftware.OASIS.STARAPI.Client.TestHarness\NextGenSoftware.OASIS.STARAPI.Client.TestHarness.csproj -c Release
set EXIT_CODE=%ERRORLEVEL%
if %EXIT_CODE% neq 0 echo Test harness failed with exit code %EXIT_CODE%
echo.
pause
exit /b %EXIT_CODE%
