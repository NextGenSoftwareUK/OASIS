# Doom - OASIS STAR API Integration Guide

## Overview

This guide explains how to integrate the OASIS STAR API into the Doom source code to enable cross-game item sharing with Quake and other games.

## Prerequisites

1. Doom source code (from [id Software's GitHub](https://github.com/id-Software/DOOM))
2. STAR API client library (built from `NativeWrapper/`)
3. STAR API credentials (API key and Avatar ID)

## Building the Integration

### Step 1: Build the Native Wrapper

```bash
cd Game Integration/NativeWrapper
mkdir build && cd build
cmake ..
make
# On Windows: cmake .. && cmake --build . --config Release
```

This will create `libstar_api.a` (or `star_api.lib` on Windows).

### Step 2: Integrate into Doom Build System

Add to Doom's Makefile or CMakeLists.txt:

```makefile
# Add STAR API library
LIBS += -L../Game\ Integration/NativeWrapper/build -lstar_api

# On Windows, add:
# LIBS += ../Game\ Integration/NativeWrapper/build/Release/star_api.lib

# Add include path
CFLAGS += -I../Game\ Integration/NativeWrapper
CFLAGS += -I../Game\ Integration/Doom
```

### Step 3: Link the Integration Code

Add to your Doom source files:
- `doom_star_integration.c` - Implementation
- `doom_star_integration.h` - Header

## Integration Points

### 1. Initialize at Game Start

In `d_main.c`, add to `D_DoomMain()`:

```c
#include "doom_star_integration.h"

void D_DoomMain(void) {
    // ... existing initialization ...
    
    // Initialize STAR API integration
    Doom_STAR_Init();
    
    // ... rest of initialization ...
}
```

### 2. Cleanup at Game Exit

In `d_main.c`, add to shutdown code:

```c
void D_DoomMain(void) {
    // ... game loop ...
    
    // Cleanup STAR API
    Doom_STAR_Cleanup();
}
```

### 3. Track Keycard Pickups

In `p_inter.c`, modify `P_TouchSpecialThing()`:

```c
#include "doom_star_integration.h"

void P_TouchSpecialThing(mobj_t* special, mobj_t* toucher) {
    player_t* player;
    int i;
    fixed_t delta;
    int sound;
    
    // ... existing pickup logic ...
    
    // STAR API Integration: Track keycard pickups
    if (special->sprite == SPR_KEYR) {
        Doom_STAR_OnKeycardPickup(1);  // Red keycard
    } else if (special->sprite == SPR_KEYB) {
        Doom_STAR_OnKeycardPickup(2);  // Blue keycard
    } else if (special->sprite == SPR_KEYY) {
        Doom_STAR_OnKeycardPickup(3);  // Yellow keycard
    } else if (special->sprite == SPR_BSKU) {
        Doom_STAR_OnKeycardPickup(4);  // Skull key
    }
    
    // ... rest of existing code ...
}
```

### 4. Check Cross-Game Inventory for Doors

In `p_doors.c`, modify `P_UseSpecialLine()`:

```c
#include "doom_star_integration.h"

bool P_UseSpecialLine(line_t* line, mobj_t* thing) {
    // ... existing door logic ...
    
    // Check if this is a key-locked door
    if (line->special == Door_Open || line->special == Door_OpenStayOpen) {
        int required_key = GetRequiredKey(line);
        
        // First check local inventory
        if (!HasLocalKeycard(required_key)) {
            // Check cross-game inventory
            if (Doom_STAR_CheckDoorAccess(line - lines, required_key)) {
                // Door opened with cross-game keycard!
                OpenDoor(line);
                return true;
            }
        }
    }
    
    // ... rest of existing code ...
}
```

### 5. Track Other Item Pickups (Optional)

For power-ups, weapons, etc.:

```c
void P_TouchSpecialThing(mobj_t* special, mobj_t* toucher) {
    // ... existing code ...
    
    // Track berserk pack
    if (special->sprite == SPR_BON1) {
        Doom_STAR_OnItemPickup("berserk_pack", "Berserk Pack - Double damage");
    }
    
    // Track invulnerability
    if (special->sprite == SPR_INVU) {
        Doom_STAR_OnItemPickup("invulnerability", "Invulnerability Sphere");
    }
    
    // ... rest of existing code ...
}
```

## Configuration

Set environment variables before running Doom:

```bash
export STAR_API_KEY="your_api_key_here"
export STAR_AVATAR_ID="your_avatar_id_here"
```

Or create a config file `doom_star_config.json`:

```json
{
  "starApiBaseUrl": "https://star-api.oasisweb4.com/api",
  "apiKey": "your_api_key_here",
  "avatarId": "your_avatar_id_here"
}
```

## Testing

1. Start Doom with STAR API integration
2. Pick up a red keycard in Doom
3. Check your inventory at the STAR API
4. Start Quake and verify the keycard is available
5. Use the keycard from Doom to open a door in Quake

## Troubleshooting

### STAR API Not Initializing

- Check that `STAR_API_KEY` and `STAR_AVATAR_ID` are set
- Verify network connectivity to STAR API
- Check console output for error messages

### Items Not Appearing in Cross-Game Inventory

- Verify the API call succeeded (check console output)
- Check STAR API logs for errors
- Ensure item names match between games

### Doors Not Opening with Cross-Game Keycards

- Verify the keycard exists in inventory: `star_api_has_item()`
- Check that door access logic is properly integrated
- Ensure keycard names match between games

## Example: Cross-Game Keycard Flow

1. **In Doom:**
   - Player picks up red keycard
   - `Doom_STAR_OnKeycardPickup(1)` is called
   - Keycard is added to STAR API inventory

2. **In Quake:**
   - Player approaches a door requiring red keycard
   - Quake checks local inventory (not found)
   - Quake checks STAR API: `star_api_has_item("red_keycard")` → true
   - Door opens using cross-game keycard!

## Future Enhancements

- Quest system integration
- NFT-based item trading
- Boss collection and deployment
- Multi-game quest chains

## License

This integration follows the same license as the Doom source code (GPL v2).



