# Windows Integration Guide for Quake Fork

**For the OQuake-branded integration and cross-game keys with ODOOM**, see **`OQuake/WINDOWS_INTEGRATION.md`** and use the files in **`OQuake/`** with Quake source at `C:\Source\quake-rerelease-qc`.

This guide is the legacy Quake integration. It is specifically for integrating the OASIS STAR API into your Quake fork located at `C:\Source\quake-rerelease-qc`.

## Prerequisites

1. **Visual Studio** (2019 or later) with C++ development tools
2. **CMake** (3.10 or later)
3. **Quake Engine** - Your fork should have a build system
4. **STAR API Credentials** - Get from OASIS platform

## Step 1: Build the Native Wrapper

Same as DOOM integration - see `Doom/WINDOWS_INTEGRATION.md` Step 1.

The library will be at: `C:\Source\OASIS-master\Game Integration\NativeWrapper\build\Release\star_api.lib`

## Step 2: Set Environment Variables

Same as DOOM - see `Doom/WINDOWS_INTEGRATION.md` Step 2.

## Step 3: Copy Integration Files to Quake

```powershell
# From OASIS project root
cd C:\Source\OASIS-master

# Copy integration files to Quake
Copy-Item "Game Integration\Quake\quake_star_integration.c" "C:\Source\quake-rerelease-qc\"
Copy-Item "Game Integration\Quake\quake_star_integration.h" "C:\Source\quake-rerelease-qc\"
Copy-Item "Game Integration\NativeWrapper\star_api.h" "C:\Source\quake-rerelease-qc\"
```

## Step 4: Modify Quake Source Files

The exact files depend on your Quake fork structure. Common files:

### Modify `host.c` (or equivalent initialization file)

Add at the top:
```c
#include "quake_star_integration.h"
```

In `Host_Init()` function:
```c
// Initialize OASIS STAR API integration
Quake_STAR_Init();
```

In `Host_Shutdown()` function:
```c
// Cleanup STAR API
Quake_STAR_Cleanup();
```

### Modify QuakeC Source (if applicable)

If your fork uses QuakeC, you'll need a native C bridge. Create `quake_star_bridge.c`:

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

### Modify `items.qc` (QuakeC) or `items.c` (C)

For QuakeC, modify `Touch_Item()`:
```c
void() Touch_Item =
{
    // ... existing pickup logic ...
    
    // OASIS STAR API Integration
    if (self.classname == "key_silver") {
        QuakeC_OnKeyPickup("silver_key");
    } else if (self.classname == "key_gold") {
        QuakeC_OnKeyPickup("gold_key");
    }
    
    // ... rest of code ...
};
```

For C source, modify the item pickup function:
```c
#include "quake_star_integration.h"

void Touch_Item(edict_t* ent, edict_t* other, cplane_t* plane, csurface_t* surf) {
    // ... existing logic ...
    
    if (ent->item && (ent->item->flags & IT_KEY)) {
        if (strstr(ent->item->pickup_name, "silver")) {
            Quake_STAR_OnKeyPickup("silver_key");
        } else if (strstr(ent->item->pickup_name, "gold")) {
            Quake_STAR_OnKeyPickup("gold_key");
        }
    }
}
```

### Modify `doors.qc` (QuakeC) or `doors.c` (C)

For QuakeC:
```c
void() door_use =
{
    // ... existing door logic ...
    
    if (self.spawnflags & DOOR_KEY_LOCKED) {
        string required_key = GetRequiredKey(self);
        
        if (!HasLocalKey(required_key)) {
            if (QuakeC_CheckDoorAccess(self.targetname, required_key)) {
                door_go_up();
                return;
            }
        }
    }
};
```

For C:
```c
#include "quake_star_integration.h"

void door_use(edict_t* self, edict_t* other, edict_t* activator) {
    // ... existing logic ...
    
    if (self->spawnflags & DOOR_KEY_LOCKED) {
        const char* required_key = GetRequiredKey(self);
        
        if (!HasLocalKey(required_key)) {
            if (Quake_STAR_CheckDoorAccess(self->targetname, required_key)) {
                door_go_up(self, activator);
                return;
            }
        }
    }
}
```

## Step 5: Update Build System

### Visual Studio Project

1. Add `quake_star_integration.c` to project
2. If using bridge, add `quake_star_bridge.c`
3. Add include directories:
   - `C:\Source\OASIS-master\Game Integration\NativeWrapper`
   - `C:\Source\OASIS-master\Game Integration\Quake`
4. Add library directory:
   - `C:\Source\OASIS-master\Game Integration\NativeWrapper\build\Release`
5. Add library: `star_api.lib`
6. Add linker input: `winhttp.lib`

### Makefile (if applicable)

```makefile
STAR_API_DIR=C:/Source/OASIS-master/Game Integration/NativeWrapper
LIBS=-L$(STAR_API_DIR)/build/Release -lstar_api
CFLAGS+=-I$(STAR_API_DIR) -IC:/Source/OASIS-master/Game Integration/Quake

OBJS= ... quake_star_integration.o quake_star_bridge.o
```

## Step 6: Build Quake

Follow your fork's build instructions, ensuring:
- STAR API library is linked
- Include paths are correct
- All source files are included

## Step 7: Test

1. Run Quake
2. Pick up a silver key
3. Check console - should see:
   ```
   STAR API: Authenticated via SSO. Cross-game features enabled.
   STAR API: Added silver_key to cross-game inventory.
   ```

4. Test cross-game access:
   - Start DOOM (after integrating)
   - Approach a door requiring red keycard
   - If mapped, door should open with Quake's silver key

## QuakeC-Specific Notes

If your Quake fork uses QuakeC (interpreted language):

1. **Native Bridge Required**: QuakeC can't directly call C functions, so you need a bridge
2. **Engine Integration**: The bridge functions must be exposed to QuakeC via the engine
3. **Function Registration**: Register bridge functions with the QuakeC VM

Example engine integration (if you have engine source):
```c
// Register bridge functions with QuakeC
PR_RegisterFunction("QuakeC_OnKeyPickup", QuakeC_OnKeyPickup);
PR_RegisterFunction("QuakeC_CheckDoorAccess", QuakeC_CheckDoorAccess);
PR_RegisterFunction("QuakeC_OnItemPickup", QuakeC_OnItemPickup);
```

## Troubleshooting

### QuakeC Functions Not Found
- Ensure bridge functions are registered with the engine
- Check function names match between C and QuakeC
- Verify the engine supports native function calls

### Build Errors
- Check include paths
- Verify library paths
- Ensure Windows SDK is installed

### Runtime Errors
- Check environment variables are set
- Verify network connectivity
- Check console for error messages

## Next Steps

1. Test with DOOM integration
2. Create cross-game quests
3. Test boss NFT collection
4. Deploy bosses across games!

## Support

For more help:
- [Main Integration Guide](../INTEGRATION_GUIDE.md)
- [Phase 2 Quest System](../PHASE2_QUEST_SYSTEM.md)
- [DOOM Windows Integration](../Doom/WINDOWS_INTEGRATION.md)



