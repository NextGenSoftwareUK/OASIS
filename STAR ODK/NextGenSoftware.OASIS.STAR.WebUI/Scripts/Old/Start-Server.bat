@echo off
echo ========================================
echo    Starting STAR Web UI Server...
echo ========================================
echo.

echo Building and starting server on http://localhost:5000
echo.
echo Press Ctrl+C to stop the server
echo.

dotnet run --urls "http://localhost:5000"

pause
