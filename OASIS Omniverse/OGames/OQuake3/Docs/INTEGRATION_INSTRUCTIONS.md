# OQuake3 — OASIS STAR API Integration Instructions

This document describes how to integrate the OASIS STAR API into the Quake3e engine source code.

Quake III Arena is an arena/deathmatch game — it has no traditional key/door locks. Runes (from Q3:TA modes), powerups, and bot kills serve the collectible and XP roles in OASIS.

---

## Prerequisites

- Quake3e source: https://github.com/ec-/Quake3e
- OGEngineClient built to produce `ogengine.dll` (Windows) or `libstar_api.so` (Linux)
- CMake 3.16+ and SDL2 (required by Quake3e)
- Visual Studio 2022 (Windows) or GCC/Clang (Linux/macOS)

---

## Step 1 — Copy Integration Files into Engine Source

The build script does this automatically. To do it manually:

```
Copy to <Q3E_SRC>/code/game/:
  oquake3_ogengine_integration.c
  oquake3_ogengine_integration.h
  ogengine.h         (from OGEngineClient\)
  ogengine_sync.h    (from OGEngineClient\)
```

Copy `ogengine.dll` / `libstar_api.so` to the Quake3e source root.

---

## Step 2 — Add to CMakeLists.txt (or Makefile)

Quake3e uses its own build system. Add the integration file to the game DLL sources.

**If using CMake:**

```cmake
target_sources(game PRIVATE
    # ... existing sources ...
    code/game/oquake3_ogengine_integration.c
)

if(WIN32)
    target_link_libraries(game PRIVATE ogengine.lib winhttp.lib)
else()
    target_link_libraries(game PRIVATE star_api)
    target_link_directories(game PRIVATE ${CMAKE_SOURCE_DIR})
endif()
```

**If using Quake3e's Makefile directly**, add to `GFILES` in the Makefile:

```makefile
GFILES += code/game/oquake3_ogengine_integration.c
```

And add link flags for `ogengine.lib` / `-lstar_api`.

---

## Step 3 — Hook Init and Cleanup

In `code/game/g_main.c`:

```c
#include "oquake3_ogengine_integration.h"

void G_InitGame(int levelTime, int randomSeed, int restart)
{
    /* ... existing init ... */
    OQuake3_STAR_Init();
}

void G_ShutdownGame(int restart)
{
    OQuake3_STAR_Cleanup();
    /* ... existing shutdown ... */
}
```

---

## Step 4 — Frame Pump

Call PollItems every game frame. In `code/game/g_main.c`:

```c
void G_RunFrame(int levelTime)
{
    OQuake3_STAR_PollItems();
    /* ... existing frame logic ... */
}
```

`OQuake3_STAR_PollItems()` calls `ogengine_sync_pump()` to advance async operations. Safe to call before Init — it is a no-op until initialized.

---

## Step 5 — Rune Pickup Hook (Q3:TA Modes)

Quake III: Team Arena adds runes. When a player picks up a rune entity:

```c
/* In code/game/g_items.c or wherever item touch is handled */
#include "oquake3_ogengine_integration.h"

qboolean Pickup_Rune(gentity_t* ent, gclient_t* client)
{
    /* ... existing rune logic ... */

    if (strcmp(ent->classname, "item_rune1") == 0)
        OQuake3_STAR_OnRunePickup("rune_strength");
    else if (strcmp(ent->classname, "item_rune2") == 0)
        OQuake3_STAR_OnRunePickup("rune_haste");
    else if (strcmp(ent->classname, "item_rune3") == 0)
        OQuake3_STAR_OnRunePickup("rune_regeneration");
    else if (strcmp(ent->classname, "item_rune4") == 0)
        OQuake3_STAR_OnRunePickup("rune_resistance");

    return qtrue;
}
```

To check rune possession from cross-game inventory:

```c
if (OQuake3_STAR_HasRune("rune_strength")) {
    /* Player has the rune in OASIS inventory — apply bonus */
}
```

---

## Step 6 — Item Pickup Hook

```c
/* In code/game/g_items.c Touch_Item or pickup functions */
#include "oquake3_ogengine_integration.h"

qboolean Pickup_Weapon(gentity_t* ent, gclient_t* client)
{
    /* ... existing logic ... */
    OQuake3_STAR_OnItemPickup(ent->item->pickup_name, "Weapon", 1, NULL);
    return qtrue;
}

qboolean Pickup_Armor(gentity_t* ent, gclient_t* client)
{
    /* ... existing logic ... */
    OQuake3_STAR_OnItemPickup(ent->item->pickup_name, "Armor", 1, NULL);
    return qtrue;
}

qboolean Pickup_Health(gentity_t* ent, gclient_t* client)
{
    /* ... existing logic ... */
    OQuake3_STAR_OnItemPickup(ent->item->pickup_name, "Health", ent->count, NULL);
    return qtrue;
}

qboolean Pickup_Powerup(gentity_t* ent, gclient_t* client)
{
    /* ... existing logic ... */
    OQuake3_STAR_OnItemPickup(ent->item->pickup_name, "Powerup", 1, NULL);
    return qtrue;
}

qboolean Pickup_Ammo(gentity_t* ent, gclient_t* client)
{
    /* ... existing logic ... */
    OQuake3_STAR_OnItemPickup(ent->item->pickup_name, "Ammo", ent->count, NULL);
    return qtrue;
}
```

---

## Step 7 — Bot / Player Kill Hook

```c
/* In code/game/g_combat.c or wherever kills are processed */
#include "oquake3_ogengine_integration.h"

void player_die(gentity_t* self, gentity_t* inflictor, gentity_t* attacker, int damage, int meansOfDeath)
{
    /* ... existing death logic ... */

    if (attacker && attacker->client && attacker != self) {
        qboolean victim_is_bot = (self->r.svFlags & SVF_BOT) ? qtrue : qfalse;

        if (victim_is_bot) {
            /* In SP/bot match — track by bot skill/name */
            OQuake3_STAR_OnBotKilled(self->client->pers.netname);
        } else {
            /* PvP frag */
            OQuake3_STAR_OnPlayerFragged(self->client->pers.netname, qfalse);
        }
    }
}
```

For single-player mode with named bots (Grunt, Klesk, Xaero, Orbb), pass the bot classname directly:

```c
OQuake3_STAR_OnBotKilled("bot_xaero");  /* Xaero = final boss, 200 XP, is_boss=1 */
```

---

## Step 8 — Input Blocking (Optional)

If you implement OASIS HUD popups:

```c
void IN_Move(usercmd_t* cmd)
{
    if (OQuake3_STAR_IsQuestPopupOpen() || OQuake3_STAR_IsInventoryPopupOpen()) {
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
BUILD_OQUAKE3.bat
```

The script:
1. Builds OGEngineClient (produces ogengine.dll + ogengine.lib)
2. Copies headers and integration files into Q3E_SRC\code\game\
3. Runs cmake + cmake --build
4. Copies OQUAKE3.exe and ogengine.dll to build\

Link flags added to CMake: `ogengine.lib winhttp.lib`

### Linux / macOS

```bash
./BUILD_OQUAKE3.sh
```

Link flags: `-lstar_api` with the library in Q3E_SRC.

### Quake3e Makefile build

If using the native Quake3e Makefile:

```bash
cd $Q3E_SRC
make BUILD_SERVER=0 BUILD_CLIENT=1 BUILD_RENDERER_OPENGL=1 \
     GFILES_EXTRA="code/game/oquake3_ogengine_integration.c" \
     LDFLAGS="-L. -lstar_api"
```

---

## oasisstar.json Field Reference

| Field                            | Type    | Description                                              |
|---------------------------------|---------|----------------------------------------------------------|
| `ogengine_url`                   | string  | OGEngineClient base URL (default: http://localhost:8888) |
| `oasis_api_url`                  | string  | OASIS API base URL (default: http://localhost:7777)      |
| `offline_mode`                   | int     | 1 = skip STAR API calls entirely                         |
| `saved_jwt`                      | string  | Persisted JWT from last session                          |
| `refresh_token`                  | string  | Refresh token for session renewal                        |
| `mint_monsters`                  | int     | 1 = bot/monster minting enabled globally                 |
| `mint_weapons`                   | int     | 1 = weapon pickups trigger mint                          |
| `mint_armor`                     | int     | 1 = armor pickups trigger mint                           |
| `mint_powerups`                  | int     | 1 = powerup pickups trigger mint                         |
| `mint_keys`                      | int     | 1 = rune pickups trigger mint                            |
| `use_health_on_pickup`           | int     | 1 = apply health immediately; 0 = store in inventory     |
| `max_health`                     | int     | Max health cap (default: 100)                            |
| `max_armor`                      | int     | Max armor cap (default: 200)                             |
| `nft_provider`                   | string  | NFT provider name (e.g. "SolanaOASIS")                  |
| `send_to_address_after_minting`  | string  | Wallet address for post-mint transfer (optional)         |
| `beamedin_avatar`                | string  | Avatar ID set after successful beam-in                   |
| `mint_monster_oquake3_*`         | int     | Per-bot mint flag (1=mint, 0=skip)                       |

---

## OASIS Thing Type Table (OQuake3)

| Thing Type | Name                | Category  |
|------------|---------------------|-----------|
| 5900       | Portal              | Portal    |
| 7001       | rune_strength       | Rune      |
| 7002       | rune_haste          | Rune      |
| 7003       | rune_regeneration   | Rune      |
| 7004       | rune_resistance     | Rune      |
| 7100       | Gauntlet            | Weapon    |
| 7101       | Machinegun          | Weapon    |
| 7102       | Shotgun             | Weapon    |
| 7103       | Grenade Launcher    | Weapon    |
| 7104       | Rocket Launcher     | Weapon    |
| 7105       | Lightning Gun       | Weapon    |
| 7106       | Railgun             | Weapon    |
| 7107       | Plasma Gun          | Weapon    |
| 7108       | BFG                 | Weapon    |
| 7200       | Armor Shard         | Armor     |
| 7201       | Yellow Armor        | Armor     |
| 7202       | Red Armor           | Armor     |
| 7300       | Small Health        | Health    |
| 7301       | Large Health        | Health    |
| 7302       | Mega Health         | Health    |
| 7400       | Bullets             | Ammo      |
| 7401       | Shells              | Ammo      |
| 7402       | Grenades            | Ammo      |
| 7403       | Rockets             | Ammo      |
| 7404       | Lightning           | Ammo      |
| 7405       | Slugs               | Ammo      |
| 7406       | Plasma              | Ammo      |
| 7407       | BFG Ammo            | Ammo      |
| 7450       | Quad Damage         | Powerup   |
| 7451       | Battle Suit         | Powerup   |
| 7452       | Haste               | Powerup   |
| 7453       | Invisibility        | Powerup   |
| 7454       | Regeneration        | Powerup   |
| 7500       | Grunt Bot           | Bot       |
| 7501       | Klesk Bot           | Bot       |
| 7502       | Xaero Bot (boss)    | Bot       |
| 7503       | Orbb Bot            | Bot       |
| 7504–7899  | Reserved            | —         |

---

## Q3 Arena vs Key/Door Notes

Unlike OQuake and OQuake2, Quake III has no key/door system. The OASIS integration adapts as follows:

- **Runes** (Q3:TA item_rune1–4) replace the key role. `OQuake3_STAR_HasRune()` checks cross-game inventory, so a rune collected in one game could theoretically grant a bonus in another.
- **Portal (5900)** is the shared cross-game connection point. When the player touches an OASIS portal, the engine calls the portal transition logic to move between OASIS Omniverse games.
- **Bot kills** replace monster kills from Q1/Q2. Xaero is the SP final boss (is_boss=1).
- The `stack_keys` field in oasisstar.json controls rune stacking behavior.

---

## Quake3e VM vs Native Notes

Quake3e supports both QVM (Quake virtual machine bytecode) and native DLL builds. The OASIS integration requires a native DLL build because:

- `ogengine_*` functions require native C ABI linkage
- The sync layer uses platform threads (`ogengine_sync_pump()`)
- JWT/JSON config I/O uses standard C file I/O

To build as a native game DLL with Quake3e, pass `-DUSE_SYSTEM_JPEG=0` (optional) and ensure `BUILD_GAME_SHARED=1` or the equivalent flag for your build system.
