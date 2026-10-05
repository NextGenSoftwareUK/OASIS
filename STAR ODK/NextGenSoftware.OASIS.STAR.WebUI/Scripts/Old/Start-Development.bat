@echo off
echo ========================================
echo    STAR Web UI - Development Mode
echo ========================================
echo.

echo Starting development servers...
echo.
echo Frontend will be available at: http://localhost:3000
echo WEB5 API will be available at: http://localhost:50564
echo.

echo Starting WEB5 STAR API server in new window...
start "WEB5 STAR API" cmd /k "cd /d %~dp0..\NextGenSoftware.OASIS.STAR.WebAPI && dotnet run --urls http://localhost:50564"

echo Waiting 5 seconds for WEB5 API to start...
timeout /t 5 /nobreak > nul

echo Starting frontend development server...
cd ClientApp
start "STAR Web UI Frontend" cmd /k "npm start"

echo.
echo Both servers are starting up!
echo Check the new windows for any errors.
echo.
pause

