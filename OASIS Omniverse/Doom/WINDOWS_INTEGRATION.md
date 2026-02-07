# Windows Integration Guide for DOOM Fork

This guide is specifically for integrating the OASIS STAR API into your DOOM fork located at `C:\Source\DOOM`.

## Prerequisites

1. **Visual Studio** (2019 or later) with C++ development tools
2. **CMake** (3.10 or later) - [Download](https://cmake.org/download/)
3. **Git** - Already installed (you have the fork)
4. **STAR API Credentials** - Get from OASIS platform

## Step 1: Build the Native Wrapper

### Option A: Using Visual Studio

```powershell
# Navigate to OASIS project
cd C:\Source\OASIS-master

# Navigate to native wrapper
cd "Game Integration\NativeWrapper"

# Create build directory
mkdir build
cd build

# Configure with CMake (Visual Studio generator)
cmake .. -G "Visual Studio 16 2019" -A x64

# Build
cmake --build . --config Release
```

### Option B: Using Command Line (MinGW)

If you have MinGW installed:

```powershell
cd C:\Source\OASIS-master\Game Integration\NativeWrapper
mkdir build
cd build
cmake .. -G "MinGW Makefiles"
cmake --build . --config Release
```

The library will be at: `C:\Source\OASIS-master\Game Integration\NativeWrapper\build\Release\star_api.lib` (or `.dll`)

## Step 2: Set Environment Variables

Open PowerShell as Administrator:

```powershell
# SSO Authentication (Recommended)
[System.Environment]::SetEnvironmentVariable("STAR_USERNAME", "your_username", "User")
[System.Environment]::SetEnvironmentVariable("STAR_PASSWORD", "your_password", "User")

# Or API Key Authentication
[System.Environment]::SetEnvironmentVariable("STAR_API_KEY", "your_api_key", "User")
[System.Environment]::SetEnvironmentVariable("STAR_AVATAR_ID", "your_avatar_id", "User")
```

Or set them for current session:

```powershell
$env:STAR_USERNAME = "your_username"
$env:STAR_PASSWORD = "your_password"
```

## Step 3: Copy Integration Files to DOOM

```powershell
# From OASIS project root
cd C:\Source\OASIS-master

# Copy integration files to DOOM
Copy-Item "Game Integration\Doom\doom_star_integration.c" "C:\Source\DOOM\linuxdoom-1.10\"
Copy-Item "Game Integration\Doom\doom_star_integration.h" "C:\Source\DOOM\linuxdoom-1.10\"
Copy-Item "Game Integration\NativeWrapper\star_api.h" "C:\Source\DOOM\linuxdoom-1.10\"
```

## Step 4: Modify DOOM Source Files

### Modify `C:\Source\DOOM\linuxdoom-1.10\d_main.c`

Add at the top (after includes):
```c
#include "doom_star_integration.h"
```

Add in `main()` function (before `D_DoomMain()`):
```c
// Initialize OASIS STAR API integration
Doom_STAR_Init();
```

Add before return in `main()`:
```c
// Cleanup STAR API
Doom_STAR_Cleanup();
```

### Modify `C:\Source\DOOM\linuxdoom-1.10\p_inter.c`

Add at the top:
```c
#include "doom_star_integration.h"
```

In `P_TouchSpecialThing()` function, add after existing pickup logic:
```c
// OASIS STAR API Integration: Track keycard pickups
if (special->sprite == SPR_KEYR) {
    Doom_STAR_OnKeycardPickup(1);  // Red keycard
} else if (special->sprite == SPR_KEYB) {
    Doom_STAR_OnKeycardPickup(2);  // Blue keycard
} else if (special->sprite == SPR_KEYY) {
    Doom_STAR_OnKeycardPickup(3);  // Yellow keycard
} else if (special->sprite == SPR_BSKU) {
    Doom_STAR_OnKeycardPickup(4);  // Skull key
}
```

### Modify `C:\Source\DOOM\linuxdoom-1.10\p_doors.c`

Add at the top:
```c
#include "doom_star_integration.h"
```

In `P_UseSpecialLine()` function, add door access check:
```c
// Check if door requires a keycard
if (line->special == Door_Open || line->special == Door_OpenStayOpen) {
    int required_key = GetRequiredKey(line);
    
    // First check local Doom inventory
    if (!HasLocalKeycard(required_key)) {
        // Check cross-game inventory via STAR API
        if (Doom_STAR_CheckDoorAccess(line - lines, required_key)) {
            // Door opened with cross-game keycard!
            OpenDoor(line);
            return true;
        }
    }
}
```

## Step 5: Update Makefile or Build System

### If using Makefile

Edit `C:\Source\DOOM\linuxdoom-1.10\Makefile`:

```makefile
# Add these lines near the top
STAR_API_DIR=C:/Source/OASIS-master/Game Integration/NativeWrapper
LIBS=-L$(STAR_API_DIR)/build/Release -lstar_api
CFLAGS+=-I$(STAR_API_DIR) -IC:/Source/OASIS-master/Game Integration/Doom

# Add to OBJS line
OBJS= ... doom_star_integration.o
```

### If using Visual Studio Project

1. Open Visual Studio
2. Add `doom_star_integration.c` to the project
3. Add include directories:
   - `C:\Source\OASIS-master\Game Integration\NativeWrapper`
   - `C:\Source\OASIS-master\Game Integration\Doom`
4. Add library directory:
   - `C:\Source\OASIS-master\Game Integration\NativeWrapper\build\Release`
5. Add library: `star_api.lib`
6. Add linker input: `winhttp.lib` (for Windows HTTP support)

## Step 6: Build DOOM

### Using Make (if available)

```powershell
cd C:\Source\DOOM\linuxdoom-1.10
make
```

### Using Visual Studio

1. Open the project in Visual Studio
2. Set configuration to Release
3. Build Solution (Ctrl+Shift+B)

## Step 7: Test

1. Run DOOM:
   ```powershell
   cd C:\Source\DOOM\linuxdoom-1.10
   .\doom.exe
   ```

2. Pick up a red keycard
3. Check console output - should see:
   ```
   STAR API: Authenticated via SSO. Cross-game features enabled.
   STAR API: Added red_keycard to cross-game inventory.
   ```

4. Verify in Quake (after integrating):
   - Start Quake
   - Approach a door requiring silver key
   - Door should open if keycard mapping is configured

## Troubleshooting

### "star_api.lib not found"
- Verify the library was built successfully
- Check the path in Makefile/project settings
- Ensure you're using the correct architecture (x64 vs x86)

### "Cannot open include file 'star_api.h'"
- Verify include paths are set correctly
- Check that `star_api.h` was copied to DOOM directory

### "STAR API: Failed to initialize"
- Check environment variables are set
- Verify network connectivity to STAR API
- Check firewall settings

### Build Errors
- Ensure you have the Windows SDK installed
- Verify CMake generated the correct project files
- Check that all dependencies are available

## Next Steps

1. Integrate into Quake fork (see `Quake/WINDOWS_INTEGRATION.md`)
2. Test cross-game item sharing
3. Create your first cross-game quest
4. Start collecting boss NFTs!

## Support

For more help, see:
- [Main Integration Guide](../INTEGRATION_GUIDE.md)
- [Phase 2 Quest System](../PHASE2_QUEST_SYSTEM.md)



