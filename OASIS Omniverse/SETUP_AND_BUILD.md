# Complete Setup and Build Guide

## Step 1: Build the Native Wrapper

### Quick Method (Visual Studio Project)

1. **Open Visual Studio**
2. **Open Project**: `Game Integration/NativeWrapper/star_api.vcxproj`
3. **Set Configuration**: Release, x64
4. **Build**: Right-click project → Build
5. **Output**: `build/Release/star_api.dll`

### Alternative: Command Line

Open **Developer Command Prompt for VS** and run:

```cmd
cd C:\Source\OASIS-master\Game Integration\NativeWrapper
mkdir build
cd build
cmake .. -G "Visual Studio 16 2019" -A x64
cmake --build . --config Release
```

Or use the batch script:
```cmd
cd C:\Source\OASIS-master\Game Integration\NativeWrapper
build_windows.bat
```

## Step 2: Set Environment Variables

### Option A: SSO Authentication (Recommended)

```powershell
# PowerShell (Current Session)
$env:STAR_USERNAME = "your_username"
$env:STAR_PASSWORD = "your_password"

# PowerShell (Permanent - User)
[System.Environment]::SetEnvironmentVariable("STAR_USERNAME", "your_username", "User")
[System.Environment]::SetEnvironmentVariable("STAR_PASSWORD", "your_password", "User")
```

### Option B: API Key Authentication

```powershell
# PowerShell (Current Session)
$env:STAR_API_KEY = "your_api_key"
$env:STAR_AVATAR_ID = "your_avatar_id"

# PowerShell (Permanent - User)
[System.Environment]::SetEnvironmentVariable("STAR_API_KEY", "your_api_key", "User")
[System.Environment]::SetEnvironmentVariable("STAR_AVATAR_ID", "your_avatar_id", "User")
```

### Option C: Command Prompt (CMD)

```cmd
set STAR_USERNAME=your_username
set STAR_PASSWORD=your_password
```

## Step 3: Update DOOM Makefile Paths

The Makefile has been updated, but you may need to adjust paths. Edit `C:\Source\DOOM\linuxdoom-1.10\Makefile`:

```makefile
# Update these paths to match your setup
STAR_API_DIR=C:/Source/OASIS-master/Game Integration/NativeWrapper
LDFLAGS=-L/usr/X11R6/lib -L$(STAR_API_DIR)/build/Release
LIBS=-lXext -lX11 -lnsl -lm -lstar_api
```

**Note**: On Windows with MinGW/MSYS2, paths might need adjustment.

## Step 4: Build DOOM

### Using Make (if available):

```bash
cd C:\Source\DOOM\linuxdoom-1.10
make
```

### Using Visual Studio:

1. Create a Visual Studio project for DOOM
2. Add all source files
3. Add `doom_star_integration.c` to project
4. Set include directories:
   - `C:\Source\OASIS-master\Game Integration\NativeWrapper`
   - `C:\Source\DOOM\linuxdoom-1.10`
5. Set library directories:
   - `C:\Source\OASIS-master\Game Integration\NativeWrapper\build\Release`
6. Add libraries: `star_api.lib`, `winhttp.lib`
7. Build

## Step 5: Test the Integration

1. **Run DOOM**:
   ```cmd
   cd C:\Source\DOOM\linuxdoom-1.10
   .\linux\linuxxdoom.exe
   ```

2. **Check Console Output**:
   - Should see: "STAR API: Authenticated via SSO. Cross-game features enabled."
   - Or: "STAR API: Initialized with API key. Cross-game features enabled."

3. **Pick up a Keycard**:
   - Collect a red keycard in-game
   - Check console: "STAR API: Added red_keycard to cross-game inventory."

4. **Test Door Access**:
   - Approach a door requiring a keycard
   - If you have the keycard (local or cross-game), door should open

## Troubleshooting

### "star_api.dll not found"
- Copy `star_api.dll` to DOOM executable directory
- Or add to PATH
- Or update library search paths

### "Cannot find star_api.h"
- Verify include paths in Makefile/project settings
- Check that `star_api.h` is in the correct location

### "STAR API: Failed to initialize"
- Check environment variables are set
- Verify network connectivity
- Check API credentials are correct

### Build Errors
- Ensure Visual Studio C++ tools are installed
- Check Windows SDK is installed
- Verify all include paths are correct

## Next Steps

1. ✅ Build native wrapper
2. ✅ Set environment variables
3. ✅ Build DOOM
4. ✅ Test integration
5. ⏳ Integrate Quake (see `quake-rerelease-qc/QUAKE_STAR_INTEGRATION.md`)
6. ⏳ Test cross-game item sharing
7. ⏳ Create cross-game quests!

## Quick Test Script

Create `test_integration.bat`:

```batch
@echo off
echo Testing STAR API Integration...
echo.

REM Set credentials (replace with yours)
set STAR_USERNAME=your_username
set STAR_PASSWORD=your_password

REM Run DOOM
cd C:\Source\DOOM\linuxdoom-1.10
.\linux\linuxxdoom.exe

pause
```



