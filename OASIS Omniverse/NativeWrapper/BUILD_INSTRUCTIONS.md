# Building the STAR API Native Wrapper — **DEPRECATED**

**Do not use NativeWrapper.** ODOOM and OQuake use **STARAPIClient** only. Build the STAR API client from `OASIS Omniverse/STARAPIClient` (see `OASIS Omniverse/STARAPIClient/README.md`). This file is kept for reference only.

## Windows Build Options

### Option 1: Visual Studio (Recommended)

1. **Install Visual Studio** (2019 or later) with C++ development tools

2. **Open Developer Command Prompt**:
   - Search for "Developer Command Prompt for VS" in Start Menu
   - Or: Start → Visual Studio → Tools → Command Prompt

3. **Navigate to wrapper directory**:
   ```cmd
   cd C:\Source\OASIS-master\OASIS Omniverse\NativeWrapper
   ```

4. **Build using CMake** (if installed):
   ```cmd
   mkdir build
   cd build
   cmake .. -G "Visual Studio 16 2019" -A x64
   cmake --build . --config Release
   ```

5. **Or build manually**:
   ```cmd
   cl /EHsc /LD /O2 /I. /D_WIN32 /D_WINHTTP /link winhttp.lib /OUT:star_api.dll star_api.cpp
   ```

### Option 2: MinGW

1. **Install MinGW-w64**

2. **Build**:
   ```cmd
   cd C:\Source\OASIS-master\OASIS Omniverse\NativeWrapper
   mkdir build
   cd build
   g++ -shared -fPIC -O2 -I.. -o star_api.dll ..\star_api.cpp -lwinhttp
   ```

### Option 3: Use the Build Script

Run the provided batch file:
```cmd
cd C:\Source\OASIS-master\Game Integration\NativeWrapper
build_windows.bat
```

## Output

The build will create:
- `build/star_api.dll` - Dynamic library
- `build/star_api.lib` - Import library (for static linking)

## Troubleshooting

### "cl is not recognized"
- Open "Developer Command Prompt for VS" instead of regular CMD
- Or run: `"C:\Program Files\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat"`

### "winhttp.lib not found"
- This is part of Windows SDK (should be included with Visual Studio)
- If missing, install Windows SDK

### "cmake not found"
- Install CMake from https://cmake.org/download/
- Or use manual compilation (Option 1, step 5)

## Next Steps

After building:
1. The library will be at: `build/star_api.dll`
2. Copy to DOOM/Quake build directories if needed
3. Or update Makefile/library paths to point to this location



