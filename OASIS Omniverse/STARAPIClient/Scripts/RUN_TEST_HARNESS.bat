@echo off
setlocal
cd /d "%~dp0"
set STARAPI_HARNESS_USE_FAKE_SERVER=false
echo Running STAR API Client test harness [Release] against real APIs (WEB5 localhost:5556, WEB4 localhost:5555)...
dotnet run --project ../TestProjects\NextGenSoftware.OASIS.STARAPI.Client.TestHarness\NextGenSoftware.OASIS.STARAPI.Client.TestHarness.csproj -c Release
set EXIT_CODE=%ERRORLEVEL%
if %EXIT_CODE% neq 0 echo Test harness failed with exit code %EXIT_CODE%
echo.
pause
exit /b %EXIT_CODE%
