# OQuake2 Integration Instructions

High-level steps to integrate the OASIS STAR API into a Yamagi Quake II engine so it becomes **OQuake2** (cross-game keys, quests, inventory, and XP with OQuake, ODOOM, OQuake3, and other OASIS Omniverse games).

## 1. Get the integration files

From **OASIS Omniverse\OQuake2** you need:

- `oquake2_ogengine_integration.c`
- `oquake2_ogengine_integration.h`
- `ogengine.h` (from **OGEngineClient** — the build script copies this automatically)
- `ogengine_sync.h` (from **OGEngineClient** — copied by build script)
- `ogengine.lib` and `ogengine.dll` (Windows) or `libstar_api.so` / `libstar_api.dylib` (Linux/macOS)

Run **BUILD_OQUAKE2.bat** (Windows) or **BUILD_OQUAKE2.sh** (Linux/macOS) to deploy all files automatically.

## 2. Engine C code — required call sites

Add the following hooks in the Yamagi Q2 engine source:

### Startup and shutdown

```c
#include "oquake2_ogengine_integration.h"

// In game startup (e.g. G_Init or main init):
OQuake2_STAR_Init();

// In game shutdown (e.g. G_Shutdown):
OQuake2_STAR_Cleanup();
```

### Frame pump (every frame)

```c
// In the main game loop or server frame (e.g. SV_Frame):
OQuake2_STAR_PollItems();
```

### Key pickup

```c
// When the player picks up a blue or red key:
OQuake2_STAR_OnKeyPickup("blue_key");   // or "red_key"
```

### Key door check

```c
// When a key-locked door is triggered and the player does not have the key locally:
if (OQuake2_STAR_CheckDoorAccess(door->targetname, "blue_key")) {
    // Open the door — STAR had the key cross-game
}
```

### Weapon / armor / health / ammo pickup

```c
// When the player picks up any item:
OQuake2_STAR_OnItemPickup("Shotgun", "Weapon", 1, "Shotgun - Standard shotgun (Quake II)");
OQuake2_STAR_OnItemPickup("Combat Armor", "Armor", 50, "Combat Armor (+50 armor)");
OQuake2_STAR_OnItemPickup("Medium Health", "Health", 25, "Medium Health (+25 HP)");
OQuake2_STAR_OnItemPickup("Bullets", "Ammo", 50, "Bullets for Machinegun");
```

When the player is at max and the engine would leave the item on the floor:

```c
// Engine should call this and then remove the entity:
OQuake2_STAR_OnPickupLeftOnFloor("Mega Health", "Health", 100, "Mega Health (+100 HP)");
```

### Monster kill

```c
// When any monster is killed, pass the engine classname:
OQuake2_STAR_OnMonsterKilled("monster_gunner");   // Gunner
OQuake2_STAR_OnMonsterKilled("monster_makron");   // Makron (boss)
```

### Movement blocking for popups

```c
// In input handling: suppress movement if any popup is open
if (OQuake2_STAR_IsQuestPopupOpen() || OQuake2_STAR_IsInventoryPopupOpen()) {
    // clear forwardmove / sidemove / upmove
}
```

## 3. Build

- Add `oquake2_ogengine_integration.c` to the engine or game project (place in `src/game/` for Yamagi Q2).
- On Windows: link **ogengine.lib** and **winhttp.lib**; ensure **ogengine.dll** is next to the exe.
- On Linux/macOS: link **-lstar_api** (or the `libstar_api.so` / `.dylib` from OGEngineClient publish); pass `-Wl,-rpath,.` so the engine finds it at runtime.
- Include path: `src/game/` (where the headers are copied).

## 4. oasisstar.json

Place `oasisstar.json` in the same folder as the game exe (or in the `build/` subfolder). The file is loaded at startup and saved when the player signs in. Key fields:

| Field | Description |
|-------|-------------|
| `ogengine_url` | STAR API base URL (default: `http://localhost:8888`) |
| `oasis_api_url` | OASIS WEB4 API URL (default: `http://localhost:7777`) |
| `avatar_id` | Optional: pre-set avatar ID to skip login prompt |
| `saved_jwt` / `jwt_token` | Persisted JWT for auto-login on next launch |
| `offline_mode` | 1 = disable all STAR API calls (local-only mode) |
| `mint_monsters` | 1 = mint NFTs for monsters (per-monster flags override) |
| `use_health_on_pickup` | 0 = health goes to STAR inventory only; 1 = engine applies it |
| `max_health` | Cap for health use-from-inventory (default 100) |
| `max_armor` | Cap for armor use-from-inventory (default 200 for Q2 body armor) |
| `assets` | Array of OASIS thing type definitions for this game |

## 5. OASIS Thing Types (6000–6899)

| Name | Thing Type | Item Type |
|------|-----------|-----------|
| Blue Key | 6001 | Key |
| Red Key | 6002 | Key |
| Commander's Head | 6003 | Key |
| Blaster | 6011 | Weapon |
| Shotgun | 6012 | Weapon |
| Super Shotgun | 6013 | Weapon |
| Machinegun | 6014 | Weapon |
| Chaingun | 6015 | Weapon |
| Grenade Launcher | 6016 | Weapon |
| Rocket Launcher | 6017 | Weapon |
| Hyperblaster | 6018 | Weapon |
| Railgun | 6019 | Weapon |
| BFG10K | 6020 | Weapon |
| Bullets | 6021 | Ammo |
| Shells | 6022 | Ammo |
| Grenades | 6023 | Ammo |
| Rockets | 6024 | Ammo |
| Cells | 6025 | Ammo |
| Slugs | 6026 | Ammo |
| Small Health | 6031 | Health |
| Health | 6032 | Health |
| Mega Health | 6033 | Health |
| Jacket Armor | 6041 | Armor |
| Combat Armor | 6042 | Armor |
| Body Armor | 6043 | Armor |
| Soldier | 6101 | Monster |
| Infantry | 6102 | Monster |
| Gunner | 6103 | Monster |
| Berserker | 6104 | Monster |
| Gladiator | 6105 | Monster |
| Flyer | 6106 | Monster |
| Medic | 6107 | Monster |
| Parasite | 6108 | Monster |
| Brain | 6109 | Monster |
| Supertank | 6110 | Monster |
| Tank | 6111 | Monster |
| Makron | 6112 | Monster |

**Portal thing type: 5900** (shared cross-game portal — same for all OASIS Omniverse games).

## 6. Run

Set **STAR_USERNAME** / **STAR_PASSWORD** or **OGENGINE_KEY** / **STAR_AVATAR_ID**, then launch with your Quake II game data:

```bash
./OQUAKE2 +set game baseq2
```

Or from the in-game console: `star beamin <username> <password>`

Keys picked up in OQuake2, OQuake, or ODOOM will then open doors in the other games when the STAR API is authenticated.

## 7. Cross-game notes

- **Blue key / Red key** are Quake II's key classnames. OQuake and OQuake3 use their own key names; the STAR API translates all keys to canonical cross-game tokens.
- OQuake2 armor caps at 200 (Body Armor), higher than OQuake's 100; set `max_armor: 200` in `oasisstar.json`.
- Ammo type mapping for cross-game beam-in: Shells↔Shells, Bullets↔Bullets, Rockets↔Rockets, Cells↔Cells, Slugs↔Slugs.
- Monsters award XP as configured in `oasisstar.json` (`mint_monster_oquake2_*`). Set to 0 to disable minting for individual monsters.
