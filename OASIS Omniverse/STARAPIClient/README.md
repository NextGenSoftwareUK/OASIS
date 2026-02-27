# WEB5 STAR API C# Client (C/C++ Compatible)

This project is **the** STAR API client for ODOOM, OQuake, and other C/C++ games. ODOOM and OQuake use **STARAPIClient only**—do not use NativeWrapper. This C# client implements the C ABI entry points (`star_api_*`) used by game integrations.

## STARAPIClient vs star_sync (why both?)

**STARAPIClient** (this project)

- **Role:** API client.
- **What it does:** Implements the C ABI (`star_api_has_item`, `star_api_get_inventory`, `star_api_use_item`, etc.) and performs the actual HTTP calls to the STAR backend. Games link against the client DLL and call `star_api_*` from C/C++.
- **Cache:** Holds the local inventory cache (see below). GetInventory / HasItem / UseItem use it first and only hit the API when the cache is empty or invalidated.

**star_sync**

- **Role:** Generic game-integration layer (C library: `star_sync.c` / `star_sync.h`). Sits between the game and the client.
- **What it does:** Provides async auth and async inventory on background threads, with completion on the main thread via `star_sync_pump()`, so games don’t implement threading. Can sync local items (e.g. push pickups with `has_item` / `add_item`) before fetching inventory. Same flow for ODOOM, OQuake, and other games.
- **Cache:** Does not hold a cache; it calls `star_api_*` and benefits from the client’s cache.

**Why both:** The client does HTTP and caching; star_sync does threading and flow so games stay simple and reusable. Games typically use both: `star_sync_*` for async auth and inventory (e.g. `star_sync_inventory_start` after beam-in), and `star_api_*` for door checks, has_item, use_item, etc.

### Why is star_sync in C? Could it live in STARAPIClient (C#)?

**Why it’s in C today**

- Games (ODOOM, OQuake) are C/C++. They compile and link `star_sync.c` with the game; star_sync then calls `star_api_*`, which is implemented by the STARAPIClient DLL. So the chain is: game (C++) → star_sync (C) → star_api (C ABI) → STARAPIClient (C#). C was a natural fit for a small, portable layer that only does threading and flow and stays client-agnostic (it just calls the C ABI).

**Could star_sync be moved into STARAPIClient?**

- Yes. The same C entry points (`star_sync_init`, `star_sync_pump`, `star_sync_inventory_start`, etc.) could be implemented in C# and exported from the client DLL. Background work would use `Task`/async; `star_sync_pump()` would call into C# to run any completed callbacks on the “main” thread (the one that called pump).

**Pros of moving star_sync into STARAPIClient (C#)**

- One codebase and one DLL: no separate C sync library to build or ship.
- Sync and client share the same process and cache with no extra ABI crossing.
- One place for all STAR integration; no platform-specific C threading (Win32 vs pthreads) in star_sync.

**Cons of moving star_sync into STARAPIClient (C#)**

- Sync becomes tied to this client. Any other implementation of `star_api_*` (e.g. a C++-only client) would need its own sync or a C sync that calls that client.
- The “pump + callback on main thread” design has to be done carefully in C# (marshalling completion from background tasks into the C-called pump).

**Pros of keeping star_sync in C**

- Client-agnostic: star_sync only depends on the C ABI, so it works with any `star_api_*` implementation (this client or another).
- Familiar for C/C++ game devs: “sync is a small C lib that calls the same star_api I call.”

**Cons of keeping star_sync in C**

- Two artifacts to maintain (STARAPIClient + star_sync.c/h) and, in C, platform-specific threading code.

**Recommendation:** **Move star_sync into STARAPIClient.** You already use a single client (STARAPIClient) for ODOOM and OQuake, so keeping sync client-agnostic doesn’t buy much in practice. One C# DLL and one codebase is easier to maintain, version, and ship; sync and cache live in the same process with no extra ABI boundary. The main cost is implementing the pump + main-thread callback contract carefully in C#. If you later introduce another `star_api_*` implementation, you can either add a minimal C shim for sync or re-extract a small sync layer. For the current setup, consolidating in STARAPIClient is the better trade-off.

## Local inventory cache (single cache, minimal game hooks)

The **only** inventory cache lives **inside STARAPIClient**. Games (ODOOM, OQuake, etc.) do **not** keep a second cache: they call `star_api_has_item`, `star_api_get_inventory`, `star_api_use_item` and only refresh the overlay (e.g. push to CVars) when needed (after sync done, send, or use). This keeps integration minimal and generic. Behaviour:

- **GetInventoryAsync** / **star_api_get_inventory**: Return the cached list when present; only call the API when the cache is null (e.g. first load or after `InvalidateInventoryCache()`).
- **HasItemAsync** / **star_api_has_item**: Resolve from the cache when available; only call the API (via GetInventory) when the cache is null.
- **UseItemAsync** / **star_api_use_item**: Use inventory (from cache when available) to decide if the item exists, then call the API to record use.
- **AddItemAsync** / **SendItemToAvatarAsync** / **SendItemToClanAsync**: On success, the client updates the cache (add one item, or remove the sent item(s)) so the next get/has reflects the change without a refetch.

So games (ODOOM, OQuake, etc.) get cache-first behaviour automatically: no need to keep a separate game-side inventory list for door/has/use. Call `star_api_has_item` and `star_api_get_inventory` as needed; the client uses the cache and only hits the API when necessary. Use **InvalidateInventoryCache()** when you know the inventory changed outside this client (e.g. after receiving items from another source).

## API URI Configuration

- WEB5 STAR API URI:
  - `Web5StarApiBaseUrl`
- WEB4 OASIS API URI:
  - `Web4OasisApiBaseUrl`
- WEB4 URI can also be provided via environment variable `OASIS_WEB4_API_BASE_URL`.
- Native C/C++ callers can set WEB4 URI at runtime with `star_api_set_oasis_base_url(...)`.

## Inventory item NFT minting

NFT minting uses the **WEB4 OASIS API** (not the WEB5 STAR API). Games (e.g. ODOOM) can **mint an NFT** when the player collects an item, then add that item to STAR inventory with the **NFT ID** in metadata so it links to the NFTHolon on WEB4.

- **`star_api_mint_inventory_nft`** (C) / **`MintInventoryItemNftAsync`** (C#): Calls the WEB4 OASIS API `/api/nft/mint-nft` to create an NFTHolon. Returns an NFT ID. Default **provider** is `SolanaOASIS`; can be overridden (e.g. from game config).
- **`star_api_add_item(..., nft_id)`** (C) / **`AddItemAsync(..., nftId)`** (C#): When `nft_id` is set, the inventory item’s MetaData stores **NFTId** linking to that NFTHolon.
- **Inventory overlay:** Items with a non-empty **NFTId** can be shown with a **[NFT]** prefix (e.g. `[NFT] quake_weapon_shotgun`) and grouped separately from non-NFT items of the same type (e.g. “NFT Shotgun” x2 and “Shotgun” x2).

Config options (e.g. in **oasisstar.json** or game ini): **mint_weapons**, **mint_armor**, **mint_powerups**, **mint_keys** (0/1), and **nft_provider** (default `SolanaOASIS`). When mint is on for a category, the game mints on pickup then adds the item with the returned NFT ID.

## Binary Compatibility

- Export names match the original wrapper: `star_api_init`, `star_api_authenticate`, `star_api_has_item`, etc.
- Calling convention is `cdecl`.
- Struct layouts match `star_api.h`.
- `star_api.h` is included in this folder for direct use by existing game code.

## Performance Strategy

- Uses `UnmanagedCallersOnly` exports (no COM or reverse P/Invoke marshaling glue).
- Built as NativeAOT for direct native DLL loading and fast startup.
- Uses a shared `HttpClient` instance and minimal allocation interop conversions.
- **Local inventory cache:** The client keeps a single in-memory cache of the last loaded inventory. `GetInventory`, `HasItem`, and `UseItem` use this cache first and only hit the API when the cache is null or the item is not found (for has_item). Cache is updated on add/send so games get correct state without extra refetches. See [Local inventory cache](#local-inventory-cache-single-cache-minimal-game-hooks) above.
- Includes an optional add-item job queue (`QueueAddItemAsync`, `QueueAddItemsAsync`, `FlushAddItemJobsAsync`) for high-frequency item collection events.
- Includes optional high-throughput queues for:
  - add item (`QueueAddItemAsync`, `QueueAddItemsAsync`, `FlushAddItemJobsAsync`)
  - use item (`QueueUseItemAsync`, `FlushUseItemJobsAsync`)
  - quest objective updates (`QueueCompleteQuestObjectiveAsync`, `FlushQuestObjectiveJobsAsync`)

## Build (Native DLL + Import Library)

From repo root:

```bash
dotnet publish "OASIS Omniverse/STARAPIClient/STARAPIClient.csproj" \
  -c Release \
  -r win-x64 \
  -p:PublishAot=true \
  -p:SelfContained=true \
  -p:NoWarn=NU1605
```

Outputs:

- `OASIS Omniverse/STARAPIClient/bin/Release/net8.0/win-x64/publish/star_api.dll`
- `OASIS Omniverse/STARAPIClient/bin/Release/net8.0/win-x64/native/star_api.lib`

Drop `star_api.dll` (and `star_api.lib`) next to the game exe. Use `star_api.h` from this folder. ODOOM and OQuake use STARAPIClient only.

## Diagnostic logging

The client writes **add_item** (and related) diagnostics to:

1. **`star_api.log`** – In the process current directory (e.g. the game exe folder when running OQuake). Each line is timestamped UTC. Append-only; delete the file to start fresh.
2. **`System.Diagnostics.Trace`** – Same lines are sent to `Trace.WriteLine`. Attach a `TraceListener` (e.g. to a file or the debug output window) to capture them.

Logged events include: `star_api_add_item` entry (item name, game source), result (success/failure and message), and inside `AddItemCoreAsync`: avatar check, POST URL, response success/error, and exceptions. Use this to confirm whether add_item is invoked, whether the HTTP request is sent, and how the server responds.
</think>
Verifying there are no linter errors:
<｜tool▁calls▁begin｜><｜tool▁call▁begin｜>
ReadLints

Use the helper script to publish and copy artifacts into the game integration folders:

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/publish_and_deploy_star_api.ps1"
```

Optional smoke test compile/run (requires `cl.exe` or `gcc.exe`):

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/publish_and_deploy_star_api.ps1" -RunSmokeTest
```

## VS Build Tools assisted smoke test

If `cl.exe` is not on PATH, use the helper below. It discovers Visual Studio C++ tools via `vswhere`, loads `vcvars64.bat`, compiles the smoke test, and can run it:

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/compile_smoke_test_with_msvc.ps1" -Run
```

## Inventory test (C)

The C inventory test (`test_inventory.c`) exercises init, auth, get inventory, has_item, add_item, sync, **send-to-avatar**, and **send-to-clan**. Build and run with:

**One-click (defaults: `http://localhost:5556`, user `dellams`):**

```batch
OASIS Omniverse\STARAPIClient\TEST_INVENTORY.bat
```

**PowerShell (custom URL/auth):**

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/compile_and_test_inventory.ps1" -BaseUrl "http://localhost:5556" -Username "user" -Password "pass"
```

**Optional: send-to-avatar and send-to-clan**

- Steps 9 and 10 always run. Without extra args they use placeholder targets so you can see API errors (e.g. “network call failed”, “holon not found”).
- To test with real targets, pass the 6th and/or 7th arguments:
  - **6th** = `send_avatar_target` (username or avatar id to send an item to).
  - **7th** = `send_clan_name` (clan name to send an item to).

Example (PowerShell):

```powershell
.\compile_and_test_inventory.ps1 -BaseUrl "http://localhost:5556" -Username "user" -Password "pass" -SendAvatarTarget "other_user" -SendClanName "MyClan"
```

Example (run built exe directly):

```text
test_inventory.exe http://localhost:5556 user pass "" "" other_user MyClan
```

(Use `""` for api_key and avatar_id if you don’t need them.)

## Unit + Integration + Harness (one click)

Run the full WEB5 STAR API client validation suite:

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/run_star_api_test_suite.ps1"
```

Optional parameters:

- `-SkipHarness` to run only Unit + Integration tests.
- `-KillStaleTestHosts` defaults to `true` (recommended). Set `-KillStaleTestHosts:$false` to disable.
- `-HarnessMode fake|real-local|real-live`:
  - `fake` = use in-process fake WEB5/WEB4 APIs (default)
  - `real-local` = spin up local `STAR.WebAPI` + `ONODE.WebAPI` processes for harness, then stop them after run
  - `real-live` = target live endpoints (defaults to `https://oasisweb4.one/star/api` for WEB5 and `https://oasisweb4.one/api` for WEB4, both normalized to base URI)
- `-Web5StarApiBaseUrl` / `-Web4OasisApiBaseUrl` to override target endpoints for `real-local` or `real-live`.
- `-RealLiveWeb5StarApiBaseUrl` / `-RealLiveWeb4OasisApiBaseUrl` to override default live endpoints in `real-live` mode.
- `-Username` / `-Password` / `-ApiKey` / `-AvatarId` for harness auth flows.

Standardized test artifacts are written to:

- `OASIS Omniverse/STARAPIClient/TestResults/Unit` (TRX + JUnit)
- `OASIS Omniverse/STARAPIClient/TestResults/Integration` (TRX + JUnit)
- `OASIS Omniverse/STARAPIClient/TestResults/Harness` (JUnit)

## Start local WEB4 + WEB5 APIs (ODOOM/OQUAKE)

Use this helper to start local APIs for game integration testing:

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/start_local_web4_and_web5_apis.ps1"
```

Behavior:

- Starts **WEB4 OASIS API first**, waits until ready, then starts **WEB5 STAR API**.
- Uses sequential startup by design to reduce heavy startup contention on local machines.
- Keeps both APIs running until Ctrl+C (or use `-NoWait` to return immediately).

Stop APIs started by the helper:

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/stop_local_web4_and_web5_apis.ps1"
```

Optional fallback stop by known local ports:

```powershell
powershell -ExecutionPolicy Bypass -File "OASIS Omniverse/STARAPIClient/stop_local_web4_and_web5_apis.ps1" -UsePortFallback
```

Fake server implementation locations:

- Integration test fake server: `OASIS Omniverse/STARAPIClient/TestProjects/NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests/FakeStarApiServer.cs`
- Harness fake server: `OASIS Omniverse/STARAPIClient/TestProjects/NextGenSoftware.OASIS.STARAPI.Client.TestHarness/FakeHarnessApiServer.cs`

Harness runtime config env vars:

- `STARAPI_HARNESS_MODE` (`fake`, `real-local`, `real-live`)
- `STARAPI_HARNESS_USE_FAKE_SERVER` (legacy compatibility switch)
- `STARAPI_WEB5_BASE_URL` / `STARAPI_WEB4_BASE_URL`
- `STARAPI_HARNESS_JUNIT_PATH`

## Usage patterns (direct vs queued)

Use direct calls for low-frequency actions:

```csharp
var addResult = await client.AddItemAsync("Red Keycard", "Collected in level 1", "Doom", "KeyItem");
var useResult = await client.UseItemAsync("Red Keycard", "door_123");
var objectiveResult = await client.CompleteQuestObjectiveAsync("quest_alpha", "objective_1", "Doom");
```

Use queued mode for high-frequency gameplay events (pickup spam, rapid interactions, burst objective updates):

```csharp
// Suggested starting defaults for real-time loops
client.AddItemBatchSize = 64;
client.AddItemBatchWindow = TimeSpan.FromMilliseconds(75);
client.UseItemBatchSize = 64;
client.UseItemBatchWindow = TimeSpan.FromMilliseconds(50);
client.QuestObjectiveBatchSize = 64;
client.QuestObjectiveBatchWindow = TimeSpan.FromMilliseconds(50);

// Queue work (non-blocking enqueue)
var addTask = client.QueueAddItemAsync("Ammo Box", "Dropped by enemy", "Doom", "PowerUp");
var useTask = client.QueueUseItemAsync("Silver Key", "gate_a2");
var objectiveTask = client.QueueCompleteQuestObjectiveAsync("quest_alpha", "kill_10", "Doom");

// Optional: await individual results later
var addResult = await addTask;
```

Flush queues at safe checkpoints (end of frame batch, checkpoint save, level complete, or shutdown):

```csharp
await client.FlushAddItemJobsAsync();
await client.FlushUseItemJobsAsync();
await client.FlushQuestObjectiveJobsAsync();
```

Queueing is optional. Existing direct APIs remain unchanged and can be used side-by-side with queued mode.

## Game integration (async layer: `star_sync.h` / `star_sync.c`)

For C/C++ games (e.g. OQUAKE, ODOOM) that run on the main/game thread and must not block on network, use the **generic async layer** in `star_sync.h` and `star_sync.c`. It provides:

- **Async authentication** – start auth on a background thread, poll from the main loop, then read result (success, username, avatar_id, error).
- **Async inventory refresh** – optionally sync a list of “local” items to the remote (has_item → add_item if missing), then get_inventory; poll from main thread and get the list + result.
- **Single-item sync** – `star_sync_single_item()` for immediate sync of one item from the main thread (e.g. key pickup).

### Build

Compile your game sources and **include `star_sync.c`** in the build (or build a static lib from `star_sync.c`). Link with `star_api.lib` and the C runtime. Include `star_api.h` and `star_sync.h` where needed. On Windows the layer uses Win32 threads and `CRITICAL_SECTION`; elsewhere it uses `pthreads`.

### Usage pattern

1. **Init** – Call `star_api_init()` as usual (e.g. at startup).
2. **Auth** – Call `star_sync_auth_start(username, password)`. Each frame (or where you draw status), call `star_sync_auth_poll()`: if it returns `1`, call `star_sync_auth_get_result()` to get success/username/avatar_id/error and update your game state (e.g. set avatar_id for later API calls); if it returns `0`, still in progress; if `-1`, no pending result.
3. **Inventory** – When authenticated, call `star_sync_inventory_start(local_items, local_count, "GameName")` with your array of `star_sync_local_item_t` (or `NULL`/`0` to only fetch). Each frame call `star_sync_inventory_poll()`: if it returns `1`, call `star_sync_inventory_get_result()` to get the `star_item_list_t*`, process it, then call `star_api_free_item_list(list)` and optionally `star_sync_inventory_clear_result()`.
4. **Single item** – For one-off sync (e.g. key pickup), call `star_sync_single_item(name, description, game_source, item_type, nft_id)` from the main thread. Pass `NULL` for `nft_id` for non-NFT items.

Keep `local_items` and its `synced` flags valid until the inventory refresh completes (poll returns 1). The sync layer updates `synced` in the background. Using this layer keeps threading and sync logic out of game code and makes porting other games to OASIS easier.

### Inventory UI patterns (game-side)

These patterns are implemented in OQUAKE’s integration; they are **not** part of `star_sync` (which is game-agnostic). Other games (e.g. ODOOM) can reuse the same ideas:

1. **Single place for pickup reporting** – Report item/stats pickups from **one** call site (e.g. a per-frame poll in your main loop), not from both a frame poll and the HUD/sbar draw. Otherwise the same pickup can be sent twice (e.g. armor +2 instead of +1).

2. **Show just-added items before the next fetch** – After you add an item (e.g. `star_api_add_item` or via local list + `star_sync_inventory_start`), the next async inventory result might not include it yet. When merging “remote list” + “local items” for display, **also show local items that are marked synced but not yet present in the remote list**. That way pickups appear immediately instead of only after the next fetch.

3. **Append local to display on add** – When you add a pickup, push that entry into your visible inventory list (or equivalent) right away, so the user sees the new item without waiting for the next `star_sync_inventory_poll` result. `star_sync` does not manage your UI; the game is responsible for this.

