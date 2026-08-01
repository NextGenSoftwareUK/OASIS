# OQuake2-RTX — OASIS STAR API Integration Instructions

This document describes how to integrate the OASIS STAR API into the NVIDIA Q2 RTX engine source code.

Q2 RTX is based on Yamagi Quake II with NVIDIA's Vulkan RTX renderer. It shares the same OASIS thing type range (6000–6899) as OQuake2 — both games contribute to the same cross-game inventory.

---

## Prerequisites

- Q2 RTX source: https://github.com/NVIDIA/Q2RTX
- OGEngineClient built to produce `ogengine.dll` (Windows) or `libstar_api.so` (Linux)
- CMake 3.16+ and Vulkan SDK installed
- Visual Studio 2022 (Windows) or GCC/Clang (Linux/macOS)

---

## Step 1 — Copy Integration Files into Engine Source

The build script does this automatically. To do it manually:

```
Copy to <Q2RTX_SRC>/src/game/:
  oquake2rtx_ogengine_integration.c
  oquake2rtx_ogengine_integration.h
  ogengine.h         (from OGEngineClient\)
  ogengine_sync.h    (from OGEngineClient\)
```

Copy `ogengine.dll` / `libstar_api.so` to the Q2 RTX source root.

---

## Step 2 — Add to CMakeLists.txt

In `<Q2RTX_SRC>/CMakeLists.txt`, add the integration file to the game library sources:

```cmake
target_sources(game PRIVATE
    # ... existing sources ...
    src/game/oquake2rtx_ogengine_integration.c
)

# Link STAR API
if(WIN32)
    target_link_libraries(game PRIVATE ogengine.lib winhttp.lib)
else()
    target_link_libraries(game PRIVATE star_api)
    target_link_directories(game PRIVATE ${CMAKE_SOURCE_DIR})
endif()
```

---

## Step 3 — Hook Init and Cleanup

In `src/game/g_main.c` (or the equivalent Q2 RTX game init entry point):

```c
#include "oquake2rtx_ogengine_integration.h"

void G_Init(void)
{
    /* ... existing init ... */
    OQuake2RTX_STAR_Init();
}

void G_Shutdown(void)
{
    OQuake2RTX_STAR_Cleanup();
    /* ... existing shutdown ... */
}
```

---

## Step 4 — Frame Pump

Call PollItems every game frame. In `src/game/g_main.c` or the frame tick function:

```c
void G_RunFrame(void)
{
    OQuake2RTX_STAR_PollItems();
    /* ... existing frame logic ... */
}
```

`OQuake2RTX_STAR_PollItems()` calls `ogengine_sync_pump()` to advance async operations and drains the mint/log/error queues. Safe to call before Init — it is a no-op until initialized.

---

## Step 5 — Key Pickup Hook

When the player picks up a key entity, call:

```c
#include "oquake2rtx_ogengine_integration.h"

void Touch_Item(edict_t* ent, edict_t* other, cplane_t* plane, csurface_t* surf)
{
    /* ... existing touch logic ... */
    if (ent->item->flags & IT_KEY) {
        if (strcmp(ent->item->classname, "key_silver") == 0) {
            OQuake2RTX_STAR_OnKeyPickup("silver_key");
        } else if (strcmp(ent->item->classname, "key_gold") == 0) {
            OQuake2RTX_STAR_OnKeyPickup("gold_key");
        }
    }
}
```

---

## Step 6 — Door Access Check (Cross-Game Keys)

When a key-locked door is triggered and the player does not have the key locally:

```c
qboolean LockedDoorTouch(edict_t* ent, edict_t* other)
{
    /* Player already has the key locally — open door */
    if (PlayerHasKeyLocally(other, ent->item))
        return qtrue;

    /* Check OASIS cross-game inventory */
    const char* key_name = (ent->item->flags & SILVER_KEY) ? "silver_key" : "gold_key";
    if (OQuake2RTX_STAR_CheckDoorAccess(ent->targetname, key_name)) {
        /* Player had the key in OASIS inventory — door opens */
        return qtrue;
    }
    return qfalse;
}
```

`CheckDoorAccess` calls `ogengine_has_item()` which checks the cross-game inventory including items picked up in OQuake2 or other OASIS Omniverse games.

---

## Step 7 — Item Pickup Hooks

```c
void Pickup_Weapon(edict_t* ent, edict_t* other)
{
    /* ... existing logic ... */
    OQuake2RTX_STAR_OnItemPickup(ent->item->pickup_name, "Weapon", 1, NULL);
}

void Pickup_Armor(edict_t* ent, edict_t* other)
{
    /* ... existing logic ... */
    OQuake2RTX_STAR_OnItemPickup(ent->item->pickup_name, "Armor", 1, NULL);
}

void Pickup_Health(edict_t* ent, edict_t* other)
{
    /* ... existing logic ... */
    OQuake2RTX_STAR_OnItemPickup(ent->item->pickup_name, "Health", ent->count, NULL);
}

void Pickup_Ammo(edict_t* ent, edict_t* other)
{
    /* ... existing logic ... */
    OQuake2RTX_STAR_OnItemPickup(ent->item->pickup_name, "Ammo", ent->count, NULL);
}
```

---

## Step 8 — Monster Kill Hook

```c
void M_Die(edict_t* self, edict_t* inflictor, edict_t* attacker, int damage, vec3_t point)
{
    /* ... existing death logic ... */
    if (attacker && attacker->client) {
        OQuake2RTX_STAR_OnMonsterKilled(self->classname);
    }
}
```

The integration looks up `self->classname` in the monster table. Bosses (Makron, Jorg) are detected automatically by the `is_boss` flag in the table and call `ogengine_queue_monster_kill` with `is_boss=1`.

---

## Step 9 — Input Blocking (Optional)

If you implement OASIS HUD popups:

```c
void IN_Move(usercmd_t* cmd)
{
    if (OQuake2RTX_STAR_IsQuestPopupOpen() || OQuake2RTX_STAR_IsInventoryPopupOpen()) {
        /* suppress movement input */
        memset(cmd, 0, sizeof(*cmd));
        return;
    }
    /* ... existing input handling ... */
}
```

---

## Build Instructions

### Windows

```bat
BUILD_OQUAKE2RTX.bat
```

The script:
1. Builds OGEngineClient (produces ogengine.dll + ogengine.lib)
2. Copies headers and integration files into Q2RTX_SRC\src\game\
3. Runs cmake + cmake --build
4. Copies OQUAKE2RTX.exe and ogengine.dll to build\

Link flags added automatically: `ogengine.lib winhttp.lib`

### Linux / macOS

```bash
./BUILD_OQUAKE2RTX.sh
```

Link flags: `-lstar_api` (from the library search path set to Q2 RTX source root)

---

## oasisstar.json Field Reference

| Field                          | Type    | Description                                              |
|-------------------------------|---------|----------------------------------------------------------|
| `ogengine_url`                 | string  | OGEngineClient base URL (default: http://localhost:8888) |
| `oasis_api_url`                | string  | OASIS API base URL (default: http://localhost:7777)      |
| `offline_mode`                 | int     | 1 = skip STAR API calls entirely                         |
| `saved_jwt`                    | string  | Persisted JWT from last session                          |
| `refresh_token`                | string  | Refresh token for session renewal                        |
| `mint_monsters`                | int     | 1 = monster minting enabled globally                     |
| `mint_weapons`                 | int     | 1 = weapon pickups trigger mint                          |
| `mint_armor`                   | int     | 1 = armor pickups trigger mint                           |
| `mint_powerups`                | int     | 1 = powerup pickups trigger mint                         |
| `mint_keys`                    | int     | 1 = key pickups trigger mint                             |
| `use_health_on_pickup`         | int     | 1 = apply health immediately; 0 = store in inventory     |
| `max_health`                   | int     | Max health cap (default: 100)                            |
| `max_armor`                    | int     | Max armor cap (default: 200)                             |
| `nft_provider`                 | string  | NFT provider name (e.g. "SolanaOASIS")                  |
| `send_to_address_after_minting`| string  | Wallet address for post-mint transfer (optional)         |
| `beamedin_avatar`              | string  | Avatar ID set after successful beam-in                   |
| `mint_monster_oquake2rtx_*`    | int     | Per-monster mint flag (1=mint, 0=skip)                   |

---

## OASIS Thing Type Table (OQuake2-RTX)

| Thing Type | Name               | Category |
|------------|--------------------|----------|
| 5900       | Portal             | Portal   |
| 6001       | silver_key         | Key      |
| 6002       | gold_key           | Key      |
| 6100       | Blaster            | Weapon   |
| 6101       | Shotgun            | Weapon   |
| 6102       | Super Shotgun      | Weapon   |
| 6103       | Machinegun         | Weapon   |
| 6104       | Chaingun           | Weapon   |
| 6105       | Grenade Launcher   | Weapon   |
| 6106       | Rocket Launcher    | Weapon   |
| 6107       | Hyperblaster       | Weapon   |
| 6108       | Railgun            | Weapon   |
| 6109       | BFG10K             | Weapon   |
| 6200       | Jacket Armor       | Armor    |
| 6201       | Combat Armor       | Armor    |
| 6202       | Body Armor         | Armor    |
| 6300       | Small Health       | Health   |
| 6301       | Medium Health      | Health   |
| 6302       | Mega Health        | Health   |
| 6400       | Shells             | Ammo     |
| 6401       | Bullets            | Ammo     |
| 6402       | Grenades           | Ammo     |
| 6403       | Rockets            | Ammo     |
| 6404       | Cells              | Ammo     |
| 6405       | Slugs              | Ammo     |
| 6500       | Gunner             | Monster  |
| 6501       | Gladiator          | Monster  |
| 6502       | Tank               | Monster  |
| 6503       | Makron (boss)      | Monster  |
| 6504       | Jorg (boss)        | Monster  |
| 6505       | Brain              | Monster  |
| 6506       | Floater            | Monster  |
| 6507       | Mutant             | Monster  |
| 6508       | Medic              | Monster  |
| 6509       | Soldier            | Monster  |
| 6510–6899  | Reserved           | —        |

---

## Shared Inventory Note

OQuake2-RTX shares the 6xxx thing type range with OQuake2. Items added to the OASIS inventory from either game are visible in both. This means:
- A silver key picked up in OQuake2 can open a silver door in OQuake2-RTX (via `CheckDoorAccess`)
- Monster kills in either game contribute to the same cross-game XP pool
- The portal (type 5900) connects both Q2 variants to the wider OASIS Omniverse

This sharing is intentional — OQuake2 and OQuake2-RTX are the same game content (Quake II) with different renderers.
