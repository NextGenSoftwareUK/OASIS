# WEB5 STAR API C# Native Wrapper (C/C++ Compatible)

This project ports the C++ WEB5 STAR API wrapper to C# while preserving the same C ABI entry points used by existing C and C++ game integrations.

## API URI Configuration

- WEB5 STAR API URI:
  - `Web5StarApiBaseUrl`
- WEB4 OASIS API URI:
  - `Web4OasisApiBaseUrl`
- WEB4 URI can also be provided via environment variable `OASIS_WEB4_API_BASE_URL`.
- Native C/C++ callers can set WEB4 URI at runtime with `star_api_set_oasis_base_url(...)`.

## Binary Compatibility

- Export names match the original wrapper: `star_api_init`, `star_api_authenticate`, `star_api_has_item`, etc.
- Calling convention is `cdecl`.
- Struct layouts match `star_api.h`.
- `star_api.h` is included in this folder for direct use by existing game code.

## Performance Strategy

- Uses `UnmanagedCallersOnly` exports (no COM or reverse P/Invoke marshaling glue).
- Built as NativeAOT for direct native DLL loading and fast startup.
- Uses a shared `HttpClient` instance and minimal allocation interop conversions.
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

Drop `star_api.dll` in place of the existing native wrapper DLL and keep using the same `star_api.h`/import-library workflow.

## One-click publish + deploy

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
4. **Single item** – For one-off sync (e.g. key pickup), call `star_sync_single_item(name, description, game_source, item_type)` from the main thread.

Keep `local_items` and its `synced` flags valid until the inventory refresh completes (poll returns 1). The sync layer updates `synced` in the background. Using this layer keeps threading and sync logic out of game code and makes porting other games to OASIS easier.

