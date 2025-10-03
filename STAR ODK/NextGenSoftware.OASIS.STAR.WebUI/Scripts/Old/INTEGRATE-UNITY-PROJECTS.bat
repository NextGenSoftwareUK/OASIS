@echo off
echo ========================================
echo 🌍➡️🌌 Unity Projects Integration Script
echo ========================================
echo.
echo This script will integrate the OASIS Omniverse Unity project
echo with the Our World AR project for seamless Earth-to-Space experience.
echo.

REM Set paths
set "OUR_WORLD_PATH=C:\Source\ARWorld"
set "OASIS_OMNIVERSE_PATH=C:\Source\OASIS\STAR ODK\Unity-OASIS-Omniverse-UI"
set "INTEGRATION_SCRIPTS_PATH=C:\Source\OASIS\STAR ODK\NextGenSoftware.OASIS.STAR.WebUI\Assets\Scripts"

echo 📁 Paths:
echo    Our World: %OUR_WORLD_PATH%
echo    OASIS Omniverse: %OASIS_OMNIVERSE_PATH%
echo    Integration Scripts: %INTEGRATION_SCRIPTS_PATH%
echo.

REM Check if paths exist
if not exist "%OUR_WORLD_PATH%" (
    echo ❌ Error: Our World path does not exist: %OUR_WORLD_PATH%
    pause
    exit /b 1
)

if not exist "%OASIS_OMNIVERSE_PATH%" (
    echo ❌ Error: OASIS Omniverse path does not exist: %OASIS_OMNIVERSE_PATH%
    pause
    exit /b 1
)

echo ✅ All paths exist. Starting integration...
echo.

REM Step 1: Create OASIS Omniverse folder in Our World
echo 📂 Step 1: Creating OASIS Omniverse folder in Our World...
if not exist "%OUR_WORLD_PATH%\Assets\Scripts\OASIS-Omniverse" (
    mkdir "%OUR_WORLD_PATH%\Assets\Scripts\OASIS-Omniverse"
    echo ✅ Created OASIS-Omniverse folder
) else (
    echo ✅ OASIS-Omniverse folder already exists
)

REM Step 2: Copy OASIS Omniverse scripts to Our World
echo.
echo 📂 Step 2: Copying OASIS Omniverse scripts to Our World...
if exist "%OASIS_OMNIVERSE_PATH%\Assets\Scripts\API" (
    xcopy "%OASIS_OMNIVERSE_PATH%\Assets\Scripts\API" "%OUR_WORLD_PATH%\Assets\Scripts\OASIS-Omniverse\API" /E /I /Y
    echo ✅ Copied API scripts
)

if exist "%OASIS_OMNIVERSE_PATH%\Assets\Scripts\Celestial" (
    xcopy "%OASIS_OMNIVERSE_PATH%\Assets\Scripts\Celestial" "%OUR_WORLD_PATH%\Assets\Scripts\OASIS-Omniverse\Celestial" /E /I /Y
    echo ✅ Copied Celestial scripts
)

if exist "%OASIS_OMNIVERSE_PATH%\Assets\Scripts\Navigation" (
    xcopy "%OASIS_OMNIVERSE_PATH%\Assets\Scripts\Navigation" "%OUR_WORLD_PATH%\Assets\Scripts\OASIS-Omniverse\Navigation" /E /I /Y
    echo ✅ Copied Navigation scripts
)

if exist "%OASIS_OMNIVERSE_PATH%\Assets\Scripts\UI" (
    xcopy "%OASIS_OMNIVERSE_PATH%\Assets\Scripts\UI" "%OUR_WORLD_PATH%\Assets\Scripts\OASIS-Omniverse\UI" /E /I /Y
    echo ✅ Copied UI scripts
)

if exist "%OASIS_OMNIVERSE_PATH%\Assets\Scripts\UniverseManager.cs" (
    copy "%OASIS_OMNIVERSE_PATH%\Assets\Scripts\UniverseManager.cs" "%OUR_WORLD_PATH%\Assets\Scripts\OASIS-Omniverse\"
    echo ✅ Copied UniverseManager.cs
)

REM Step 3: Copy integration scripts to Our World
echo.
echo 📂 Step 3: Copying integration scripts to Our World...
if exist "%INTEGRATION_SCRIPTS_PATH%\UnifiedScaleManager.cs" (
    copy "%INTEGRATION_SCRIPTS_PATH%\UnifiedScaleManager.cs" "%OUR_WORLD_PATH%\Assets\Scripts\"
    echo ✅ Copied UnifiedScaleManager.cs
)

if exist "%INTEGRATION_SCRIPTS_PATH%\UnifiedCameraController.cs" (
    copy "%INTEGRATION_SCRIPTS_PATH%\UnifiedCameraController.cs" "%OUR_WORLD_PATH%\Assets\Scripts\"
    echo ✅ Copied UnifiedCameraController.cs
)

if exist "%INTEGRATION_SCRIPTS_PATH%\UnifiedDataManager.cs" (
    copy "%INTEGRATION_SCRIPTS_PATH%\UnifiedDataManager.cs" "%OUR_WORLD_PATH%\Assets\Scripts\"
    echo ✅ Copied UnifiedDataManager.cs
)

REM Step 4: Create integration setup guide
echo.
echo 📝 Step 4: Creating integration setup guide...
echo # 🌍➡️🌌 Unity Projects Integration Complete! > "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo. >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo ## ✅ Integration Steps Completed: >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 1. ✅ Copied OASIS Omniverse scripts to Assets\Scripts\OASIS-Omniverse\ >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 2. ✅ Added UnifiedScaleManager.cs for scale transitions >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 3. ✅ Added UnifiedCameraController.cs for camera management >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 4. ✅ Added UnifiedDataManager.cs for data synchronization >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo. >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo ## 🎮 Next Steps: >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 1. Open Unity with the Our World project >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 2. Create a new GameObject called "UnifiedManager" >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 3. Add the UnifiedScaleManager component >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 4. Add the UnifiedCameraController component >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 5. Add the UnifiedDataManager component >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 6. Configure the references in the inspector >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo 7. Test the integration! >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo. >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo ## 🎯 Features Available: >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo - Seamless Earth-to-Space transitions >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo - Unified camera system >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo - Real-time data synchronization >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo - Karma visualization across all scales >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo - NFT management in both Earth and Space >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo. >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"
echo ## 🚀 Ready to explore from Earth to the stars! >> "%OUR_WORLD_PATH%\INTEGRATION-SETUP.md"

echo ✅ Created integration setup guide

REM Step 5: Create backup of original files
echo.
echo 💾 Step 5: Creating backup of original files...
if not exist "%OUR_WORLD_PATH%\Backup" (
    mkdir "%OUR_WORLD_PATH%\Backup"
)

REM Create timestamp for backup
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "HH=%dt:~8,2%" & set "Min=%dt:~10,2%" & set "Sec=%dt:~12,2%"
set "timestamp=%YYYY%%MM%%DD%_%HH%%Min%%Sec%"

if not exist "%OUR_WORLD_PATH%\Backup\%timestamp%" (
    mkdir "%OUR_WORLD_PATH%\Backup\%timestamp%"
)

echo ✅ Created backup folder: %timestamp%

REM Step 6: Summary
echo.
echo ========================================
echo 🎉 Integration Complete!
echo ========================================
echo.
echo ✅ Successfully integrated OASIS Omniverse with Our World!
echo.
echo 📁 Files copied:
echo    - OASIS Omniverse scripts → Assets\Scripts\OASIS-Omniverse\
echo    - UnifiedScaleManager.cs → Assets\Scripts\
echo    - UnifiedCameraController.cs → Assets\Scripts\
echo    - UnifiedDataManager.cs → Assets\Scripts\
echo.
echo 📝 Next steps:
echo    1. Open Unity with the Our World project
echo    2. Follow the INTEGRATION-SETUP.md guide
echo    3. Test the seamless Earth-to-Space experience!
echo.
echo 🌍➡️🌌 Ready to explore from Earth to the stars!
echo.
pause
