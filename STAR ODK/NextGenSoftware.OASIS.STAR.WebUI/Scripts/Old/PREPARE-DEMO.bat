@echo off
echo ========================================
echo 🎪 OASIS ECOSYSTEM DEMO PREPARATION
echo ========================================
echo.
echo This script will prepare everything for a comprehensive
echo OASIS ecosystem demonstration including:
echo - Starting all services
echo - Verifying connections
echo - Preparing demo scenarios
echo - Opening demo materials
echo.

REM Set paths
set "WEB5_API_PATH=C:\Source\OASIS\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI"
set "WEB_UI_PATH=C:\Source\OASIS\STAR ODK\NextGenSoftware.OASIS.STAR.WebUI"

echo 📁 Demo Paths:
echo    WEB5 STAR API: %WEB5_API_PATH%
echo    Web UI: %WEB_UI_PATH%
echo.

REM Check if paths exist
if not exist "%WEB5_API_PATH%" (
    echo ❌ WEB5 STAR API path does not exist: %WEB5_API_PATH%
    pause
    exit /b 1
)

if not exist "%WEB_UI_PATH%" (
    echo ❌ Web UI path does not exist: %WEB_UI_PATH%
    pause
    exit /b 1
)

echo ✅ All paths exist. Preparing demo...
echo.

REM Step 1: Build everything
echo ========================================
echo 🔨 Step 1: Building All Components
echo ========================================

echo Building WEB5 STAR API...
cd /d "%WEB5_API_PATH%"
dotnet build --configuration Release --verbosity quiet
if %ERRORLEVEL% EQU 0 (
    echo ✅ WEB5 STAR API built successfully
) else (
    echo ❌ WEB5 STAR API build failed
    pause
    exit /b 1
)

echo Building Web UI...
cd /d "%WEB_UI_PATH%"
dotnet build --configuration Release --verbosity quiet
if %ERRORLEVEL% EQU 0 (
    echo ✅ Web UI built successfully
) else (
    echo ❌ Web UI build failed
    pause
    exit /b 1
)

echo Installing Web UI frontend dependencies...
cd /d "%WEB_UI_PATH%\ClientApp"
npm install --silent
if %ERRORLEVEL% EQU 0 (
    echo ✅ Web UI frontend dependencies installed
) else (
    echo ❌ Web UI frontend dependency installation failed
    pause
    exit /b 1
)

REM Step 2: Start services
echo.
echo ========================================
echo 🚀 Step 2: Starting Services
echo ========================================

echo Starting WEB5 STAR API on port 50564...
cd /d "%WEB5_API_PATH%"
start "WEB5 STAR API" cmd /k "dotnet run --urls http://localhost:50564"

echo Waiting for WEB5 STAR API to start...
timeout /t 5 /nobreak >nul

echo Starting Web UI on port 3000...
cd /d "%WEB_UI_PATH%\ClientApp"
start "Web UI Frontend" cmd /k "npm start"

echo Waiting for Web UI to start...
timeout /t 10 /nobreak >nul

REM Step 3: Verify services
echo.
echo ========================================
echo 🔍 Step 3: Verifying Services
echo ========================================

echo Testing WEB5 STAR API...
curl -s http://localhost:50564/api/star/status >nul
if %ERRORLEVEL% EQU 0 (
    echo ✅ WEB5 STAR API is responding
) else (
    echo ⚠️  WEB5 STAR API may not be ready yet
)

echo Testing Web UI...
curl -s http://localhost:3000 >nul
if %ERRORLEVEL% EQU 0 (
    echo ✅ Web UI is responding
) else (
    echo ⚠️  Web UI may not be ready yet
)

REM Step 4: Open demo materials
echo.
echo ========================================
echo 📖 Step 4: Opening Demo Materials
echo ========================================

echo Opening demo documentation...
cd /d "%WEB_UI_PATH%"

if exist "PROFESSIONAL-ECOSYSTEM-REVIEW.md" (
    start notepad "PROFESSIONAL-ECOSYSTEM-REVIEW.md"
    echo ✅ Opened Professional Ecosystem Review
)

if exist "COMPLETE-ECOSYSTEM-SUMMARY.md" (
    start notepad "COMPLETE-ECOSYSTEM-SUMMARY.md"
    echo ✅ Opened Complete Ecosystem Summary
)

if exist "INVESTMENT-DEMO-GUIDE.md" (
    start notepad "INVESTMENT-DEMO-GUIDE.md"
    echo ✅ Opened Investment Demo Guide
)

REM Step 5: Open browsers
echo.
echo ========================================
echo 🌐 Step 5: Opening Demo Interfaces
echo ========================================

echo Opening Web UI in browser...
start http://localhost:3000
echo ✅ Web UI opened in browser

echo Opening WEB5 STAR API Swagger...
start http://localhost:50564/swagger
echo ✅ WEB5 STAR API Swagger opened

REM Step 6: Demo scenarios
echo.
echo ========================================
echo 🎯 Step 6: Demo Scenarios Ready
echo ========================================

echo.
echo 🎪 DEMO SCENARIOS AVAILABLE:
echo.
echo 1. 🌟 STAR Status Demo (2 minutes)
echo    - Open Web UI: http://localhost:3000
echo    - Click "Ignite STAR" button
echo    - Show karma visualization
echo    - Demonstrate real-time updates
echo.
echo 2. 🔍 Karma System Demo (3 minutes)
echo    - Show karma search functionality
echo    - Filter by karma levels
echo    - Display karma visualizations
echo    - Explain multi-platform sync
echo.
echo 3. 🌍➡️🌌 Unity Integration Demo (5 minutes)
echo    - Show Unity integration scripts
echo    - Explain Earth-to-Space experience
echo    - Demonstrate seamless transitions
echo    - Show unified data management
echo.
echo 4. 🚀 API Demo (3 minutes)
echo    - Open Swagger: http://localhost:50564/swagger
echo    - Test STAR endpoints
echo    - Show OASISResult format
echo    - Demonstrate error handling
echo.
echo 5. 📊 Investment Demo (10 minutes)
echo    - Review Professional Ecosystem Review
echo    - Show Complete Ecosystem Summary
echo    - Present market opportunities
echo    - Highlight competitive advantages
echo.

REM Step 7: Final status
echo ========================================
echo ✅ DEMO PREPARATION COMPLETE
echo ========================================
echo.
echo 🌟 Services Running:
echo    - WEB5 STAR API: http://localhost:50564
echo    - Web UI: http://localhost:3000
echo    - Swagger API Docs: http://localhost:50564/swagger
echo.
echo 📖 Documentation Open:
echo    - Professional Ecosystem Review
echo    - Complete Ecosystem Summary
echo    - Investment Demo Guide
echo.
echo 🎯 Ready for Demo!
echo.
echo 💡 Demo Tips:
echo    - Start with STAR Status demo
echo    - Show karma visualization
echo    - Explain Unity integration
echo    - Highlight investment potential
echo    - Emphasize technical excellence
echo.
echo 🚀 Good luck with your demo!
echo.
pause
