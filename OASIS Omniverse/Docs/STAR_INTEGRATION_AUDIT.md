# STAR API Integration – Full Audit & Design Report

This document describes how the STAR API integration works across **STARAPIClient** (C#), **ODOOM** (UZDoom), and **OQuake**, and why the current design is brittle. It is intended to support a redesign so each module is self-contained, robust under load, and does not depend on timing or fragile flags.

---

## Build and edit locations

**You only run BUILD DOOM or BUILD QUAKE.** Those scripts copy from the OASIS repo into the game trees (UZDoom, vkQuake) and then build there. **All edits must be in the OASIS repo only:**

| Build        | Copy-from (edit here)                    | Copy-to (do not edit)   |
|-------------|------------------------------------------|--------------------------|
| BUILD DOOM  | `OASIS Omniverse/ODOOM/`                 | UZDoom (e.g. `C:\Source\UZDoom`) |
| BUILD QUAKE | `OASIS Omniverse/OQuake/` (Code, build)  | vkQuake (e.g. `C:\Source\vkQuake`) |

STARAPIClient is under `OASIS Omniverse/STARAPIClient/`. Editing files under UZDoom or vkQuake is overwritten on the next build.

### Where shared C code lives (star_sync)

The **star_sync** layer (async auth/inventory/send-item, C) is shared. The copy flow determines where to edit it:

- **BUILD DOOM:** `BUILD ODOOM.bat` first copies **STARAPIClient** `star_sync.c` and `star_sync.h` **into ODOOM** (if they exist in STARAPIClient), then copies ODOOM files (including `star_sync.*`) to UZDoom.
- **BUILD QUAKE:** `BUILD_OQUAKE.bat` always copies **STARAPIClient** `star_sync.c` and `star_sync.h` **into OQuake/Code** (overwriting), then OQuake/Code to quake-rerelease-qc and vkQuake/Quake. OQuake/Code no longer keeps its own copy.

**Edits to star_sync for both Doom and Quake must be in `OASIS Omniverse/STARAPIClient/star_sync.c` and `star_sync.h`** — single source of truth. OQuake/Code and ODOOM no longer contain committed copies; build scripts copy from STARAPIClient only.

### quake-rerelease-qc vs vkQuake (why two trees, no duplication of “the same engine”)

**quake-rerelease-qc** and **vkQuake** are not two copies of the same thing:

| Repo / tree | What it is | What gets built |
|-------------|------------|------------------|
| **quake-rerelease-qc** | **QuakeC source** (game logic): `.qc` files (defs, items, doors, etc.), maps, 2021 rerelease content. This is the *content* that runs *inside* the engine. | **progs.dat** (and related game data). No executable. |
| **vkQuake** | **Engine** (C/C++): the executable that loads progs and runs the game. OQuake = vkQuake + OASIS STAR integration. | **OQUAKE.exe** (or vkquake.exe), which loads progs from the game dir. |

So quake-rerelease-qc is the **game/mod side** (QuakeC + data); vkQuake is the **engine**. There is no duplication of the engine between them.

**If you only run BUILD QUAKE, quake-rerelease-qc is redundant.**

The script does **not** compile progs.dat or copy any output from quake-rerelease-qc into the exe. The exe is built only from vkQuake’s C/C++. So for the typical workflow (“I only run BUILD QUAKE”):

- **You only need vkQuake.** Set `VKQUAKE_SRC` (e.g. `C:\Source\vkQuake`). The script copies the integration into vkQuake and builds the exe. **quake-rerelease-qc (QUAKE_SRC) is optional**; if you don’t set it or the folder is missing, the script skips copying there and still builds the engine.
- **quake-rerelease-qc** is only needed if you separately compile QuakeC (defs.qc, items.qc, etc.) to progs.dat and want the OQuake integration files in that tree. The engine loads progs.dat at **runtime** from your game dir (id1/, -basedir); it is not part of the BUILD QUAKE step.

**Why does BUILD_OQUAKE copy the same C files into both?**

BUILD_OQUAKE.bat copies OQuake integration (e.g. `oquake_star_integration.c`, `star_sync.c`, `star_api.h`) into:

1. **quake-rerelease-qc** – so the QuakeC tree has the headers and any C sources for reference, or for a build that compiles progs in that tree and expects those files nearby.
2. **vkQuake/Quake** – this is the tree that actually **compiles** the engine; the exe is built from here.

Only **vkQuake** is compiled into the executable. The copy into quake-rerelease-qc is for consistency/reference; the “single source of truth” for the C integration is **OASIS Omniverse/OQuake/Code/** (and STARAPIClient for star_sync when used). If you only care about running OQuake, you need vkQuake built with those files; quake-rerelease-qc is needed for the **QuakeC progs** (keys, doors, etc.) that the engine loads at runtime.

### Star_sync migration into STARAPIClient (one place for both Quake and Doom)

**Plan:** star_sync (C layer for async auth/inventory/send/use with main-thread callbacks) should live in **one place** – STARAPIClient – and be copied from there for both ODOOM and OQuake, so it is not duplicated and cannot diverge between the two games.

**Current stage**

| Aspect | Status | Notes |
|--------|--------|-------|
| **ODOOM** | **Done** | BUILD ODOOM copies **STARAPIClient** `star_sync.c` / `star_sync.h` into ODOOM, then ODOOM → UZDoom. Doom uses only STARAPIClient's star_sync. |
| **OQuake** | **Done** | BUILD_OQUAKE always copies **STARAPIClient** `star_sync.c` / `star_sync.h` into OQuake/Code (overwriting), then OQuake/Code → quake-rerelease-qc and vkQuake. OQuake/Code no longer keeps its own star_sync; single source is STARAPIClient. |
| **Full migration** | **Done** | Both Doom and Quake use only STARAPIClient's star_sync. |

**What currently uses star_sync (C layer)**

Both games use star_sync for **async wrappers** so the main thread never blocks on auth/send/use; completion is delivered via **star_sync_pump()** and callbacks.

| Consumer | star_sync APIs used |
|----------|---------------------|
| **ODOOM** (uzdoom_star_integration.cpp) | `star_sync_init`, `star_sync_auth_start`, `star_sync_pump`, `star_sync_send_item_start` / `star_sync_send_item_in_progress` / `star_sync_send_item_get_result`, `star_sync_use_item_start` / `star_sync_use_item_in_progress`, `star_sync_inventory_deliver_result` (from operation callback). |
| **OQuake** (oquake_star_integration.c) | Same pattern: `star_sync_init`, `star_sync_auth_*`, `star_sync_pump`, `star_sync_send_item_*`, `star_sync_use_item_*`, `star_sync_inventory_deliver_result`, `star_sync_inventory_in_progress`. |

star_sync does **not** implement the API; it runs **star_api_*** calls on a worker thread and invokes the game's callback on the main thread. So it is a thin C "async + pump" layer on top of the client.

**What uses STARAPIClient (C# / star_api.dll)**

- **All `star_api_*` native exports** (init, authenticate, get_inventory, set_operation_callback, send_item, use_item, quest APIs, etc.) are implemented in **STARAPIClient** (C# NativeAOT) and exposed via the native AOT DLL (star_api.dll / libstar_api.so).
- **ODOOM and OQuake** both link to that DLL and call `star_api_*` for every operation that talks to the STAR backend. They also call **star_sync_*** for the async/callback pattern; star_sync in turn calls `star_api_*` on a background thread.

**Summary:** STARAPIClient is the single implementation of the STAR API (C#). star_sync is a small C layer that "async-ifies" a subset of those calls for the games. star_sync now lives only in STARAPIClient and is copied from there for both Doom and Quake (migration complete).

### C API export automation (no manual .def or stubs)

When you add a new `[UnmanagedCallersOnly(EntryPoint = "star_api_foo", ...)]` (or `star_sync_*`) in C#, **you do not need to edit star_api.def or add stub files.** BUILD DOOM and BUILD QUAKE both run the STAR API build/deploy first, which:

1. **Regenerates star_api.def** from the C# sources via `STARAPIClient/Scripts/generate_star_api_def.ps1` (invoked by `publish_and_deploy_star_api.ps1`). The script scans `StarApiClient.cs` and `StarSyncExports.cs` for `EntryPoint = "name"` and writes the full export list. One symbol is excluded from the .lib by design: `star_api_authenticate_with_jwt_out` (game forwarder provides it at runtime).
2. **Builds star_api.lib** from that .def so the import library has every export. Games link against this .lib and resolve all symbols without LNK2001.
3. **Deploys** the DLL, .lib, and header to the game trees. BUILD DOOM / BUILD QUAKE then build the engine against the deployed .lib.

So: add the export in C#, declare it in `star_api.h` for the games to call, and run BUILD DOOM or BUILD QUAKE as usual. No manual .def edits and no per-symbol stub files. To regenerate the .def by hand (e.g. after adding exports without running a full deploy), run `STARAPIClient\Scripts\generate_star_api_def.ps1`.

### Quest progress endpoint (POST /api/quests/{id}/progress)

The client sends in-game progress (kills, pickups, level time) to `POST {baseApiUrl}/api/quests/{questId}/progress`. **This route is implemented in STAR ODK** (`STAR ODK/.../Controllers/QuestsController.cs`). If your base URL points at ONODE only (no STAR ODK or proxy), you will get **404** for progress calls. Either point the STAR API base URL at the STAR ODK service that exposes `/api/quests/{id}/progress`, or add an equivalent route/proxy on ONODE. The client skips sending when all deltas are zero (no 0-delta progress calls).

### Star_sync: in-client (C#) default vs C implementation

STARAPIClient provides **two** ways to get the star_sync_* API. **The default is the in-client (C#) implementation.**

| Mode | How it works | When to use |
|------|----------------|-------------|
| **In-client (C#) — default** | **OASIS_STAR_SYNC_IN_CLIENT=1** (set by BUILD ODOOM / BUILD_OQUAKE by default). The symbols `star_sync_*` are exported from star_api.dll (implemented in `StarSyncExports.cs`). Do **not** compile `star_sync.c`; link only star_api. | Default; one less C file to build and maintain. |
| **C implementation** | Set **OASIS_STAR_SYNC_IN_CLIENT=0** (or undefine it in the build). Build scripts copy `star_sync.c` and `star_sync.h`; the game compiles `star_sync.c` and links it with star_api. | If you need to fall back to the C implementation; `star_sync.c` remains in the repo. |

**Default (in-client):** BUILD ODOOM.bat and BUILD_OQUAKE.bat set `OASIS_STAR_SYNC_IN_CLIENT=1` by default, so the game uses star_sync from star_api.dll and does not compile `star_sync.c`.

**To switch back to the C implementation**

1. Set **OASIS_STAR_SYNC_IN_CLIENT=0** before running the build (e.g. `set OASIS_STAR_SYNC_IN_CLIENT=0` then run BUILD ODOOM.bat), or change the default from `1` to `0` in the one build script you use (one line).
2. Rebuild. The C implementation in `star_sync.c` will be used again.

The C file **star_sync.c** remains in the repo and is still the single source for the C implementation; it is not deleted so you can switch back at any time.

### JWT expiration and session handling (Doom and Quake)

Both ODOOM and OQuake persist **jwt_token** and **refresh_token** in **oasisstar.json** so the user stays logged in between sessions. The C# client handles expiration and refresh as follows.

| What | Behaviour |
|------|-----------|
| **Auto-renew (stay logged in)** | **On load (restore session):** Before validating the saved JWT with GET avatar/current, the client checks JWT expiry; if the token is expired or expiring within 60 seconds, it calls the **refresh-token** endpoint first, then proceeds with GET. So Quake/Doom restore succeeds without the user seeing "beamed in" with broken tracker. **On any API 401:** The client also calls refresh once, retries the request; if refresh fails, session is cleared. A background task runs periodically and refreshes the JWT shortly before expiry when possible. |
| **When refresh fails or there is no refresh token** | The client **clears** the in-memory session (JWT, refresh token, avatar id) and sets an internal "session expired" flag. Later API calls return a clear "Session expired. Please beam in again." so the game does not hammer the API. |
| **Clearing tokens in oasisstar.json** | When the game **saves** oasisstar.json (on exit, config save, etc.), it calls **star_api_is_session_expired()**. If that returns 1, the game clears **jwt_token** and **refresh_token** in memory and does not write them to the file. So the next launch does not try to restore a dead session; the user sees the login/beam-in flow again. |
| **Where tokens live** | **In memory:** STARAPIClient holds JWT and refresh token; games hold copies in `g_odoom_saved_jwt` / `g_oq_saved_jwt` and refresh for writing to oasisstar.json. **On disk:** oasisstar.json (written only when saving config; cleared when session expired). |

**Summary:** Yes, we clear token and refresh token when the session is expired (in memory when refresh fails; in oasisstar.json when the game next saves). Yes, we try to automatically renew the token so the user stays logged in (on 401 + background refresh); both Doom and Quake use the same client and behaviour.

**Why refresh-token was returning HTTP 401 (Unauthorized):** ONODE’s **JwtMiddleware** runs on every request. If the request has an `Authorization: Bearer <token>` header, it validates the JWT (including lifetime). When the client called POST `/api/avatar/refresh-token` to renew an expired token, it was still sending the **expired** JWT in the header (from `HttpClient.DefaultRequestHeaders`). The middleware validated it, saw “token expired”, and returned **401** before the request reached the refresh-token controller. So the refresh endpoint never ran. **Fix:** In `TryRefreshTokenAsync`, the client now **removes** the `Authorization` header before sending the refresh request. The refresh-token endpoint uses only the `refreshToken` cookie; it does not require a valid JWT. After a successful refresh, the new JWT is set on the client and used for subsequent requests.

---

## 1) STARAPIClient (C# NativeAOT)

**File:** `OASIS Omniverse/STARAPIClient/StarApiClient.cs`

### 1.1 Significant mutable shared state (fields, locks, flags)

| State | Guard / purpose |
|-------|------------------|
| **`_stateLock`** | Protects `_httpClient`, `_baseApiUrl`, `_oasisBaseUrl`, `_jwtToken`, `_refreshToken`, `_avatarId`, `_loggedInUsername`, `_cachedActiveQuestId`, `_cachedActiveObjectiveId`, `_initialized`. Used for auth/session and avatar/quest cache. |
| **`_inventoryCacheLock`** | Protects `_cachedInventory`, `_inventoryFetchTask`. Single-flight inventory fetch and cache. |
| **`_questsCacheLock`** | Protects `_questsCacheString`, `_cachedQuestList`, `_questsRefreshInProgress`, and all quest filter/log state. |
| **`_localPendingLock`** | Protects `_localPending` (pickup delta: name → pending qty). |
| **`_jobLock`** | Protects start/stop of add-item, use-item, quest-objective workers (`_jobWorker`, `_useItemJobWorker`, `_questObjectiveJobWorker`, their CTS). |
| **`_genericBackgroundLock`** | Protects `_genericBackgroundWorker`, `_genericBackgroundCts`. |
| **`_questsRefreshInProgress`** | Prevents multiple concurrent quest cache refreshes (set under `_questsCacheLock`). |
| **`_questObjectivesHydrating`** | Set of quest IDs currently fetching objectives on-demand; avoids duplicate fetches. |
| **`_questTrackerSavedSinceLastGet`** | When true, GET avatar/current must not overwrite `_cachedActiveQuestId` / `_cachedActiveObjectiveId` (user saved after GET started). Set in `SetActiveQuestAndObjectiveAsync`; cleared after applying in `GetCurrentAvatarAsync`. |
| **`_addItemSignal`, `_useItemSignal`, `_questObjectiveSignal`, `_genericBackgroundSignal`** | SemaphoreSlims for add-item, use-item, quest-objective, and generic background queues. |
| **`_activeAddItemJobs`, `_activeUseItemJobs`, `_activeQuestObjectiveJobs`** | Counts under `_jobLock` / worker logic; Interlocked in add/use/quest workers. |
| **`_pendingXp`** | Interlocked; queued XP for flush. |
| **`_lastMintLock`** | Protects `_lastMintItemName`, `_lastMintNftId`, `_lastMintHash`. |
| **StarApiExports: `Sync`, `NativeStateLock`, `BackgroundErrorLock`** | Protect `_client`, `_lastError`, `_callback`, `_operationCallback`, `_lastBackgroundError`. |
| **`_starDebug`** | `volatile int`; debug flag from games. |

### 1.2 Auth/session flow

- **SetSavedSession** (~429–447): Synchronous. Validates JWT non-empty, extracts avatar id from JWT, sets `_jwtToken` and `Authorization` under `_stateLock`. Does **not** invoke callback. Used to load session from e.g. `oasisstar.json` before restore.
- **RestoreSessionAsync** (~449–473): Reads `_jwtToken` under `_stateLock`. If missing, returns fail and does not invoke callback. Calls GET avatar/current to validate; on success updates avatar cache and **invokes operation callback with `StarApiOpProfileLoaded`** so the game runs “beamed-in” logic. Does **not** call RefreshAvatarProfileInBackground; the single GET is the profile load. Games typically call this via `star_api_restore_session` → `QueueRestoreSessionAsync`.
- **AuthenticateAsync** (~222–390): POST to WEB4 auth; parses JWT/RefreshToken, sets `_jwtToken`/`_refreshToken` and Authorization. Does **not** invoke profile-loaded callback; comment states games call `star_api_refresh_avatar_profile` in auth-done handler and “profile loaded” runs when that refresh completes.
- **JWT/refresh**: `_jwtToken` and `_refreshToken` set on auth and in SetSavedSession. No automatic refresh logic in the code audited; token is sent as Bearer. Refresh token is stored but not used in the flows seen.
- **Session cleared**: In **Cleanup()** (~605–618): under `_stateLock`, `_jwtToken`, `_refreshToken`, `_avatarId` set to null, `_initialized = false`, `_httpClient` disposed. **Dispose()** (~3017) calls Cleanup. **StarApiCleanup** (exports) disposes `_client` and sets it to null under `Sync`.

### 1.3 Inventory flow

- **GetInventoryAsync** (~703–755): Under `_inventoryCacheLock`, if `_cachedInventory` is set returns merged list (with `MergeLocalPendingIntoInventory`) and invokes callback. If cache miss, sets `_inventoryFetchTask = FetchInventoryOnceAsync()` if null and awaits that task. On completion clears `_inventoryFetchTask`, and on success sets `_cachedInventory`; if fetch returns 0 items but current cache has items, keeps prior cache. Returns merged list.
- **FetchInventoryOnceAsync** (~809–858): Ensures avatar id (EnsureAvatarIdAsync), then GET api/avatar/inventory; one retry on network error (200 ms). On success maps to `List<StarItem>` and returns. Used only as the single-flight task stored in `_inventoryFetchTask`.
- **InvalidateInventoryCache** (~861–868): Under `_inventoryCacheLock` sets `_cachedInventory = null`, `_inventoryFetchTask = null`. **ClearCache** calls InvalidateInventoryCache (and InvalidateQuestCache).
- **Native usage**: **star_api_get_inventory** (exports ~5439) calls `client.GetInventoryAsync().GetAwaiter().GetResult()` (blocking). **star_api_invalidate_inventory_cache** calls `client.InvalidateInventoryCache()`. **star_api_has_item** calls `HasItemAsync(...).GetAwaiter().GetResult()` (can trigger GetInventoryAsync). Add-item worker and **AddItemCoreAsync** update `_cachedInventory` in place on add success (no invalidation there to avoid refetch and stale data).

### 1.4 Quest flow

- **Cache**: `_questsCacheString` (serialized full list), `_cachedQuestList` (structured), `_questsRefreshInProgress`, `_questObjectivesCacheVersion`, `_questObjectivesHydrating`; all under `_questsCacheLock`. Top-level / sub-quests / objectives / prereqs served by filtering `_cachedQuestList`. Objectives for a quest can be filled on-demand and merged into `_cachedQuestList` (MergeObjectivesIntoQuestCache), bumping `_questObjectivesCacheVersion`.
- **Refresh**: **RequestQuestCacheRefreshInBackground** sets `_questsRefreshInProgress` and runs a background task that fetches quests and updates cache. **EnsureQuestsCacheInBackground** (used when cache is empty from TryGetQuestsCache / TryGetTopLevelQuestsCache / TryGetQuestObjectivesCache) does the same. **InvalidateQuestCache** clears `_questsCacheString` and `_cachedQuestList`.
- **GetActiveQuestId / GetActiveObjectiveId**: Return `_cachedActiveQuestId` and `_cachedActiveObjectiveId` under `_stateLock`; these are set by GetCurrentAvatarAsync (from GET avatar/current) unless `_questTrackerSavedSinceLastGet` is true, and by SetActiveQuestAndObjectiveAsync.
- **Native**: **star_api_get_quests_string**, **star_api_get_top_level_quests_string**, **star_api_get_quest_sub_quests_string**, **star_api_get_quest_objectives_string**, **star_api_get_quest_tracker_objectives_string**, **star_api_get_quest_tracker_active_objective_index**, **star_api_get_active_quest_id**, **star_api_get_active_objective_id**, **star_api_get_tracker_quest_name**, **star_api_invalidate_quest_cache**, **star_api_refresh_quest_cache_in_background** all go through the same cache/try-get/ensure-background logic. Quest string APIs use TryGet* and return “Loading” (null/empty) when cache not ready; they do not block.

### 1.5 Background worker / RunOnBackgroundAsync

- **ProcessGenericBackgroundJobsAsync** (~4119–4148): Single loop: wait on `_genericBackgroundSignal`, then drain `_genericBackgroundQueue` and await each job in order. **One worker; FIFO.** Exceptions in a job are swallowed (TCS already set); cancellation stops the loop.
- **RunOnBackgroundAsync** (~4152–4186): Enqueues a wrapper that runs the operation and completes a TCS; releases `_genericBackgroundSignal`, starts worker if needed. Used for: QueueAuthenticateAsync, QueueRestoreSessionAsync, QueueGetCurrentAvatarAsync, QueueHasItemAsync, QueueGetInventoryAsync, RefreshAvatarProfileInBackground (fire-and-forget), RequestQuestCacheRefreshInBackground (fire-and-forget), EnsureQuestsCacheInBackground (fire-and-forget), and all other Queue* (start quest, complete quest, create monster NFT, send item, etc.). So **a single background thread processes all of these in order; no parallelism among them.**
- Add-item / use-item / quest-objective have **separate** workers (ProcessAddItemJobsAsync, ProcessUseItemJobsAsync, ProcessQuestObjectiveJobsAsync) with their own queues and signals; not the generic worker.

### 1.6 Native entry points (UnmanagedCallersOnly) and shared state

- **star_api_init**: Creates new `StarApiClient`, Init (sets `_baseApiUrl`, `_oasisBaseUrl`, `_avatarId`, `_jwtToken`, clears inventory/quest task state, starts add/use/quest workers). Uses `Sync` for client replace.
- **star_api_authenticate / star_api_authenticate_with_jwt_out**: Blocking call to AuthenticateAsync; reads/writes session under `_stateLock`; no operation callback from auth.
- **star_api_set_saved_session**: SetSavedSession; `_stateLock` for JWT/avatar.
- **star_api_restore_session**: Queues RestoreSessionAsync (generic worker); touches session under `_stateLock`; invokes operation callback on success.
- **star_api_get_current_username / star_api_get_current_jwt**: Read under `_stateLock`.
- **star_api_cleanup**: Disposes client under `Sync`.
- **star_api_has_item**: GetInventoryAsync (blocking); uses `_inventoryCacheLock`, `_cachedInventory`, `_inventoryFetchTask`; can invoke operation callback.
- **star_api_get_inventory**: GetInventoryAsync (blocking); same inventory state; allocates native list and copies items.
- **star_api_free_item_list**: Frees native list (no client state).
- **star_api_invalidate_inventory_cache / star_api_clear_cache**: InvalidateInventoryCache / ClearCache; `_inventoryCacheLock`, `_questsCacheLock`.
- **star_api_get_quests_string**, **star_api_get_top_level_quests_string**, **star_api_get_quest_sub_quests_string**, **star_api_get_quest_objectives_string**, **star_api_get_quest_prereqs_string**, **star_api_get_quest_objective_requirements_string**, **star_api_get_quest_tracker_objectives_string**, **star_api_get_quest_tracker_active_objective_index**: TryGet* under `_questsCacheLock`; may call EnsureQuestsCacheInBackground (enqueue on generic worker).
- **star_api_get_tracker_quest_name**, **star_api_get_active_quest_id**, **star_api_get_active_objective_id**: Read `_cachedActiveQuestId` / `_cachedActiveObjectiveId` under `_stateLock` (and quest list for name).
- **star_api_refresh_avatar_profile**: Starts RefreshAvatarProfileInBackground (generic worker); on completion invokes operation callback (ProfileLoaded).
- **star_api_set_active_quest**: SetAvatarId-like path + SetActiveQuestAndObjectiveAsync (can be queued); sets `_cachedActiveQuestId`, `_cachedActiveObjectiveId`, `_questTrackerSavedSinceLastGet` under `_stateLock`.
- **star_api_add_item**, **star_api_queue_add_item**, **star_api_flush_add_item_jobs**, **star_api_queue_use_item**, **star_api_flush_use_item_jobs**, **star_api_queue_add_xp**, **star_api_queue_monster_kill**: Add/use item workers and `_localPending` / `_cachedInventory` updates.
- **star_api_get_avatar_xp**: Returns `_cachedAvatarXp` (set by GetCurrentAvatarAsync).
- **star_api_set_callback / star_api_set_operation_callback**: Write under NativeStateLock (`_callback` / `_operationCallback`).
- **star_api_set_oasis_base_url**, **star_api_get_avatar_id**, **star_api_set_avatar_id**: Client methods under `_stateLock`.
- **star_api_get_last_error**, **star_api_consume_last_mint_result**, **star_api_consume_last_background_error**, **star_api_consume_console_log**: Last-error / mint / background-error / console-log state in exports.

### 1.7 Timing/order dependencies (client)

- **Restore vs beam-in**: RestoreSessionAsync invokes ProfileLoaded when GET avatar/current succeeds. Games that then call `star_api_refresh_avatar_profile` get a second GET and a second ProfileLoaded; design intent is “profile loaded” when refresh completes (cache has XP/quest).
- **Must call SetSavedSession before RestoreSession** when using saved JWT; otherwise RestoreSession has no JWT.
- **star_api_get_inventory** blocks until cache is filled or fetch completes; first call can block on network. Quest string APIs do not block; they return “Loading” and trigger background fetch.
- **Generic worker**: All Queue* and refresh operations share one worker; order of completion matches enqueue order. If RestoreSession is queued then RefreshAvatarProfile, profile is validated first, then refreshed.

---

## 2) ODOOM (UZDoom STAR integration)

**Files:** `OASIS Omniverse/ODOOM/uzdoom_star_integration.cpp`, `star_sync.c`, `star_sync.h` (and deployed copies under build/).

### 2.1 Loading star_api.dll and resolving symbols

- **Windows**: Under `ODOOM_STAR_API_SESSION_IMPL`, session/JWT APIs are implemented locally with `GetModuleHandleA("star_api.dll")` and `GetProcAddress` for: `star_api_authenticate_with_jwt_out`, `star_api_set_saved_session`, `star_api_restore_session`, `star_api_get_current_username`, `star_api_get_current_jwt`. The game links to a stub/import lib and the real implementation is in the DLL; session symbols are resolved at first use from the already-loaded DLL.
- **Non-Windows**: Same pattern with `dlopen("libstar_api.so", RTLD_NOW | RTLD_NOLOAD)` or `dlopen(nullptr)` and `dlsym`. No explicit LoadLibrary/dlopen of the STAR DLL at init; the engine loads it as a dependency (e.g. linked lib).

### 2.2 Startup flow: STAR init, restore vs beam-in, order of star_api_* calls

- **UZDoom_STAR_Init** (uzdoom_star_integration.cpp ~2709): Calls **star_sync_init()**, loads oasisstar.json into `g_odoom_json_config_path` and applies config (e.g. mint, URLs), sets default face, default binds, then **StarTryInitializeAndAuthenticate(true)**.
- **StarTryInitializeAndAuthenticate(true)** (~2464): If already initialized returns. If not:
  - **star_api_init(&g_star_config)** once (`!g_star_client_ready`), then sets `g_star_client_ready = true`.
  - **star_api_set_oasis_base_url** if configured.
  - If username+password: **star_sync_auth_start(username, password, ODOOM_OnAuthDone, nullptr)**, sets `g_star_async_auth_pending = true`, returns false (result in pump).
  - Else if api_key+avatar_id: sets `g_star_initialized = true` and returns (no restore).
  - Else if **g_odoom_saved_jwt[0]**: **star_api_set_saved_session(g_odoom_saved_jwt)** then **star_api_restore_session()** (async). On success sets `g_star_initialized = true`, **star_api_refresh_avatar_profile()**, **g_odoom_pending_loading_tracker = true**, returns.
- So order: **star_sync_init** → (optional) load oasisstar.json → **star_api_init** → **star_api_set_oasis_base_url** → then either sync auth start, or api_key path, or **set_saved_session** → **restore_session** → **refresh_avatar_profile** and pending_loading_tracker.
- **ODOOM_OnAuthDone** (auth callback from star_sync_pump): On success saves JWT, sets `g_star_initialized = true`, `g_star_just_beamed_in = true`, calls **star_api_refresh_avatar_profile()**, sets tracker CVars to "..." and "Loading...", calls **star_api_refresh_quest_cache_in_background** (if defined), **ODOOM_RefreshQuestCVars()**, saves config. So after SSO, Doom does **not** call restore_session; it uses the auth result and then refresh_avatar_profile.

### 2.3 Inventory: API, when called, loading vs loaded

- **API**: **star_api_get_inventory** (blocking). Used in **ODOOM_RefreshOverlayFromClient** (when overlay refresh is needed) and in the first-frame-after-beam-in key CVar refresh.
- **When**: RefreshOverlayFromClient is called every frame while inventory popup is open (odoom_inventory_open); when not beamed in it pushes empty. After beam-in, when overlay is closed, a 10-frame throttle calls get_inventory to refresh gold/silver key CVars. First frame after `g_star_just_beamed_in` also calls get_inventory for key CVars.
- **Loading vs loaded**: If `!g_star_initialized`, overlay is cleared (empty list, keys 0). Otherwise get_inventory is called; success → push list to CVars and update key CVars. No separate “loading” state for inventory; the blocking call provides the result for that frame.

### 2.4 Quest/tracker: APIs, when polled/refreshed, CVars/flags

- **APIs**: **star_api_get_top_level_quests_string**, **star_api_get_quest_tracker_objectives_string**, **star_api_get_quest_tracker_active_objective_index**, **star_api_set_active_quest**; also **star_api_get_active_quest_id**, **star_api_get_active_objective_id** (in auth done and in frame pump when tracker is placeholder).
- **When**: **ODOOM_RefreshQuestCVars** runs: when quest popup opens (and every 60 frames while open); when tracker id is placeholder or empty (pump fills from get_active_quest_id / get_active_objective_id and calls refresh_quest_cache_in_background + RefreshQuestCVars); when tracker id is set (every 60 frames). Detail CVars (prereqs, objectives, subquests) via **ODOOM_RefreshQuestDetailCVars** when detail popup is open (odoom_quest_detail_quest_id set).
- **Loading vs loaded**: Tracker shows "Loading..." when `odoom_quest_tracker_quest_id` is "..." (set in OnAuthDone or by **g_odoom_pending_loading_tracker** in pump). Pump replaces "..." with real id when **star_api_get_active_quest_id** returns non-empty; then RefreshQuestCVars fills title/objectives from cache. So “loaded” when quest list cache has data and tracker id is not "...".

### 2.5 star_sync_* usage and what blocks/waits

- **star_sync_init**: In UZDoom_STAR_Init (once).
- **star_sync_auth_start**: From StarTryInitializeAndAuthenticate when username+password; completion in **star_sync_pump** → **ODOOM_OnAuthDone**. No blocking in game code; pump runs per frame.
- **star_sync_inventory_***: Not used by ODOOM; C# client does inventory; Doom uses star_api_get_inventory only.
- **star_sync_send_item_start**: Used when sending item from overlay; result in **star_sync_pump** → **ODOOM_OnSendItemDone**. Send popup shows "Sending..." while **star_sync_send_item_in_progress()**; pump is called again while send popup is open so callback runs.
- **star_sync_use_item_start**: Used for “use item” from inventory (E key); completion in **star_sync_pump** → **ODOOM_OnUseItemFromInventoryDone**. **star_sync_use_item_in_progress()** guards starting another use.

All sync completion callbacks run on the main thread when the game calls **star_sync_pump()** (each frame in the STAR tick).

### 2.6 Flags/state that can get stuck or overwritten (ODOOM)

- **g_odoom_pending_loading_tracker**: Set true on restore-session success; pump clears it when setting "Loading..." CVars. If pump never runs (e.g. early exit), it stays true; next run applies "Loading..." and clears it.
- **g_star_initialized**: Set on auth success, api_key path, or restore success. Cleared in UZDoom_STAR_Cleanup. If init fails, **g_star_init_failed_this_session** is set and StarTryInitializeAndAuthenticate(false) skips retry until explicit beamin (avoids spam).
- **Tracker "..."**: Replaced when get_active_quest_id returns; if profile never loads (e.g. refresh_avatar_profile fails or is slow), tracker can stay "Loading..." until that succeeds. **ODOOM does not set star_api_set_operation_callback**, so it does not react to ProfileLoaded from the client; it only reacts to **star_sync** auth callback (ODOOM_OnAuthDone), where it sets "Loading..." and calls refresh_avatar_profile. Tracker “Loading...” is cleared by the frame pump when **star_api_get_active_quest_id** returns data (cache filled by refresh).
- **g_star_just_beamed_in**: Cleared the frame after beam-in when overlay is closed; used to refresh key CVars once. Safe.

---

## 3) OQuake (STAR integration)

**Files:** `OASIS Omniverse/OQuake/Code/oquake_star_integration.c`, `star_sync.c`, `star_sync.h` (and deployed copies).

### 3.1 DLL load, forwarders, startup flow, restore vs beam-in order

- **DLL/symbols**: Same pattern as ODOOM. With **OQUAKE_STAR_API_SESSION_IMPL**, session/JWT APIs are implemented in the game: Windows uses **GetModuleHandleA("star_api.dll")** and **GetProcAddress**; non-Windows uses **dlopen("libstar_api.so", RTLD_NOW | RTLD_NOLOAD)** or **dlopen(NULL)** and **dlsym**. **OQUAKE_STAR_API_REFRESH_AVATAR_PROFILE_IMPL** provides a local **star_api_refresh_avatar_profile** that forwards to the DLL when the linked lib doesn’t export it.
- **Startup**: **OQuake_STAR_Init** (oquake_star_integration.c ~2588): Load oasisstar.json (OQ_LoadJsonConfig), set g_star_config from CVars/env, **star_api_init(&g_star_config)**, **star_api_set_operation_callback(OQ_StarApiOperationCallback, NULL)**, **star_api_set_oasis_base_url** if set. Then: if username+password → **star_api_authenticate** (blocking); on success sets g_star_initialized, g_star_beamed_in, **star_api_refresh_avatar_profile**. Else if api_key+avatar_id → g_star_initialized and g_star_beamed_in, **star_api_refresh_avatar_profile**. Else if **g_oq_saved_jwt[0]** → **star_api_set_saved_session(g_oq_saved_jwt)**, g_star_initialized = 1, **star_api_restore_session()** (async), log “restore started, profile load will set beamed_in”. So restore path does **not** set g_star_beamed_in at init; it is set when the **profile loaded** operation callback runs (in OQ_StarApiOperationCallback → g_star_profile_loaded_pending = 1, then next frame in OQuake_STAR_PollItems).
- **Order**: init → set_operation_callback → set_oasis_base_url → (auth or api_key or set_saved_session + restore_session). For restore, beam-in is effectively “done” when RestoreSessionAsync or RefreshAvatarProfileInBackground invokes ProfileLoaded and Quake handles it in the next pump.

### 3.2 Inventory: API, when called, loading vs loaded

- **API**: **star_api_get_inventory** (blocking). Used in **OQ_RefreshOverlayFromClient** and **OQ_RefreshInventoryCache**.
- **When**: **OQ_RefreshInventoryCache** runs when opening inventory (OQ_InventoryToggle_f), and is used by the overlay. It skips if star_sync_inventory_in_progress or star_sync_auth_in_progress (shows "Authenticating..."); if !star_initialized it still calls OQ_RefreshOverlayFromClient and sets status "Offline - use STAR BEAMIN". So “loading” is only implied by auth in progress; otherwise get_inventory is called and result is shown.
- **Loading vs loaded**: No separate loading flag; status string is "Authenticating...", "Offline - use STAR BEAMIN", "STAR inventory is empty.", "Synced (N items)", or "Sending..." when send in progress.

### 3.3 Quest/tracker: APIs, when polled, CVars/flags

- **APIs**: **star_api_get_active_quest_id**, **star_api_get_active_objective_id** (in profile-loaded handler and for tracker), **star_api_invalidate_quest_cache**, **star_api_refresh_quest_cache_in_background**, plus get_quests_string, get_top_level_quests_string, get_quest_sub_quests_string, get_quest_objectives_string, get_quest_tracker_objectives_string, etc., for popup.
- **When**: On **g_star_profile_loaded_pending** (in OQuake_STAR_PollItems): clear pending, set g_star_beamed_in = 1, read get_active_quest_id / get_active_objective_id into g_quest_tracker_*, **star_api_invalidate_quest_cache**, **star_api_refresh_quest_cache_in_background**, save config. Quest popup uses the same get_*_string APIs and filter CVars (g_quest_filter_*, g_quest_selected_index, etc.).
- **CVars/flags**: g_quest_tracker_id, g_quest_tracker_name, g_quest_tracker_show, g_quest_tracker_active_objective_id, g_quest_status_message, g_quest_start_pending_id, etc. “Loading” for quest list is implicit (cache not ready → string APIs return empty/loading); tracker is filled when profile loaded runs.

### 3.4 star_sync_* usage and blocking

- **star_sync_init**: In OQuake_STAR_Init (implied by use of star_sync_*).
- **star_sync_auth_start** / **star_sync_auth_poll** / **star_sync_auth_get_result** / **star_sync_auth_get_result_jwt** / **star_sync_auth_in_progress**: Used for “star beamin” async auth; completion in **star_sync_pump** → auth callback (sets g_star_initialized, **star_api_refresh_avatar_profile**). No blocking in game; pump runs each frame.
- **star_sync_inventory_***: Not used for normal overlay; OQ_RefreshOverlayFromClient uses star_api_get_inventory directly. star_sync_inventory_in_progress is still checked in OQ_RefreshInventoryCache (skip if in progress).
- **star_sync_send_item_start**: Send from overlay; **star_sync_pump** → **OQ_OnSendItemDone**. **star_sync_send_item_in_progress()** used to show "Sending..." and to block starting another send.
- **star_sync_use_item_start**: Use health/armor/item from overlay; **star_sync_pump** → **OQ_OnUseItemFromOverlayDone**. **star_sync_use_item_in_progress()** blocks starting another use (e.g. UseHealth_f, UseArmor_f).

**star_sync_pump()** is called every frame in **OQuake_STAR_PollItems** so auth/send/use completions are processed on the main thread.

### 3.5 Flags/state that can get stuck or missed (OQuake)

- **g_star_profile_loaded_pending**: Set in OQ_StarApiOperationCallback when operation_type == STAR_API_OP_PROFILE_LOADED and result == success. Consumed next frame in OQuake_STAR_PollItems. If PollItems is not called (e.g. not in game), pending is never consumed; once PollItems runs it’s cleared and g_star_beamed_in set.
- **g_star_beamed_in**: Set when profile loaded is handled; gates mint/add so pickups aren’t sent before beam-in. If restore or refresh fails, ProfileLoaded never fires and g_star_beamed_in stays 0 until user runs “star beamin” and auth succeeds (then auth callback sets g_star_initialized and refresh_avatar_profile; profile loaded will set g_star_beamed_in when that completes).
- **star_sync_auth_in_progress**: If auth never completes (e.g. hang), OQ_RefreshInventoryCache keeps showing "Authenticating..."; no timeout.

---

## 4) Cross-cutting

### 4.1 Where “operation callback” is invoked and what Doom/Quake do

- **Invoked from C#** (InvokeOperationCallback):
  - **StarApiOpProfileLoaded (0)**: RestoreSessionAsync success; RefreshAvatarProfileInBackground success/failure (SoSuccess or error code).
  - Other ops (has_item, get_inventory, get_quests_string, get_avatar_id, use_item, start_quest, mint, send_item, etc.) on their respective native API completion paths.
- **ODOOM**: Does **not** call **star_api_set_operation_callback**. So all InvokeOperationCallback calls fall back to the legacy **star_api_set_callback**; ODOOM also does not set that in the audited code. So ODOOM does not react to ProfileLoaded from the client; it only reacts to **star_sync** auth callback (ODOOM_OnAuthDone), where it sets "Loading..." and calls refresh_avatar_profile. Tracker “Loading...” is cleared by the frame pump when **star_api_get_active_quest_id** returns data (cache filled by refresh).
- **OQuake**: Calls **star_api_set_operation_callback(OQ_StarApiOperationCallback, NULL)** after init. **OQ_StarApiOperationCallback** only sets **g_star_profile_loaded_pending = 1** when operation_type == STAR_API_OP_PROFILE_LOADED and result == success. On the next frame, **OQuake_STAR_PollItems** sees g_star_profile_loaded_pending, clears it, sets g_star_beamed_in = 1, restores tracker from get_active_quest_id / get_active_objective_id, invalidates quest cache, starts quest refresh, saves config. So Quake uses the operation callback for “profile loaded” only; other operation types are ignored for logic (only logged when not success).

### 4.2 If restore succeeds but avatar/quest/inventory fetch fails or is slow

- **RestoreSessionAsync**: One GET avatar/current; success → ProfileLoaded invoked. Failure → no callback (or error callback if applicable). So “restore success” here means GET succeeded; no separate avatar/quest/inventory fetch in that path.
- **After restore**, games typically call **star_api_refresh_avatar_profile**. That queues GET avatar/current on the generic worker. If that **fails or is slow**: OQuake never gets ProfileLoaded (or gets error), so g_star_profile_loaded_pending may not be set (or set with error); g_star_beamed_in may stay 0; tracker/quest cache not updated. ODOOM already set g_star_initialized and "Loading..." in restore path; if refresh never completes, tracker can stay "Loading..." until get_active_quest_id returns (which depends on cache from refresh). So both games can show “loading” indefinitely if refresh fails or never completes.
- **Inventory**: First **star_api_get_inventory** blocks until cache is filled. If EnsureAvatarId or GET inventory fails, GetInventoryAsync returns error; games show empty or status message. No automatic retry in the client for that call.

### 4.3 Brittle patterns (summary)

- **Shared state without single owner**: Multiple locks (_stateLock, _questsCacheLock, _inventoryCacheLock) and flags (_questTrackerSavedSinceLastGet, _questsRefreshInProgress) shared between native calls and background tasks; ordering and lock nesting must be kept consistent.
- **Order-dependent init**: Games must call star_api_init before set_saved_session/restore_session; restore requires SetSavedSession first when using JWT from file. ODOOM does init then (optionally) set_saved_session + restore_session in one path; OQuake same. If init fails, ODOOM sets g_star_init_failed_this_session and skips retry until explicit beamin.
- **Single-thread assumption for native**: star_api_get_inventory and star_api_has_item block on the calling thread (GetAwaiter().GetResult()); if called from a non-main thread they can block that thread. Quest string APIs are non-blocking but can enqueue work on the generic worker.
- **No retries**: FetchInventoryOnceAsync has one retry on network error; RestoreSessionAsync and GET avatar/current have no retry. Quest fetch has no retry in the audited code.
- **Flags that can desync**: _questTrackerSavedSinceLastGet is set on set_active_quest and cleared when GET avatar/current applies; if two GETs overlap or order is wrong, tracker can be overwritten. g_star_profile_loaded_pending is volatile and consumed once; if two ProfileLoaded callbacks race, only one consumption may happen (second frame sees 0). g_odoom_pending_loading_tracker is cleared when applying "Loading..." so a second run doesn’t re-apply.
- **ODOOM does not set operation callback**: ProfileLoaded from C# is ignored by ODOOM; tracker “loaded” state depends on polling get_active_quest_id and RefreshQuestCVars. If refresh_avatar_profile is slow or fails, "Loading..." can persist with no timeout.
- **star_sync auth vs RestoreSession**: For saved session, ODOOM calls star_api_restore_session() (async restore); completion is in C# (RestoreSessionAsync) which invokes ProfileLoaded, but ODOOM doesn’t listen. ODOOM sets g_star_initialized and g_odoom_pending_loading_tracker and refresh_avatar_profile in the same synchronous block after restore_session returns success (restore_session only starts the async work; return value is “started”). So ODOOM’s “restore success” path does not wait for RestoreSessionAsync to complete; it sets state and kicks refresh. The actual “profile loaded” (GET avatar/current done) is not wired to ODOOM UI except via later polling of get_active_quest_id and RefreshQuestCVars.

---

## 5) Design principles and recommendations

The following principles are aimed at making the integration **self-contained**, **resilient**, and **independent of timing**, so adding features (e.g. JWT polling, auto-renew) does not break existing behaviour.

### 5.1 Self-contained modules

- **Each domain (auth, inventory, quests, profile) should own its own state and lifecycle.** Avoid one “god” lock or generic worker that serialises unrelated work. For example:
  - **Auth/session**: Own thread or dedicated queue; token refresh, restore, and validate run in that context only. No shared lock with inventory or quests.
  - **Inventory**: Own cache and fetch task; GetInventory / HasItem / AddItem only touch inventory state. No dependency on “profile loaded” having run first for correctness (only for “has avatar id”).
  - **Quests**: Own cache and refresh; quest/tracker APIs only touch quest state. Tracker IDs can be filled from profile when available, but quest list and “Loading…” should not depend on profile callback order.
- **Games should not rely on “magic” ordering.** Init → set_saved_session → restore_session → refresh_avatar_profile is a chain of side effects. Prefer: “restore” = “validate JWT and load profile (avatar + quest tracker + whatever the API returns) in one logical operation,” and “refresh” is an optional follow-up that does not change “beamed in” semantics.

### 5.2 No timing dependencies

- **“Profile loaded” should be the single source of truth for “we have a valid session and profile data.”** Both ODOOM and OQuake should register **star_api_set_operation_callback** and react to **StarApiOpProfileLoaded**: set “beamed in,” apply tracker from client cache, set “Loading…” for quest list and then trigger quest refresh. No “we set Loading... and hope get_active_quest_id gets filled later.”
- **Avoid “restore returns before work is done.”** Today restore_session queues work and returns; the game then sets flags and calls refresh_avatar_profile. Prefer either: (a) restore_session is blocking until profile is loaded and then invokes ProfileLoaded, or (b) restore_session is fire-and-forget and the **only** way the game learns “restore done” is via ProfileLoaded (no setting g_star_initialized or pending_loading_tracker before the callback).
- **Decouple “has session” from “has profile data.”** Session = valid JWT (and maybe refresh token). Profile = avatar id, XP, active quest/objective, etc. Inventory can depend on “has avatar id” and retry when it gets one; it should not depend on “profile loaded callback ran this frame.”

### 5.3 Retries and resilience

- **Every network operation (GET/POST) should have a bounded retry with backoff** (e.g. 2–3 retries, exponential backoff), and optionally a “max wait” so the UI can show “Still loading…” or “Temporary error, retrying…”
- **No “one shot” flags that leave the system stuck.** If refresh_avatar_profile fails, the client should either retry in the background or surface a clear error and allow “retry” from the UI. Tracker should not stay "Loading..." forever; after a timeout, show "Could not load quests" or similar and allow manual refresh.
- **Inventory**: EnsureAvatarId and GET inventory should retry on transient failure. First star_api_get_inventory from the game can still block, but internally the client should retry until success or max retries.

### 5.4 Avoid shared serialisation bottlenecks

- **Generic background worker is a single point of contention.** Restore, refresh profile, quest refresh, and all Queue* operations share one FIFO. Under load or when one operation is slow, everything else stalls. Recommendation: separate workers or at least separate queues for (1) auth/session (restore, refresh token, validate), (2) profile (GET avatar/current, refresh avatar profile), (3) inventory (fetch, add-item flush), (4) quests (fetch all-for-avatar, refresh cache). Each can run concurrently and use its own state.
- **Blocking native calls (get_inventory, has_item) block the game thread.** Where possible, offer async or callback-based APIs so the game can show “Loading…” and update when the result arrives, instead of blocking the frame.

### 5.5 Clear ownership of “loading” and “loaded”

- **Each UI surface (tracker, quest list, inventory) should have an explicit state:** Loading | Loaded | Error (with message). Not “we set a placeholder and hope something fills it.”
- **Profile loaded callback should deliver everything the UI needs for “beamed in”:** avatar id, XP, active quest id, active objective id (and optionally a snapshot of quest list or a hint to refresh). Games then set “loaded” for tracker (and optionally start quest list refresh). No reliance on “next frame get_active_quest_id will be set.”

### 5.6 No silent failure or desync

- **Operation callback should be invoked for both success and failure** (with result code), so the game can set “Error” state and optionally retry. No “we only set pending on success” so that failure leaves the UI stuck.
- **Avoid flags that are set in one path and cleared in another** (e.g. _questTrackerSavedSinceLastGet) unless there is a single, documented owner and clear state machine. Prefer explicit “version” or “generation” so that “apply profile” only overwrites when the data is newer.

### 5.7 Suggested next steps (no code changes in this doc)

1. **Client**: Introduce separate workers or queues for auth/session, profile, inventory, quests; add retries and timeouts to all HTTP calls; ensure ProfileLoaded carries enough data (or a single “profile snapshot” handle) so games do not need to poll get_active_quest_id to “see” loaded state.
2. **ODOOM**: Register **star_api_set_operation_callback** and drive “beamed in” and “tracker loaded” from ProfileLoaded (and error path). Remove reliance on “restore returns and we set pending_loading_tracker and hope refresh fills cache.”
3. **OQuake**: Keep using operation callback; add handling for ProfileLoaded failure (set error state, allow retry). Ensure inventory and quest list have explicit Loading | Loaded | Error and timeouts.
4. **Both games**: Add timeouts for “Loading…” (e.g. 10–15 s); after timeout show “Timed out” or “Retry” and optionally auto-retry in the background once.
5. **Document** the intended state machines (session, profile, inventory, quests) in a short design doc so future changes (e.g. JWT refresh) don’t re-introduce order dependencies or shared bottlenecks.

### 5.8 Implementation status

**Done**

- **5.7.1 (partial) – Client separate workers**: AuthSession, Profile, Inventory, and Quests each have a dedicated queue and worker; restore, auth, profile refresh, inventory fetch, and quest cache refresh no longer share one FIFO. Generic worker still used for all other Queue* operations.
- **5.7.1 (partial) – Client retries**: `SendRawWithRetryAsync` added (3 attempts, 200/400/800 ms backoff on network errors). Used for GET avatar/current, EnsureAvatarId, GET inventory, and GET quests/all-for-avatar.
- **5.7.2 (partial) – ODOOM operation callback**: ODOOM registers `star_api_set_operation_callback` after init. On ProfileLoaded success, frame pump sets tracker from `get_active_quest_id` / `get_active_objective_id` and triggers quest cache refresh. Error path (callback on failure) not yet handled.
- **5.4 – Avoid single bottleneck**: Dedicated workers implemented as above.

**Not yet implemented**

- **5.7.1 – Client timeouts**: No HTTP or "max wait" timeouts; no "Still loading…" / retry UI.
- **5.7.1 – ProfileLoaded payload**: Games still poll `get_active_quest_id` after callback; no profile snapshot handle.
- **5.7.2 – ODOOM error path**: Callback only sets pending on success; no Error state or retry on ProfileLoaded failure.
- **5.7.3 – OQuake**: ProfileLoaded failure handling, explicit Loading | Loaded | Error, and timeouts for inventory/quest list not done.
- **5.7.4 – Both games**: No 10–15 s timeout for "Loading…", no "Timed out" / "Retry" UI, no auto-retry.
- **5.7.5 – Document state machines**: No separate state-machine design doc.
- **5.5 – Explicit UI state**: No formal Loading | Loaded | Error per surface.
- **5.4 – Non-blocking get_inventory**: **Done.** `star_api_get_inventory` is now cache-only (no network). Use `star_api_request_inventory_in_background()` to fetch; when operation_callback(STAR_API_OP_GET_INVENTORY) fires, call `star_api_get_inventory()` to read cache. OQuake and ODOOM use this pattern; star_sync inventory thread requests in background and result is delivered via `star_sync_inventory_deliver_result()` from the game's callback.

**Other blocking native exports (not yet converted to background)**

The following star_api_* exports still block the calling thread (they use .GetAwaiter().GetResult() on async work): `star_api_authenticate`, `star_api_has_item`, `star_api_add_item`, `star_api_set_active_quest`, `star_api_flush_add_item_jobs`, `star_api_mint_inventory_nft`, `star_api_use_item`, `star_api_flush_use_item_jobs`, `star_api_complete_quest_objective`, `star_api_complete_quest`, `star_api_create_monster_nft`, `star_api_deploy_boss_nft`, `star_api_send_item_to_avatar`, `star_api_send_item_to_clan`, and the internal `GetCurrentAvatarAsync` path. Converting these to request-in-background + callback would follow the same pattern as inventory.

**Why Quake inventory used to be brittle**

The dedicated **Inventory** worker only runs work that is explicitly queued via `QueueGetInventoryAsync` / `QueueHasItemAsync`. In OQuake, the overlay and UI call `star_api_get_inventory()` directly (blocking). That path does **not** go through the worker: the game thread blocks in `GetInventoryAsync().GetAwaiter().GetResult()`. So the worker does not isolate inventory from the rest of the game; the first fetch (or any cache miss) runs on whatever thread calls `get_inventory`, and if that coincides with profile load, quest refresh, or other work, ordering and contention can still cause flakiness. Making inventory robust for Quake would require either (a) having the game use an async/callback inventory API and feed the worker, or (b) ensuring the first fetch is triggered and completed (e.g. after ProfileLoaded) before the overlay can call `get_inventory`.

---

*Audit generated to support a robust redesign of the STAR API integration. Section 5.8 added to track implementation status.*
