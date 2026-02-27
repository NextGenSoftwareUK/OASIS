# Quake - OASIS STAR API Integration Guide

## Overview

This guide explains how to integrate the OASIS STAR API into the Quake source code to enable cross-game item sharing with Doom and other games.

**Note:** Use **STARAPIClient** only for the STAR API client (see `OASIS Omniverse/STARAPIClient/README.md`). Do not use NativeWrapper.

## Prerequisites

1. Quake source code (from [id Software's GitHub](https://github.com/id-Software/Quake))
2. STAR API client library (built from **STARAPIClient**)
3. STAR API credentials (API key and Avatar ID)

## Building the Integration

### Step 1: Build the STAR API client (STARAPIClient)

Build from **STARAPIClient** (see `OASIS Omniverse/STARAPIClient/README.md`):

```bash
cd "OASIS Omniverse/STARAPIClient"
dotnet publish STARAPIClient.csproj -c Release -r win-x64 -p:PublishAot=true -p:SelfContained=true
```

Outputs: `star_api.dll` and `star_api.lib`. Use `star_api.h` from the STARAPIClient folder.

### Step 2: Integrate into Quake Build System

Add to Quake's Makefile:

```makefile
# Add STAR API library (from STARAPIClient publish)
LIBS += -L../OASIS\ Omniverse/STARAPIClient/bin/Release/net8.0/win-x64 -lstar_api

# Add include path
CFLAGS += -I../OASIS\ Omniverse/STARAPIClient
CFLAGS += -I../OASIS\ Omniverse/Quake
```

### Step 3: Link the Integration Code

Add to your Quake source files:
- `quake_star_integration.c` - Implementation
- `quake_star_integration.h` - Header

## Integration Points

### 1. Initialize at Game Start

In `host.c`, add to `Host_Init()`:

```c
#include "quake_star_integration.h"

void Host_Init(void) {
    // ... existing initialization ...
    
    // Initialize STAR API integration
    Quake_STAR_Init();
    
    // ... rest of initialization ...
}
```

### 2. Cleanup at Game Exit

In `host.c`, add to shutdown code:

```c
void Host_Shutdown(void) {
    // ... existing cleanup ...
    
    // Cleanup STAR API
    Quake_STAR_Cleanup();
}
```

### 3. Track Key Pickups

In `items.c`, modify `Touch_Item()`:

```c
#include "quake_star_integration.h"

void Touch_Item(edict_t* ent, edict_t* other, cplane_t* plane, csurface_t* surf) {
    int index;
    gitem_t* it;
    bool taken;
    
    // ... existing pickup logic ...
    
    // STAR API Integration: Track key pickups
    if (ent->item && (ent->item->flags & IT_KEY)) {
        // Determine key type
        if (strstr(ent->item->pickup_name, "silver")) {
            Quake_STAR_OnKeyPickup(QUAKE_ITEM_SILVER_KEY);
        } else if (strstr(ent->item->pickup_name, "gold")) {
            Quake_STAR_OnKeyPickup(QUAKE_ITEM_GOLD_KEY);
        }
    }
    
    // ... rest of existing code ...
}
```

### 4. Check Cross-Game Inventory for Doors

In `doors.c`, modify `door_use()`:

```c
#include "quake_star_integration.h"

void door_use(edict_t* self, edict_t* other, edict_t* activator) {
    // ... existing door logic ...
    
    // Check if this is a key-locked door
    if (self->spawnflags & DOOR_KEY_LOCKED) {
        const char* required_key = GetRequiredKey(self);
        
        // First check local inventory
        if (!HasLocalKey(required_key)) {
            // Check cross-game inventory
            if (Quake_STAR_CheckDoorAccess(self->targetname, required_key)) {
                // Door opened with cross-game key!
                door_go_up(self, activator);
                return;
            }
        }
    }
    
    // ... rest of existing code ...
}
```

### 5. Support Doom Keycards

To allow Doom keycards to work in Quake, map them to Quake keys:

```c
bool Quake_CheckDoorWithDoomKeycard(const char* door_name, int doom_keycard_type) {
    const char* keycard_name = NULL;
    
    switch (doom_keycard_type) {
        case 1: keycard_name = DOOM_ITEM_RED_KEYCARD; break;
        case 2: keycard_name = DOOM_ITEM_BLUE_KEYCARD; break;
        case 3: keycard_name = DOOM_ITEM_YELLOW_KEYCARD; break;
        default: return false;
    }
    
    // Check if player has this keycard from Doom
    if (Quake_STAR_HasCrossGameKeycard(keycard_name)) {
        Con_Printf("Using Doom keycard to open Quake door!\n");
        star_api_use_item(keycard_name, door_name);
        return true;
    }
    
    return false;
}
```

## Configuration

Set environment variables before running Quake:

```bash
export STAR_API_KEY="your_api_key_here"
export STAR_AVATAR_ID="your_avatar_id_here"
```

Or create a config file `quake_star_config.json`:

```json
{
  "starApiBaseUrl": "https://star-api.oasisplatform.world/api",
  "apiKey": "your_api_key_here",
  "avatarId": "your_avatar_id_here"
}
```

## Testing

1. Start Quake with STAR API integration
2. Pick up a silver key in Quake
3. Check your inventory at the STAR API
4. Start Doom and verify the key is available (if mapped)
5. Use a keycard from Doom to open a door in Quake

## Item Mapping

For cross-game compatibility, items need consistent naming:

| Quake Item | Doom Equivalent | STAR API Name |
|------------|------------------|---------------|
| Silver Key | Red Keycard | `red_keycard` or `silver_key` |
| Gold Key | Blue Keycard | `blue_keycard` or `gold_key` |
| Rune 1 | Yellow Keycard | `yellow_keycard` or `rune_1` |

You can create item mapping configuration to handle different naming conventions.

## Troubleshooting

### STAR API Not Initializing

- Check that `STAR_API_KEY` and `STAR_AVATAR_ID` are set
- Verify network connectivity to STAR API
- Check console output for error messages

### Items Not Appearing in Cross-Game Inventory

- Verify the API call succeeded (check console output)
- Check STAR API logs for errors
- Ensure item names match between games

### Doors Not Opening with Cross-Game Keys

- Verify the key exists in inventory: `star_api_has_item()`
- Check that door access logic is properly integrated
- Ensure key names match between games

## Example: Cross-Game Keycard Flow

1. **In Doom:**
   - Player picks up red keycard
   - Keycard is added to STAR API inventory as `red_keycard`

2. **In Quake:**
   - Player approaches a door requiring silver key
   - Quake checks local inventory (not found)
   - Quake checks STAR API: `star_api_has_item("red_keycard")` → true
   - Door opens using Doom's red keycard!

## Future Enhancements

- Quest system integration
- NFT-based item trading
- Boss collection and deployment
- Multi-game quest chains
- Weapon and power-up sharing

## License

This integration follows the same license as the Quake source code (GPL v2).



