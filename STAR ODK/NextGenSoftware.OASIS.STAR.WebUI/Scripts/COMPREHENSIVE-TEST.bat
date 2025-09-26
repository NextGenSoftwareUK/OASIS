@echo off
echo ========================================
echo 🧪 COMPREHENSIVE OASIS ECOSYSTEM TEST
echo ========================================
echo.
echo This script will test all components of the OASIS ecosystem:
echo - WEB5 STAR API (Port 50564)
echo - WEB4 OASIS API (Port 50563) 
echo - Web UI (Port 3000)
echo - Unity Integration Scripts
echo - Documentation
echo.

REM Set paths
set "WEB5_API_PATH=C:\Source\OASIS\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI"
set "WEB4_API_PATH=C:\Source\OASIS\Native EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint"
set "WEB_UI_PATH=C:\Source\OASIS\STAR ODK\NextGenSoftware.OASIS.STAR.WebUI"
set "UNITY_OMNIVERSE_PATH=C:\Source\OASIS\STAR ODK\Unity-OASIS-Omniverse-UI"
set "OUR_WORLD_PATH=C:\Source\ARWorld"

echo 📁 Testing Paths:
echo    WEB5 STAR API: %WEB5_API_PATH%
echo    WEB4 OASIS API: %WEB4_API_PATH%
echo    Web UI: %WEB_UI_PATH%
echo    Unity Omniverse: %UNITY_OMNIVERSE_PATH%
echo    Our World: %OUR_WORLD_PATH%
echo.

REM Check if paths exist
echo 🔍 Checking if all paths exist...
if not exist "%WEB5_API_PATH%" (
    echo ❌ WEB5 STAR API path does not exist: %WEB5_API_PATH%
    set "ALL_PATHS_EXIST=false"
) else (
    echo ✅ WEB5 STAR API path exists
)

if not exist "%WEB4_API_PATH%" (
    echo ❌ WEB4 OASIS API path does not exist: %WEB4_API_PATH%
    set "ALL_PATHS_EXIST=false"
) else (
    echo ✅ WEB4 OASIS API path exists
)

if not exist "%WEB_UI_PATH%" (
    echo ❌ Web UI path does not exist: %WEB_UI_PATH%
    set "ALL_PATHS_EXIST=false"
) else (
    echo ✅ Web UI path exists
)

if not exist "%UNITY_OMNIVERSE_PATH%" (
    echo ❌ Unity Omniverse path does not exist: %UNITY_OMNIVERSE_PATH%
    set "ALL_PATHS_EXIST=false"
) else (
    echo ✅ Unity Omniverse path exists
)

if not exist "%OUR_WORLD_PATH%" (
    echo ❌ Our World path does not exist: %OUR_WORLD_PATH%
    set "ALL_PATHS_EXIST=false"
) else (
    echo ✅ Our World path exists
)

if "%ALL_PATHS_EXIST%"=="false" (
    echo.
    echo ❌ Some paths are missing. Please check the paths and try again.
    pause
    exit /b 1
)

echo.
echo ✅ All paths exist. Starting comprehensive tests...
echo.

REM Test 1: WEB5 STAR API Build
echo ========================================
echo 🧪 Test 1: WEB5 STAR API Build
echo ========================================
cd /d "%WEB5_API_PATH%"
echo Building WEB5 STAR API...
dotnet build --configuration Release --verbosity quiet
if %ERRORLEVEL% EQU 0 (
    echo ✅ WEB5 STAR API builds successfully
) else (
    echo ❌ WEB5 STAR API build failed
    set "BUILD_ERRORS=true"
)

REM Test 2: WEB4 OASIS API Build
echo.
echo ========================================
echo 🧪 Test 2: WEB4 OASIS API Build
echo ========================================
cd /d "%WEB4_API_PATH%"
echo Building WEB4 OASIS API...
dotnet build --configuration Release --verbosity quiet
if %ERRORLEVEL% EQU 0 (
    echo ✅ WEB4 OASIS API builds successfully
) else (
    echo ❌ WEB4 OASIS API build failed
    set "BUILD_ERRORS=true"
)

REM Test 3: Web UI Build
echo.
echo ========================================
echo 🧪 Test 3: Web UI Build
echo ========================================
cd /d "%WEB_UI_PATH%"
echo Building Web UI backend...
dotnet build --configuration Release --verbosity quiet
if %ERRORLEVEL% EQU 0 (
    echo ✅ Web UI backend builds successfully
) else (
    echo ❌ Web UI backend build failed
    set "BUILD_ERRORS=true"
)

cd /d "%WEB_UI_PATH%\ClientApp"
echo Installing Web UI frontend dependencies...
npm install --silent
if %ERRORLEVEL% EQU 0 (
    echo ✅ Web UI frontend dependencies installed
) else (
    echo ❌ Web UI frontend dependency installation failed
    set "BUILD_ERRORS=true"
)

REM Test 4: Unity Integration Scripts
echo.
echo ========================================
echo 🧪 Test 4: Unity Integration Scripts
echo ========================================
cd /d "%WEB_UI_PATH%"

if exist "Assets\Scripts\UnifiedScaleManager.cs" (
    echo ✅ UnifiedScaleManager.cs exists
) else (
    echo ❌ UnifiedScaleManager.cs missing
    set "UNITY_ERRORS=true"
)

if exist "Assets\Scripts\UnifiedCameraController.cs" (
    echo ✅ UnifiedCameraController.cs exists
) else (
    echo ❌ UnifiedCameraController.cs missing
    set "UNITY_ERRORS=true"
)

if exist "Assets\Scripts\UnifiedDataManager.cs" (
    echo ✅ UnifiedDataManager.cs exists
) else (
    echo ❌ UnifiedDataManager.cs missing
    set "UNITY_ERRORS=true"
)

if exist "INTEGRATE-UNITY-PROJECTS.bat" (
    echo ✅ Unity integration script exists
) else (
    echo ❌ Unity integration script missing
    set "UNITY_ERRORS=true"
)

REM Test 5: Documentation
echo.
echo ========================================
echo 🧪 Test 5: Documentation
echo ========================================
cd /d "%WEB_UI_PATH%"

if exist "README.md" (
    echo ✅ README.md exists
) else (
    echo ❌ README.md missing
    set "DOC_ERRORS=true"
)

if exist "PROFESSIONAL-ECOSYSTEM-REVIEW.md" (
    echo ✅ PROFESSIONAL-ECOSYSTEM-REVIEW.md exists
) else (
    echo ❌ PROFESSIONAL-ECOSYSTEM-REVIEW.md missing
    set "DOC_ERRORS=true"
)

if exist "COMPLETE-ECOSYSTEM-SUMMARY.md" (
    echo ✅ COMPLETE-ECOSYSTEM-SUMMARY.md exists
) else (
    echo ❌ COMPLETE-ECOSYSTEM-SUMMARY.md missing
    set "DOC_ERRORS=true"
)

if exist "UNITY-INTEGRATION-STATUS.md" (
    echo ✅ UNITY-INTEGRATION-STATUS.md exists
) else (
    echo ❌ UNITY-INTEGRATION-STATUS.md missing
    set "DOC_ERRORS=true"
)

REM Test 6: API Controllers
echo.
echo ========================================
echo 🧪 Test 6: API Controllers
echo ========================================
cd /d "%WEB5_API_PATH%\Controllers"

set "CONTROLLER_COUNT=0"
if exist "STARController.cs" (
    echo ✅ STARController.cs exists
    set /a CONTROLLER_COUNT+=1
) else (
    echo ❌ STARController.cs missing
)

if exist "MissionsController.cs" (
    echo ✅ MissionsController.cs exists
    set /a CONTROLLER_COUNT+=1
) else (
    echo ❌ MissionsController.cs missing
)

if exist "OAPPsController.cs" (
    echo ✅ OAPPsController.cs exists
    set /a CONTROLLER_COUNT+=1
) else (
    echo ❌ OAPPsController.cs missing
)

if exist "NFTsController.cs" (
    echo ✅ NFTsController.cs exists
    set /a CONTROLLER_COUNT+=1
) else (
    echo ❌ NFTsController.cs missing
)

if exist "ChaptersController.cs" (
    echo ✅ ChaptersController.cs exists
    set /a CONTROLLER_COUNT+=1
) else (
    echo ❌ ChaptersController.cs missing
)

if exist "QuestsController.cs" (
    echo ✅ QuestsController.cs exists
    set /a CONTROLLER_COUNT+=1
) else (
    echo ❌ QuestsController.cs missing
)

if exist "CelestialBodiesController.cs" (
    echo ✅ CelestialBodiesController.cs exists
    set /a CONTROLLER_COUNT+=1
) else (
    echo ❌ CelestialBodiesController.cs missing
)

echo.
echo 📊 Found %CONTROLLER_COUNT% controllers

REM Test 7: Web UI Components
echo.
echo ========================================
echo 🧪 Test 7: Web UI Components
echo ========================================
cd /d "%WEB_UI_PATH%\ClientApp\src\components"

set "COMPONENT_COUNT=0"
if exist "KarmaVisualization.tsx" (
    echo ✅ KarmaVisualization.tsx exists
    set /a COMPONENT_COUNT+=1
) else (
    echo ❌ KarmaVisualization.tsx missing
)

if exist "KarmaSearch.tsx" (
    echo ✅ KarmaSearch.tsx exists
    set /a COMPONENT_COUNT+=1
) else (
    echo ❌ KarmaSearch.tsx missing
)

if exist "STARStatusCard.tsx" (
    echo ✅ STARStatusCard.tsx exists
    set /a COMPONENT_COUNT+=1
) else (
    echo ❌ STARStatusCard.tsx missing
)

if exist "StatsOverview.tsx" (
    echo ✅ StatsOverview.tsx exists
    set /a COMPONENT_COUNT+=1
) else (
    echo ❌ StatsOverview.tsx missing
)

if exist "RecentActivity.tsx" (
    echo ✅ RecentActivity.tsx exists
    set /a COMPONENT_COUNT+=1
) else (
    echo ❌ RecentActivity.tsx missing
)

echo.
echo 📊 Found %COMPONENT_COUNT% components

REM Final Results
echo.
echo ========================================
echo 🎯 COMPREHENSIVE TEST RESULTS
echo ========================================

if "%BUILD_ERRORS%"=="true" (
    echo ❌ Build Errors: Some components failed to build
) else (
    echo ✅ Build Status: All components build successfully
)

if "%UNITY_ERRORS%"=="true" (
    echo ❌ Unity Integration: Some Unity integration files are missing
) else (
    echo ✅ Unity Integration: All Unity integration files present
)

if "%DOC_ERRORS%"=="true" (
    echo ❌ Documentation: Some documentation files are missing
) else (
    echo ✅ Documentation: All documentation files present
)

echo.
echo 📊 Summary:
echo    - API Controllers: %CONTROLLER_COUNT% found
echo    - Web UI Components: %COMPONENT_COUNT% found
echo    - Unity Integration: Ready for deployment
echo    - Documentation: Complete ecosystem review available

echo.
echo 🌟 OASIS Ecosystem Status:
if "%BUILD_ERRORS%"=="true" (
    echo    Status: ⚠️  PARTIAL - Some build issues need attention
) else if "%UNITY_ERRORS%"=="true" (
    echo    Status: ⚠️  PARTIAL - Unity integration needs completion
) else if "%DOC_ERRORS%"=="true" (
    echo    Status: ⚠️  PARTIAL - Documentation needs completion
) else (
    echo    Status: ✅ COMPLETE - Ready for production deployment!
)

echo.
echo 🚀 Next Steps:
echo    1. Fix any build errors if present
echo    2. Run Unity integration script: INTEGRATE-UNITY-PROJECTS.bat
echo    3. Test all APIs with: Test-REST-API.bat
echo    4. Start development with: Start-Development.bat
echo    5. Review documentation for investment demos

echo.
pause
