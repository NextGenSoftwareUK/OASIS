# Quake Integration Instructions for NextGenSoftwareUK/quake-rerelease-qc Fork

This guide explains how to integrate the OASIS STAR API into the [NextGenSoftwareUK/quake-rerelease-qc](https://github.com/NextGenSoftwareUK/quake-rerelease-qc) fork.

## Prerequisites

1. Clone the Quake fork:
   ```bash
   git clone https://github.com/NextGenSoftwareUK/quake-rerelease-qc.git
   cd quake-rerelease-qc
   ```

2. Build the STAR API native wrapper (see main README)

3. Set environment variables:
   ```bash
   export STAR_API_KEY="your_api_key"  # Optional if using SSO
   export STAR_AVATAR_ID="your_avatar_id"  # Optional if using SSO
   ```

## Integration Steps

### Step 1: Copy Integration Files

Copy the integration files to your Quake source directory:

```bash
# From OASIS-master root
cp "Game Integration/Quake/quake_star_integration.c" quake-rerelease-qc/
cp "Game Integration/Quake/quake_star_integration.h" quake-rerelease-qc/
cp "Game Integration/NativeWrapper/star_api.h" quake-rerelease-qc/
```

### Step 2: Modify host.c

Add to `host.c` (or equivalent initialization file):

```c
#include "quake_star_integration.h"

void Host_Init(void)
{
    // ... existing initialization ...
    
    // Initialize OASIS STAR API integration
    Quake_STAR_Init();
    
    // ... rest of initialization ...
}

void Host_Shutdown(void)
{
    // ... existing cleanup ...
    
    // Cleanup STAR API
    Quake_STAR_Cleanup();
}
```

### Step 3: Modify items.c (QuakeC)

For QuakeC source, modify `items.qc`:

```c
// In Touch_Item function
void() Touch_Item =
{
    // ... existing pickup logic ...
    
    // OASIS STAR API Integration: Track key pickups
    if (self.classname == "key_silver") {
        Quake_STAR_OnKeyPickup("silver_key");
    } else if (self.classname == "key_gold") {
        Quake_STAR_OnKeyPickup("gold_key");
    }
    
    // Track other items for quests
    if (self.classname == "item_quad") {
        Quake_STAR_OnItemPickup("quad_damage", "Quad Damage Power-up");
    } else if (self.classname == "item_pent") {
        Quake_STAR_OnItemPickup("pentagram", "Pentagram of Protection");
    }
    
    // ... rest of existing code ...
};
```

### Step 4: Modify doors.c (QuakeC)

For QuakeC source, modify `doors.qc`:

```c
// In door_use function
void() door_use =
{
    // ... existing door logic ...
    
    // Check if door requires a key
    if (self.spawnflags & DOOR_KEY_LOCKED) {
        string required_key = GetRequiredKey(self);
        
        // First check local Quake inventory
        if (!HasLocalKey(required_key)) {
            // Check cross-game inventory via STAR API
            if (Quake_STAR_CheckDoorAccess(self.targetname, required_key)) {
                // Door opened with cross-game key!
                door_go_up();
                return;
            }
        }
    }
    
    // ... rest of existing code ...
};
```

### Step 5: Create Native C Bridge

Since QuakeC is interpreted, you'll need a native C bridge. Create `quake_star_bridge.c`:

```c
#include "quake_star_integration.h"
#include "progs.h"  // QuakeC header

// Bridge function called from QuakeC
void QuakeC_OnKeyPickup(const char* key_name) {
    Quake_STAR_OnKeyPickup(key_name);
}

bool QuakeC_CheckDoorAccess(const char* door_name, const char* required_key) {
    return Quake_STAR_CheckDoorAccess(door_name, required_key);
}

void QuakeC_OnItemPickup(const char* item_name, const char* item_desc) {
    Quake_STAR_OnItemPickup(item_name, item_desc);
}
```

### Step 6: Update Build System

Add to your Makefile or build script:

```makefile
STAR_API_DIR=../Game\ Integration/NativeWrapper
LIBS=-L$(STAR_API_DIR)/build -lstar_api
CFLAGS+=-I$(STAR_API_DIR) -I../Game\ Integration/Quake

OBJS= ... quake_star_integration.o quake_star_bridge.o
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

```c
// In quake_star_integration.c, modify Quake_STAR_Init():
void Quake_STAR_Init(void) {
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
// In quake_star_integration.c
void Quake_STAR_OnKeyPickup(const char* key_name) {
    // Add to inventory
    star_api_add_item(...);
    
    // Check if this completes a quest objective
    // (Quest checking happens server-side)
}
```

## NFT Boss Collection (Phase 3)

When a boss is defeated in QuakeC:

```c
// In monsters.qc or wherever boss death is handled
void() monster_death =
{
    // ... existing death logic ...
    
    // Check if this is a boss
    if (self.classname == "monster_boss" || self.flags & FL_BOSS) {
        // Create NFT for defeated boss
        string boss_stats = sprintf("{\"health\":%d,\"damage\":%d}",
            self.health, self.dmg);
        
        char nft_id[64];
        star_api_result_t result = star_api_create_boss_nft(
            self.netname,
            "Defeated boss from Quake",
            "Quake",
            boss_stats,
            nft_id
        );
        
        if (result == STAR_API_SUCCESS) {
            bprint(sprintf("Boss NFT created: %s\n", nft_id));
        }
    }
};
```

## Testing

1. Start Quake with STAR API integration
2. Pick up a silver key
3. Check console: Should see "STAR API: Added silver_key to cross-game inventory"
4. Start Doom and verify the key is available (if mapped)
5. Use a keycard from Doom to open a door in Quake

## Troubleshooting

See main [INTEGRATION_GUIDE.md](../INTEGRATION_GUIDE.md) for troubleshooting tips.

## QuakeC-Specific Notes

Since Quake uses QuakeC (an interpreted language), you'll need to:

1. Create native C bridge functions
2. Expose these functions to QuakeC via the engine
3. Call the bridge functions from QuakeC code

The exact method depends on your Quake engine. Most modern engines support native function calls from QuakeC.



