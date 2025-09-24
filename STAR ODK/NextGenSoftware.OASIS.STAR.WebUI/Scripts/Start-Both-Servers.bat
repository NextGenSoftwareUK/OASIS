@echo off
echo Starting STAR Web UI - WEB5 API and Frontend
echo.

echo Starting WEB5 STAR API Server...
start "WEB5 STAR API" cmd /k "cd /d %~dp0..\NextGenSoftware.OASIS.STAR.WebAPI && dotnet run --urls http://localhost:50564"

echo Waiting for WEB5 API to start...
timeout 10

echo Starting React Frontend...
start "STAR Frontend" cmd /k "cd /d %~dp0ClientApp && npm start"

echo.
echo Both servers are starting...
echo WEB5 API: http://localhost:50564
echo Frontend: http://localhost:3000
echo.
echo Press any key to exit this window (servers will continue running)
pause > nul


