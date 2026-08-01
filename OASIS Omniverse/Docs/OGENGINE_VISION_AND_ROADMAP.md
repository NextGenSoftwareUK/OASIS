# OGEngine — Full Vision, Architecture & Roadmap

*The OASIS Omniverse: a Ready Player One-style infinite open metaverse spanning all OGames.*

---

## 1. The Vision

The OASIS Omniverse is not a collection of separate games with a shared leaderboard. It is **one infinite, borderless world** where:

- Every OGame is a **region** of the same universe, not a silo.
- You can **walk through a portal in a Doom map and appear in a Quake 2 level**.
- A **Quake Shambler can spawn into a Duke Nukem 3D episode**. A Cacodemon can hunt you in Wolfenstein 3D.
- A **gold key found in Wolf3D opens a door in Doom**. An ammo crate picked up in Quake restocks your shotgun in Duke3D.
- **Quests weave across all games** — start a mission in Doom, collect an artefact in Quake 3, deliver it in Quake 2, complete it in Wolf3D.
- The **OASIS HUD** — a neon-blue Steam/Xbox-style overlay — works identically in every OGame. Press `I` anywhere, in any game, and your shared OASIS inventory, quests, avatar, NFTs, and clan appear.
- The **OASIS HUB** is a 3D space station from which portals lead to any game, any map, any level.
- The **OGEngine Editor** lets you place assets, monsters, weapons, ammo, and portals from ANY game into ANY map.

This is the same vision as **Ready Player One**'s OASIS: a single continuous experience where the rules, assets, and story of one world bleed into the next.

---

## 2. What Is Already Built

### 2.1 OASIS Kernel (Unity)

`OmniverseKernel.cs` — a Unity singleton that bootstraps everything and persists across scene loads.

| Component | File | What it does |
|-----------|------|-------------|
| **Kernel** | `OmniverseKernel.cs` | Bootstrap, portal dispatch, global settings |
| **Game Host Service** | `GameProcessHostService.cs` | Preloads all OGames as native Win32 processes, embeds their windows into the Unity window via `SetParent`/`WS_CHILD`, memory-aware stale unload |
| **Shared HUD Overlay** | `SharedHudOverlay.cs` | Steam/Xbox-style `I`-key overlay: Inventory, Quests, NFTs, Avatar, Karma, Settings, Diagnostics tabs |
| **Portal Trigger** | `PortalTrigger.cs` | Walk-through colliders that call `EnterPortalAsync(gameId)` |
| **Hub Builder** | `SpaceHubBuilder.cs` | Procedurally generates the 3D space hub with spinning portals |
| **Quest Tracker Widget** | `QuestTrackerWidget.cs` | Always-visible mini HUD, auto-refreshes every 20 seconds |
| **API Gateway** | `Web4Web5GatewayClient.cs` | Resilient HTTP client (retry, backoff, circuit breaker, cache) to WEB4 OASIS API and WEB5 STAR API |
| **Login Screen** | `LoginScreen.cs` | OASIS beam-in, saves JWT + avatarId to config |
| **Global Settings** | `GlobalSettingsService.cs` | Audio, graphics, keybindings; persisted per-avatar via WEB4 Settings API |

**Current portals:** ODOOM, OQuake (2 of 10 planned)

### 2.2 OGEngineClient (C# NativeAOT → ogengine.dll)

The `ogengine.dll` is the bridge between native C/C++ games and the OASIS backend. Exports the full `ogengine_*` and `star_sync_*` C ABI. Handles:

- Inventory (get, has, use, add, pickup-with-mint)
- Quests (start, complete, objectives, progress)
- NFTs (mint, get collection)
- Avatar (profile, karma, send item to avatar/clan)
- GeoHotSpots (map/AR/VR/IR + Audio/Video/Text/Website)
- Auth (JWT beam-in, refresh, star_sync async pump)

### 2.3 OGLib (C header-only)

`OGLib/oglib.h` — shared utility library (config loader, JSON parser, HTTP helpers, logging). Used by all game integrations.

### 2.4 Game Integrations (C/C++ hooks)

| OGame | Base Port | Status | Integration file |
|-------|-----------|--------|-----------------|
| ODOOM | UZDoom | ✅ Complete | `uzdoom_ogengine_integration.c` |
| OQuake | vkQuake | ✅ Complete | `oquake_ogengine_integration.c` |
| ODOOM3 | dhewm3 | ✅ Complete | `d3doom3_ogengine_integration.cpp` |
| ODOOM3-BFG | RBDOOM-3-BFG | ✅ Complete | `d3doom_ogengine_integration.cpp` |
| ODuke3D | EDuke32 | ✅ Complete | `oduke3d_ogengine_integration.c` |
| ODuke3D-RT | Duke-RT | ✅ Complete | `oduke3drt_ogengine_integration.c` |
| OWolf3D | ECWolf | ✅ Complete | `owolf3d_ogengine_integration.cpp` |
| OQuake2 | Yamagi Q2 | 🔄 Integration files ready (engine not cloned yet) | `oquake2_ogengine_integration.c` |
| OQuake2-RTX | Q2 RTX | 🔄 Integration files ready (engine not cloned yet) | `oquake2rtx_ogengine_integration.c` |
| OQuake3 | Quake3e | 🔄 Integration files ready (engine not cloned yet) | `oquake3_ogengine_integration.c` |

### 2.5 WEB4 / WEB5 APIs

- **WEB4 OASIS API** — avatar, inventory, karma, settings, NFTs, quests (persistence layer)
- **WEB5 STAR API** (`C:\Source\OASIS2\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI`) — quest definitions, objectives, GeoHotSpots, missions, STARNET holons, cross-game progress, OGAsset catalog, portal registry
- **Quest system** — cross-game quests with objectives spanning multiple games, ExternalHandoffUri for cross-app handoffs (CLI, OPortal, Telegram, Discord)
- **STAR API controllers already built:** QuestsController, MissionsController, GamesController, GeoHotSpotsController, OAPPsController, ZomesController — all data persists via HolonManager → MongoDB

### 2.6 Full OASIS Platform Stack

The OASIS Omniverse is not just the in-game layer — it is a complete creator + viewer + player platform:

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│  OASIS OMNIVERSE PLATFORM STACK                                                      │
│                                                                                      │
│  ┌────────────────────────────────────────────────────────────────────────────────┐  │
│  │  CREATOR LAYER                                                                  │  │
│  │                                                                                 │  │
│  │  ┌──────────────────────────────┐  ┌────────────────────────────────────────┐  │  │
│  │  │  OGEngine Editor (UDB-based) │  │  STARNET  (C:\Source\STARNET)          │  │  │
│  │  │  C#/.NET — map authoring     │  │  React/TypeScript web app              │  │  │
│  │  │  • Place portals & assets    │  │  • OAPP drag-and-drop SmartBrick       │  │  │
│  │  │  • Bind quest objectives     │  │    builder (plug any asset like LEGO)  │  │  │
│  │  │    to map triggers           │  │  • Quest / Mission Builder (web)       │  │  │
│  │  │  • Convert between map fmts  │  │    − story arc authoring               │  │  │
│  │  │  • Companion launch          │  │    − event-driven quest scripting      │  │  │
│  │  │    (TrenchBroom/NetRadiant/  │  │    − objectives linked to map triggers │  │  │
│  │  │     DarkRadiant/Mapster32)   │  │  • App/asset store                     │  │  │
│  │  └──────────────────────────────┘  └────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────────────────────────────────────┘  │
│                        ↕ OGEditorSDK (C#/.NET Standard 2.0 + C ABI via NativeAOT)  │
│  ┌────────────────────────────────────────────────────────────────────────────────┐  │
│  │  DATA LAYER — WEB5 STAR API                                                     │  │
│  │  C:\Source\OASIS2\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI                   │  │
│  │  All quest/game/portal/asset data stored here. Single source of truth.          │  │
│  │  Endpoints: /api/quests  /api/missions  /api/games  /api/portals               │  │
│  │             /api/geohotspots  /api/oassets  /api/stories  /api/holons          │  │
│  └────────────────────────────────────────────────────────────────────────────────┘  │
│                        ↕ HTTP REST                                                    │
│  ┌────────────────────────────────────────────────────────────────────────────────┐  │
│  │  VIEWER / BROWSE LAYER — OPORTAL  (C:\Source\OPORTAL-JS)                        │  │
│  │  Public browse portal for OGames, quests, OGAsset metadata                      │  │
│  │  Currently static HTML — to be upgraded to React + live STAR API queries        │  │
│  └────────────────────────────────────────────────────────────────────────────────┘  │
│                        ↕ ogengine.dll C ABI  /  ogeditor_api.dll C ABI              │
│  ┌────────────────────────────────────────────────────────────────────────────────┐  │
│  │  PLAYER LAYER — OGames + OASIS Kernel                                           │  │
│  │  ODOOM • OQuake • ODOOM3 • ODOOM3-BFG • ODuke3D • ODuke3D-RT • OWolf3D       │  │
│  │  OQuake2 • OQuake2-RTX • OQuake3                                               │  │
│  └────────────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

#### SmartBricks (STARNET OAPP Builder)

SmartBricks are STARNET's drag-and-drop OAPP builder. Every OASIS asset type is a "brick" that snaps together:

| Brick type | What it represents |
|------------|-------------------|
| NFT brick | Non-fungible token (item, skin, collectible) |
| GeoNFT brick | NFT anchored to a real-world GPS location |
| GeoHotSpot brick | AR/VR/IR trigger (audio, video, text, web link) |
| OAPP brick | A standalone OASIS application |
| Zome brick | A Holon cluster (sub-graph of linked assets) |
| Plugin brick | A runtime behaviour plug-in |
| Library brick | A shared code library |
| Quest brick | A quest definition with objectives |
| Mission brick | A mission within a quest |
| Portal brick | A cross-game teleport endpoint |

You plug them together in STARNET's visual builder (like LEGO bricks), then publish the OAPP to the STAR API. The OASIS Omniverse kernel loads it at runtime.

#### Quest Builder: Web or Native?

The quest authoring experience spans two surfaces — choose the right tool for each:

| Task | Tool | Why |
|------|------|-----|
| Story arc / quest narrative authoring | **STARNET web Quest Builder** | Rich event graph, audio/video/text/link scripting, multi-game arc design, SmartBrick drag-and-drop — these are web-native workflows |
| Bind objectives to specific map locations | **UDB Quest Weaver panel** (in OGEngine Editor) | You need to see the actual map, click a linedef/sector/thing, and tag it with an objective — that's a map-editor task |
| Preview quest in context | **UDB + STAR API live panel** | Fetch the authored quest from STAR API, display objectives, show which triggers are bound |

The two halves communicate via the STAR API: STARNET writes quest JSON → STAR API stores it → UDB fetches it → UDB binds objectives to map triggers → UDB writes trigger binding back to STAR API + map sidecar.

**Embedding option:** UDB can optionally embed the STARNET Quest Builder web UI via **WebView2** (Chromium for WinForms, `.NET 4.7.2` compatible) in a docker panel. This gives creators a single app experience without sacrificing the web-native quest scripting interface. Recommended for the long-term UX.

#### OGEditorSDK

`C:\Source\UltimateDoomBuilder\Source\OGEditorSDK` — a .NET Standard 2.0 library that makes all the above portable:

| File | What it provides |
|------|-----------------|
| `OGAssetCatalog.cs` | Canonical ~140-asset catalog across all 10 OGames (single source of truth) |
| `OGMapSidecar.cs` | `oasis_{mapname}.json` reader/writer (portals + cross-game entities) |
| `OGStarApiClient.cs` | HTTP client for STAR API (`/api/quests`, `/api/games`, `/api/portals`, …) |
| `OGEntityMappings.cs` | Bidirectional classname ↔ OASIS thing type lookup (Q1/Q2/Q3/Duke/Wolf) |
| `Native/ogeditor_api.h` | C ABI header — same pattern as `ogengine.h` |
| `Native/NativeExports.cs` | NativeAOT [UnmanagedCallersOnly] exports → `ogeditor_api.dll` |

**Any editor can use it:**
- **UDB** — references `OGEditorSDK.csproj` directly (already done)
- **TrenchBroom (C++)** — `#include "ogeditor_api.h"`, `LoadLibrary("ogeditor_api.dll")`
- **NetRadiant (C++)** — same pattern
- **DarkRadiant (C++)** — same pattern
- **Mapster32 (C)** — same pattern (C linkage via `extern "C"`)
- **STARNET web** — calls STAR API directly (no SDK needed — browser can't load native DLL)

---

## 3. The Full Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│  OASIS OMNIVERSE — SYSTEM ARCHITECTURE                                                      │
│                                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────────────────────┐   │
│  │  LAYER 5: OASIS HUB (Unity)                                                          │   │
│  │  3D space station • portals to all 10 OGames • SpaceHubBuilder                       │   │
│  │  OmniverseKernel • GameProcessHostService (embeds native game windows)                │   │
│  └──────────────────────────────────────────────────────────────────────────────────────┘   │
│                        ↕ Win32 window embed / process IPC                                   │
│  ┌──────────────────────────────────────────────────────────────────────────────────────┐   │
│  │  LAYER 4: OASIS HUD OVERLAY                                                          │   │
│  │  SharedHudOverlay.cs (Unity) — neon-blue Steam/Xbox-style overlay (I key)            │   │
│  │  In-game native HUD (per-game C overlay drawn by each OGame's integration layer)     │   │
│  │  Tabs: Inventory • Quests • NFTs • Avatar • Karma • Settings • Diagnostics           │   │
│  │  QuestTrackerWidget — always-visible mini HUD top-right                              │   │
│  └──────────────────────────────────────────────────────────────────────────────────────┘   │
│                        ↕ ogengine.dll C ABI                                                 │
│  ┌──────────────────────────────────────────────────────────────────────────────────────┐   │
│  │  LAYER 3: OGEngineClient (C# NativeAOT → ogengine.dll)                                │   │
│  │  Inventory cache • Mint + add-item queue • Quest objectives • NFT mint               │   │
│  │  GeoHotSpot fetch • Auth (JWT) • Karma • Send to avatar/clan                        │   │
│  │  star_sync_* (async auth/inventory pump) • circuit breaker • retry/backoff           │   │
│  └──────────────────────────────────────────────────────────────────────────────────────┘   │
│                        ↕ HTTP REST                                                           │
│  ┌──────────────────────────────────────────────────────────────────────────────────────┐   │
│  │  LAYER 2: WEB4 OASIS API + WEB5 STAR API                                            │   │
│  │  Avatar • Inventory • Karma • Settings • NFTs • Quests • Objectives • GeoHotSpots   │   │
│  │  Missions • STARNET holons • Cross-game entity catalog • Teleporter registry         │   │
│  └──────────────────────────────────────────────────────────────────────────────────────┘   │
│                        ↕ native C game hooks                                                │
│  ┌──────────────────────────────────────────────────────────────────────────────────────┐   │
│  │  LAYER 1: OGames (C/C++ native processes)                                            │   │
│  │  ODOOM • OQuake • ODOOM3 • ODOOM3-BFG • ODuke3D • ODuke3D-RT • OWolf3D             │   │
│  │  OQuake2 • OQuake2-RTX • OQuake3  (each with *_ogengine_integration.c/cpp)              │   │
│  └──────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                             │
│  OGEngine Editor — standalone tool, edits maps for all Layer 1 games                       │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. What Remains to Build

Grouped by subsystem, ordered by dependency:

---

### 4.1 Hub Expansion — 8 More Portals

**Status:** Only ODOOM and OQuake portals exist in the Unity hub.

**What to do:**
- Add 8 more `HostedGameDefinition` entries to `omniverse_host_config.json`:  
  ODOOM3, ODOOM3-BFG, ODuke3D, ODuke3D-RT, OWolf3D, OQuake2, OQuake2-RTX, OQuake3
- `SpaceHubBuilder` procedurally generates portals from config — portals auto-appear once config is updated
- Each portal gets its own `portalColorR/G/B` and label in config
- Suggested portal colour palette:

| OGame | Colour | Hex |
|-------|--------|-----|
| ODOOM | Blood red | `#CC1100` |
| OQuake | Poison green | `#00CC44` |
| ODOOM3 | Deep blue | `#0044CC` |
| ODOOM3-BFG | Cyan | `#00CCCC` |
| ODuke3D | Orange | `#CC6600` |
| ODuke3D-RT | Gold | `#CCAA00` |
| OWolf3D | Yellow | `#CCCC00` |
| OQuake2 | Purple | `#8800CC` |
| OQuake2-RTX | Magenta | `#CC00AA` |
| OQuake3 | White | `#CCCCCC` |

**Effort:** Low — config change + test per game.

---

### 4.2 Cross-Game Teleportation (In-Map Portals)

This is the "instant teleport between any map in any game" feature — not just Hub → game, but  
**game map A → game map B** while playing.

#### Architecture

**Two-layer portal system:**

```
[Hub Portals]   — Unity walk-through triggers → ActivateGameAsync(gameId)
                   (already built)

[In-Map Portals] — OASIS portal entities placed IN a game's map
                   → call ogengine_teleport(target_game, target_map, spawn_x, spawn_y, spawn_z)
                   → OmniverseKernel receives the cross-process signal
                   → suspends current game (hides window)
                   → activates target game at target map + position
```

#### STAR API extension needed

New endpoint: `POST /api/teleport`

```json
{
  "sourceGame":    "ODOOM",
  "sourceMap":     "E1M1",
  "targetGame":    "OQUAKE",
  "targetMap":     "e1m1",
  "spawnX":        128.0,
  "spawnY":        0.0,
  "spawnZ":        64.0,
  "avatarId":      "...",
  "sessionToken":  "..."
}
```

New C API exports in `ogengine.h`:

```c
/* Request a cross-game teleport — game calls this when player steps on OASIS portal */
void ogengine_request_teleport(const char *target_game, const char *target_map,
                                float x, float y, float z);

/* Poll: did the kernel request a teleport INTO this game? */
int  ogengine_poll_teleport_request(char *out_map, size_t map_len,
                                    float *out_x, float *out_y, float *out_z);

/* Game calls this after it has loaded the target map at the requested position */
void ogengine_confirm_teleport_arrival(void);
```

#### Kernel IPC

`GameProcessHostService` needs a lightweight IPC channel between processes (e.g. named pipe or shared-memory flag file). When a game calls `ogengine_request_teleport`:

1. The C integration writes the teleport request to `%TEMP%\oasis_teleport_{avatarId}.json`
2. `OmniverseKernel.TickMaintenance()` polls this file (or uses a `FileSystemWatcher`)
3. Kernel hides current game, activates target game, passes map/position via command-line arg or IPC file
4. Target game's integration calls `ogengine_poll_teleport_request()` on start and warps player

#### In-game portal entity

Each OGame needs an **OASIS Portal entity** added to its entity definitions:

| Game | Entity name | Hook |
|------|-------------|------|
| ODOOM | `thing_oasis_portal` | Touch → `ogengine_request_teleport(...)` |
| OQuake | `oasis_portal` | Touch trigger → same |
| ODOOM3 | `trigger_oasis_portal` | idTrigger → same |
| ODuke3D | `SE_OASIS_PORTAL` | Sector effect → same |
| OWolf3D | `OASISPortal` (DECORATE) | Actor touch → same |
| OQuake2 | `trigger_oasis_portal` | Touch → same |
| OQuake3 | `trigger_oasis_portal` | Touch → same |

These entities are placed via the **OGEngine Editor** (see §4.4).

---

### 4.3 Cross-Game Entity System

This is the "Doom monsters spawn in Quake, Wolf3D enemies appear in Duke3D" feature — and the "any item/weapon/ammo from any game can be picked up anywhere" feature.

#### Architecture

```
[OGAsset Catalog] — JSON database of all cross-game entities
                    lives in STAR API: GET /api/oassets
                    entries: { id, name, game_source, type, cross_game_id, spawn_params }

[Map Entity Layer] — per-map sidecar: oasis_{mapname}.json
                    lists OASIS cross-game entities placed in this map
                    { entity_id, position, game_source, spawn_params }

[Game Integration] — on map load, game calls:
                    ogengine_get_map_entities(game_id, map_name, &entity_list)
                    → parses sidecar → spawns foreign entities via game's native spawn API
```

#### Foreign entity spawning per game

Each game integration needs a new function: `OGame_STAR_SpawnCrossGameEntity(entity_id, x, y, z)`.

This maps the `cross_game_id` from the OASIS catalog to a native game entity:

**ODOOM (GZDoom)** — spawn via ACS/ZScript using `Thing_Spawn` with a DECORATE actor class.  
Foreign actors are defined in a `OASIS_CROSSGAME.pk3` that ships with ODOOM:
```
ACTOR OQuake_Shambler : OASISForeignMonster { ... AI, sprites ... }
ACTOR ODuke_Pig : OASISForeignMonster { ... }
ACTOR OWolf_Guard : OASISForeignMonster { ... }
```

**OQuake (QuakeC)** — new QuakeC entity `oasis_spawn_point` with `cross_game_id` key.  
On map load, OQuake's integration reads the sidecar and calls QuakeC's `spawn_entity(classname, origin)`.

**ODOOM3 / ODOOM3-BFG (idTech4)** — new entity type `oasis_spawn_trigger`.  
The C++ integration spawns it via `gameLocal.SpawnEntityType(classname, dict)`.

**ODuke3D (EDuke32)** — new tile index range reserved for OASIS foreign actors.  
The integration spawns them via `A_InsertSprite(...)`.

**OWolf3D (ECWolf)** — new DECORATE class `OASISSpawnPoint` per foreign entity.

#### OGAsset Catalog (STAR API extension)

New endpoint: `GET /api/oassets?game={game_id}&type={Monster|Weapon|Key|Ammo|Powerup}`

Returns:
```json
[
  {
    "id": "oasset_quake_shambler",
    "name": "Shambler",
    "game_source": "OQUAKE",
    "type": "Monster",
    "cross_game_id": "shambler",
    "xp": 200,
    "is_boss": false,
    "native_classnames": {
      "ODOOM":    "OQuake_Shambler",
      "ODOOM3":   "monster_oquake_shambler",
      "ODUKE3D":  "OQUAKE_SHAMBLER_TILE",
      "OWOLF3D":  "OQuake_Shambler"
    }
  },
  ...
]
```

#### Runtime cross-game monster spawning (event-driven)

In addition to placed entities, the STAR API can **push cross-game spawn events** during play:

New C API:
```c
/* Poll for a pending cross-game spawn event */
int ogengine_poll_spawn_event(char *out_entity_id, size_t len,
                               float *out_x, float *out_y, float *out_z);

/* Game confirms spawn succeeded */
void ogengine_confirm_spawn(const char *entity_id);
```

A quest objective can trigger: "spawn a Cacodemon in the current Quake map" by pushing a spawn event to the STAR API, which the OQuake integration picks up on the next tick.

---

### 4.4 OGEngine Editor

The universal cross-game map editor — the tool that makes all of the above *creatable*.

#### What it needs to do

- Open and edit maps from all 10 OGame formats (Doom WAD, Quake BSP/MAP, Quake 2 BSP, Quake 3 BSP, Wolf3D map, Duke3D MAP, Doom 3 MAP)
- Display them in a unified 3D view
- Show a **cross-game asset browser** (the OGAsset Catalog from §4.3)
- Drag-drop monsters/weapons/items from any game into any map
- Place **OASIS Portal entities** (cross-game teleporters) with a destination picker
- Edit portal destination: target game, target map, spawn position
- Create and assign cross-game quests/objectives to map regions
- Save back to native format + `oasis_{mapname}.json` sidecar
- Preview what foreign entities look like (2D icon or placeholder 3D mesh)
- Connect to the live STAR API to browse quests, objectives, and the OGAsset catalog

#### Base editor: Ultimate Doom Builder (UDB) — already started ✅

> **Updated 2026-08-01:** Original recommendation was a TrenchBroom fork. Replaced after
> discovering that `C:\Source\UltimateDoomBuilder` already has significant OASIS integration
> in C#/.NET — the same stack as the rest of the OASIS Omniverse. UDB is now the primary base.

**Ultimate Doom Builder** is the correct base because:

| Criterion | UDB (C#/.NET) | TrenchBroom (C++/Qt) |
|-----------|--------------|----------------------|
| OASIS integration | ✅ Already built and growing | 🔜 Would need a new codebase |
| Language / stack | ✅ C#/.NET — matches OGEngineClient | C++ / Qt — different stack |
| Doom WAD editing | ✅ Native (the whole app is this) | ❌ Not supported |
| Plugin architecture | ✅ Rich C# `Plug` base class | C++ only |
| Q-engine .map support | ✅ Via OASISMapConverter (import/export) | ✅ Native |
| Q3 curve/patch | Via companion NetRadiant | ✅ Native |
| Build engine (Duke3D) | Via companion Mapster32 | Via companion |
| Doom 3 maps | Via companion DarkRadiant | Via companion |
| License | GPL2 | GPL3 |

**Multi-editor strategy:**
- **UDB** = primary OASIS host (Doom WAD maps — the most complex; also the import/export hub for all other formats)
- **TrenchBroom** = companion for Q1/Q2 geometry editing; UDB can launch it as a subprocess
- **NetRadiant-custom** = companion for Q3 maps (curve/patch editing)
- **Mapster32** (bundled with EDuke32) = companion for Duke3D BUILD maps
- **DarkRadiant** = companion for Doom 3 maps (`C:\Source\ODOOM3-Editor`)

All companion editors write/read the same `oasis_{mapname}.json` sidecar, so OASIS metadata is portable.

#### What is already built in UDB (as of 2026-08-01)

| File | Status | What it does |
|------|--------|-------------|
| `OGEditorSDK/OGAssetCatalog.cs` | ✅ Done | Canonical ~140-asset catalog across all 10 OGames (SDK, used by all editors) |
| `OGEditorSDK/OGMapSidecar.cs` | ✅ Done | `oasis_{mapname}.json` reader/writer — SDK version usable by any editor |
| `OGEditorSDK/OGStarApiClient.cs` | ✅ Done | HTTP client for STAR API (/api/quests, /api/games, /api/portals, …) |
| `OGEditorSDK/OGEntityMappings.cs` | ✅ Done | Bidirectional classname ↔ OASIS thing type lookup (Q1/Q2/Q3/Duke/Wolf) |
| `OGEditorSDK/Native/ogeditor_api.h` | ✅ Done | C ABI header for C++ editor plugins (same pattern as ogengine.h) |
| `OGEditorSDK/Native/NativeExports.cs` | ✅ Done | NativeAOT exports → ogeditor_api.dll |
| `Plugins/UDBScript/Controls/OGEnginePanel.cs` | ✅ Done | Asset browser — live catalog from STAR API, falls back to OGAssetCatalog offline; category filter, config UI |
| `Plugins/UDBScript/Controls/OGQuestWeaverPanel.cs` | ✅ Done | Quest Weaver — fetches quests from STAR API, binds objectives to sector/thing/linedef/script triggers |
| `Plugins/UDBScript/Controls/OASISPortalPanel.cs` | ✅ Done | Portal placement UI — picks destination game/map/coords, writes sidecar on placement |
| `Plugins/UDBScript/OASISMapConverter.cs` | ✅ Done | Bidirectional entity conversion: OQUAKE↔ODOOM, OQUAKE2↔ODOOM, OQUAKE3→ODOOM, ODUKE3D→ODOOM |
| `Plugins/UDBScript/OASISMapSidecar.cs` | ✅ Done | Reads/writes `oasis_{mapname}.json` sidecar (portals + cross-game entities) |
| `Tools/ExtractOquakeSprites/` | ✅ Done | Extracts OQUAKE thing sprites from pak0.pak for UDB thing icon display |
| Improved sprite extraction | 🔜 Next | 3D render Quake MDL models instead of cropping MDL skin texture (fixes "rough sprites") |

#### OGEngine Editor architecture

```
┌───────────────────────────────────────────────────────────────────────────┐
│  OGEngine Editor (Ultimate Doom Builder — primary OASIS host)             │
│                                                                           │
│  ┌─────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐ │
│  │  3D Map View    │  │ OGAsset Browser  │  │ OASIS Portal Editor      │ │
│  │  (brush editor) │  │ (all 10 OGames)  │  │ (target game/map/pos)    │ │
│  └─────────────────┘  └──────────────────┘  └──────────────────────────┘ │
│  ┌─────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐ │
│  │ Entity Browser  │  │ Quest Weaver     │  │ Cross-game Spawn Table   │ │
│  │ (native + OASIS)│  │ (assign quest    │  │ (per-map entity list)    │ │
│  │                 │  │  objectives to   │  │                          │ │
│  │                 │  │  map triggers)   │  │                          │ │
│  └─────────────────┘  └──────────────────┘  └──────────────────────────┘ │
│                                                                           │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  OASIS Layer (C# process, communicates via named pipe)             │  │
│  │  • Live STAR API connection (OGAsset catalog, quests, teleporters) │  │
│  │  • Sidecar file reader/writer (oasis_{mapname}.json)               │  │
│  │  • Portal registry (map all teleporter endpoints)                  │  │
│  │  • Entity resolution (cross_game_id → native classnames)           │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
         ↓ saves                           ↓ saves
  [native .map/.wad]            [oasis_{mapname}.json sidecar]
```

#### OGEngine Editor entity definition extension

For Q-engine games, add to each game's `.fgd` / `.def` file (used by TrenchBroom/NetRadiant companion):

```
// OASIS cross-game portal
@PointClass = oasis_portal : "OASIS cross-game teleporter"
[
    target_game(string) : "Target OGame ID (e.g. OQUAKE, ODOOM)"
    target_map(string)  : "Target map name"
    spawn_x(float)      : "Spawn X in target map" : 0
    spawn_y(float)      : "Spawn Y in target map" : 0
    spawn_z(float)      : "Spawn Z in target map" : 64
    label(string)       : "Portal label (shown in HUD)"
]

// OASIS cross-game entity spawn
@PointClass = oasis_spawn : "Cross-game entity (monster/item)"
[
    oasset_id(string)   : "OGAsset catalog ID (e.g. oasset_quake_shambler)"
    spawn_flags(integer): "Native spawn flags for target game"
]
```

---

### 4.5 Native In-Game HUD Overlay

The **native HUD overlay** is the neon-blue panel that drops down when you press `I` inside any OGame — without the Unity HUB being visible. This is different from the existing `SharedHudOverlay.cs` (which is the Unity layer's overlay).

**Status:** Each game already has a basic in-game overlay (inventory popup + quest popup + beamed-in status + XP) from the `*_ogengine_integration.c/cpp` files. But it is minimal — text drawn with the game's own font API.

**What's needed:** A richer, neon-blue "OASIS Control Center" panel drawn natively in each game, matching the style of `SharedHudOverlay.cs` but rendered by the game's own 2D draw path.

**Architecture options:**

Option A: **Each game's integration renders a full native panel** (complex per-game, consistent with native rendering)  
Option B: **Unity HUD is always-on-top** (simpler, single implementation — since the Unity process embeds the game window as a child, the Unity canvas renders on top of the game via `sortingOrder: 9999`)

**Recommendation: Option B — Unity HUD is the primary overlay.**

Since `GameProcessHostService` embeds the game window as a `WS_CHILD` of the Unity window, the Unity canvas overlay (`SharedHudOverlay`) already renders on top of the embedded game window. The game's own in-game overlay (built per-game) is a **fallback** for when the player runs the game standalone (outside the HUB).

Action items:
- Verify the Unity overlay renders correctly on top of all embedded game windows (especially OpenGL/Vulkan fullscreen modes — may need `borderless windowed` flag for each game)
- Each game's native in-game overlay (already partially built) stays as the standalone fallback

---

### 4.6 Cross-Game Quests / Missions / Story

**Status:** The quest system backbone is complete (WEB5 STAR API + OGEngineClient + per-game `ogengine_complete_quest_objective` hooks). Cross-game quest seeding is demonstrated in `OGEngineClient/TestProjects/DemoQuestSeed`.

**What's needed:**

1. **Quest Weaver tool** (part of OGEngine Editor, §4.4) — author quests that span multiple games, assigning objectives to specific maps and map regions.

2. **Cross-game objective triggers** — when a map trigger fires (player enters a room, kills a specific monster), the game calls `ogengine_complete_quest_objective(quest_id, objective_id, game_source)`. The STAR API then:
   - Marks the objective complete
   - Unlocks the next objective
   - Optionally sends a cross-game spawn event (e.g. spawns an enemy wave in the next game as part of the story)
   - Optionally sends a cross-game teleport hint (shows "portal has appeared" toast in HUD)

3. **Narrative delivery** — quest objectives with `GeoHotSpotType.Text` or `GeoHotSpotType.Audio` deliver in-world story beats. The game integration fetches the linked hotspot and:
   - Displays a scrolling text panel (neon blue, HUD overlay)
   - Plays audio via the game's sound API

4. **Infinite story seed format** — a JSON schema for defining multi-game story arcs:

```json
{
  "story_id": "oasis_arc_001",
  "title": "The Dimensional Rift",
  "chapters": [
    {
      "game": "ODOOM",
      "map": "E1M3",
      "trigger": "kill_cyberdemon",
      "reward": "open_portal_to_oquake",
      "narration": "The demon's death tears a rift in space-time..."
    },
    {
      "game": "OQUAKE",
      "map": "e2m3",
      "trigger": "collect_rune",
      "cross_spawn": { "game": "ODOOM", "entity": "cacodemon", "count": 3 },
      "narration": "As you grasp the rune, the darkness follows..."
    }
  ]
}
```

5. **Bot/social handoffs** — `ExternalHandoffUri` fields in quest objectives already support Telegram/Discord/WhatsApp bot URLs and OPortal deep links. The OGEngine Editor's Quest Weaver panel exposes these as "cross-platform objective" nodes.

---

### 4.7 OQuake2, OQuake2-RTX, OQuake3 Integrations

(See `BEST_PORTS_AND_EDITORS.md` and the port selection summary for base ports.)

Architecture mirrors existing ports:

**OQuake2 / OQuake2-RTX** (Yamagi Q2 / Q2 RTX):
- Game logic lives in `game.dll` / `game.so` (same DLL-based model as ODOOM3)
- Hook points: `G_InitGame` / `G_ShutdownGame` / `G_RunFrame` / `SpawnEntities` / `Use` (item pickup) / `Think` (door access)
- Key mapping: Q2 uses `item_key_red`, `item_key_blue` → OASIS `red_key`, `blue_key`
- Q2 monsters: Gunner, Berserker, Gladiator, Tank Commander, etc. — 30 monsters + bosses (Makron, Jorg, Brain)

**OQuake3** (Quake3e):
- Game logic in `qagame.dll` / `cgame.dll` / `ui.dll` QVM or native
- Hook points: `vmMain` with `GAME_INIT`, `GAME_SHUTDOWN`, `GAME_RUN_FRAME`, item pickup entity touch functions
- Q3 has no traditional keys (all areas open) but has power-ups (Quad, BFH, etc.) that map to OASIS power-ups
- Q3 monsters: bots are AI entities (Sarge, Grunt, Keel, etc.) — can treat them as "monsters" for XP

---

### 4.8 STAR API Extensions Needed

| Feature | New endpoint | Notes |
|---------|-------------|-------|
| Cross-game teleport | `POST /api/teleport` | Source + target game/map/position |
| Teleport poll | `GET /api/teleport/pending?avatarId=...` | For target game to check on load |
| OGAsset catalog | `GET /api/oassets` | All cross-game entities |
| Map entity list | `GET /api/maps/{game}/{map}/entities` | Cross-game entities placed in a map |
| Save map entities | `PUT /api/maps/{game}/{map}/entities` | From OGEngine Editor |
| Cross-game spawn push | `POST /api/spawn-events` | Push entity spawn into a live game |
| Poll spawn events | `GET /api/spawn-events/pending?game=...&avatarId=...` | Game polls on tick |
| Story arc | `GET/POST /api/stories` | Multi-game narrative arcs |
| Portal registry | `GET/POST /api/portals` | All teleporter endpoints, shown in OGEngine Editor |

---

## 5. Build Roadmap

### Phase 1 — Hub Expansion (low effort, high visibility)
- [ ] Add 8 more game configs to `omniverse_host_config.json`
- [ ] Test all 10 portals in the Unity Hub
- [x] Build and verify OQuake2, OQuake2-RTX, OQuake3 game integrations

### Phase 2 — Cross-Game Teleportation
- [ ] Add `ogengine_request_teleport` / `ogengine_poll_teleport_request` to `ogengine.h`
- [ ] Implement in OGEngineClient (write/poll teleport JSON via IPC file or named pipe)
- [ ] Implement `OmniverseKernel` FileSystemWatcher for teleport requests
- [ ] Add `oasis_portal` entity to each OGame's integration
- [ ] Test ODOOM → OQuake portal

### Phase 3 — OGAsset Catalog + Cross-Game Entities
- [ ] Design OGAsset catalog schema (JSON + STAR API endpoint)
- [ ] Seed catalog with all monsters/keys/weapons from all 10 OGames
- [ ] Add `ogengine_get_map_entities` / `ogengine_poll_spawn_event` to C API
- [ ] Implement per-game `OGame_STAR_SpawnCrossGameEntity` function
- [ ] Test: Quake Shambler spawning in ODOOM map

### Phase 4 — OGEngine Editor (MVP) — UDB-based ✅ foundation done
- [x] UDB OASIS plugin foundation: OGEnginePanel (all 10 OGames), OASISPortalPanel, OASISMapConverter, OASISMapSidecar
- [x] OGEditorSDK: OGAssetCatalog, OGMapSidecar, OGStarApiClient, OGEntityMappings, C ABI header + NativeAOT wrapper
- [x] OGEnginePanel refactored to use OGAssetCatalog.ForGame() + category filter (drops inline asset list)
- [x] Quest Weaver panel in UDB (drag objectives onto map triggers, write to STAR API)
- [x] Live STAR API asset catalog panel (replaces hardcoded list with server-driven data)
- [ ] Improved Quake sprite extraction (render MDL from fixed angle vs. cropping skin)
- [ ] Add `oasis_portal` and `oasis_spawn` entity definitions to all game `.fgd` / `.def` files
- [ ] Companion editor launch from UDB (TrenchBroom for Q1/Q2, NetRadiant for Q3, DarkRadiant for D3)
- [ ] STARNET Quest Builder embedded via WebView2 in UDB (optional hybrid UX)
- [ ] Test: place a portal in a Doom map, walk through it in ODOOM → appear in OQuake

### Phase 5 — Quest Weaver + Infinite Story
- [ ] Add Quest Weaver panel to OGEngine Editor
- [ ] Implement story arc JSON schema and STAR API endpoints
- [ ] Implement `GeoHotSpotType.Text/Audio` narration in game integrations
- [ ] Write first cross-game story arc spanning ODOOM → OQuake → OWolf3D

### Phase 6 — Native HUD Polish
- [ ] Verify Unity overlay renders on top of all 10 embedded games (borderless windowed)
- [ ] Enrich per-game native fallback HUD (for standalone mode)
- [ ] Add "Friends" tab to SharedHudOverlay (clan chat, item gifting)
- [ ] Add "Teleport" tab to SharedHudOverlay (jump to any map in any game)

---

## 6. OGEngine Editor: Build Plan (UDB-based)

> **Updated 2026-08-01.** Original plan said "Fork TrenchBroom". Replaced with UDB-based plan
> because significant UDB OASIS integration is already in place at `C:\Source\UltimateDoomBuilder`.

### Step 1 — ✅ DONE: UDB OASIS Plugin Foundation

Already built:

```
Source/OGEditorSDK/
  OGEditorSDK.csproj        — .NET Standard 2.0, no external deps, used by UDB + C++ editors
  OGAssetCatalog.cs         — canonical ~140-asset catalog across all 10 OGames
  OGMapSidecar.cs           — oasis_{mapname}.json reader/writer (SDK version)
  OGStarApiClient.cs        — HTTP client for STAR API
  OGEntityMappings.cs       — Q1/Q2/Q3/Duke/Wolf classname ↔ OASIS thing type tables
  Native/ogeditor_api.h     — C ABI header for C++ editor plugins
  Native/NativeExports.cs   — NativeAOT [UnmanagedCallersOnly] → ogeditor_api.dll

Source/Plugins/UDBScript/
  Controls/OGEnginePanel.cs    — Asset browser (uses OGAssetCatalog.ForGame() + category filter)
  Controls/OASISPortalPanel.cs  — Portal placement: destination game/map/coords picker
  OASISMapConverter.cs          — Entity conversion: OQUAKE↔ODOOM, OQUAKE2↔ODOOM, OQUAKE3→ODOOM, Duke→ODOOM
  OASISMapSidecar.cs            — oasis_{mapname}.json reader/writer (UDB-side, references OGEditorSDK)
```

### Step 2 — Quest Weaver Panel (next)

New UDB docker panel `OASISQuestWeaverPanel.cs`:
- Tree view of cross-game quests fetched from STAR API (`/api/quests`)
- Drag objectives onto map triggers (linedef specials / thing tags)
- Generates `oasis_objective_trigger` linedefs with OASIS metadata
- Saves objective → trigger binding to the sidecar file

### Step 3 — Live STAR API Asset Catalog Panel

New UDB docker panel `OASISCatalogPanel.cs`:
- Connects to `STAR_BASE_URL/api/assets` — live OGAsset catalog
- Replaces the hardcoded asset list in `OGEnginePanel.cs` with server-driven data
- Still works offline (falls back to last-cached catalog)

### Step 4 — Improved Quake Sprites

The current `ExtractOquakeSprites` tool (`Tools/ExtractOquakeSprites/`) extracts MDL skin
textures (which pack front+back side-by-side — cropping to left half gives "rough sprites").

Fix: render each MDL model from a fixed front-facing angle using a software rasteriser
(or a headless OpenGL context), producing a true sprite-sheet view for each Quake monster.
Reference: vkQuake's MDL loader code or GLQuake's `GL_DrawAliasModel`.

### Step 5 — Companion Editor Launch (TrenchBroom / NetRadiant / DarkRadiant)

Add a UDB STAR menu entry: "Edit in native editor…"

- **OQUAKE / OQUAKE2 maps** → launches TrenchBroom with the exported `.map` file
- **OQUAKE3 maps** → launches NetRadiant-custom
- **ODOOM3 / ODOOM3-BFG maps** → launches DarkRadiant (`C:\Source\ODOOM3-Editor`)

All companions write the same `oasis_{mapname}.json` sidecar — UDB reads it back on next open.

### Step 6 — Format Support Summary

| Format | UDB status | Companion | Notes |
|--------|-----------|-----------|-------|
| Doom WAD | ✅ Native | — | Primary use case |
| Quake .map | ✅ Import/export via converter | TrenchBroom | OASISMapConverter converts entities; geometry via TB |
| Quake 2 .map | ✅ Import/export via converter | TrenchBroom | Same pipeline |
| Quake 3 .map | ✅ Entity import | NetRadiant | Curves must be made in NetRadiant |
| Duke3D BUILD | 🔜 Entity import (actor list) | Mapster32 | BUILD geometry via Mapster32 |
| Wolf3D (ECWolf) | 🔜 DECORATE actor import | ECWolf editor | Grid maps; no brush geometry |
| Doom 3 .map | 🔜 Companion launch | DarkRadiant | id4 brush format |

### Step 7 — Entity Definitions

For companion editors (TrenchBroom, NetRadiant), add OASIS entities to each game's `.fgd` / `.def`:
- `oasis_portal`: cross-game teleporter (target game/map/position)
- `oasis_spawn`: cross-game entity spawn point (OGAsset catalog ID)
Auto-populated from the live OGAsset catalog panel (Step 3) or the `oasis_star_assets.json` local cache.

---

## 7. Cross-Game Compatibility Map

The table below shows which item types are already cross-game mapped (from `oasisstar.json` files) and what still needs mapping:

| Item class | ODOOM | OQuake | ODOOM3 | ODOOM3-BFG | ODuke3D | ODuke3D-RT | OWolf3D | OQ2 | OQ2-RT | OQ3 |
|------------|-------|--------|--------|------------|---------|------------|---------|-----|--------|-----|
| Keys | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | N/A |
| Monster XP | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🔜 | 🔜 | 🔜 |
| Weapons | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 |
| Ammo | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 |
| Power-ups | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 |
| Cross-spawn | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 |
| In-map portals | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 | 🔜 |

---

## 8. Related Documents

| Document | What it covers |
|----------|---------------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Three-layer OGEngine architecture (game → C ABI → OGEngineClient) |
| [OGEngine_Overview.md](OGEngine_Overview.md) | WEB4/WEB5 API overview, GeoHotSpots, quest handoff |
| [STAR_Quest_System_Developer_Guide.md](STAR_Quest_System_Developer_Guide.md) | Quest/objective API reference |
| [DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md) | Setup, build, config |
| [BEST_PORTS_AND_EDITORS.md](BEST_PORTS_AND_EDITORS.md) | Recommended source ports and map editors per game |
| [CROSS_GAME_POWERUP_WEAPON_MAP.md](CROSS_GAME_POWERUP_WEAPON_MAP.md) | Cross-game weapon/powerup equivalence table |
| OASIS Omniverse/README.md | Unity Hub shell: what's built, controls, config |

---

*Last updated: 2026-08-01 — OGEditorSDK created (OGAssetCatalog, OGMapSidecar, OGStarApiClient, OGEntityMappings, C ABI + NativeAOT); OGEnginePanel refactored to use SDK; full platform stack documented (STAR API, OPORTAL, STARNET, SmartBricks, hybrid Quest Builder)*
*Vision: NextGen World Ltd — "One Infinite World"*
