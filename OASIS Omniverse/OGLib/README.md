# OGLib — OASIS Game Integration Library

OGLib is a lightweight C library that sits between the raw `star_api.h` exports and your game engine. It contains all the boilerplate that every OASIS-integrated game needs: config loading, session management, beamin/beamout workflow, cross-game mappings, and common utilities.

**Games that want minimal ceremony** use `STARAPIClient` directly via `star_api.h`.  
**Games that want the full workflow** include OGLib and get all the plumbing for free.

---

## Architecture

```
Game engine (ODOOM / OQuake / your game)
      ↓  engine hooks only
   OGLib   ← this library (C, header-only / single-TU impl)
      ↓  star_api.h / star_sync.h
  STARAPIClient  (C# NativeAOT → star_api.dll / libstar_api.so)
      ↓
  WEB4 / WEB5 OASIS APIs
```

See [`OASIS Omniverse/ARCHITECTURE.md`](../ARCHITECTURE.md) for the full system design.

---

## Files

| File | Purpose |
|------|---------|
| `oglib.h` | Master include — pulls in all headers below |
| `oglib_str.h` | String helpers: `contains_nocase`, safe copy, trim |
| `oglib_json.h` | Minimal JSON key→value extractor (no dependencies) |
| `oglib_config.h` | `star_config_t` struct + `oasisstar.json` load/save |
| `oglib_beamin.h` | Beamin/beamout workflow (auth, restore session, persist JWT) |
| `oglib_session.h` | Runtime DLL forwarders (`GetProcAddress` / `dlsym` shims) |
| `oglib_crossgame.h` | Cross-game ammo/weapon mapping defaults |

All implementation headers follow the **single-header library** pattern (a la `stb`): declarations are always compiled; implementations are compiled only in the one translation unit that defines the matching `OGLIB_*_IMPL` macro.

---

## Quick Start

### 1. Copy OGLib into your project

Copy the `OGLib/` folder (or add it as a submodule path) so your compiler can find the headers.

### 2. Add the implementation TU

In exactly **one** `.c` or `.cpp` file in your project (e.g. `my_game_star_integration.c`):

```c
#define OGLIB_SESSION_IMPL
#define OGLIB_CONFIG_IMPL
#define OGLIB_BEAMIN_IMPL
#include "oglib.h"
#include "star_api.h"
#include "star_sync.h"
```

All other files that use OGLib just include `oglib.h` without the defines.

### 3. Include star_api.h / star_sync.h

OGLib wraps the STAR API — you still need to link `star_api.lib` (Windows) or `libstar_api.so` (Linux). Get the canonical copies from `STARAPIClient/`:

- `STARAPIClient/star_api.h`
- `STARAPIClient/star_sync.h`
- `STARAPIClient/star_sync.c` (add to your build, or use `OASIS_STAR_SYNC_IN_CLIENT=1` to skip)

---

## Config: oasisstar.json

`oglib_config.h` defines `star_config_t` — all the shared fields from `oasisstar.json`:

```c
star_config_t cfg;
memset(&cfg, 0, sizeof(cfg));
oglib_config_load("oasisstar.json", &cfg, NULL, NULL);

// Initialize STAR API
star_api_config_t api_cfg = { ... };
snprintf(api_cfg.base_url_buf, sizeof(api_cfg.base_url_buf), "%s", cfg.star_api_url);
star_api_init(&api_cfg);

// Restore saved session if available
if (cfg.jwt_token[0]) {
    star_api_set_saved_session(cfg.jwt_token);
    if (cfg.refresh_token[0])
        star_api_set_refresh_token(cfg.refresh_token);
    star_api_restore_session();
}
```

### Game-specific fields

Use the extension hook to read/write fields that only your game needs:

```c
static void my_game_ext_load(const char* json, void* fp, void* user)
{
    MyGameConfig* g = (MyGameConfig*)user;
    if (json) {
        oglib_json_extract(json, "quest_progress_refresh", g->quest_progress_refresh, sizeof(g->quest_progress_refresh));
    }
}

oglib_config_load("oasisstar.json", &cfg, my_game_ext_load, &myGameCfg);
```

---

## Beamin

```c
static void on_beamin_done(oglib_beamin_result_t result,
                            const char* username, void* user)
{
    if (result == OGLIB_BEAMIN_OK)
        Con_Printf("Logged in as %s. Cross-game assets enabled.\n", username);
    else
        Con_Printf("Beamin failed.\n");
}

static oglib_beamin_ctx_t s_beamin_ctx;

void MyGame_Beamin(const char* username, const char* password)
{
    s_beamin_ctx.config      = &g_star_config;
    s_beamin_ctx.config_path = "oasisstar.json";
    s_beamin_ctx.done_cb     = on_beamin_done;
    s_beamin_ctx.done_user   = NULL;
    oglib_beamin_start(&s_beamin_ctx, username, password);
}
```

---

## Session Forwarders

`oglib_session.h` provides Win32/POSIX runtime forwarders for the nine session/auth functions declared in `star_api.h`. Include `OGLIB_SESSION_IMPL` in your implementation TU and they resolve automatically at runtime from the loaded `star_api.dll` / `libstar_api.so`.

This avoids link-time symbol resolution issues that occur when the DLL is loaded by the engine after your code runs.

---

## Cross-Game Mappings

```c
#include "oglib_crossgame.h"

oglib_crossgame_maps_t maps;
oglib_crossgame_init_defaults(&maps);

// Override one entry
snprintf(maps.doom_ammo_to_quake[0].to, 64, "SuperNails");

// Look up a mapping
const char* quake_ammo = oglib_crossgame_lookup(
    maps.doom_ammo_to_quake, maps.doom_ammo_to_quake_count, "Bullets");
// quake_ammo = "SuperNails"
```

---

## String & JSON Helpers

```c
#include "oglib_str.h"
#include "oglib_json.h"

// Case-insensitive search
if (oglib_str_contains_nocase(item_name, "key")) { ... }

// Extract a JSON value
char url[512];
oglib_json_extract(json_text, "star_api_url", url, sizeof(url));
```

---

## What OGLib does NOT do

OGLib deliberately excludes anything that is engine-specific or game-specific:

- HUD / overlay rendering (each engine has its own draw calls)
- Input handling for inventory/quest popups
- QuakeC builtins or ZScript definitions
- Monster XP tables or pickup detection (game-specific actor names)
- Logging to the engine console (`Con_Printf`, `Printf`, etc.)

Those belong in your game's own integration file (`uzdoom_star_integration.cpp`, `oquake_star_integration.c`, etc.).

---

## Adding OGLib to a New Game

1. Copy `OGLib/` headers into your project (or reference via include path).
2. Copy `star_api.h`, `star_sync.h`, `star_sync.c` from `STARAPIClient/`.
3. Add `star_sync.c` to your build (or set `OASIS_STAR_SYNC_IN_CLIENT=1` if the client DLL already includes it).
4. Link `star_api.lib` (Win) / `libstar_api.so` (Linux).
5. Create `my_game_star_integration.c` with the `OGLIB_*_IMPL` defines and your engine hooks.
6. Call `star_sync_pump()` every frame from your game loop.
7. Add `oasisstar.json` to your game directory and fill in `star_api_url`.
