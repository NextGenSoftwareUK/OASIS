@echo off
setlocal
cd /d "%~dp0"
set TOTAL_EXIT=0

echo ========== 1/3 Unit tests ==========
dotnet test TestProjects\NextGenSoftware.OASIS.STARAPI.Client.UnitTests\NextGenSoftware.OASIS.STARAPI.Client.UnitTests.csproj -c Release --no-restore
if %ERRORLEVEL% neq 0 set TOTAL_EXIT=1

echo.
echo ========== 2/3 Integration tests ==========
echo Default: real APIs. Set STARAPI_INTEGRATION_USE_FAKE=true for fake servers.
dotnet test TestProjects\NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests\NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests.csproj -c Release --no-restore
if %ERRORLEVEL% neq 0 set TOTAL_EXIT=1

echo.
echo ========== 3/3 Test harness ==========
echo Default: real APIs. Set STARAPI_HARNESS_USE_FAKE_SERVER=true for fake servers.
dotnet run --project TestProjects\NextGenSoftware.OASIS.STARAPI.Client.TestHarness\NextGenSoftware.OASIS.STARAPI.Client.TestHarness.csproj -c Release
if %ERRORLEVEL% neq 0 set TOTAL_EXIT=1

echo.
if %TOTAL_EXIT% equ 0 (
    echo All tests passed.
) else (
    echo One or more test runs failed.
)
echo.
pause
exit /b %TOTAL_EXIT%
