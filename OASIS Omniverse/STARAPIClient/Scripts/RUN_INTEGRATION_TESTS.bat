@echo off
setlocal
cd /d "%~dp0"
set STARAPI_INTEGRATION_USE_FAKE=false
echo Running STAR API Client integration tests [Release] against real APIs (WEB5 localhost:5556, WEB4 localhost:5555)...
dotnet test ../TestProjects\NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests\NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests.csproj -c Release --no-restore
set EXIT_CODE=%ERRORLEVEL%
if %EXIT_CODE% neq 0 echo Integration tests failed with exit code %EXIT_CODE%
echo.
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul
exit /b %EXIT_CODE%
