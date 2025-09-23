@echo off
echo Starting STAR Web UI - Backend and Frontend
echo.

echo Starting .NET Backend Server...
cd "STAR ODK\NextGenSoftware.OASIS.STAR.WebUI"
start "STAR Backend" cmd /k "dotnet run --urls http://localhost:50563"

echo Waiting for backend to start...
timeout /t 15 /nobreak > nul

echo Starting React Frontend...
cd "STAR ODK\NextGenSoftware.OASIS.STAR.WebUI\ClientApp"
start "STAR Frontend" cmd /k "npm start"

echo.
echo Both servers are starting...
echo Backend: http://localhost:50563
echo Frontend: http://localhost:3000
echo.
echo Press any key to exit this window (servers will continue running)
pause > nul

