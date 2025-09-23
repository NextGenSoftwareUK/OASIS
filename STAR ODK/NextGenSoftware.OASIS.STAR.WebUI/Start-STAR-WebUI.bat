@echo off
echo ========================================
echo    STAR Web UI - Starting Up...
echo ========================================
echo.

echo [1/3] Installing Frontend Dependencies...
cd ClientApp
call npm install
if %errorlevel% neq 0 (
    echo ERROR: Failed to install frontend dependencies
    pause
    exit /b 1
)

echo.
echo [2/3] Building Frontend...
call npm run build
if %errorlevel% neq 0 (
    echo ERROR: Failed to build frontend
    pause
    exit /b 1
)

cd ..
echo.
echo [3/3] Starting Backend Server...
echo.
echo STAR Web UI will be available at:
echo   Frontend: http://localhost:3000
echo   Backend API: https://localhost:7001
echo   Swagger UI: https://localhost:7001/swagger
echo.
echo Press Ctrl+C to stop the server
echo.

dotnet run

pause

