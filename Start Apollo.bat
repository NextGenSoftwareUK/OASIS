NextGenSoftware.OASIS.API.Core.Apollo.Server.TestHarness\bin\Debug\netcoreapp3.0\NextGenSoftware.OASIS.API.Core.Apollo.Server.TestHarness.exe

REM Give the server 30 seconds to start before starting the client...
timeout /t 30 /nobreak

NextGenSoftware.OASIS.API.Core.Apollo.Client.TestHarness\bin\Debug\netcoreapp3.0\NextGenSoftware.OASIS.API.Core.Apollo.Client.TestHarness.exe

REM OASIS: Explorer pause (OASIS_BAT_NO_PAUSE=1 skips)
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul
