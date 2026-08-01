# ODOOM3-BFG Integration Instructions

## Overview

ODOOM3-BFG integrates **RBDOOM-3-BFG** (RBDOOM Edition of Doom 3 BFG) with the
**OASIS STAR API** (Web5 WEB4 layer), enabling cross-game inventory, quests, NFT minting,
and avatar XP across the OASIS Omniverse.  Keys and items are shared across all seven
OASIS Omniverse games: **ODOOM**, **OQuake**, **ODOOM3** (Doom 3 classic), **ODOOM3-BFG**,
**ODuke3D** (Duke Nukem 3D, EDuke32 fork), **ODuke3D-RT** (Duke Nukem 3D ray-traced), and **OWolf3D** (Wolfenstein 3D, ECWolf fork).

Engine: [RBDOOM-3-BFG](https://github.com/RobertBeckebans/RBDOOM-3-BFG)
Source port of Doom 3 BFG Edition (id Tech 4/5 C++, CMake, x64, VS2019+)

---

## Architecture

```
RBDOOM-3-BFG (C++, d3xp/)
      │
      │  6 hook calls
      ▼
d3doom_star_integration.cpp   ← delta file (lives in OASIS Omniverse/ODOOM3-BFG/)
      │
      ├── star_api.h           ← C ABI (links against star_api.dll)
      ├── star_sync.h          ← async auth/inventory helpers
      └── OGLib/oglib.h        ← header-only utility library
                │
                ▼
          star_api.dll         ← STARAPIClient (C# NativeAOT)
                │
                ▼
         OASIS STAR API
         (HTTP WEB5/WEB4)
```

---

## Hook Points (6 lines of engine code total)

| File | Function | Hook |
|------|----------|------|
| `d3xp/Game_local.cpp` | `idGameLocal::Init()` | `D3Doom_STAR_Init();` |
| `d3xp/Game_local.cpp` | `idGameLocal::Shutdown()` | `D3Doom_STAR_Cleanup();` |
| `d3xp/Game_local.cpp` | `idGameLocal::RunFrame()` | `D3Doom_STAR_Tick();` |
| `d3xp/Game_local.cpp` | `idGameLocal::RequirementMet()` | STAR fallback in `else` branch |
| `d3xp/Player.cpp` | `idPlayer::GiveInventoryItem()` | `D3Doom_STAR_OnItemPickup(...)` |
| `d3xp/ai/AI.cpp` | `idAI::Killed()` | `D3Doom_STAR_OnMonsterKilled(...)` |

See `Docs/WINDOWS_INTEGRATION.md` for exact line numbers and code diffs.

---

## Features

### Cross-Game Keys
Blue Key / Red Key / Yellow Key picked up in Doom 3 BFG are stored in STAR.
If you later load ODOOM (classic Doom) and a door requires a blue_keycard, STAR
provides it from your cross-game inventory. This works in both directions.

### Monster XP & NFTs
Every monster kill reports to STAR (XP). Bosses (Cyberdemon, Maledict, etc.)
trigger optional NFT minting via the WEB4 OASIS API.

The monster table is **JSON-driven**: edit `oasisstar.json` → `odoom3bfg.monsters[]`
to tune XP values, boss flags, and mint flags without recompiling.

### Cross-Game Quests
STAR quests can track objectives across all OASIS games. Picking up a Doom 3
keycard or killing a boss advances objectives whether the quest was started in
ODOOM, OQuake, or any other OASIS game.

### In-Game Console
Type `star` in the Doom 3 console (CTRL+ALT+~) to access:
- `star version` — version info
- `star status` — init/auth status
- `star beamin <user> <pass>` — authenticate with OASIS
- `star beamout` — log out
- `star inventory` — list STAR inventory
- `star debug on|off` — toggle verbose logging
- `star add <name>` — manually add an item (testing)

---

## Config: oasisstar.json

Placed next to the executable at runtime. Created automatically on first launch.
Edit the template at `OASIS Omniverse/ODOOM3-BFG/oasisstar.json` before building.

Key fields:
| Field | Default | Description |
|-------|---------|-------------|
| `star_api_url` | OASIS STAR URL | WEB5 STAR API endpoint |
| `oasis_api_url` | OASIS API URL | WEB4 OASIS API (for NFT mint) |
| `mint_keys` | 0 | Mint NFT on keycard pickup |
| `mint_monsters` | 1 | Mint NFT on boss kill (respects do_mint per monster) |
| `star_debug` | 1 | Verbose logging |
| `consume_key_on_door` | 1 | Remove STAR key when door opens |
| `beamedin_avatar` | — | Saved username (auto-written on login) |
| `jwt_token` | — | Saved JWT (auto-written; auto-restored on next launch) |

---

## Build Process

1. Build STARAPIClient first (generates `star_api.dll` / `star_api.lib`).
2. Run `BUILD_ODOOM3BFG.bat` (Windows) or `BUILD_ODOOM3BFG.sh` (Linux/macOS).
3. Script copies integration files into `C:\Source\ODOOM3-BFG\neo\d3xp\` and runs CMake.
4. Output: `build-vs2019-win64\Release\d3game.dll` + `star_api.dll` + `oasisstar.json`.

---

## Files in this Folder

| File | Purpose |
|------|---------|
| `d3doom_star_integration.h` | Public API (6 hook function declarations) |
| `d3doom_star_integration.cpp` | Full integration implementation |
| `oasisstar.json` | Config + monster table template |
| `BUILD_ODOOM3BFG.bat/.sh` | Build entry points |
| `RUN_ODOOM3BFG.bat/.sh` | Launch entry points |
| `Scripts/COPY_TO_RBDOOM3_AND_BUILD.ps1` | Copy + CMake build script |
| `Docs/INTEGRATION_INSTRUCTIONS.md` | This file |
| `Docs/WINDOWS_INTEGRATION.md` | Step-by-step engine diff guide |
