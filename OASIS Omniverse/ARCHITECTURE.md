# OASIS Omniverse – Architecture & Design

This document describes how the STAR integration is designed so that **the C# client does all the heavy lifting** and games (ODOOM, OQuake, and future ports) need only **minimal hooks**. The goal is a generic, client-centric design that makes it quicker and easier to port other games.

---

## Design principle: client-centric, minimal game hooks

- **Heavy lifting lives in the C# STARAPIClient:** HTTP, caching, queuing, background workers, mint + add_item batching, and all API logic.
- **Games are thin:** They call a small C API (`star_api_*` and, where needed, `star_sync_*`). They do **not** implement threads for API work, sync logic, or inventory merging.
- **Porting a new game** means: add a few call sites (pickup, door check, beam-in, inventory UI) that call the same C API; no game-specific backend or queue logic.

---

## Three layers

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  LAYER 1: Game (C/C++)                                                      │
│  ODOOM, OQuake, or any future game                                          │
│  • Detects: pickup, door use, beam-in, inventory open                       │
│  • Calls: star_api_* (and star_sync_* for auth/inventory flow only)        │
│  • Does NOT: run HTTP, queues, or background API threads                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  LAYER 2: C API (ABI boundary)                                             │
│  star_api.h + star_api.dll exports                                         │
│  • star_api_queue_pickup_with_mint, star_api_queue_add_item                 │
│  • star_api_has_item, star_api_get_inventory, star_api_use_item             │
│  • star_api_authenticate, star_api_init, star_api_invalidate_inventory_cache │
│  • star_api_complete_quest_objective, star_api_send_item_to_avatar, etc.     │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  LAYER 3: C# STARAPIClient (all heavy lifting)                              │
│  • Single inventory cache (get/has/use are cache-first)                     │
│  • Dedicated queues: add_item (merge by type, pickup-with-mint), use_item,   │
│    quest objectives; each has its own worker and flush-by-category           │
│  • Generic background queue: auth, get avatar, get inventory, quests, NFTs,  │
│    send item; one worker for all one-off operations (Queue* for each)       │
│  • HTTP to WEB5 STAR API and WEB4 OASIS API                                 │
│  • No game-specific code                                                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## What the C# client owns (generic, no game logic)

| Responsibility | Where it lives | What the game does |
|----------------|----------------|---------------------|
| **Auth (SSO/beam-in)** | C# client + optional star_sync (async auth thread + callback) | Calls `star_sync_auth_start` (or equivalent); no auth HTTP in game. |
| **Inventory cache** | C# client only | Calls `star_api_has_item`, `star_api_get_inventory`; client uses cache and hits API only when needed. |
| **Pickup (add item)** | C# client | Calls `star_api_queue_add_item` (no mint) or `star_api_queue_pickup_with_mint` (mint + add). Client queues and processes in background. |
| **Mint + add** | C# client | One call: `star_api_queue_pickup_with_mint`. Client mints (if do_mint), then adds item with NFT id, in its existing add-item worker. |
| **Use item (e.g. door)** | C# client (cache + API) | Calls `star_api_has_item` then `star_api_use_item` (or queue/flush). No game-side use-item queue. |
| **Quest objectives** | C# client (optional queue) | Calls `star_api_complete_quest_objective` (fire-and-forget or queued). |
| **Send item** | C# client | Calls `star_api_send_item_to_avatar` (or async via star_sync). |
| **Config (URLs, mint flags, provider)** | Game reads oasisstar.json / CVars; client uses values passed per call | Game passes provider, send_to_address, etc. into `star_api_queue_pickup_with_mint`; client does not read game config. |

The game never implements:

- A separate inventory cache
- Background threads for mint or add_item
- Sync/merge logic for “pending” vs “API” inventory
- Retry or batching logic for API calls

---

## Queue and background design (non-blocking UI/game thread)

The C# client uses two kinds of queues so the UI/game thread never has to block on network calls.

**Dedicated queues (add-item, use-item, quest-objective)**  
These have their own worker and queue because they are high-frequency and benefit from batching and merging. The add-item worker merges by item type (e.g. many “+1 shell” into one or a few API calls), uses a batch window, and handles pickup-with-mint (mint then add) in one flow. Use-item and quest-objective can batch in a time window. You also get flush-by-category: `FlushAddItemJobsAsync()` means “wait until all pending add-item work is done” (e.g. before a checkpoint), without waiting on get-avatar or get-inventory. Separate workers mean a flood of add-item does not block other API calls.

**Generic background queue (everything else)**  
One shared worker runs all other operations: auth, get current avatar, get inventory, has item, start/complete quest, create quest, add/remove quest objective, get active quests, create/mint/deploy NFT, get NFT collection, send item to avatar/clan. These are low-frequency and do not need batching or merge-by-type; the goal is only “run off the calling thread and return one result per call.” One generic queue is enough for that. Each of these methods has a `Queue*` overload (e.g. `QueueAuthenticateAsync`, `QueueGetCurrentAvatarAsync`) that enqueues the work and returns a `Task` that completes when the operation finishes.

**Why keep both**  
Dedicated queues give you batching, merging, and flush-by-category for gameplay-critical, high-volume operations. The generic queue gives you a consistent non-blocking API for every other method without adding a separate queue per operation. Migrating everything into one generic queue would lose batching and flush semantics; migrating the generic operations into the add-item worker would mix unrelated work and complicate flush and ordering.

**Why not a dedicated queue per generic operation**  
Giving each of the generic operations (auth, get avatar, get inventory, etc.) its own dedicated queue and worker would be overkill. Those calls are low-frequency (you do not call get-avatar or authenticate in a tight loop), so there is no throughput or batching benefit. One generic worker is enough to keep the caller from blocking. Extra queues and workers would add complexity with no real gain.

---

## Minimal hooks in the game

To port a new game, you only need to hook a small set of events and call the C API.

### 1. Startup / shutdown

- **Init:** Call `star_api_init` (config: STAR API URL, WEB4 URL, etc.).
- **Shutdown:** Optional cleanup (client can be left to process in-flight work).

### 2. Beam-in (login)

- **Trigger:** User chooses “Beam in” or equivalent.
- **Game calls:** `star_sync_auth_start(username, password, callback, userdata)` (or equivalent that ends up calling the client’s auth).
- **Per frame:** Call `star_sync_pump()` so auth completion (and any other star_sync callbacks) run on the main thread.
- **No game logic:** No auth HTTP, no token handling; client does it.

### 3. Pickup (collect item)

- **Trigger:** Player picks up key, weapon, ammo, etc.
- **Game calls:**
  - If **mint enabled** for this category:  
    `star_api_queue_pickup_with_mint(item_name, description, game_source, item_type, 1, provider, send_to_address, quantity)`  
  - Else:  
    `star_api_queue_add_item(item_name, description, game_source, item_type, NULL, quantity, 1)`  
- **Optional:** `star_api_complete_quest_objective(quest_id, objective_id, game_source)` for quest progress.
- **No game logic:** No mint HTTP, no add_item batching, no threads; client queues and runs everything in the background.

### 4. Door / use item

- **Trigger:** Player tries to open a door (or use a key/item).
- **Game calls:**  
  `star_api_has_item(key_name)` → if true, allow door and optionally  
  `star_api_use_item(key_name, context)` (or queue + flush if using use_item queue).
- **No game logic:** No inventory merge; client’s cache is the source of truth.

### 5. Inventory UI

- **Trigger:** Player opens inventory overlay.
- **Game calls:** `star_api_get_inventory(&list)`, then iterate list to fill UI; `star_api_free_item_list(list)` when done.
- **Optional:** After send-item or after beam-in, call `star_api_invalidate_inventory_cache()` so next get sees fresh data.
- **No game logic:** No game-side cache; client returns cached or fetches when needed.

### 6. Config

- **Game reads:** oasisstar.json (or equivalent) for: `star_api_url`, `oasis_api_url`, `mint_weapons`, `mint_keys`, `nft_provider`, `send_to_address_after_minting`, etc.
- **Game passes:** Those values into the C API (e.g. into `star_api_queue_pickup_with_mint`). The client does not read config files; it stays generic.

---

## C API surface (games only need this)

Games depend only on the C ABI in `star_api.h` (and optionally `star_sync.h` for auth/inventory flow). Summary:

| Purpose | C API | Notes |
|--------|--------|--------|
| Init | `star_api_init`, `star_api_set_oasis_base_url` | Once at startup. |
| Auth | `star_sync_auth_start`, `star_sync_auth_get_result`, … | Or equivalent that drives client auth. |
| Pickup (no mint) | `star_api_queue_add_item` | Client queues and processes in background. |
| Pickup (with mint) | `star_api_queue_pickup_with_mint` | Client mints then adds in background; same worker as add_item. |
| Flush (optional) | `star_api_flush_add_item_jobs` | Only if game needs to block until queue is empty; usually not required. |
| Door / has item | `star_api_has_item` | Cache-first in client. |
| Use item | `star_api_use_item` or queue + `star_api_flush_use_item_jobs` | Client updates cache/API. |
| Inventory UI | `star_api_get_inventory`, `star_api_free_item_list` | Client returns cached or fetches. |
| Cache refresh | `star_api_invalidate_inventory_cache` | After send-item or when game knows inventory changed elsewhere. |
| Quests | `star_api_complete_quest_objective`, `star_api_start_quest`, `star_api_complete_quest` | Fire-and-forget or queued in client. |
| Send item | `star_api_send_item_to_avatar`, … | Or async via star_sync. |

No game-specific APIs; the same surface is used by ODOOM, OQuake, and any future game.

---

## star_sync: optional thin layer for auth/inventory flow

**star_sync** (C, in each game tree or shared) is an optional layer that only coordinates **async auth** and **async inventory refresh** with main-thread callbacks (e.g. `star_sync_pump()`). It does **not** implement:

- Mint
- Add-item queue
- Inventory cache

Those stay in the C# client. star_sync only:

- Runs auth on a background thread and invokes a callback on the main thread when done.
- Can start an inventory refresh (sync local items then get_inventory) and invoke a callback when done.

So: **heavy work (HTTP, queues, mint, add) is in the client; star_sync only adds async flow for auth (and optionally inventory) so the game stays single-threaded from its perspective.**

---

## ODOOM and OQuake as reference integrations

- **ODOOM:** Pickup → `star_api_queue_pickup_with_mint` or `star_api_queue_add_item`; door → `star_api_has_item` + `star_api_use_item`; beam-in → star_sync auth; inventory UI → `star_api_get_inventory`. No game-side mint or add_item threads.
- **OQuake:** Same pattern: pickup uses `star_api_queue_pickup_with_mint` or `star_api_queue_add_item`; door check uses `star_api_has_item`; inventory and auth follow the same C API and star_sync flow.

Both games share the same client and same C API; only the hook points (where the game calls the API) are game-specific.

---

## Porting a new game: checklist

1. **Link** the STARAPIClient native DLL and use `star_api.h` (and optionally `star_sync.h`).
2. **Init** with `star_api_init` (and set WEB4 URL if needed).
3. **Beam-in:** Use star_sync auth (or equivalent) so the client performs auth; call `star_sync_pump()` (or equivalent) each frame for callbacks.
4. **Pickup:** On collect, call `star_api_queue_pickup_with_mint` or `star_api_queue_add_item`; optionally `star_api_complete_quest_objective`. No mint or add logic in the game.
5. **Door / use:** Call `star_api_has_item`; if true, allow action and optionally call `star_api_use_item`.
6. **Inventory UI:** Call `star_api_get_inventory` and display; call `star_api_free_item_list` when done.
7. **Config:** Read oasisstar.json (or similar) and pass provider, send_to_address, mint flags, etc., into the C API.

No new client code is required for a new game; the client remains generic and does all the heavy lifting.

---

## Future / TODO

### Move star_sync into the C# client

**Goal:** Implement all of **star_sync** (async auth, async inventory refresh, async send item, async use item, init/cleanup/pump) inside the C# STARAPIClient and export the same `star_sync_*` C API from the client DLL. Then remove the C implementation (`star_sync.c` / `star_sync.h` usage) from ODOOM and OQuake so games link only the client.

**Why:** One codebase for all STAR integration; no duplicate C threading (Win32/pthreads) in each game tree; client owns both API work and async flow. Same contract for games: they still call `star_sync_pump()` once per frame and get callbacks on the main thread.

**Feasibility:** Safe. The client would run auth/inventory/send/use_item on background tasks, push completed work (callback pointer + user_data) into a queue, and when the game calls `star_sync_pump()` the client would drain the queue and invoke each callback from C# (e.g. via `Marshal.GetDelegateForFunctionPointer`). Keep `star_sync_local_item_t` and all function signatures ABI-compatible.

**TODO:**

- [ ] Implement in C#: async auth, async inventory (with local-items sync), async send item, async use item; completion queue and pump that invokes game callbacks.
- [ ] Export all `star_sync_*` symbols from the client (init, cleanup, pump, auth_start/get_result, inventory_start/get_result/clear_result, send_item_start/get_result, use_item_start/get_result, single_item).
- [ ] Remove `star_sync.c` from ODOOM and OQuake builds; ensure `star_sync.h` (or equivalent) remains for the API; games call the client’s exports.
- [ ] Re-test beam-in, inventory refresh, send item, and door use-item in both games after the move.

---

## Related docs

- **[INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md)** – Cross-game item sharing, quests, setup.
- **[STARAPIClient/README.md](STARAPIClient/README.md)** – C# client behaviour, cache, queues, build.
- **[DEVELOPER_ONBOARDING.md](DEVELOPER_ONBOARDING.md)** – Repo setup, build, config.
