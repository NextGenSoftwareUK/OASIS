# Complete Setup Guide - Step by Step

## Overview

This guide walks you through the complete setup process for integrating OASIS STAR API into DOOM and Quake.

## Prerequisites Checklist

- [ ] Visual Studio 2019+ with C++ tools OR MinGW
- [ ] CMake (optional, but recommended)
- [ ] DOOM fork at `C:\Source\DOOM`
- [ ] Quake fork at `C:\Source\quake-rerelease-qc`
- [ ] OASIS project at `C:\Source\OASIS-master`
- [ ] STAR API credentials (username/password or API key)

## Step 1: Build Native Wrapper Library

### Method A: Using Visual Studio Project (Easiest)

1. Open Visual Studio
2. File → Open → Project/Solution
3. Navigate to: `C:\Source\OASIS-master\Game Integration\NativeWrapper\star_api.vcxproj`
4. Set Configuration to **Release**, Platform to **x64**
5. Right-click project → **Build**
6. Output: `build/Release/star_api.dll` and `star_api.lib`

### Method B: Using Quick Build Script

1. Open Command Prompt or PowerShell
2. Run:
   ```cmd
   cd C:\Source\OASIS-master\Game Integration
   QUICK_BUILD.bat
   ```

### Method C: Manual CMake Build

1. Open **Developer Command Prompt for VS**
2. Run:
   ```cmd
   cd C:\Source\OASIS-master\Game Integration\NativeWrapper
   mkdir build
   cd build
   cmake .. -G "Visual Studio 16 2019" -A x64
   cmake --build . --config Release
   ```

### Verify Build

Check that these files exist:
- `Game Integration/NativeWrapper/build/Release/star_api.dll`
- `Game Integration/NativeWrapper/build/Release/star_api.lib`

## Step 2: Configure Authentication

### Get Your Credentials

1. Sign up at [OASIS Platform](https://oasisplatform.world)
2. Get your username/password OR API key + Avatar ID

### Set Environment Variables

**PowerShell (Recommended)**:
```powershell
# SSO Authentication
[System.Environment]::SetEnvironmentVariable("STAR_USERNAME", "your_username", "User")
[System.Environment]::SetEnvironmentVariable("STAR_PASSWORD", "your_password", "User")

# Or API Key
[System.Environment]::SetEnvironmentVariable("STAR_API_KEY", "your_api_key", "User")
[System.Environment]::SetEnvironmentVariable("STAR_AVATAR_ID", "your_avatar_id", "User")
```

**Command Prompt**:
```cmd
setx STAR_USERNAME "your_username"
setx STAR_PASSWORD "your_password"
```

**Note**: You may need to restart your terminal/IDE for environment variables to take effect.

## Step 3: Build DOOM with Integration

### Option A: Using Make (Linux-style, if available)

```bash
cd C:\Source\DOOM\linuxdoom-1.10
make
```

### Option B: Using Visual Studio

1. Create new Visual Studio project
2. Add all DOOM source files from `linuxdoom-1.10/`
3. Add `doom_star_integration.c`
4. Configure project:
   - **Include Directories**:
     - `C:\Source\OASIS-master\Game Integration\NativeWrapper`
     - `C:\Source\DOOM\linuxdoom-1.10`
   - **Library Directories**:
     - `C:\Source\OASIS-master\Game Integration\NativeWrapper\build\Release`
   - **Additional Dependencies**:
     - `star_api.lib`
     - `winhttp.lib`
5. Build project

### Option C: Manual Compilation

If you have a C compiler available, compile manually:
```cmd
cd C:\Source\DOOM\linuxdoom-1.10
cl /I"C:\Source\OASIS-master\Game Integration\NativeWrapper" /LD /link /LIBPATH:"C:\Source\OASIS-master\Game Integration\NativeWrapper\build\Release" star_api.lib winhttp.lib *.c
```

## Step 4: Test DOOM Integration

1. **Run DOOM**:
   ```cmd
   cd C:\Source\DOOM\linuxdoom-1.10
   .\linux\linuxxdoom.exe
   ```

2. **Check Console Output**:
   - Look for: "STAR API: Authenticated via SSO. Cross-game features enabled."
   - If you see an error, check environment variables

3. **Test Keycard Pickup**:
   - Start a game
   - Pick up a red keycard
   - Console should show: "STAR API: Added red_keycard to cross-game inventory."

4. **Test Cross-Game Door**:
   - Approach a door requiring a keycard
   - If you have the keycard (from DOOM or another game), door opens
   - Console shows: "STAR API: Door opened using cross-game keycard: red_keycard"

## Step 5: Integrate Quake (Next)

See `C:\Source\quake-rerelease-qc\QUAKE_STAR_INTEGRATION.md` for Quake-specific instructions.

## Verification Checklist

- [ ] Native wrapper built successfully
- [ ] Environment variables set
- [ ] DOOM builds without errors
- [ ] DOOM runs and shows STAR API initialization message
- [ ] Keycard pickup is tracked (check console)
- [ ] Doors check cross-game inventory

## Troubleshooting

### Build Issues

**"star_api.lib not found"**
- Verify the library was built: Check `build/Release/star_api.lib` exists
- Update library path in Makefile/project settings

**"Cannot open include file 'star_api.h'"**
- Check include paths are set correctly
- Verify `star_api.h` exists in NativeWrapper directory

**"Unresolved external symbol"**
- Ensure `star_api.lib` is linked
- Check that `winhttp.lib` is also linked (Windows)

### Runtime Issues

**"STAR API: Failed to initialize"**
- Check environment variables: `echo %STAR_USERNAME%`
- Verify network connectivity
- Check API credentials are correct

**"STAR API: Warning - No authentication configured"**
- Set environment variables (see Step 2)
- Restart terminal/application after setting variables

**No console output**
- Check that DOOM is outputting to console
- Verify integration code is being called
- Check for compilation errors

## Next Steps

1. ✅ Build native wrapper
2. ✅ Set credentials
3. ✅ Build DOOM
4. ✅ Test DOOM integration
5. ⏳ Integrate Quake
6. ⏳ Test cross-game item sharing
7. ⏳ Create cross-game quests
8. ⏳ Collect boss NFTs!

## Support

For detailed documentation:
- `INTEGRATION_GUIDE.md` - Complete integration guide
- `PHASE2_QUEST_SYSTEM.md` - Quest system documentation
- `Doom/WINDOWS_INTEGRATION.md` - DOOM-specific guide
- `Quake/WINDOWS_INTEGRATION.md` - Quake-specific guide



