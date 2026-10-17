# OGEngine Edge Runtime in ODOOM and OQuake

## Contract

ODOOM and OQuake use the versioned `ogengine` NativeAOT ABI. The original ABI remains binary-compatible;
Edge configuration and status use `ogengine_edge_config_t` version 1. Every symbol declared in `ogengine.def` is
mandatory. The games do not resolve missing shipped symbols dynamically and do not contain alternate session or
profile implementations.

The Edge profile is the normal game profile. Offline synchronization is enabled unless an explicit game setting or
OASIS DNA value disables it. The Remote-Only profile is built from the same project with Edge excluded and exposes
the same ABI; its capability result clearly disables the in-game controls.

Each installation persists a stable device identity next to the configured database root. Private databases are
then scoped by normalized ONODE host and avatar ID. A saved session may start offline only after its signed,
device-bound grant, expiry and requested scopes validate against the configured public key. The private signing key
is an ONODE deployment secret and must never be copied into a game config or release artifact.

Supported local gameplay commits include XP, inventory grants and consumption, active quest/objective selection,
quest progress, quest start, objective completion and quest completion. A native call returns success only after the
local SQLite journal commit. Hosted command execution uses stable operation IDs and durable receipts so replay after
an acknowledgement loss cannot apply an action twice. NFT minting and ownership transfer remain hosted-authoritative;
they are outside this native offline gameplay contract and are not silently replaced by a local ownership write.

## Game configuration and controls

The shared `oasisstar.json` fields are:

```json
{
  "offline_sync_enabled": -1,
  "edge_device_id": "",
  "edge_database_path": "",
  "offline_grant_public_key": "<deployment public key>",
  "offline_scopes": "avatar,holon,inventory,quest,karma,nft,geonft,geohotspot",
  "offline_grant_lifetime_minutes": 1440
}
```

An empty device ID is generated and persisted. An empty database path selects the per-user game default. A real
Edge beam-in requires the deployment public key; the example deliberately does not invent one.

ODOOM exposes **Options → OASIS Offline Sync**. OQuake exposes **Options → Game Options → Offline Sync** and
**Sync and disable**. Both also support `star offline status|on|off|sync-and-off`. Disabling is refused while durable
work is unsettled. The HUD shows Online, Working offline, Synchronizing, pending count, or Remote only. Normal
notifications are queued by the shared runtime and consumed on the game thread:

- `Working offline - changes will sync automatically.`
- `Back online - synchronizing changes.`
- `Synchronization complete.`

Hosted command rejection is also surfaced once through this queue. Transport diagnostics stay in `ogengine.log`.
A valid offline restore produces one terminal successful profile callback, so the game does not flash a transient
login or network error.

## Validation

On Windows, run the two native profiles and the real-DLL lifecycle gate:

```powershell
& 'OASIS Omniverse/OGEngineClient/Scripts/publish_and_deploy_star_api.ps1' -Profile Edge -RunSmokeTest
& 'OASIS Omniverse/OGEngineClient/Scripts/publish_and_deploy_star_api.ps1' -Profile RemoteOnly -RunSmokeTest -NoDeploy
$env:OGENGINE_NATIVE_TEST_DLL = "$PWD/artifacts/native-games/native/Edge/win-x64/publish/ogengine.dll"
dotnet test 'OASIS Omniverse/OGEngineClient/TestProjects/OGEngine.Client.Tests/OGEngine.Client.Tests.csproj' `
  -c Release -p:PublishAot=false --filter FullyQualifiedName~NativeAbiGameplayTests
```

The native test loads the published DLL and verifies ABI sizes, capabilities, one terminal restore result, offline
restart with an expired JWT, durable XP and inventory, pending-work disable refusal, lost-acknowledgement replay,
reconnection, drain and disable. `native-profile-report.json` records the exact DLL hash, size, export count and smoke
result for each profile.

The managed game-client suite also runs `ThreeGameConvergenceTests`. It creates independent SQLite databases and
device identities for ODOOM, OQuake and Our World against one authoritative hosted transport. Each game queues an
inventory grant and quest-progress command offline. After reconnection the host records each command once, publishes
durable command receipts, and all three clients pull identical inventory and quest projections. A subsequent online
inventory command is then verified in the other two clients on their next synchronization exchange.

The opt-in deployment gate `LiveThreeGameOnodeTests` uses the production Edge client, real HTTP transport, three
device identities, three SQLite stores and ONODE-signed offline grants. Run it through
`Scripts/run_live_three_game_sync_test.ps1`; credentials and bearer tokens remain process-local. The runner first
checks the deployed grant contract, then requires the deployment public key and an actual development quest ID.
It queues inventory and quest progress while each client is offline, reconnects, and verifies all six durable
command receipts plus converged inventory and quest state on ODOOM, OQuake and Our World.

Before packaging, run `Scripts/assert_no_packaged_game_credentials.ps1`. Release configuration must not contain
`jwt_token`, `refresh_token` or `beamedin_avatar` fields.

Then run the official game builds. These copy the exact validated DLL, import library, header and SQLite native
dependency before compiling the engines:

```powershell
& 'OASIS Omniverse/OGames/ODOOM/BUILD ODOOM.bat' batch
& 'OASIS Omniverse/OGames/OQuake/BUILD_OQUAKE.bat' batch
```

The Windows builders require the Visual Studio Desktop development with C++ workload. OQuake also requires the
Vulkan SDK. A machine without those system prerequisites cannot produce credible final game binaries; do not replace
that build gate with precompiled objects or unchecked copies.

## 2026-09-25 local evidence

- OGEngineClient: 16 managed tests passed; the native-only test is separately opt-in. This includes the three-game
  offline convergence and online propagation test.
- Shared Edge runtime: 50 tests passed.
- Hosted ONODE WebAPI: built with zero errors.
- Edge NativeAOT profile: 110 mandatory exports and ABI smoke passed. The import library is regenerated from every
  project-root `UnmanagedCallersOnly` entry point during publish so it cannot silently omit partial-class exports.
- Remote-Only NativeAOT profile: 110 mandatory exports and ABI smoke passed.
- Real Edge NativeAOT lifecycle: 1 test passed, including durable restart and replay.
- Canonical ODOOM and OQuake engine patches applied, including both settings menus and removal of legacy symbol shims.
- Official Windows ODOOM build passed and produced `OGames/ODOOM/build/ODOOM.exe`.
- Official Windows OQuake build passed and produced `OGames/OQuake/build/OQUAKE.exe`.
- The development account authenticated successfully against the configured WEB4 host, but the deployed
  `POST /api/hyperdrive/sync/offline-session-grant` contract returned HTTP 404. The live three-game gate therefore
  remains correctly failing at the deployment boundary until the current ONODE WebAPI build and its signing-key
  configuration are deployed. No local fake transport is accepted by this gate.

Production grant rollout, signed Android release configuration, and the HoloOASIS provider comparison begin only
after both official game builds pass. No production credential, private signing key, or fabricated public key is
recorded here.
