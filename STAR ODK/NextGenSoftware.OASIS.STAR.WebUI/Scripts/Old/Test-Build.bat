@echo off
echo ========================================
echo    Testing STAR Web UI Build
echo ========================================
echo.

echo [1/2] Testing Backend Build...
dotnet build --verbosity quiet
if %errorlevel% neq 0 (
    echo ❌ Backend build FAILED
    echo.
    echo Running detailed build to see errors:
    dotnet build
    pause
    exit /b 1
) else (
    echo ✅ Backend build SUCCESS
)

echo.
echo [2/2] Testing Frontend Dependencies...
cd ClientApp
if not exist node_modules (
    echo Installing frontend dependencies...
    npm install
    if %errorlevel% neq 0 (
        echo ❌ Frontend dependencies FAILED
        pause
        exit /b 1
    )
) else (
    echo ✅ Frontend dependencies already installed
)

echo.
echo ✅ ALL TESTS PASSED!
echo.
echo The STAR Web UI is ready to run!
echo Use Start-STAR-WebUI.bat or Start-Development.bat to launch
echo.
pause

