# Doom Integration Instructions for NextGenSoftwareUK/DOOM Fork

This guide explains how to integrate the OASIS STAR API into the [NextGenSoftwareUK/DOOM](https://github.com/NextGenSoftwareUK/DOOM) fork.

## Prerequisites

1. Clone the Doom fork:
   ```bash
   git clone https://github.com/NextGenSoftwareUK/DOOM.git
   cd DOOM
   ```

2. Build the STAR API native wrapper (see main README)

3. Set environment variables:
   ```bash
   export STAR_API_KEY="your_api_key"  # Optional if using SSO
   export STAR_AVATAR_ID="your_avatar_id"  # Optional if using SSO
   ```

## Integration Steps

### Step 1: Copy Integration Files

Copy the integration files to your Doom source directory:

```bash
# From OASIS-master root
cp "Game Integration/Doom/doom_star_integration.c" DOOM/linuxdoom-1.10/
cp "Game Integration/Doom/doom_star_integration.h" DOOM/linuxdoom-1.10/
cp "Game Integration/NativeWrapper/star_api.h" DOOM/linuxdoom-1.10/
```

### Step 2: Modify d_main.c

Add to `linuxdoom-1.10/d_main.c`:

```c
#include "doom_star_integration.h"

int main(int argc, char **argv)
{
    // ... existing code ...
    
    // Initialize OASIS STAR API integration
    Doom_STAR_Init();
    
    D_DoomMain();
    
    // Cleanup on exit
    Doom_STAR_Cleanup();
    
    return 0;
}
```

### Step 3: Modify p_inter.c

Add to `linuxdoom-1.10/p_inter.c`:

```c
#include "doom_star_integration.h"

void P_TouchSpecialThing(mobj_t* special, mobj_t* toucher)
{
    // ... existing pickup logic ...
    
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
    
    // Track other items for quests
    if (special->sprite == SPR_BON1) {
        Doom_STAR_OnItemPickup("berserk_pack", "Berserk Pack - Double damage");
    } else if (special->sprite == SPR_INVU) {
        Doom_STAR_OnItemPickup("invulnerability", "Invulnerability Sphere");
    }
}
```

### Step 4: Modify p_doors.c

Add to `linuxdoom-1.10/p_doors.c`:

```c
#include "doom_star_integration.h"

bool P_UseSpecialLine(line_t* line, mobj_t* thing)
{
    // ... existing door logic ...
    
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
}
```

### Step 5: Update Makefile

Add to `linuxdoom-1.10/Makefile`:

```makefile
STAR_API_DIR=../../Game\ Integration/NativeWrapper
LIBS=-L$(STAR_API_DIR)/build -lstar_api
CFLAGS+=-I$(STAR_API_DIR) -I../../Game\ Integration/Doom

OBJS= ... doom_star_integration.o
```

### Step 6: Build

```bash
cd linuxdoom-1.10
make
```

## Avatar/SSO Authentication

The integration supports both API key and SSO authentication:

### Option 1: API Key (Simple)

Set environment variables:
```bash
export STAR_API_KEY="your_api_key"
export STAR_AVATAR_ID="your_avatar_id"
```

### Option 2: SSO Authentication (Recommended)

The integration will prompt for login on first run, or you can authenticate programmatically:

```c
// In doom_star_integration.c, modify Doom_STAR_Init():
void Doom_STAR_Init(void) {
    // Try SSO authentication first
    const char* username = getenv("STAR_USERNAME");
    const char* password = getenv("STAR_PASSWORD");
    
    if (username && password) {
        star_api_result_t auth_result = star_api_authenticate(username, password);
        if (auth_result == STAR_API_SUCCESS) {
            // Authenticated via SSO
            return;
        }
    }
    
    // Fall back to API key
    // ... existing API key code ...
}
```

## Quest Integration

When a player collects items, the system automatically checks for active quests:

```c
// In doom_star_integration.c
void Doom_STAR_OnKeycardPickup(int keycard_type) {
    // Add to inventory
    star_api_add_item(...);
    
    // Check if this completes a quest objective
    // (Quest checking happens server-side)
}
```

## NFT Boss Collection (Phase 3)

When a boss is defeated:

```c
// In p_enemy.c or wherever boss death is handled
void P_KillMobj(mobj_t* source, mobj_t* target) {
    // ... existing death logic ...
    
    // Check if this is a boss
    if (target->flags & MF_BOSS) {
        // Create NFT for defeated boss
        char boss_stats[512];
        snprintf(boss_stats, sizeof(boss_stats), 
            "{\"health\":%d,\"damage\":%d,\"speed\":%d}",
            target->health, target->info->damage, target->info->speed);
        
        char nft_id[64];
        star_api_result_t result = star_api_create_boss_nft(
            target->info->name,
            "Defeated boss from Doom",
            "Doom",
            boss_stats,
            nft_id
        );
        
        if (result == STAR_API_SUCCESS) {
            printf("Boss NFT created: %s\n", nft_id);
        }
    }
}
```

## Testing

1. Start Doom with STAR API integration
2. Pick up a red keycard
3. Check console: Should see "STAR API: Added red_keycard to cross-game inventory"
4. Start Quake and verify the keycard is available
5. Use the keycard from Doom to open a door in Quake

## Troubleshooting

See main [INTEGRATION_GUIDE.md](../INTEGRATION_GUIDE.md) for troubleshooting tips.



