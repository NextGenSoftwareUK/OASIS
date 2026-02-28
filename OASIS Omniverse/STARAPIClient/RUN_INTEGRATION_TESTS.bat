@echo off
setlocal
cd /d "%~dp0"
echo Running STAR API Client integration tests [Release]...
echo Default: real APIs [WEB5 localhost:5556, WEB4 localhost:5555]. Set STARAPI_INTEGRATION_USE_FAKE=true for fake servers.
dotnet test TestProjects\NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests\NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests.csproj -c Release --no-restore
set EXIT_CODE=%ERRORLEVEL%
if %EXIT_CODE% neq 0 echo Integration tests failed with exit code %EXIT_CODE%
echo.
pause
exit /b %EXIT_CODE%
