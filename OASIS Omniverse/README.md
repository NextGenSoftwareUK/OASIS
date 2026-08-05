# OASIS Omniverse

**OASIS Omniverse** is a unified cross-game metaverse powered by the **OASIS STAR API** — spanning every game genre, not just FPS. The name means exactly what it says: *every* game, *every* genre, *one* universe. Keys, inventory, quests, XP, NFTs, avatars, and story arcs flow freely between all OGames, regardless of what kind of game they are.

We started where 3D gaming was born — **Doom** and **Quake** — then expanded across the open-source FPS classics that built the genre. The thirteen FPS integrations below are **Generation 1**. **Generation 2** is next: an open-world RPG (Morrowind via OpenMW) and a voxel sandbox (OMineCraft via Minetest). After that: strategy games, racing, platformers, and everything beyond — the real-life Ready Player One.

**All Markdown guides live under [`Docs/`](Docs/). This README is the entry-point index; use the tables below to jump to what you need.**

---

## Games at a glance

| OGame | Engine base | Integration file | Cross-game teleport | Cross-game spawn |
|-------|-------------|-----------------|---------------------|-----------------|
| **ODOOM** | UZDoom (GZDoom) | `ODOOM/uzdoom_ogengine_integration.cpp` | ✅ `P_TeleportMove` | ✅ `C_DoCommand("summon …")` |
| **OQuake** | vkQuake | `OQuake/Code/oquake_ogengine_integration.c` | ✅ `SV_LinkEdict` | ✅ `ED_Alloc` / QuakeC |
| **ODOOM3** | dhewm3 (idTech4) | `ODOOM3/d3doom3_ogengine_integration.cpp` | ✅ `idPlayer::Teleport` | ✅ `cmdSystem` spawn |
| **ODOOM3-BFG** | RBDOOM-3-BFG | `ODOOM3-BFG/d3doom_ogengine_integration.cpp` | ✅ `idPlayer::Teleport` | ✅ `cmdSystem` spawn |
| **ODuke3D** | EDuke32 | `ODuke3D/oduke3d_ogengine_integration.c` | ✅ `DukePlayer_t pos` | ✅ `A_InsertSprite` |
| **ODuke3D-RT** | Duke-RT | `ODuke3D-RT/oduke3drt_ogengine_integration.c` | ✅ `DukePlayer_t pos` | ✅ `A_InsertSprite` |
| **OWolf3D** | ECWolf | `OWolf3D/owolf3d_ogengine_integration.cpp` | ✅ `player.position` | ⏳ ECWolf API deferred |
| **OQuake2** | Yamagi Q2 | `OQuake2/oquake2_ogengine_integration.c` | ✅ `gi.linkentity` | ⏳ G_Spawn deferred |
| **OQuake2-RTX** | Q2 RTX | `OQuake2-RTX/oquake2rtx_ogengine_integration.c` | ✅ `gi.linkentity` | ⏳ G_Spawn deferred |
| **OQuake3** | Quake3e | `OQuake3/oquake3_ogengine_integration.c` | ✅ `trap_LinkEntity` | ✅ `trap_SendConsoleCommand` |
| **OHeretic** | UZDoom (GZDoom fork) | `OHeretic/oheretic_ogengine_integration.cpp` | ✅ portal system | ✅ GZDoom actor spawn |
| **OHexen** | UZDoom (GZDoom fork) | `OHexen/ohexen_ogengine_integration.cpp` | ✅ portal system | ✅ GZDoom actor spawn |
| **OShadowWarrior** | Raze (Build engine) | `OShadowWarrior/oshadowwarrior_ogengine_integration.cpp` | ✅ portal system | ✅ Raze actor spawn |

### Generation 2 — coming next

| OGame | Genre | Base engine | Status |
|-------|-------|-------------|--------|
| **OMorrowind** | Open-world RPG | OpenMW | 🔧 Integration files complete |
| **OMineCraft** | Voxel sandbox | Minetest / MineClone2 | 🔜 Planned |

### Generation 3 and beyond

Strategy (open-source RTS/4X), racing (SuperTuxKart), platformers, flight sims, survival, horror, fighting — the OGEngine integration pattern is game-engine-agnostic. Any game with an open-source base and a native C/C++ hook layer can become an OGame. The OASIS STAR API, OGEngineClient (`ogengine.dll`), OGLib, and the OGEngine Editor work the same way regardless of genre.

---

## New here? Start with a Getting Started guide

| Platform | Guide |
|----------|--------|
| **Windows** | [Docs/GettingStarted_Windows.md](Docs/GettingStarted_Windows.md) |
| **Linux** | [Docs/GettingStarted_Linux.md](Docs/GettingStarted_Linux.md) |
| **macOS** | [Docs/GettingStarted_Mac.md](Docs/GettingStarted_Mac.md) |

**→ [Docs/DEVELOPER_ONBOARDING.md](Docs/DEVELOPER_ONBOARDING.md)** — canonical setup (repos, tools, build, run, `oasisstar.json`).

---

## Documentation index (`Docs/`)

### Setup and daily workflow

| Document | What it's for |
|----------|----------------|
| [Docs/DEVELOPER_ONBOARDING.md](Docs/DEVELOPER_ONBOARDING.md) | Repos, tools, build/run scripts, `oasisstar.json` quick ref |
| [Docs/QUICKSTART.md](Docs/QUICKSTART.md) | Minimal build + run checklist |
| [Docs/LINUX_BUILD.md](Docs/LINUX_BUILD.md) | Linux/macOS script equivalents |
| [Docs/GettingStarted_Windows.md](Docs/GettingStarted_Windows.md) | Windows: prerequisites, clone, build all games |
| [Docs/GettingStarted_Linux.md](Docs/GettingStarted_Linux.md) | Linux: same |
| [Docs/GettingStarted_Mac.md](Docs/GettingStarted_Mac.md) | macOS: same |

### Architecture, integration, and roadmap

| Document | What it's for |
|----------|----------------|
| [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) | Client-centric design, layers, porting checklist |
| [Docs/INTEGRATION_GUIDE.md](Docs/INTEGRATION_GUIDE.md) | Cross-game items, quests, API usage, phases, troubleshooting |
| [Docs/CROSS_GAME_POWERUP_WEAPON_MAP.md](Docs/CROSS_GAME_POWERUP_WEAPON_MAP.md) | Canonical item IDs and cross-game mappings |
| [Docs/STAR_INTEGRATION_AUDIT.md](Docs/STAR_INTEGRATION_AUDIT.md) | Integration audit (sync vs C implementation) |
| [Docs/OGENGINE_VISION_AND_ROADMAP.md](Docs/OGENGINE_VISION_AND_ROADMAP.md) | Full OGEngine vision, phases, status checklist for all 13 games |
| [Docs/OGEngine_Overview.md](Docs/OGEngine_Overview.md) | WEB4/WEB5 APIs, GeoHotSpot media types, quest handoff |

### Quests and story system

| Document | What it's for |
|----------|----------------|
| [Docs/PHASE2_QUEST_SYSTEM.md](Docs/PHASE2_QUEST_SYSTEM.md) | Quest system design |
| [Docs/STAR_Quest_System_Developer_Guide.md](Docs/STAR_Quest_System_Developer_Guide.md) | WEB5 quest API, ogengine_* hooks, cross-game events (developers) |
| [Docs/STAR_Games_User_Guide.md](Docs/STAR_Games_User_Guide.md) | Beam-in, inventory, quest keys for all games (players/testers) |
| [Docs/ODOOM_Quest_List_STAR.md](Docs/ODOOM_Quest_List_STAR.md) | ODOOM quest list CVars, ZScript, scroll/filter invariants |

### Hub and HUD overlay

| Document | What it's for |
|----------|----------------|
| [Docs/OMNIVERSE_HUD_USER_GUIDE.md](Docs/OMNIVERSE_HUD_USER_GUIDE.md) | Player guide: all 9 Control Center tabs, hotkeys, presets, toasts, status strip, Return to Hub, Quest Tracker |
| [Docs/OMNIVERSE_HUD_DEVELOPER_GUIDE.md](Docs/OMNIVERSE_HUD_DEVELOPER_GUIDE.md) | Developer reference: architecture, class relationships, data flow, toast system, preset internals, extension points |

### Build sync and native library

| Document | What it's for |
|----------|----------------|
| [Docs/ODOOM_UZDoom_Build_Sync.md](Docs/ODOOM_UZDoom_Build_Sync.md) | ODOOM build sync, copy step, `star_api` / `libstar_api` deploy |
| [Docs/OGENGINE_Native_Transport_Architecture.md](Docs/OGENGINE_Native_Transport_Architecture.md) | `star_transport` native vs remote, size/AOT |

### Per-game READMEs

| Game | README |
|------|--------|
| ODOOM | [ODOOM/README.md](ODOOM/README.md) |
| OQuake | [OQuake/README.md](OQuake/README.md) |
| ODuke3D | [ODuke3D/README.md](ODuke3D/README.md) |
| ODuke3D-RT | [ODuke3D-RT/README.md](ODuke3D-RT/README.md) |
| OWolf3D | [OWolf3D/README.md](OWolf3D/README.md) |
| OQuake2 | [OQuake2/README.md](OQuake2/README.md) |
| OQuake2-RTX | [OQuake2-RTX/README.md](OQuake2-RTX/README.md) |
| OQuake3 | [OQuake3/README.md](OQuake3/README.md) |
| OGEngineClient | [OGEngineClient/README.md](OGEngineClient/README.md) |

---

## Repositories to clone

| Repository | Purpose | Default path |
|------------|---------|--------------|
| **OASIS** (this repo) | Backend, OGEngineClient, all 10 integrations | `C:\Source\OASIS2` |
| **ODOOM engine** (`UZDOOM_SRC`) | UZDoom/GZDoom fork | `C:\Source\UZDoom` |
| **OQuake engine** (`VKQUAKE_SRC`) | vkQuake fork | `C:\Source\vkQuake` |
| **quake-rerelease-qc** | QuakeC for OQuake | `C:\Source\quake-rerelease-qc` |
| **ODuke3D engine** | EDuke32 fork | `C:\Source\ODuke3D` |
| **ODuke3D-RT engine** | Duke-RT fork | `C:\Source\ODuke3D-RT` |
| **OWolf3D engine** | ECWolf fork | `C:\Source\OWolf3D` |
| **OQuake2 engine** | Yamagi Q2 fork (`YQUAKE2_SRC`) | `C:\Source\yquake2` |
| **OQuake2-RTX engine** | Q2 RTX fork (`YQUAKE2RTX_SRC`) | `C:\Source\yquake2rtx` |
| **OQuake3 engine** | Quake3e fork (`Q3E_SRC`) | `C:\Source\Quake3e` |
| **ODOOM3 engine** | dhewm3 fork (`DHEWM3_SRC`) | `C:\Source\dhewm3` |
| **ODOOM3-BFG engine** | RBDOOM-3-BFG fork (`RBD3_SRC`) | `C:\Source\RBDOOM-3-BFG` |

Use OASIS-maintained forks (under `NextGenSoftwareUK/`) so the engine already tracks OASIS integration; vanilla upstream requires relying entirely on the copy/patch steps from this repo.

---

## Building

### Build everything (recommended)

From `OASIS Omniverse\`:

```batch
BUILD EVERYTHING.bat
```

Builds OGEngineClient (the shared C# NativeAOT `ogengine.dll`/`libstar_api.so`) then builds all 10 game integrations in sequence. No prompts, no launch.

### Build a single game

| Game | Windows | Linux/macOS |
|------|---------|-------------|
| ODOOM | `ODOOM\BUILD ODOOM.bat` | `./ODOOM/BUILD_ODOOM.sh` |
| OQuake | `OQuake\BUILD_OQUAKE.bat` | `./OQuake/BUILD_OQUAKE.sh` |
| ODOOM3 | `ODOOM3\BUILD_ODOOM3.bat` | `./ODOOM3/BUILD_ODOOM3.sh` |
| ODOOM3-BFG | `ODOOM3-BFG\BUILD_ODOOM3BFG.bat` | `./ODOOM3-BFG/BUILD_ODOOM3BFG.sh` |
| ODuke3D | `ODuke3D\BUILD_ODUKE3D.bat` | `./ODuke3D/BUILD_ODUKE3D.sh` |
| ODuke3D-RT | `ODuke3D-RT\BUILD_ODUKE3DRT.bat` | `./ODuke3D-RT/BUILD_ODUKE3DRT.sh` |
| OWolf3D | `OWolf3D\BUILD_OWOLF3D.bat` | `./OWolf3D/BUILD_OWOLF3D.sh` |
| OQuake2 | `OQuake2\BUILD_OQUAKE2.bat` | `./OQuake2/BUILD_OQUAKE2.sh` |
| OQuake2-RTX | `OQuake2-RTX\BUILD_OQUAKE2RTX.bat` | `./OQuake2-RTX/BUILD_OQUAKE2RTX.sh` |
| OQuake3 | `OQuake3\BUILD_OQUAKE3.bat` | `./OQuake3/BUILD_OQUAKE3.sh` |
| STAR API client only | `BUILD_AND_DEPLOY_STAR_CLIENT.bat` | `./BUILD_AND_DEPLOY_STAR_CLIENT.sh` |

### Run a game

| Game | Windows | Linux/macOS |
|------|---------|-------------|
| ODOOM | `ODOOM\RUN ODOOM.bat` | `./ODOOM/RUN_ODOOM.sh` |
| OQuake | `OQuake\RUN OQUAKE.bat` | `./OQuake/RUN_OQUAKE.sh` |
| ODOOM3 | `ODOOM3\RUN_ODOOM3.bat` | `./ODOOM3/RUN_ODOOM3.sh` |
| ODOOM3-BFG | `ODOOM3-BFG\RUN_ODOOM3BFG.bat` | `./ODOOM3-BFG/RUN_ODOOM3BFG.sh` |
| ODuke3D | `ODuke3D\RUN_ODUKE3D.bat` | `./ODuke3D/RUN_ODUKE3D.sh` |
| ODuke3D-RT | `ODuke3D-RT\RUN_ODUKE3DRT.bat` | `./ODuke3D-RT/RUN_ODUKE3DRT.sh` |
| OWolf3D | `OWolf3D\RUN_OWOLF3D.bat` | `./OWolf3D/RUN_OWOLF3D.sh` |
| OQuake2 | `OQuake2\RUN_OQUAKE2.bat` | `./OQuake2/RUN_OQUAKE2.sh` |
| OQuake2-RTX | `OQuake2-RTX\RUN_OQUAKE2RTX.bat` | `./OQuake2-RTX/RUN_OQUAKE2RTX.sh` |
| OQuake3 | `OQuake3\RUN_OQUAKE3.bat` | `./OQuake3/RUN_OQUAKE3.sh` |

---

## Directory structure

```
OASIS Omniverse/
├── README.md                        ← This file
├── Docs/                            ← All Markdown guides
│   ├── OGENGINE_VISION_AND_ROADMAP.md
│   ├── OGEngine_Overview.md
│   ├── ARCHITECTURE.md
│   ├── DEVELOPER_ONBOARDING.md
│   ├── STAR_Quest_System_Developer_Guide.md
│   └── …
├── Config/
│   ├── oasis_star_assets.json       ← Cross-game entity / asset catalog (all 10 games)
│   └── stories/
│       └── oasis_arc_001_dimensional_rift.json  ← First cross-game story arc
├── BUILD EVERYTHING.bat / .sh       ← Build OGEngineClient + all 10 games
├── BUILD_AND_DEPLOY_STAR_CLIENT.bat/.sh
├── OGEngineClient/                  ← C# NativeAOT STAR API client (ogengine.dll)
│   ├── OGEngineClient.cs
│   ├── OGEngineExports.cs           ← NativeAOT [UnmanagedCallersOnly] exports
│   └── ogengine.h                   ← C ABI header (canonical; copied to each game)
├── NativeWrapper/                   ← Deprecated; reference only. Use OGEngineClient.
│   └── ogengine.h                   ← Shared C ABI header (kept in sync with OGEngineClient/ogengine.h)
├── OGLib/                           ← Header-only C utility library (monster table, session, config)
│   └── oglib.h
├── ODOOM/                           ← Doom (UZDoom/GZDoom)
├── OQuake/                          ← Quake (vkQuake)
├── ODOOM3/                          ← Doom 3 classic (dhewm3)
├── ODOOM3-BFG/                      ← Doom 3 BFG Edition (RBDOOM-3-BFG)
├── ODuke3D/                         ← Duke Nukem 3D (EDuke32)
├── ODuke3D-RT/                      ← Duke Nukem 3D ray-traced (Duke-RT)
├── OWolf3D/                         ← Wolfenstein 3D (ECWolf)
├── OQuake2/                         ← Quake II (Yamagi Q2)
├── OQuake2-RTX/                     ← Quake II ray-traced (Q2 RTX)
├── OQuake3/                         ← Quake III Arena (Quake3e)
└── OASIS Omniverse/                 ← Unity hub project (optional embedded shell)
    └── README.md
```

Each game folder contains:
- `{Game}_ogengine_integration.{c,cpp}` — engine hook implementation
- `{Game}_ogengine_integration.h` — public API header
- `ogengine.h` / `ogengine_sync.h` — STAR API C ABI (build script copies from OGEngineClient)
- `oasisstar.json` — per-game config (API URL, session, mint flags, monster XP table)
- `BUILD_*.bat` / `BUILD_*.sh` — build scripts
- `RUN_*.bat` / `RUN_*.sh` — launch scripts
- `Docs/` — per-game integration guide

---

## Overview

### OGEngineClient

The **C# NativeAOT** STAR API client used by all 10 games. Builds `ogengine.dll` (Windows) or `libstar_api.so` (Linux/macOS). Exposes a C ABI (`ogengine_*`) that each game's integration C/C++ file links against.

Key exports: `ogengine_init`, `ogengine_authenticate`, `ogengine_get_inventory`, `ogengine_add_item`, `ogengine_complete_quest_objective`, `ogengine_request_teleport`, `ogengine_poll_teleport_request`, `ogengine_poll_spawn_event`, `ogengine_poll_cross_game_event`, `ogengine_poll_inventory_grant`, `ogengine_get_quests_string`, and more.

### OGLib

A **header-only C utility library** shared by all 10 games. Provides:
- Monster XP table loaded from `oasisstar.json`
- Session management helpers
- Config file I/O

### OGEngine Editor

**Ultimate Doom Builder** is the primary host. Already built:

| Tool | What it does |
|------|-------------|
| **OGEditorSDK** (`Source/OGEditorSDK/`) | .NET Standard 2.0 library — `OGAssetCatalog` (140-asset cross-game catalog), `OGMapSidecar` (`oasis_{map}.json` reader/writer), `OGStarApiClient` (live STAR API HTTP client), `OGEntityMappings` (classname ↔ OASIS thing-type bidirectional lookup). Also exports a C ABI (`OGEditorClient.h` / `OGEditorClient.dll`) so TrenchBroom, NetRadiant, DarkRadiant, and Mapster32 can call it too. |
| **OGEngine Panel** | Live asset browser — pulls catalog from STAR API (`/api/oassets`), falls back to offline catalog; category filter; shows all 10 OGames |
| **OASIS Portal Editor** | Drag-drop portal placement UI — picks target game, target map, spawn coords; writes to map sidecar on placement |
| **Quest Weaver Panel** | Fetches quests from STAR API, lets you drag objectives onto sector/thing/linedef map triggers; saves objective↔trigger binding to sidecar |
| **OASISMapConverter** | Bidirectional entity conversion: OQuake↔ODOOM, OQuake2↔ODOOM, OQuake3→ODOOM, ODuke3D→ODOOM |
| **OASISMapSidecar** | Reads/writes `oasis_{mapname}.json` sidecar (portals, cross-game entities, objective triggers) |
| **Companion launch** | UDB "Edit in native editor…" — opens TrenchBroom for Q1/Q2 maps, NetRadiant for Q3, DarkRadiant for Doom 3, Mapster32 for Duke3D; all companions share the same sidecar |
| **Sprite extractor** | `Tools/ExtractOquakeSprites/` — extracts Quake MDL sprites from pak0 for UDB thing display |
| **Entity definitions** | `oasis_entities.fgd` / `oasis_entities.def` — `oasis_portal`, `oasis_spawn`, `oasis_objective_trigger` for TrenchBroom/NetRadiant |

---

## Features

### Cross-game item sharing

Collect keys/weapons/powerups in one game, use them in another. Persistent inventory via the STAR API. All 10 games share the same inventory namespace.

| Game | Key types |
|------|-----------|
| ODOOM / ODOOM3 / ODOOM3-BFG | Blue keycard, Red keycard, Yellow keycard |
| OQuake | Silver key, Gold key |
| ODuke3D / ODuke3D-RT | Blue access card, Red access card, Yellow access card |
| OWolf3D | Gold key, Silver key |
| OQuake2 / OQuake2-RTX | Bluekey, Redkey |
| OQuake3 | Runes (Team Arena) |

### Cross-game teleportation

Step on an `oasis_portal` entity (thing type 5900) in any game → OmniverseKernel teleports you to the target game at the target map position. All 10 games implement `{Prefix}_STAR_CheckIncomingTeleport()` and warp the player using each engine's native position API.

### Cross-game entity spawning

Quest objectives can trigger entity spawns in other games:
- ODOOM: `C_DoCommand("summon <classname>")`
- OQuake: QuakeC `ED_Alloc` / `PR_ExecuteProgram`
- ODOOM3 / ODOOM3-BFG: `cmdSystem->BufferCommandText("spawn <classname>\n")`
- ODuke3D / ODuke3D-RT: `A_InsertSprite` with picnum lookup from asset catalog
- OQuake3: `trap_SendConsoleCommand("spawn <classname>\n")`
- OWolf3D, OQuake2, OQuake2-RTX: deferred (engine API constraints)

### Cross-game quests and story arcs

Multi-game quests spanning multiple games. Each objective has:
- `GameSource` + `MapName` — which game and map
- `CrossGameEventsOnActivate` — events fired when objective first activates (opening narration, audio)
- `CrossGameEventsOnComplete` — events fired in other games on completion (spawn enemies, unlock portals, show narration, teleport, play audio/video, open website)
- `CrossGameEventsOnGeoHotSpotTriggered` — events fired when a linked real-world GeoHotSpot is visited
- `NeedToKillMonstersByType` — per-classname kill requirements
- `RewardInventoryItemIds` — OASIS inventory item GUIDs granted on completion

The first cross-game story arc (`oasis_arc_001_dimensional_rift.json`) spans ODOOM → OQuake → OWolf3D.

### Cross-game events (in-game delivery)

All 10 games poll `ogengine_poll_cross_game_event()` every frame. When a quest triggers a cross-game event:
- `ShowNarration` → toast notification in-game (all 10 games)
- `SpawnEntity` → calls the game's native spawn path
- `TeleportTo` → queues a teleport request
- `PlayAudio` / `PlayVideo` / `OpenWebsite` → logged + toasted (full media streaming is future work)
- `UnlockPortal` → notifies OGEditor portal system (future work)

### Inventory NFT minting

When enabled in `oasisstar.json` (`mint_weapons`, `mint_keys`, `mint_monsters`), collecting items mints an NFT (WEB4 NFTHolon) attached to the inventory item. Per-category opt-in.

### Avatar / SSO

Log in with STAR username/password or API key + avatar ID. In-game console: `star beamin <username> <password>`.

### OASIS Omniverse HUD overlay (Unity hub)

When running OGames through the OASIS Omniverse Unity hub, a **Steam/Xbox-style overlay** (`SharedHudOverlay.cs`) sits at `sortingOrder 9999` above all embedded game windows. Press `I` in any OGame to open it.

**Control Center tabs:** Inventory · Quests · NFTs · Avatar · Karma · Settings · Diagnostics · Friends · Teleport

**Always-visible:** Runtime Status Strip (API health, active game, free RAM, polled every 0.6 s) and toast notifications (success/warning/error, animated, configurable duration and max count).

**Also always-visible:** `QuestTrackerWidget` — a compact mini-HUD showing active quests, auto-refreshes every 20 s, independently repositionable.

See the full documentation:
- **[Docs/OMNIVERSE_HUD_USER_GUIDE.md](Docs/OMNIVERSE_HUD_USER_GUIDE.md)** — player guide (all tabs, hotkeys, presets, Return to Hub)
- **[Docs/OMNIVERSE_HUD_DEVELOPER_GUIDE.md](Docs/OMNIVERSE_HUD_DEVELOPER_GUIDE.md)** — developer reference (architecture, data flow, extension points)

### Native HUD (fallback / standalone)

Each game has a native C/C++ HUD that renders without Unity:
- Inventory popup (`I` key) — live fetch from `ogengine_get_inventory`
- Quest popup (`Q` key) — live fetch from `ogengine_get_quests_string`, parses quest list
- Toast notifications — XP awards, key pickups, cross-game events
- XP / karma counter and beamed-in label

---

## Quick reference

| Task | Command |
|------|---------|
| Build everything | `BUILD EVERYTHING.bat` (Windows) / `./BUILD_EVERYTHING.sh` (Linux/macOS) |
| Build STAR client only | `BUILD_AND_DEPLOY_STAR_CLIENT.bat` |
| Run local APIs | `Scripts\start_web4_and_web5_apis.bat` (from repo root) |
| Game config | `{Game}\oasisstar.json` — set `ogengine_url`, `oasis_api_url`, credentials |
| Cross-game assets | `Config\oasis_star_assets.json` — entity catalog for all 10 games |
| Story arcs | `Config\stories\*.json` — cross-game quest/objective definitions |

---

## License

All 10 game integrations follow the same license as the base engine they extend (GPL-2.0 for all, since ODOOM, OQuake, ODOOM3, ODOOM3-BFG, ODuke3D, ODuke3D-RT, OWolf3D, OQuake2, OQuake2-RTX, and OQuake3 are all GPL-licensed source ports). The OASIS integration layer (OGEngineClient, OGLib, integration C/C++ files) is licensed under the OASIS project license. See `OASIS Omniverse/LICENSE.md` or each game's `Docs/CREDITS_AND_LICENSE.md`.
