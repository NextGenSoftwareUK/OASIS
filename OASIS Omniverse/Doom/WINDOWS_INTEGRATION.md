# Windows Integration Guide for DOOM Fork

**Recommended:** For the **working Doom port** with STAR and cross-game keys (OQuake silver/gold at doors), use **UZDoom/ODOOM** and run `OASIS Omniverse\UZDoom\BUILD_ODOOM.bat` (or `BUILD_UZDOOM_STAR.bat`). See `UZDoom\WINDOWS_INTEGRATION.md`.

This guide is for the older Linux Doom port. It is specifically for integrating the OASIS STAR API into your DOOM fork located at `C:\Source\DOOM`.

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
cd "OASIS Omniverse\NativeWrapper"

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
cd C:\Source\OASIS-master\OASIS Omniverse\NativeWrapper
mkdir build
cd build
cmake .. -G "MinGW Makefiles"
cmake --build . --config Release
```

The library will be at: `C:\Source\OASIS-master\OASIS Omniverse\NativeWrapper\build\Release\star_api.lib` (or `.dll`)

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
Copy-Item "OASIS Omniverse\Doom\doom_star_integration.c" "C:\Source\DOOM\linuxdoom-1.10\"
Copy-Item "OASIS Omniverse\Doom\doom_star_integration.h" "C:\Source\DOOM\linuxdoom-1.10\"
Copy-Item "OASIS Omniverse\NativeWrapper\star_api.h" "C:\Source\DOOM\linuxdoom-1.10\"
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

### If using Makefile (MinGW on Windows)

The Makefile is already set up. From `C:\Source\DOOM\linuxdoom-1.10`:

```bash
mkdir -p linux
make WINDOWS=1
```

The executable is `linux/linuxxdoom` (or `linux\linuxxdoom.exe`). Ensure `star_api.dll` is in the same directory as the exe or on PATH (copy from `OASIS Omniverse\NativeWrapper\build\` or `build\Release\`).

### If using Visual Studio Project

1. Open Visual Studio
2. Add `doom_star_integration.c` to the project
3. Add include directories:
   - `C:\Source\OASIS-master\OASIS Omniverse\NativeWrapper`
   - `C:\Source\OASIS-master\OASIS Omniverse\Doom`
4. Add library directory:
   - `C:\Source\OASIS-master\OASIS Omniverse\NativeWrapper\build\Release`
5. Add library: `star_api.lib`
6. Add linker input: `winhttp.lib` (for Windows HTTP support)

## Step 6: Build DOOM on Windows (STAR integrated)

**Recommended: use the Windows SDL2 + STAR build** so you can run Doom natively with the STAR API:

1. Install **Visual Studio** (C++), **CMake**, and **SDL2** (e.g. `vcpkg install sdl2:x64-windows`).

2. **If you still get: visible mouse cursor, no 360° turn, no sound/music, or no console messages** — the DOOM tree may not have the latest fixes. Do this once:
   - Open **`OASIS Omniverse\Doom\windows_sdl2_fixes\`** in this repo.
   - Run **`COPY_TO_DOOM_AND_REBUILD.bat`** (it copies `i_main.c`, `i_video_sdl2.c`, `i_sound_win.c`, and `odoom_version.h` into `C:\Source\DOOM\linuxdoom-1.10`, deletes `build`, and runs a clean CMake build). The game window title will show **ODOOM** and version (e.g. “ODOOM 1.0 (Build 1)”); edit `OASIS Omniverse\Doom\odoom_version.h` to change the version or build number.
   - If your DOOM source is not at `C:\Source\DOOM\linuxdoom-1.10`, edit the batch file and set `DOOM_SRC` to your path.
   - Then run **`build\Release\doom_star.exe`** (with **doom2.wad** in that folder). You should see a console window and the line `I_InitSound: SDL audio opened (...)`.

3. In **DOOM\linuxdoom-1.10** see **BUILD_WINDOWS_STAR.md** for full steps, or run:
   ```powershell
   cd C:\Source\DOOM\linuxdoom-1.10
   .\build_and_run_star.ps1
   ```
3. Put **doom2.wad** (or doom.wad) in the same folder as **doom_star.exe** and run it.

### Using Make (MinGW / WSL)

```powershell
cd C:\Source\DOOM\linuxdoom-1.10
make WINDOWS=1
```

### Using Visual Studio (legacy)

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

4. Verify in OQuake (after integrating):
   - Start OQuake (Quake with STAR API at `C:\Source\quake-rerelease-qc`)
   - Approach a door requiring silver key
   - Door should open if you have a red keycard from ODOOM (cross-game mapping)

## Key bindings (like original Doom)

Keys are **not** rebindable from an in-game menu in this port. They are read from a **default config file** in the same folder as the executable.

- **Config file:** `default.cfg` (next to `doom_star.exe`).
- **First run:** If the file doesn’t exist, the game creates it when you change options and exit (e.g. from the Options menu, then quit). You can also create it by hand.
- **Edit to rebind:** Use a text editor. Each line is: `name` then tab then number (key code) or string in quotes.

Common bindings (names used in `default.cfg`):

| Setting            | Default key   | Meaning        |
|--------------------|---------------|----------------|
| `key_right`        | Right arrow   | Turn right     |
| `key_left`         | Left arrow    | Turn left      |
| `key_up`           | Up arrow      | Forward        |
| `key_down`         | Down arrow    | Back           |
| `key_strafeleft`   | `,`           | Strafe left    |
| `key_straferight`  | `.`           | Strafe right   |
| `key_fire`         | Right Ctrl    | Fire           |
| `key_use`          | Space         | Use / open     |
| `key_strafe`       | Right Alt     | Hold to strafe |
| `key_speed`        | Right Shift   | Run            |

Other options in the same file: `mouse_sensitivity`, `sfx_volume`, `music_volume`, `screenblocks`, `usegamma`, `use_mouse`, `mouseb_fire`, `mouseb_strafe`, `mouseb_forward`, etc. Change a value, save the file, then restart the game for it to take effect.

## Mouse (360° turn, like original Doom)

- **In game:** The mouse is captured: cursor is hidden and movement uses **relative** mode so you can turn 360° without the pointer leaving the screen.
- **In menus:** Press **ESC** to open the menu; the cursor is shown and you can move it normally. Close the menu (e.g. Start Game or Back) to return to captured mouse and 360° turning.

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

### HUD / widget positions wrong (health, ammo, armor, face)
- **Confirm which binary runs:** Build from `C:\Source\DOOM\linuxdoom-1.10` and run that folder’s `.exe`. If you run an exe from another path or an old build, widget positions can look wrong.
- **How it works:** The status bar uses screen 4 (32-row buffer) in `v_video.c`. Bar background is drawn to screen 4, then `V_CopyRect` copies it to screen 0 at `(0, ST_Y)` (row 168). Widgets (numbers, face, keys) are drawn to **screen 0** with `V_DrawPatch(x, y, 0, patch)` where `(x,y)` are the `ST_*` constants in `st_stuff.c` (e.g. `ST_AMMOX` 44, `ST_AMMOY` 171, `ST_FACESX` 143, `ST_FACESY` 168). The same formula as the reference is used: `desttop = screens[0] + y*SCREENWIDTH + x`.
- **If positions are still wrong:** (1) Run a **clean** rebuild (`COPY_TO_DOOM_AND_REBUILD.bat clean`). (2) Compare with the reference fork: run `C:\Source\DOOM-linux\linuxdoom-1.10` side-by-side and note which widget is shifted (e.g. “health 20 px left”). (3) As a test, replace `v_video.c` with the reference’s `v_video.c` and add **only** the screen-4 early-clip block (the `if (scrn == 4) { ... return; }` block); rebuild. If widgets then match the reference, the difference is in our current routing or clip logic.

### No sound or music
- **Sound effects:** Run from a console (or check build output). You should see `I_InitSound: SDL audio opened (11025 Hz, 2 ch)`. If you see `SDL_OpenAudioDevice failed`, try another audio device or update SDL2. Ensure in-game **Options → Sound Volume** is not 0.
- **Music:** Uses Windows MCI MIDI. Ensure you have a default MIDI output (e.g. Windows GS Wavetable Synth or a real device). In-game **Options → Music Volume** must not be 0.
- **Both:** Ensure `default.cfg` has `sfx_volume` and `music_volume` at 15 (or 1–15) if you edited it.

### Build Errors
- Ensure you have the Windows SDK installed
- Verify CMake generated the correct project files
- Check that all dependencies are available

## Testing STAR API Integration

To confirm the STAR API is working:

1. **Run from a console**  
   Start the game from Command Prompt or PowerShell (or use the console that opens with the game). Watch the startup output.

2. **Check init messages**  
   After `STAR API: Initializing OASIS STAR API integration.` you should see one of:
   - **`STAR API: Authenticated via SSO. Cross-game features enabled.`** – SSO (STAR_USERNAME / STAR_PASSWORD) worked.
   - **`STAR API: Initialized with API key. Cross-game features enabled.`** – API key (STAR_API_KEY / STAR_AVATAR_ID) worked.
   - **`STAR API: Warning - No authentication configured.`** – No env vars set; cross-game features are off.
   - **`STAR API: Failed to initialize: ...`** – e.g. missing `star_api.dll` or network issue.

3. **Test keycard pickup**  
   In-game, pick up a keycard (red/blue/yellow). With STAR enabled you should see:
   - **`STAR API: Added doominv_red_keycard to cross-game inventory.`** (or blue/yellow/skull).  
   If you never see this, STAR init failed or env vars are not set.

4. **Test cross-game door**  
   Approach a keyed door **without** the in-game key. If STAR has that key in inventory (e.g. from another game or a previous pickup in this run), the door should still open and you’ll have used the STAR API for access.

5. **Quick “no config” check**  
   Run with no STAR env vars. You should see `STAR API: Warning - No authentication configured.` and normal Doom key doors still work with in-game keys only.

## Next Steps

1. Integrate into OQuake (Quake fork at `C:\Source\quake-rerelease-qc`; see `OQuake/WINDOWS_INTEGRATION.md`)
2. Test cross-game key sharing (ODOOM keys open OQuake doors and vice versa)
3. Create your first cross-game quest
4. Start collecting boss NFTs!

## Support

For more help, see:
- [Main Integration Guide](../INTEGRATION_GUIDE.md)
- [Phase 2 Quest System](../PHASE2_QUEST_SYSTEM.md)



