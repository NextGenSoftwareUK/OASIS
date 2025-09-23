@echo off
echo Testing STAR Web UI REST API...

echo.
echo --- Testing Backend Build ---
dotnet build
if %errorlevel% neq 0 (
    echo ❌ Backend: .NET build failed.
    exit /b %errorlevel%
)
echo ✅ Backend: .NET build succeeded.

echo.
echo --- Testing Frontend Build ---
cd ClientApp
call npm install
if %errorlevel% neq 0 (
    echo ❌ Frontend: npm install failed.
    cd ..
    exit /b %errorlevel%
)
call npm run build
if %errorlevel% neq 0 (
    echo ❌ Frontend: React app build failed.
    cd ..
    exit /b %errorlevel%
)
echo ✅ Frontend: React app built successfully.
cd ..

echo.
echo --- Starting Backend Server ---
echo Starting .NET backend in background...
start /B dotnet run --launch-profile "https"

echo Waiting for server to start...
timeout /t 10 /nobreak > nul

echo.
echo --- Testing REST API Endpoints ---
echo Testing GET /api/star/status...
curl -k -s https://localhost:7001/api/star/status
if %errorlevel% neq 0 (
    echo ❌ REST API: Status endpoint failed.
    echo Make sure the server is running and accessible.
) else (
    echo ✅ REST API: Status endpoint working.
)

echo.
echo --- Test Complete ---
echo The STAR Web UI REST API is ready!
echo Open your browser to: https://localhost:7001
echo.
echo Available endpoints:
echo - GET  /api/star/status
echo - POST /api/star/ignite
echo - POST /api/star/extinguish
echo - POST /api/star/beam-in
echo - POST /api/star/create-avatar
echo - POST /api/star/light
echo - POST /api/star/seed
echo - POST /api/star/unseed
echo.
pause

