# OASIS Omniverse — Architecture

This document describes the full architecture of the OASIS Omniverse game integration stack: how the layers fit together, what belongs in each layer, and how to extend the system to new games.

---

## Layered Architecture

```
┌────────────────────────────────────────────────────────────┐
│           Game Engine                                       │
│  (ODOOM / OQuake / your game)                              │
│                                                            │
│  Engine hooks only:                                        │
│  • Pickup callbacks  → star_api_queue_add_item()           │
│  • Door checks       → star_api_has_item()                 │
│  • Kill callbacks    → star_api_queue_monster_kill()        │
│  • HUD overlay       → star_api_get_inventory()            │
│  • Quest popup       → star_api_get_quests_string()        │
└───────────────────┬────────────────────────────────────────┘
                    │  includes oglib.h
                    ▼
┌────────────────────────────────────────────────────────────┐
│           OGLib   (C, header-only)                       │
│  OASIS Omniverse/OGLib/                                 │
│                                                            │
│  Shared game boilerplate:                                  │
│  • oasisstar.json load/save  (oglib_config.h)           │
│  • Beamin/beamout workflow   (oglib_beamin.h)           │
│  • Runtime DLL forwarders    (oglib_session.h)          │
│  • Cross-game asset mapping  (oglib_crossgame.h)        │
│  • JSON & string utilities   (oglib_json.h / str.h)     │
└───────────────────┬────────────────────────────────────────┘
                    │  star_api.h / star_sync.h
                    ▼
┌────────────────────────────────────────────────────────────┐
│           STARAPIClient  (C# NativeAOT)                     │
│  OASIS Omniverse/STARAPIClient/                            │
│                                                            │
│  Thin managed wrapper over WEB4 / WEB5 APIs:              │
│  • HTTP transport (HttpClient, retry, backoff)             │
│  • Avatar auth, profile, session management                │
│  • Inventory CRUD + local cache                            │
│  • Quest progress, caching, serialisation                  │
│  • NFT minting via WEB4 OASIS API                          │
│  • Background job workers (add-item, use-item, kill, …)    │
│  • Native exports [UnmanagedCallersOnly] → star_api.dll    │
│  • star_sync (async threading layer) → StarSyncExports.cs  │
└───────────────────┬────────────────────────────────────────┘
                    │  HTTPS
                    ▼
┌────────────────────────────────────────────────────────────┐
│           WEB5 STAR API  /  WEB4 OASIS API                 │
│  (hosted — see oasisstar.json for URLs)                    │
└────────────────────────────────────────────────────────────┘
```

---

## Layer Responsibilities

### Game Engine Layer

Each game contains **one integration file** (`uzdoom_star_integration.cpp` for ODOOM, `oquake_star_integration.c` for OQuake). It contains **only** what is specific to that game engine:

- Engine-specific pickup/kill callbacks that call `star_api_queue_*`
- HUD / overlay rendering using the engine's own draw API
- Console command parsing (`star beamin`, `star inventory`, …)
- QuakeC builtins / ZScript hooks
- Actor/item name tables that are game-specific
- Monster XP values (see `ODOOM/Docs/MONSTER_XP_TABLE.md`)

The integration file is **not** the place for config loading, session management, or cross-game logic — those come from OGLib.

---

### OGLib Layer

`OASIS Omniverse/OGLib/` is a C header-only library (single-header impl pattern). It provides everything a game integration file needs that is **not** game-engine-specific:

| Module | File | What it provides |
|--------|------|-----------------|
| Config | `oglib_config.h` | `star_config_t`, `oglib_config_load/save/save_session` |
| Beamin | `oglib_beamin.h` | `oglib_beamin_start`, `oglib_beamin_restore_session`, `oglib_beamout` |
| Session | `oglib_session.h` | Runtime forwarders for 9 session/auth functions via `GetProcAddress`/`dlsym` |
| Cross-game | `oglib_crossgame.h` | `oglib_crossgame_maps_t`, `oglib_crossgame_init_defaults`, `oglib_crossgame_lookup` |
| JSON | `oglib_json.h` | `oglib_json_extract`, `oglib_json_write_kv` |
| Strings | `oglib_str.h` | `oglib_str_contains_nocase`, `oglib_str_copy`, `oglib_str_trim` |

**Test:** if a piece of code compiles without any engine headers (`zdoom.h`, `quakedef.h`, etc.) then it belongs in OGLib. If it needs engine types, it belongs in the game's own integration file.

---

### STARAPIClient Layer

`OASIS Omniverse/STARAPIClient/` is the **only** place where HTTP calls to WEB4/WEB5 APIs happen. It is a C# NativeAOT project that publishes to `star_api.dll` (Windows) / `libstar_api.so` (Linux/macOS).

It exposes the C ABI via `[UnmanagedCallersOnly]` exports in `StarApiExports.cs`. Games call these through the `star_api.h` header.

**STARAPIClient must remain game-agnostic.** It knows about avatars, quests, inventory, and NFTs — it does not know about Doom keycards, Quake ammo types, or any game engine.

Key files after the recent refactor:

| File | Purpose |
|------|---------|
| `StarApiClient.cs` | Core client: HTTP, job queues, caching (7,178 lines) |
| `StarApiExports.cs` | All `[UnmanagedCallersOnly]` C exports (1,810 lines) |
| `StarSyncExports.cs` | `star_sync_*` exports (C# implementation of star_sync) |
| `StarApiConfig.cs` | Configuration model |
| `StarApiResultCode.cs`, `StarApiTransport.cs`, `QuestProgressCacheRefreshMode.cs` | Enums |
| `StarItem.cs`, `StarAvatarProfile.cs`, `StarNftInfo.cs` | Public model types |
| `StarApiCallback.cs`, `StarSyncCallbacks.cs` | Delegate types |
| `Interop/NativeStructs.cs` | C-layout structs for the native ABI |
| `Jobs/Pending*.cs` | Internal async job types |

---

### star_sync Layer

`star_sync.c` / `star_sync.h` is the **async threading bridge** between the game main thread and the `star_api_*` functions. It runs auth, inventory, send-item, and use-item operations on background threads and delivers results back via a pump function called from the game loop.

**Canonical source:** `STARAPIClient/star_sync.c` and `STARAPIClient/star_sync.h`.

By default (`OASIS_STAR_SYNC_IN_CLIENT=1`) the equivalent logic is compiled directly into `star_api.dll` as `StarSyncExports.cs`. Games that need the C implementation can compile `star_sync.c` into their own build instead.

**Do not modify `star_sync.c` in ODOOM or OQuake.** Edit the canonical copy in `STARAPIClient/` and let the build scripts copy it to game directories.

---

## Shared Files — Single Source of Truth

| File | Canonical location | Deployed to |
|------|--------------------|-------------|
| `star_api.h` | `STARAPIClient/star_api.h` | ODOOM/, OQuake/Code/, OQuake/build/ |
| `star_sync.h` | `STARAPIClient/star_sync.h` | ODOOM/, OQuake/Code/ |
| `star_sync.c` | `STARAPIClient/star_sync.c` | ODOOM/, OQuake/Code/ |

The build scripts (`BUILD ODOOM.bat`, `BUILD_OQUAKE.bat`) copy these files from STARAPIClient before compiling. **Never edit the deployed copies** — changes will be overwritten on the next build.

---

## oasisstar.json

Shared config file read by both ODOOM and OQuake (and any future game). All shared fields are defined in `oglib_config.h` as `star_config_t`. Game-specific extra fields are handled via the extension hook.

### Shared fields

```jsonc
{
    "star_api_url": "https://api.oasisomniverse.one",
    "oasis_api_url": "https://api.oasisomniverse.one",
    "star_transport": "remote",
    "jwt_token": "",           // persisted after beamin
    "refresh_token": "",       // persisted after beamin
    "username": "",            // persisted after beamin
    "beam_face": "",
    "max_health": 0,
    "max_armor": 0,
    "stack_armor": false,
    "stack_weapons": false,
    "stack_powerups": false,
    "stack_keys": false,
    "mint_weapons": false,
    "mint_armor": false,
    "mint_powerups": false,
    "mint_keys": false,
    "nft_provider": "SolanaOASIS",
    "send_to_address_after_minting": "",
    "always_allow_pickup_if_max": false,
    "always_add_items_to_inventory": false,
    "cross_game_doom_ammo_to_quake": "",
    "cross_game_quake_ammo_to_doom": "",
    "cross_game_doom_weapon_to_quake": "",
    "cross_game_quake_weapon_to_doom": ""
}
```

### ODOOM-only fields

```jsonc
{
    "quest_progress_refresh": "merge",
    "use_health_on_pickup": false,
    "use_armor_on_pickup": false
}
```

### OQuake-only fields

```jsonc
{
    "stack_sigils": false,
    "mint_monster_oquake_zombie": false,
    ...
}
```

---

## Adding a New OASIS Game

1. **Create your integration file** (e.g. `mygame_star_integration.c`).
2. **Include OGLib** with the impl defines in that one file:
   ```c
   #define OGLIB_SESSION_IMPL
   #define OGLIB_CONFIG_IMPL
   #define OGLIB_BEAMIN_IMPL
   #include "oglib.h"
   #include "star_api.h"
   #include "star_sync.h"
   ```
3. **Copy shared files** from STARAPIClient: `star_api.h`, `star_sync.h`, `star_sync.c`.
4. **Link** `star_api.lib` (Win) / `libstar_api.so` (Linux).
5. **Add `star_sync.c`** to your build (or set `OASIS_STAR_SYNC_IN_CLIENT=1`).
6. **Call `star_sync_pump()`** every frame.
7. **Implement engine hooks:**
   - Init/cleanup: `star_api_init` / `star_api_cleanup`
   - Pickup: `star_api_queue_add_item` / `star_api_queue_pickup_with_mint`
   - Door check: `star_api_has_item`
   - Kill: `star_api_queue_monster_kill`
   - Inventory display: `star_api_get_inventory`
   - Quest display: `star_api_get_quests_string`
8. **Add `oasisstar.json`** to your game directory.
9. **Document** in your game's `README.md` following the pattern in ODOOM/OQuake.

---

## Design Decisions

### Why is STARAPIClient C# and not C?

The OASIS WEB4/WEB5 APIs are built on ASP.NET Core with JWT auth, retry logic, and JSON deserialization. Implementing that correctly in C would require substantial third-party C libraries (libcurl, cJSON, mbedTLS…). C# gives us a single managed binary with all of that built in, compiled via NativeAOT to a native DLL with a clean C ABI.

### Why OGLib and not just bigger game files?

ODOOM and OQuake had ~350 lines of identical forwarder code, ~200 lines of near-identical config parsing, and the same beamin sequence. Maintaining three copies of the same bug fixes was unsustainable. OGLib makes the "standard" integration pattern explicit and testable.

### Why header-only?

C games have wildly different build systems (CMake, VS projects, Makefiles, Meson). A header-only library with a single-TU implementation pattern requires no changes to the build system — you add one `#define` and the library is compiled where you need it.

### Why not put OGLib code into STARAPIClient?

STARAPIClient is a managed C# library with a C ABI. Adding C game-integration logic there would pollute a clean, reusable API wrapper with game-specific concerns (file I/O, threading patterns specific to game loops, engine-adjacent types). The separation keeps STARAPIClient usable by non-game consumers (web apps, CLI tools, Unity, etc.).

---

## Repository Layout

```
OASIS Omniverse/
├── ARCHITECTURE.md              ← this file
├── STARAPIClient/               ← C# NativeAOT; star_api.dll
│   ├── StarApiClient.cs         ← core HTTP client (7,178 lines)
│   ├── StarApiExports.cs        ← [UnmanagedCallersOnly] C exports
│   ├── StarSyncExports.cs       ← star_sync C# implementation
│   ├── StarApi*.cs              ← model types, enums, delegates
│   ├── Jobs/Pending*.cs         ← internal async job types
│   ├── Interop/NativeStructs.cs ← C-layout interop structs
│   ├── star_api.h               ← C header (canonical)
│   ├── star_sync.h              ← C header (canonical)
│   └── star_sync.c              ← C implementation (canonical)
├── OGLib/                    ← C game integration library
│   ├── oglib.h               ← master include
│   ├── oglib_config.h        ← oasisstar.json + star_config_t
│   ├── oglib_beamin.h        ← beamin/beamout workflow
│   ├── oglib_session.h       ← runtime DLL forwarders
│   ├── oglib_crossgame.h     ← cross-game asset mapping
│   ├── oglib_json.h          ← JSON key extractor
│   ├── oglib_str.h           ← string utilities
│   └── README.md
├── ODOOM/                       ← UZDoom + OASIS integration
│   ├── uzdoom_star_integration.cpp
│   ├── star_api.h               ← deployed copy (from STARAPIClient)
│   ├── star_sync.c              ← deployed copy (from STARAPIClient)
│   └── star_sync.h              ← deployed copy (from STARAPIClient)
└── OQuake/                      ← vkQuake + OASIS integration
    ├── Code/oquake_star_integration.c
    ├── Code/star_api.h          ← deployed copy (from STARAPIClient)
    ├── Code/star_sync.c         ← deployed copy (from STARAPIClient)
    └── Code/star_sync.h         ← deployed copy (from STARAPIClient)
```
