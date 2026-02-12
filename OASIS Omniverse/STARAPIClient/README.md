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

