# ODOOM3 — OASIS STAR Integration Instructions

**Port:** dhewm3 (classic Doom 3 GPL source port)  
**Game source ID:** `ODOOM3`  
**Game DLL target:** `base.dll` (compiled separately from the engine executable)

**Cross-game partners:** ODOOM, OQuake, ODOOM3-BFG, **ODuke3D** (Duke Nukem 3D, EDuke32 fork), **ODuke3D-RT** (Duke Nukem 3D ray-traced), and **OWolf3D** (Wolfenstein 3D, ECWolf fork).  Keys, inventory, XP, and quests are shared across all seven OASIS Omniverse games.

---

## Prerequisites

| Tool | Version |
|------|---------|
| Visual Studio | 2019 or later (C++ Desktop workload) |
| CMake | 3.15+ |
| SDL2 | 2.x (for Windows build) |
| Git | any |

- dhewm3 source cloned at `C:\Source\ODOOM3`
- OGEngineClient built; `ogengine.dll` / `ogengine.lib` in `OASIS Omniverse\OGEngineClient\`

---

## Build (one command)

```bat
BUILD_ODOOM3.bat
```

Or for CI/batch mode (no prompts):

```bat
BUILD_ODOOM3.bat batch
```

The script:
1. Copies `d3doom3_ogengine_integration.h/.cpp` → `C:\Source\ODOOM3\neo\game\`
2. Copies `OGLib\` headers → `C:\Source\ODOOM3\neo\game\OGLib\`
3. Copies `ogengine.h`, `ogengine_sync.h`, `ogengine.lib`, `ogengine.dll` → `neo\game\`
4. CMake-configures and builds the `base` target (→ `base.dll`)
5. Deploys `ogengine.dll` and `oasisstar.json` next to the output

---

## Engine hook locations (applied once to dhewm3 source)

| File | Location | Hook |
|------|----------|------|
| `neo/game/Game_local.cpp` | `idGameLocal::Init()` end | `D3Doom3_STAR_Init()` |
| `neo/game/Game_local.cpp` | `idGameLocal::Shutdown()` after common guard | `D3Doom3_STAR_Cleanup()` |
| `neo/game/Game_local.cpp` | `idGameLocal::RunFrame()` after `GetLocalPlayer()` | `D3Doom3_STAR_Tick()` |
| `neo/game/Game_local.cpp` | `idGameLocal::RequirementMet()` `else` branch | `D3Doom3_STAR_CheckDoorAccess()` |
| `neo/game/Player.cpp` | `idPlayer::GiveInventoryItem()` after `inventory.items.Append` | `D3Doom3_STAR_OnItemPickup()` |
| `neo/game/ai/AI.cpp` | `idAI::Killed()` after `AI_DEAD` guard | `D3Doom3_STAR_OnMonsterKilled()` |

See `Docs/WINDOWS_INTEGRATION.md` for the exact diff snippets.

---

## dhewm3 vs RBDOOM-3-BFG differences

| Aspect | ODOOM3 (dhewm3) | ODOOM3-BFG (RBDOOM) |
|--------|-----------------|----------------------|
| Build output | `base.dll` (separate DLL) | monolithic exe |
| GAME_DLL | defined | not defined |
| Engine globals | extern pointers in DLL | PCH / global framework |
| Precompiled headers | none | `precompiled.h` + `#pragma hdrstop` |
| GiveInventoryItem | no `giveFlags` param | has `giveFlags` param |
| Monster roster | base game only (21 monsters) | base + d3xp (22 monsters, incl. Maledict) |
| game_source | `"ODOOM3"` | `"ODOOM3BFG"` |
| CVar prefix | `d3doom3_*` | `d3doom_*` |

---

## In-game console commands

| Command | Description |
|---------|-------------|
| `star` | Show command list |
| `star version` | Show STAR integration version |
| `star status` | Show init/auth status |
| `star beamin <user> <pass>` | Authenticate with OASIS |
| `star beamout` | Log out |
| `star inventory` | List STAR cross-game inventory |
| `star add <name>` | Add item (testing) |
| `star debug on\|off` | Toggle debug logging |

---

## Configuration (oasisstar.json)

Placed next to `dhewm3.exe`. Generated automatically on first run.
Edit the `"odoom3"` section to adjust XP values or enable boss NFT minting.

## Cross-Game Teleportation

Call `D3Doom3_STAR_CheckIncomingTeleport()` at the start of every map load to detect incoming teleports from other OGames:

```c
// In your map load / level start hook:
D3Doom3_STAR_CheckIncomingTeleport();
```

This reads `%TEMP%\oasis_teleport_arrive_{avatarId}.json`, warps the player to the requested position (game-specific — see the TODO comment in the integration file), then calls `ogengine_confirm_teleport_arrival()`.

To place an outbound portal in a map, use the OGEngine Editor's `OASISPortalPanel` (UDB) or add an `oasis_portal` entity using the companion editor and `oasis_entities.fgd`.

**Spawn-event polling** is also active in the tick function — the integration now polls `ogengine_poll_spawn_event()` each frame and logs any incoming cross-game entity spawn requests via `oglib_log`.
