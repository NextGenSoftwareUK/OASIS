@echo off
echo Testing STAR Web UI REST API...

echo.
echo --- Testing WEB5 API Build ---
cd ..\NextGenSoftware.OASIS.STAR.WebAPI
dotnet build
if %errorlevel% neq 0 (
    echo ❌ WEB5 API: .NET build failed.
    cd ..\NextGenSoftware.OASIS.STAR.WebUI
    exit /b %errorlevel%
)
echo ✅ WEB5 API: .NET build succeeded.
cd ..\NextGenSoftware.OASIS.STAR.WebUI

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
echo --- Starting WEB5 API Server ---
echo Starting WEB5 STAR API in background...
cd ..\NextGenSoftware.OASIS.STAR.WebAPI
start /B dotnet run --urls http://localhost:50564
cd ..\NextGenSoftware.OASIS.STAR.WebUI

echo Waiting for server to start...
timeout /t 10 /nobreak > nul

echo.
echo --- Testing REST API Endpoints ---
echo Testing GET /api/star/status...
curl -s http://localhost:50564/api/star/status
if %errorlevel% neq 0 (
    echo ❌ REST API: Status endpoint failed.
    echo Make sure the server is running and accessible.
) else (
    echo ✅ REST API: Status endpoint working.
)

echo.
echo --- Test Complete ---
echo The WEB5 STAR API is ready!
echo Open your browser to: http://localhost:50564
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
echo - GET  /api/missions
echo - GET  /api/oapps
echo - GET  /api/nfts
echo.
pause

