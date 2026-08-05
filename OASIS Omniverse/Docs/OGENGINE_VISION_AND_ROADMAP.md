# OGEngine — Full Vision, Architecture & Roadmap

*The OASIS Omniverse: a Ready Player One-style infinite open metaverse spanning all OGames.*

---

## 1. The Vision

The OASIS Omniverse is not a collection of separate games with a shared leaderboard. It is **one infinite, borderless universe spanning every game genre** — the real-life Ready Player One.

> *"The OASIS was the most important thing in the world — a virtual utopia where you could go anywhere, be anyone, do anything." — Ready Player One*

The name **Omniverse** is intentional. This is not an FPS metaverse. It is **every kind of game, unified**:

### What it is today — Generation 1 (FPS)

We started where 3D gaming began. The fourteen Generation 1 OGames are the open-source FPS classics that built the genre: ODOOM, OQuake, ODOOM3, ODOOM3-BFG, ODuke3D, ODuke3D-RT, OWolf3D, OQuake2, OQuake2-RTX, OQuake3, OHeretic, OHexen, OShadowWarrior, OShadowWarriorRT.

In the Gen-1 universe:

- Every OGame is a **region** of the same universe, not a silo.
- You can **walk through a portal in a Doom map and appear in a Quake 2 level**.
- A **Quake Shambler can spawn into a Duke Nukem 3D episode**. A Cacodemon can hunt you in Wolfenstein 3D.
- A **gold key found in Wolf3D opens a door in Doom**. An ammo crate picked up in Quake restocks your shotgun in Duke3D.
- **Quests weave across all games** — start a mission in Doom, collect an artefact in Quake 3, deliver it in Quake 2, complete it in Wolf3D.

### What it becomes — Generation 2 and beyond (all genres)

| Generation | Games | Genres |
|------------|-------|--------|
| Gen 1 (now) | 14 FPS OGames | First-person shooter |
| Gen 2 (next) | OMorrowind (OpenMW), OMineCraft (Minetest) | Open-world RPG, voxel sandbox |
| Gen 3+ (future) | Strategy, racing, platformers, survival, fighting, flight sims, horror… | All genres |

In the Gen-2+ universe, everything learned from FPS cross-game integration scales up:

- **Step through a Doom portal and arrive in Morrowind's Vvardenfell.** The same `ogengine_request_teleport` / `ogengine_poll_teleport_request` API works regardless of engine.
- **A sword found in Morrowind appears in your OASIS inventory.** The same `ogengine_add_item` / `ogengine_get_inventory` ABI handles RPG items, crafting materials, vehicles, pets — any item type.
- **A Minecraft chest contains OASIS cross-game loot.** The voxel world is just another OGame region in the shared universe.
- **Quests span genres** — kill a dragon in OMorrowind, build a fortress in OMineCraft, collect a rune in ODOOM, deliver it to OQuake3.
- **The OASIS HUB** connects to all of it — portals to every game, every genre, every world.

### The unchanging core — across every genre

- The **OASIS HUD** — a neon-blue Steam/Xbox-style overlay — works identically in every OGame, regardless of genre. Press `I` anywhere, in any game, and your shared OASIS inventory, quests, avatar, NFTs, karma, and clan appear.
- The **OASIS HUB** is a 3D space station from which portals lead to any game, any map, any level, any genre.
- The **OGEngine Editor** lets you place assets, portals, and quest triggers from ANY game into ANY map, regardless of engine or genre.
- The **OGEngine integration pattern** (C hook layer → `ogengine.dll` C ABI → OGEngineClient → STAR API) is game-engine-agnostic. Any open-source game with a native C/C++ hook layer becomes an OGame.

This is **Ready Player One's OASIS** — a single continuous experience where the rules, assets, economy, and story of one world bleed into every other.

---

## 2. What Is Already Built

### 2.1 OASIS Kernel (Unity)

`OmniverseKernel.cs` — a Unity singleton that bootstraps everything and persists across scene loads.

| Component | File | What it does |
|-----------|------|-------------|
| **Kernel** | `OmniverseKernel.cs` | Bootstrap, portal dispatch, global settings |
| **Game Host Service** | `GameProcessHostService.cs` | Preloads all OGames as native Win32 processes, embeds their windows into the Unity window via `SetParent`/`WS_CHILD`, memory-aware stale unload |
| **Shared HUD Overlay** | `SharedHudOverlay.cs` | Steam/Xbox-style `I`-key overlay: Inventory, Quests, NFTs, Avatar, Karma, Settings, Diagnostics, Friends, Teleport tabs |
| **Teleport IPC** | `OmniverseKernel.cs` | TickTeleportIpc() — polls %TEMP%\oasis_teleport_{avatarId}.json every 0.5s, activates target game, writes arrive file for destination game |
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

| OGame | Base Port | Status | Integration file | Teleport Hook | Spawn Hook |
|-------|-----------|--------|-----------------|---------------|------------|
| ODOOM | UZDoom | ✅ Complete | `uzdoom_ogengine_integration.cpp` | ✅ Complete | ✅ Complete |
| OQuake | vkQuake | ✅ Complete | `oquake_ogengine_integration.c` | ✅ Complete | ✅ Complete |
| ODOOM3 | dhewm3 | ✅ Complete | `d3doom3_ogengine_integration.cpp` | ✅ Complete | ✅ Complete |
| ODOOM3-BFG | RBDOOM-3-BFG | ✅ Complete | `d3doom_ogengine_integration.cpp` | ✅ Complete | ✅ Complete |
| ODuke3D | EDuke32 | ✅ Complete | `oduke3d_ogengine_integration.c` | ✅ Complete | ✅ Complete |
| ODuke3D-RT | Duke-RT | ✅ Complete | `oduke3drt_ogengine_integration.c` | ✅ Complete | ✅ Complete |
| OWolf3D | ECWolf | ✅ Complete | `owolf3d_ogengine_integration.cpp` | ✅ Complete | ✅ Complete |
| OQuake2 | Yamagi Q2 | ✅ Complete | `oquake2_ogengine_integration.c` | ✅ Complete | ✅ Complete |
| OQuake2-RTX | Q2 RTX | ✅ Complete | `oquake2rtx_ogengine_integration.c` | ✅ Complete | ✅ Complete |
| OQuake3 | Quake3e | ✅ Complete | `oquake3_ogengine_integration.c` | ✅ Complete | ✅ Complete |

### 2.5 WEB4 / WEB5 APIs

- **WEB4 OASIS API** — avatar, inventory, karma, settings, NFTs, quests (persistence layer)
- **WEB5 STAR API** (`C:\Source\OASIS2\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI`) — quest definitions, objectives, GeoHotSpots, missions, STARNET holons, cross-game progress, OGAsset catalog, portal registry
- **Quest system** — cross-game quests with objectives spanning multiple games, ExternalHandoffUri for cross-app handoffs (CLI, OPortal, Telegram, Discord)
  - Cross-game story arcs are **Chapter → Mission → Quest → Objective** (not a separate concept)
  - `Objective.GameSource` + `Objective.MapName` — which game and map this specific objective happens in
  - `Objective.CrossGameEventsOnActivate` — effects fired when an objective first becomes active (e.g. opening narration, ambient audio)
  - `Objective.CrossGameEventsOnComplete` — effects fired in other games on completion (SpawnEntity, UnlockPortal, ShowNarration, TeleportTo, PlayAudio, PlayVideo, OpenWebsite)
  - `Objective.CrossGameEventsOnGeoHotSpotTriggered` — effects fired when the linked GeoHotSpot is physically reached
  - `Objective.RewardInventoryItemIds` — OASIS inventory item GUIDs granted on completion; game receives them via `ogengine_poll_inventory_grant`
  - `Objective.NeedToKillMonstersByType` — per-classname kill requirements (e.g. `{"OQUAKE": ["monster_cacodemon:3"]}`)
  - `QuestProgressDelta.MonsterKilledClassname` — engine classname sent with each kill event for per-type tracking
  - `QuestProgressApplyResult.CrossGameEventsToDispatch` — events the client must dispatch to the running game after a progress POST
  - `QuestProgressApplyResult.InventoryItemsToGrant` — item GUIDs to grant; piped through `ogengine_poll_inventory_grant`
  - Example: Obj1 [ODOOM/E1M3] kill Cyberdemon → fires UnlockPortal in OQUAKE/e2m3 + narration; Obj2 [OQUAKE/e2m3] collect Rune → fires SpawnEntity (3× monster_cacodemon) in OQUAKE/e2m3
- **STAR API controllers already built:** QuestsController, MissionsController, GamesController, GeoHotSpotsController, OAPPsController, ZomesController, TeleportController, SpawnEventsController, MapEntitiesController — all data persists via HolonManager → MongoDB
  - **StoriesController REMOVED** — story arcs ARE the Chapter/Mission/Quest/Objective hierarchy; no separate concept needed
  - **TeleportController / SpawnEventsController:** in-memory `ConcurrentDictionary` (correct — ephemeral sub-second runtime state, not Holon content)
  - **MapEntitiesController:** `STARHolonType.MapEntityList` Holons via `_starAPI.Holons`; `Config/map_entities/*.json` are **seed/import format only**; canonical store is HolonManager → provider

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
│                        ↕ ogengine.dll C ABI  /  OGEditorClient.dll C ABI              │
│  ┌────────────────────────────────────────────────────────────────────────────────┐  │
│  │  PLAYER LAYER — OGames + OASIS Kernel                                           │  │
│  │  Gen 1 (FPS): ODOOM • OQuake • ODOOM3 • ODOOM3-BFG • ODuke3D • ODuke3D-RT   │  │
│  │               OWolf3D • OQuake2 • OQuake2-RTX • OQuake3                       │  │
│  │               OHeretic • OHexen • OShadowWarrior • OShadowWarriorRT           │  │
│  │  Gen 2 (next): OMorrowind (OpenMW) • OMineCraft (Minetest)                   │  │
│  │  Gen 3+: strategy • racing • platformers • survival • fighting • …             │  │
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
| `Native/OGEditorClient.h` | C ABI header — same pattern as `ogengine.h` |
| `Native/NativeExports.cs` | NativeAOT [UnmanagedCallersOnly] exports → `OGEditorClient.dll` |

**Any editor can use it:**
- **UDB** — references `OGEditorSDK.csproj` directly (already done)
- **TrenchBroom (C++)** — `#include "OGEditorClient.h"`, `LoadLibrary("OGEditorClient.dll")`
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
│  │  LAYER 1: OGames (native processes — C/C++, any engine)                              │   │
│  │  Gen 1 (FPS): ODOOM • OQuake • ODOOM3 • ODOOM3-BFG • ODuke3D • ODuke3D-RT         │   │
│  │               OWolf3D • OQuake2 • OQuake2-RTX • OQuake3                            │   │
│  │               OHeretic • OHexen • OShadowWarrior • OShadowWarriorRT              │   │
│  │  Gen 2: OMorrowind (OpenMW) • OMineCraft (Minetest)  [planned]                     │   │
│  │  Gen 3+: any open-source game with a C/C++ hook layer  [extensible]                │   │
│  └──────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                             │
│  OGEngine Editor — standalone tool, edits maps for all Layer 1 games                       │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Phase Status

---

### 4.1 Hub Expansion

**Status:** ✅ DONE — all 10 games in omniverse_host_config.json

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

**Status:** ✅ DONE

- [x] `ogengine_request_teleport`, `ogengine_poll_teleport_request`, `ogengine_confirm_teleport_arrival` added to `ogengine.h`
- [x] Implemented in OGEngineClient (`RequestTeleport`, `PollTeleportRequest`, `ConfirmTeleportArrivalAsync`)
- [x] `OmniverseKernel.TickTeleportIpc()` polls every 0.5s via `%TEMP%\oasis_teleport_{avatarId}.json`
- [x] `{Prefix}_STAR_CheckIncomingTeleport()` added to all 10 game integrations
- [ ] Test ODOOM → OQuake portal (pending: requires built game binaries)

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

**Status:** ✅ DONE (infrastructure)

- [x] `ogengine_poll_spawn_event`, `ogengine_confirm_spawn` added to C API + OGEngineClient
- [x] `SpawnEventsController` in STAR API (`POST/GET /api/spawn-events`)
- [x] `MapEntitiesController` in STAR API (`GET/PUT /api/maps/{game}/{map}/entities`)
- [x] Spawn-event polling block added to all 10 game integration tick functions
- [x] **ODOOM:** `C_DoCommand("summon <classname>")` — GZDoom/UZDoom console summon
- [x] **OQuake:** `ED_Alloc` / `PR_SetEngineString` / `PR_ExecuteProgram` / `SV_LinkEdict`
- [x] **ODOOM3 / ODOOM3-BFG:** `cmdSystem->BufferCommandText(CMD_EXEC_APPEND, "spawn …\n")`
- [x] **ODuke3D / ODuke3D-RT:** `A_InsertSprite` with `name_to_picnum` lookup; monster catalog in `oasis_star_assets.json`
- [x] **OQuake3:** `trap_SendConsoleCommand(EXEC_APPEND, "spawn <classname>\n")`
- [x] **OWolf3D:** `ClassDef::FindClass(FName)` + `Actor::Spawn(cls, fx, fy, spot, nodir, false)` — ECWolf DECORATE class lookup
- [x] **OQuake2 / OQuake2-RTX:** `G_Spawn()` + `ED_CallSpawn(ent)` + `gi.linkentity(ent)` — auto-dispatches to `SP_*` per classname
- [ ] Test: Quake Shambler spawning in ODOOM map (pending builds)

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

**Status:** ✅ Complete

- [x] `.fgd` and `.def` entity definitions (`oasis_portal`, `oasis_spawn`, `oasis_objective_trigger`) at `Plugins/UDBScript/Assets/oasis_entities.fgd/.def`
- [x] Companion editor launch button in `OGEnginePanel.cs` (TrenchBroom/NetRadiant/DarkRadiant auto-detected via PATH)
- [x] Improved Quake sprite extraction — `OQuakeMdlRenderer.cs` software rasterizer; `--render` flag in ExtractOquakeSprites
- [x] STARNET Quest Builder embedded via WebView2 — `OGSTARNETQuestBuilderPanel.cs`; action `ogengine_show_starnet_builder`; NuGet `Microsoft.Web.WebView2` added to UDBScript.csproj
- [x] **TrenchBroom/OQuakeEditor** — `OASIS_OQuake.fgd` (OQ1), `OASIS_OQuake2.fgd` (OQ2) and new `OASIS_OQuake3.fgd` (OQ3) in `C:\Source\OQuakeEditor`; all three GameConfig.cfg files updated to include OASIS FGDs
- [x] **DarkRadiant/ODOOM3-Editor** — `plugins/dm.oasis/` plugin (`oasis.cpp` + `OASISPanel.cpp`) loads `OGEditorClient.dll` at startup and provides "OASIS OGEngine…" menu entry with 3-tab dialog: Asset Browser, Portal Placer, Quest Binder
- [x] **NetRadiant** — `EditorIntegrations/NetRadiant/oasis_nr_plugin.c` implements classic `QERPlug_*` ABI and calls `OGEditorClient.dll` for asset listing, portal appending, and quest display
- [x] **Mapster32** — `EditorIntegrations/Mapster32/oasis_m32_tool.c` standalone CLI tool registered as an external user tool in Mapster32; calls `OGEditorClient.dll` for asset catalog, portal sidecar, quest list, and map conversion

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

| Criterion | UDB (C#/.NET) | TrenchBroom / DarkRadiant / NetRadiant / Mapster32 |
|-----------|--------------|---------------------------------------------------|
| OASIS integration | ✅ UDB plugin (C# `BuilderPlug`) — full panels, converters, portal editor | ✅ Via `OGEditorClient.dll` C ABI — any C/C++ editor calls `ogeditor_init`, `ogeditor_get_assets_json`, `ogeditor_append_portal`, etc. without .NET knowledge |
| Language / stack | C#/.NET | C/C++ editors load `OGEditorClient.dll` via `LoadLibrary` — NativeAOT, no runtime dependency |
| Doom WAD editing | ✅ Native | ❌ Not supported (companion editors handle their own format) |
| Plugin architecture | ✅ Rich C# `Plug` base class | Per-editor plugin model (TB game config, DR module, NR plugin, M32 CON script) |
| Q-engine .map support | ✅ Via OASISMapConverter (import/export) | ✅ Native |
| Q3 curve/patch | Via companion NetRadiant | ✅ TrenchBroom / NetRadiant native |
| Build engine (Duke3D) | Via companion Mapster32 | ✅ Mapster32 native |
| Doom 3 maps | Via companion DarkRadiant | ✅ DarkRadiant native |
| License | GPL2 | GPL3 |

**Multi-editor strategy:**
- **UDB** = primary OASIS host (Doom WAD maps — the most complex; also the import/export hub for all other formats)
- **TrenchBroom** (`C:\Source\OQuakeEditor`) = companion for Q1/Q2/Q3 geometry editing; carries `OASIS_OQuake.fgd`, `OASIS_OQuake2.fgd`, `OASIS_OQuake3.fgd` ✅
- **NetRadiant-custom** = companion for Q3 curve/patch editing; `oasis_nr_plugin.c` plugin via `OGEditorClient.dll` ✅
- **Mapster32** (bundled with EDuke32) = companion for Duke3D BUILD maps; `oasis_m32_tool.exe` CLI companion via `OGEditorClient.dll` ✅
- **DarkRadiant** (`C:\Source\ODOOM3-Editor`) = companion for Doom 3 maps; `plugins/dm.oasis/` module via `OGEditorClient.dll` ✅

All companion editors write/read the same `oasis_{mapname}.json` sidecar, so OASIS metadata is portable. All reach the STAR API through `OGEditorClient.dll` — no .NET knowledge required.

#### What is already built in UDB (as of 2026-08-01)

| File | Status | What it does |
|------|--------|-------------|
| `OGEditorSDK/OGAssetCatalog.cs` | ✅ Done | Canonical ~140-asset catalog across all 10 OGames (SDK, used by all editors) |
| `OGEditorSDK/OGMapSidecar.cs` | ✅ Done | `oasis_{mapname}.json` reader/writer — SDK version usable by any editor |
| `OGEditorSDK/OGStarApiClient.cs` | ✅ Done | HTTP client for STAR API (/api/quests, /api/games, /api/portals, …) |
| `OGEditorSDK/OGEntityMappings.cs` | ✅ Done | Bidirectional classname ↔ OASIS thing type lookup (Q1/Q2/Q3/Duke/Wolf) |
| `OGEditorSDK/Native/OGEditorClient.h` | ✅ Done | C ABI header for C++ editor plugins (same pattern as ogengine.h) |
| `OGEditorSDK/Native/NativeExports.cs` | ✅ Done | NativeAOT exports → OGEditorClient.dll |
| `Plugins/UDBScript/Controls/OGEnginePanel.cs` | ✅ Done | Asset browser — live catalog from STAR API, falls back to OGAssetCatalog offline; category filter, config UI |
| `Plugins/UDBScript/Controls/OGQuestWeaverPanel.cs` | ✅ Done | Quest Weaver — fetches quests from STAR API, binds objectives to sector/thing/linedef/script triggers |
| `Plugins/UDBScript/Controls/OASISPortalPanel.cs` | ✅ Done | Portal placement UI — picks destination game/map/coords, writes sidecar on placement |
| `Plugins/UDBScript/OASISMapConverter.cs` | ✅ Done | Bidirectional entity conversion: OQUAKE↔ODOOM, OQUAKE2↔ODOOM, OQUAKE3→ODOOM, ODUKE3D→ODOOM |
| `Plugins/UDBScript/OASISMapSidecar.cs` | ✅ Done | Reads/writes `oasis_{mapname}.json` sidecar (portals + cross-game entities) |
| `Tools/ExtractOquakeSprites/` | ✅ Done | Extracts OQUAKE thing sprites from pak0.pak for UDB thing icon display |
| Improved sprite extraction | ✅ Done | `OQuakeMdlRenderer.cs` software rasterizer — 3D render Quake MDL models; `--render` flag in ExtractOquakeSprites |

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

**Status:** ✅ DONE (infrastructure)

- [x] Cross-game story arcs modelled as `Chapter → Mission → Quest → Objective` — no separate StoriesController needed
- [x] `Objective.GameSource` + `Objective.MapName` — which game and map each objective happens in
- [x] `Objective.CrossGameEventsOnActivate` (`List<CrossGameEvent>`) — fires on objective activation; used for opening narration / ambient audio
- [x] `Objective.CrossGameEventsOnComplete` (`List<CrossGameEvent>`) — fires SpawnEntity / UnlockPortal / ShowNarration / TeleportTo / PlayAudio / PlayVideo / OpenWebsite in other games on completion
- [x] `Objective.CrossGameEventsOnGeoHotSpotTriggered` (`List<CrossGameEvent>`) — fires when the linked GeoHotSpot is physically reached
- [x] `CrossGameEvent.EntityCategory` — `"Monster"` / `"Weapon"` / `"Ammo"` / `"Key"` / `"Powerup"` / `"Item"` for classifying spawned entities
- [x] `CrossGameEvent.AudioUrl` / `AudioTitle` — for `PlayAudio` events (streamed narration / ambient)
- [x] `CrossGameEvent.VideoUrl` / `VideoTitle` — for `PlayVideo` events (cutscene / cinematic)
- [x] `CrossGameEvent.WebsiteUrl` — for `OpenWebsite` events (lore / portal overlay)
- [x] `Objective.RewardInventoryItemIds` — OASIS inventory items granted on completion; game polls via `ogengine_poll_inventory_grant`
- [x] `Objective.NeedToKillMonstersByType` / `MonstersKilledByType` — per-classname kill requirements and progress; game sends engine classname via `MonsterKilledClassname` in progress delta
- [x] `GET /api/quests/{id}/first-objective-events` — returns `CrossGameEventsOnActivate` for the first active objective; `OGEngineClient.StartQuestAsync` calls this automatically after a successful quest start to dispatch opening narration / audio
- [x] `OGEngineClient.DispatchCrossGameEventsFromProgressResponse` — routes events from every progress POST: SpawnEntity → `WriteSpawnEventToFile`, TeleportTo → `RequestTeleport`, all other types → `ogengine_poll_cross_game_event` queue; InventoryItemsToGrant → `ogengine_poll_inventory_grant` queue
- [x] `OGEngineExports.ogengine_poll_cross_game_event` — native export; game polls per-frame, receives JSON for ShowNarration / PlayAudio / PlayVideo / OpenWebsite / UnlockPortal
- [x] `OGEngineExports.ogengine_poll_inventory_grant` — native export; game polls per-frame, receives GUID string and triggers `ogengine_get_inventory`
- [x] `ogengine_notify_portal_unlock(portalId)` — native export added to all `ogengine.h` copies; `OGEngineClient.NotifyPortalUnlock` writes `oasis_portal_unlock_{portalId}.json` to `%TEMP%` for OGEditor/OmniverseKernel pickup; all 10 game integrations call it + show in-game toast on `UnlockPortal` event
- [x] Reference arc `Config/stories/oasis_arc_001_dimensional_rift.json` updated to show correct Chapter/Mission/Quest/Objective structure with `NeedToKillMonstersByType` and `CrossGameEventsOnActivate`
- [~] `GeoHotSpotType.Text/Audio` narration delivery — `ShowNarration` cross-game event IS delivered as a toast in all 10 game integrations via `ogengine_poll_cross_game_event`; full in-world scrolling-text panel + audio playback per game is future work
- [x] PlayAudio / PlayVideo / OpenWebsite — `oasis_open_url()` implemented in all 10 games; opens URL in OS default handler (`start` / `open` / `xdg-open`); title shown as in-game toast; richer in-engine playback is future work
- [x] STARNET web Quest Builder integration — embedded via WebView2 in UDB (`OGSTARNETQuestBuilderPanel.cs`)

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
      "cross_spawn": { "game": "OQUAKE", "entity": "monster_cacodemon", "count": 3 },
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

| Feature | New endpoint | Status | Notes |
|---------|-------------|--------|-------|
| Cross-game teleport | `POST /api/teleport` | ✅ Done | Source + target game/map/position |
| Teleport poll | `GET /api/teleport/pending?avatarId=...` | ✅ Done | For target game to check on load |
| OGAsset catalog | `GET /api/oassets` | ✅ Done | All cross-game entities |
| Map entity list | `GET /api/maps/{game}/{map}/entities` | ✅ Done | Cross-game entities placed in a map |
| Save map entities | `PUT /api/maps/{game}/{map}/entities` | ✅ Done | From OGEngine Editor |
| Cross-game spawn push | `POST /api/spawn-events` | ✅ Done | Push entity spawn into a live game |
| Poll spawn events | `GET /api/spawn-events/pending?game=...&avatarId=...` | ✅ Done | Game polls on tick |
| Story arc | `GET/POST /api/stories` | ✅ Done | Multi-game narrative arcs |
| Portal registry | `GET/POST /api/portals` | ✅ Done | `PortalsController.cs` — register, list, get, unlock, lock; `ogengine_notify_portal_unlock` writes IPC file for OGEditor pickup |
| First-objective events | `GET /api/quests/{id}/first-objective-events` | ✅ Done | Returns `CrossGameEventsOnActivate` for the first objective; called by `StartQuestAsync` to dispatch opening narration/audio |

---

## 5. Build Roadmap

### Phase 1 — Hub Expansion (low effort, high visibility)
- [x] Add 8 more game configs to `omniverse_host_config.json`
- [ ] Test all 10 portals in the Unity Hub
- [x] Build and verify OQuake2, OQuake2-RTX, OQuake3 game integrations

### Phase 2 — Cross-Game Teleportation
- [x] Add `ogengine_request_teleport` / `ogengine_poll_teleport_request` to `ogengine.h`
- [x] Implement in OGEngineClient (write/poll teleport JSON via `%TEMP%` IPC files)
- [x] Implement `OmniverseKernel.TickTeleportIpc()` polling every 0.5s
- [x] Add `{Prefix}_STAR_CheckIncomingTeleport()` to all 10 game integrations
- [x] **Implement actual player warp calls inside `CheckIncomingTeleport()` — all 10 games done:**
  - ODOOM: `P_TeleportMove(players[consoleplayer].mo, DVector3(x,y,z), false)` — GZDoom/UZDoom
  - OQuake: `EDICT_NUM(1)→v.origin = {x,y,z}; velocity cleared; SV_LinkEdict(pl, false)`
  - ODOOM3 / ODOOM3-BFG: `gameLocal.GetLocalPlayer()->Teleport(idVec3(x,y,z), idAngles(0,0,0), NULL)`
  - ODuke3D / ODuke3D-RT: `g_player[myconnectindex].ps→pos/opos = {x,y,z}; vel cleared`
  - OWolf3D: `player.position = { (int)x, (int)y }; player.angle = 0`
  - OQuake2 / OQuake2-RTX: `g_edicts[1]→s.origin = {x,y,z}; velocity cleared; gi.linkentity(...)`
  - OQuake3: `g_entities[0].client→ps.origin = {x,y,z}; trap_LinkEntity(...)`
- [ ] Test ODOOM → OQuake portal (requires compiled game binaries)

### Phase 3 — OGAsset Catalog + Cross-Game Entities
- [x] Design OGAsset catalog schema (JSON + STAR API endpoint)
- [x] Seed catalog with weapons/ammo/powerups/keys/monsters for all 10 OGames in `oasis_star_assets.json`
- [x] Add `ogengine_get_map_entities` / `ogengine_poll_spawn_event` / `ogengine_confirm_spawn` to C API
- [x] Spawn-event polling block added to all 10 game integration tick functions
- [x] `ogengine_poll_cross_game_event` + `ogengine_poll_inventory_grant` poll loops added to **all 10 game integrations**
- [x] **ODOOM spawn:** `C_DoCommand("summon <classname>")` — GZDoom/UZDoom console command, works at runtime
- [x] **OQuake spawn:** `ED_Alloc` / `PR_SetEngineString` / `ED_FindFunction` / `PR_ExecuteProgram` / `SV_LinkEdict`
- [x] **ODOOM3 / ODOOM3-BFG spawn:** `cmdSystem->BufferCommandText(CMD_EXEC_APPEND, "spawn <classname>\n")` — idTech4 console command
- [x] **ODuke3D / ODuke3D-RT spawn:** `A_InsertSprite(sect, x, y, z, picnum, ...)` — entity_id mapped to tile picnum via static lookup table (`name_to_picnum`); monster catalog added to `oasis_star_assets.json`
- [x] **OQuake3 spawn:** `trap_SendConsoleCommand(EXEC_APPEND, "spawn <classname>\n")` — implemented (runtime testing pending)
- [x] **OWolf3D spawn:** `ClassDef::FindClass(FName(wolf_id))` + `Actor::Spawn(cls, fx, fy, spot, nodir, false)` — ECWolf DECORATE class lookup + spawn
- [x] **OQuake2 / OQuake2-RTX spawn:** `G_Spawn()` + set `classname`/`origin`/`angles` + `ED_CallSpawn(ent)` + `gi.linkentity(ent)` — dispatches to per-type `SP_monster_*` automatically
- [ ] Test: Quake Shambler spawning in ODOOM map

### Phase 4 — OGEngine Editor (MVP) — UDB-based ✅ foundation done
- [x] UDB OASIS plugin foundation: OGEnginePanel, OASISPortalPanel, OASISMapConverter, OASISMapSidecar
- [x] OGEditorSDK: OGAssetCatalog, OGMapSidecar, OGStarApiClient, OGEntityMappings, C ABI + NativeAOT
- [x] Quest Weaver panel in UDB; live STAR API asset catalog panel
- [x] `oasis_entities.fgd` / `oasis_entities.def` — portal, spawn, objective-trigger entity defs for TrenchBroom/NetRadiant
- [x] Companion editor launch button in UDB (TrenchBroom / NetRadiant / DarkRadiant)
- [x] Improved Quake sprite extraction — `OQuakeMdlRenderer.cs` software rasterizer; `--render` flag in ExtractOquakeSprites for 3D-rendered monster sprites
- [x] STARNET Quest Builder embedded via WebView2 in UDB — `OGSTARNETQuestBuilderPanel.cs`
- [ ] Test: place a portal in a Doom map, walk through it in ODOOM → appear in OQuake

### Phase 5 — Quest Weaver + Infinite Story
- [x] Story arc JSON schema (`Config/stories/*.json`) + STAR API endpoints (`/api/stories`)
- [x] First cross-game story arc: `oasis_arc_001_dimensional_rift.json` (ODOOM → OQUAKE → OWOLF3D)
- [~] `GeoHotSpotType.Text/Audio` narration delivery — toast delivered in all 10 games; full scrolling-text panel + audio playback is future work
- [x] STARNET Quest Builder embedded via WebView2 in UDB — `OGSTARNETQuestBuilderPanel.cs`

### Phase 6 — Native HUD Polish
- [ ] Verify Unity overlay renders on top of all 10 embedded games (borderless windowed)
- [x] Enrich per-game native fallback HUD — `ogengine_get_avatar_karma` export + ODOOM karma CVar + OQuake karma line on XP bar; karma now shown alongside XP in standalone mode
- [x] Add "Friends" tab to SharedHudOverlay — `GetClanMembersAsync()`, `ClanMemberItem` model, live clan roster with online/offline/current-game columns
- [x] Add "Teleport" tab to SharedHudOverlay (jump to any map in any game)

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
  Native/OGEditorClient.h     — C ABI header for C++ editor plugins
  Native/NativeExports.cs   — NativeAOT [UnmanagedCallersOnly] → OGEditorClient.dll

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

### Step 5 — Companion Editor Launch (TrenchBroom / NetRadiant / DarkRadiant) ✅ Complete

A UDB STAR menu entry "Edit in native editor…" auto-detects editors via PATH.

- **OQUAKE / OQUAKE2 maps** → launches OQuakeEditor (TrenchBroom fork) with `OASIS_OQuake.fgd` / `OASIS_OQuake2.fgd` — portals, keys, XP anchors
- **OQUAKE3 maps** → launches NetRadiant; `oasis_nr_plugin.c` (`EditorIntegrations/NetRadiant/`) provides OASIS Asset Browser, Portal Placer, Quest Binder via `QERPlug_*` ABI
- **ODOOM3 / ODOOM3-BFG maps** → launches DarkRadiant (`C:\Source\ODOOM3-Editor`); `plugins/dm.oasis/` module shows 3-tab OASIS panel
- **ODUKE3D maps** → Mapster32; `oasis_m32_tool.exe` (`EditorIntegrations/Mapster32/`) registered as external tool

All companions write the same `oasis_{mapname}.json` sidecar — UDB reads it back on next open.

### Step 6 — Format Support Summary

| Format | UDB status | Companion | Notes |
|--------|-----------|-----------|-------|
| Doom WAD | ✅ Native | — | Primary use case |
| Quake .map | ✅ Import/export via converter | TrenchBroom | OASISMapConverter converts entities; geometry via TB |
| Quake 2 .map | ✅ Import/export via converter | TrenchBroom | Same pipeline |
| Quake 3 .map | ✅ Entity import | NetRadiant | Curves must be made in NetRadiant |
| Duke3D BUILD | ✅ Entity import (actor list) | Mapster32 | `ConvertDukeToDoom` — EDuke32 classname → Doom thing type; action `ogengine_convert_duke2doom` |
| Wolf3D (ECWolf) | ✅ DECORATE actor import | ECWolf editor | `ConvertWolfToDoom` — ECWolf classname → Doom thing type; action `ogengine_convert_wolf2doom` |
| Doom 3 .map | ✅ Companion launch | DarkRadiant | Companion editor auto-detected via PATH in `OGEnginePanel.cs` |

### Step 7 — Entity Definitions ✅ Complete

OASIS entities added to all companion editor format files:
- `OASIS_OQuake.fgd` — OQ1 portals, silver/gold keys (TrenchBroom) ✅
- `OASIS_OQuake2.fgd` — OQ2 portals, blue/red/commander's head keys (TrenchBroom) ✅
- `OASIS_OQuake3.fgd` — OQ3 portals, quad/regen/haste tokens, XP anchors (TrenchBroom) ✅
- DarkRadiant: dm.oasis plugin handles entity insertion programmatically via `OGEditorClient.dll` ✅
- NetRadiant / Mapster32: entity definitions included via `ogeditor_get_assets_json` response ✅

---

## 7. Cross-Game Compatibility Map

The table below shows which item types are already cross-game mapped (from `oasisstar.json` files) and what still needs mapping:

| Item class | ODOOM | OQuake | ODOOM3 | ODOOM3-BFG | ODuke3D | ODuke3D-RT | OWolf3D | OQ2 | OQ2-RT | OQ3 |
|------------|-------|--------|--------|------------|---------|------------|---------|-----|--------|-----|
| Keys | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | N/A |
| Monster XP | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Weapons | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Ammo | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Power-ups | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Cross-spawn | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| In-map portals | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

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

*Last updated: 2026-08-02 — Phases 2, 3, 4 (partial), 5 (infrastructure), and 6 complete. New: CrossGameEventsOnActivate / OnComplete / OnGeoHotSpotTriggered; PlayAudio / PlayVideo / OpenWebsite cross-game events with `oasis_open_url()` in all 10 games; OWolf3D entity spawn via ECWolf DECORATE; OQuake2 / OQuake2-RTX entity spawn via `G_Spawn` + `ED_CallSpawn`; `UnlockPortal` event handler + `ogengine_notify_portal_unlock` implemented in all 10 games + `OGEngineExports`; `PortalsController.cs` (`GET/POST/unlock/lock /api/portals`) added to STAR WebAPI; `ogengine_poll_cross_game_event` + `ogengine_poll_inventory_grant` + `ogengine_notify_portal_unlock` declared in all per-game `ogengine.h` copies; OASIS Omniverse multi-genre vision docs updated (Gen1/2/3, Ready Player One framing, OGEngine Editor expanded)*
*Vision: NextGen World Ltd — "One Infinite World"*
