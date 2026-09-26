@echo off
REM Build OGEditorSDK NativeAOT DLL for Windows x64.
REM Output: bin\Release\net8.0\win-x64\publish\OGEditorClient.dll
REM
REM Requirements:
REM   - .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)
REM   - Visual C++ Build Tools (for MSVC linker, used by NativeAOT on Windows)
REM
REM Usage: double-click, or run from Developer Command Prompt
REM Copy OGEditorClient.dll + OGEditorClient.h to your editor's plugin directory.

echo [OGEditorSDK] Building NativeAOT shared library (win-x64)...
dotnet publish OGEditorSDK.Native.csproj -r win-x64 -c Release

if %ERRORLEVEL% neq 0 (
    echo [OGEditorSDK] BUILD FAILED.
    pause
    exit /b 1
)

echo.
echo [OGEditorSDK] Build succeeded.
echo Output: bin\Release\net8.0\win-x64\publish\OGEditorClient.dll
echo Header:  OGEditorClient.h  (copy alongside the .dll)
echo.
pause
