@echo off
REM OASIS Ecosystem Startup Script
REM Starts the complete OASIS ecosystem including Web4 API, STAR Web UI, and new marketing site

setlocal enabledelayedexpansion

echo.
echo 🚀 Starting OASIS Ecosystem...
echo =================================
echo.

REM Check if required tools are available
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET SDK not found. Please install .NET 8 SDK.
    pause
    exit /b 1
)

where npm >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Node.js/npm not found. Please install Node.js.
    pause
    exit /b 1
)

REM Build projects
echo 🔨 Building OASIS projects...
echo.

REM Build ONODE WebAPI
echo Building ONODE WebAPI...
cd "..\ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI"
dotnet build --configuration Release --verbosity quiet
if %errorlevel% neq 0 (
    echo ❌ Failed to build ONODE WebAPI
    pause
    exit /b 1
)
cd ..\..\oasisweb4.com

REM Build STAR Web UI
echo Building STAR Web UI...
cd "..\STAR ODK\NextGenSoftware.OASIS.STAR.WebUI\ClientApp"
call npm run build
if %errorlevel% neq 0 (
    echo ❌ Failed to build STAR Web UI
    pause
    exit /b 1
)
cd ..\..\..\oasisweb4.com

REM Build OASIS Web4 Marketing Site
echo Building OASIS Web4 Marketing Site...
call npm run build
if %errorlevel% neq 0 (
    echo ❌ Failed to build OASIS Web4 Marketing Site
    pause
    exit /b 1
)

echo.
echo 🚀 Starting OASIS services...
echo.

REM Start ONODE WebAPI (Web4 OASIS API)
echo Starting ONODE WebAPI (Web4 OASIS API) on port 5000...
start "ONODE WebAPI" /min dotnet run --project "..\ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI\NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" --urls "http://localhost:5000"

REM Wait a moment for the API to start
timeout /t 5 /nobreak >nul

REM Start STAR Web UI
echo Starting STAR Web UI on port 3000...
cd "..\STAR ODK\NextGenSoftware.OASIS.STAR.WebUI\ClientApp"
start "STAR Web UI" /min npm start
cd ..\..\..\oasisweb4.com

REM Wait a moment for the Web UI to start
timeout /t 5 /nobreak >nul

REM Start OASIS Web4 Marketing Site
echo Starting OASIS Web4 Marketing Site on port 5173...
start "OASIS Web4 Marketing Site" /min npm run dev

REM Wait a moment for the Marketing Site to start
timeout /t 5 /nobreak >nul

echo.
echo 🎉 OASIS Ecosystem is now running!
echo =================================
echo 📊 ONODE WebAPI (Web4 OASIS API): http://localhost:5000
echo 📊 API Health Check: http://localhost:5000/api/health
echo 📊 Subscription API: http://localhost:5000/api/subscription
echo 🎮 STAR Web UI: http://localhost:3000
echo 🌐 OASIS Web4 Marketing Site: http://localhost:5173
echo.
echo 📋 Available API Endpoints:
echo   • GET  /api/subscription/plans
echo   • POST /api/subscription/checkout/session
echo   • GET  /api/subscription/subscriptions/me
echo   • GET  /api/subscription/orders/me
echo   • POST /api/subscription/webhook
echo.
echo 🛑 To stop all services, close this window or press Ctrl+C
echo.

REM Keep the script running
:loop
timeout /t 10 /nobreak >nul
goto loop
