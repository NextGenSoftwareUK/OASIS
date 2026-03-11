@echo off
setlocal
cd /d "%~dp0"
set TOTAL_EXIT=0

REM Force real APIs (WEB5/WEB4 localhost:5556/5555). Override with STARAPI_INTEGRATION_USE_FAKE=true or STARAPI_HARNESS_USE_FAKE_SERVER=true if you need fake.
set STARAPI_INTEGRATION_USE_FAKE=false
set STARAPI_HARNESS_USE_FAKE_SERVER=false

echo ========== 1/3 Unit tests ==========
dotnet test ../TestProjects\NextGenSoftware.OASIS.STARAPI.Client.UnitTests\NextGenSoftware.OASIS.STARAPI.Client.UnitTests.csproj -c Release --no-restore
if %ERRORLEVEL% neq 0 set TOTAL_EXIT=1

echo.
echo ========== 2/3 Integration tests (real APIs: WEB5 :5556, WEB4 :5555) ==========
dotnet test ../TestProjects\NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests\NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests.csproj -c Release --no-restore
if %ERRORLEVEL% neq 0 set TOTAL_EXIT=1

echo.
echo ========== 3/3 Test harness (real APIs: WEB5 :5556, WEB4 :5555) ==========
dotnet run --project ../TestProjects\NextGenSoftware.OASIS.STARAPI.Client.TestHarness\NextGenSoftware.OASIS.STARAPI.Client.TestHarness.csproj -c Release
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
