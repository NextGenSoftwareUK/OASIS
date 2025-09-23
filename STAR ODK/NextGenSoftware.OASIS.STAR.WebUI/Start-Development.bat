@echo off
echo ========================================
echo    STAR Web UI - Development Mode
echo ========================================
echo.

echo Starting development servers...
echo.
echo Frontend will be available at: http://localhost:3000
echo Backend API will be available at: https://localhost:7001
echo.

echo Starting backend server in new window...
start "STAR Web UI Backend" cmd /k "dotnet run"

echo Waiting 5 seconds for backend to start...
timeout /t 5 /nobreak > nul

echo Starting frontend development server...
cd ClientApp
start "STAR Web UI Frontend" cmd /k "npm start"

echo.
echo Both servers are starting up!
echo Check the new windows for any errors.
echo.
pause

