# Our World offline-first delivery plan

Status: active implementation plan. This document deliberately scopes the first production milestone to the API
surface exercised by the current Our World Unity project. The broader API backlog is preserved below and in
`OASIS_EDGE_RUNTIME_OFFLINE_SYNC_ARCHITECTURE.md`; it is not silently being declared complete.

Terminology: Our World targets **HyperDrive Runtime v2** (`OASISHyperDrive2`) and **HyperDrive Sync Protocol v3**.
The latter is the Edge/ONODE wire and durable-sync contract, not a third HyperDrive runtime generation. `Legacy`
remains the Runtime v1 compatibility mode; HTTPS and ONET both transport the same Sync Protocol v3 semantics.

## Native OGEngineClient adoption checkpoint (2026-09-24)

The Edge-capable OGEngineClient now owns authenticated Edge composition and exposes an additive,
versioned native ABI (`ogengine_configure_edge` and `ogengine_get_edge_status`). The original
`ogengine_config_t` layout is unchanged, so existing native games do not suffer an ABI break.
Edge configuration includes a stable device identity, durable SQLite path, pinned offline-grant
verification key, explicit scopes, and grant lifetime. After successful hosted authentication,
OGEngineClient acquires the device-bound grant, performs the initial synchronization, owns the
connectivity monitor, and disposes the runtime with the client.

Native games must call `ogengine_configure_edge` before `ogengine_init`; callers that have not yet
migrated remain remote-only rather than failing login. This boundary is deliberate: offline support
is default-on in managed/Unity configuration, while an old native binary cannot safely invent a
stable device ID or pinned signing key. ODOOM and OQuake migration must persist a stable installation
ID, supply the release signing key/database path, poll the status API, and show the common
offline/reconnecting/synchronized notifications. Those game changes do not own synchronization logic.

The NativeAOT release scripts now keep `PublishAot` local to the top-level `ogengine` project; passing
it as a global MSBuild property previously contaminated netstandard dependencies with NETSDK1207.
The corrected Windows release script discovers the installed MSVC linker/libraries and Windows SDK,
sets the supported NativeAOT environmental-tool contract, and produces the final `net10.0/win-x64`
binary. A freestanding native smoke executable links against `ogengine.lib`, loads the produced DLL,
and calls both Edge exports; packaging fails if compilation, linking, loading, or either ABI call fails.
This passed locally on 2026-09-24. The script also fails when a required toolchain component or export
is genuinely absent; it never substitutes a stale DLL or optional symbol lookup.

Saved-session restoration now treats network absence as a first-class HyperDrive transition. It may
accept offline startup only after the device-bound signed grant validates and both durable avatar and
avatar-detail projections load successfully. Authentication rejection and corrupt/missing projections
remain hard errors. A valid offline restore hydrates XP, Karma, identity, and the tracked quest/objective
before the normal profile-loaded callback, so native games receive the same beam-in lifecycle online
and offline.

## User-visible definition of done

After one successful online sign-in and initial synchronization on a device, an Android user can start Our World
with no connectivity, enable flight mode while playing, lose a weak connection during a request, restart the app
offline, and continue every currently supported Our World workflow without a crash or lost accepted action.
Local actions are durably committed before success is shown. Reconnection requires no restart or new login and
replays each action exactly once from the user's perspective.

The UI exposes a small non-blocking state indicator and transition notification. These three notifications are now
driven from Edge runtime state transitions and marshalled through a thread-safe queue onto Unity's main thread:

1. `Working offline - changes will sync automatically.`
2. `Back online - synchronizing changes.`
3. `Synchronization complete.`

Errors that require action remain visible and structured; they are never converted into false success. Sync work is
asynchronous, bounded and off Unity's main thread. Release performance gates measure frame time, memory, storage,
battery and sync duration rather than claiming literally zero resource cost.

The first-ever launch remains an explicit boundary: without a previously issued, valid device-bound offline grant
and synchronized data, the client must present login instead of inventing an identity or authorization.

## Audited current Unity surface

| Our World surface | Current online call | Current offline state | Milestone requirement |
|---|---|---|---|
| Avatar profile | WEB4/WEB5 avatar GET candidates | Durable private Avatar projection read | Prove online bootstrap, offline restart and reconnect refresh |
| Shared inventory | WEB5/WEB4 inventory GET candidates | Durable AvatarDetail inventory projection read; inventory-grant command exists | Map every game grant/removal currently emitted and prove idempotent replay |
| Quest list/tracker | WEB5 quest GET candidates | Durable Quest projection read; quest-progress command exists | Wire every current progress/active-objective mutation and prove ordering/replay |
| NFT/GeoNFT list | WEB5 NFT/WEB4 NFT GET candidates | Durable `GeoNft` projections (not collection-command entities) | Prove that the synchronized assets match the exact assets shown by the online endpoint |
| Karma | WEB4 total and history GETs | Durable AvatarDetail total only | Keep total available; add history projection only if the current offline UI requires parity |
| Clan/social | WEB4/WEB5/holon GET candidates | Durable Clan Holon membership plus avatar projections | Member identity, owner/member role and last beam state remain available offline; live game and per-member Karma remain authoritative/live fields |
| Global preferences | WEB4 settings GET/PUT | Local device preferences; remote sync online-only | Queue only preferences that the current client actually sends and reconcile after reconnect |
| Authentication | WEB4 login plus signed offline grant | Secure device-bound grant resume exists | Prove expiry, revocation boundary, offline restart and reconnect renewal |

## OGEngineClient and Edge composition

The current source has two separate composition roots:

- Our World references the managed Unity package containing `NextGenSoftware.OGEngine.Client.Edge` and talks to
  `OGEngineEdgeClient`. `OASISEdgeUnityHost` is only the Unity lifecycle/connectivity/secure-storage binding; it does
  not own domain, persistence or synchronization behavior. Our World never constructs or routes through
  `OASISEdgeAPI` directly.
- ODOOM and OQuake load the slim NativeAOT OGEngineClient. It owns the reusable C ABI, HTTP, caches, gameplay queues
  and main-thread callback pump. Its `remote` transport is implemented; selecting `native` currently fails explicitly
  because Edge Runtime is not linked into that artifact.

The agreed target architecture is one reusable, offline-first OGEngineClient with Edge Runtime always composed:

1. `OGEngine.Shared` contains engine-neutral DTO mapping, state transitions and client contracts.
2. OGEngineClient always executes supported reads and actions through Edge Runtime and its durable local store.
3. HyperDrive synchronizes with the hosted ONODE whenever reachable and keeps durable local work pending whenever it
   is not. Online, offline, reconnecting and synchronized are runtime states, not competing transports.
4. Native games consume one stable, versioned C ABI; they do not select `remote` versus `native` during gameplay.
5. Unity consumes the managed OGEngineClient assembly through IL2CPP rather than loading the desktop NativeAOT
   binary. This is still OGEngineClient: the product API and behavior owner are identical; only the language/runtime
   binding differs.

`OASISEdgeAPI` remains a supported lower-level component for developers who intentionally want only durable Edge
storage/HyperDrive synchronization. It is the implementation layer used by OGEngineClient, not the application-facing
integration selected by OASIS games. Our World, ODOOM, OQuake and future first-party games integrate OGEngineClient.

Desktop platform services are also shared below the game adapters. Edge Runtime supplies the OS network monitor and
Windows Credential Manager secure-session store used by NativeAOT OGEngineClient; the Unity Windows adapter delegates
to that same credential implementation. Android Keystore and iOS Keychain remain Unity/mobile platform bindings.

The standard shipped OGEngineClient therefore includes Edge. A special remote-only artifact may be retained later for
genuinely constrained or unsupported targets, but it is not the normal product architecture and must not introduce a
second behavior path into the standard client. Edge remains an internal package/component boundary so Unity and
NativeAOT hosts can compose it appropriately; it is not an optional gameplay fallback. The Full OASIS BootLoader and
its provider graph are not embedded in every game.

Authentication is owned by the always-present `OGEngine.Shared` assembly rather than the Edge assembly. The shared
`OGEngineAuthenticationClient` is the single WEB4 request, nested-envelope, JWT/avatar-identity and safe-error
contract used by Our World and by the existing ODOOM/OQuake NativeAOT `AuthenticateAsync` path. Native clients retain
their mature post-authentication lifecycle (token state, WEB5 session initialization and cache warming), while Our
World retains its Unity-only animated `Beaming In...` presentation. The former Our World gateway authentication
implementation remains compile-disabled with its retirement reason for release comparison and must never be used as
a fallback.

### Explicit disable control and release profiles

Offline synchronization is enabled by default but is an explicit product feature, not mandatory behavior:

- `OASIS.OASISHyperDriveConfig.OfflineSyncEnabled` in `OASIS_DNA.json` controls client/Edge local persistence and
  automatic synchronization and defaults to `true`.
- Our World, ODOOM and OQuake expose the same setting in their settings UI. Changing it re-composes the client at a
  safe lifecycle boundary; it does not create a per-request fallback branch.
- Disabling is refused while durable operations remain pending. The UI must offer **Sync now and disable** or
  **Cancel**. Destructive discard is a separate, strongly confirmed maintenance action and is never implicit.
- Disabling preserves the local database and secure grant by default, allowing later re-enable without data loss.
- When disabled, the app uses its supported remote-only path and clearly reports that offline continuity is off.

Two releases are produced from the same source and contracts:

1. **OGEngineClient Edge** — standard/default release; Edge binaries included and `OfflineSyncEnabled=true`.
2. **OGEngineClient Remote-Only** — exceptional smallest-footprint release for constrained/IoT targets; Edge and
   SQLite binaries are absent, the toggle is unavailable, and capability metadata explicitly reports no offline sync.

The Edge release may be configured with offline sync disabled. The Remote-Only release cannot enable a capability it
does not contain. Neither release silently changes profile because the network failed.

Measured local artifact sizes on 2026-09-23 are evidence, not final mobile budgets: the existing NativeAOT Windows
OGEngine DLLs are approximately 5.53-6.97 MiB. The manifested cross-platform Unity Edge package is 22.81 MiB only
because it includes SQLite binaries for Windows, Linux, macOS, Android and iOS together. An Android ARM64 selection
is approximately 2.23 MiB of managed dependencies plus a 1.69 MiB SQLite native library before APK compression,
IL2CPP stripping and dependency deduplication. OASIS-owned Edge assemblies account for roughly 0.29 MiB of that
managed set. Runtime RAM, database growth, sync CPU, radio use and battery cost have not yet been measured on a
physical device and must not be inferred from disk size.

### Performance evidence still required

It is not yet proven that the always-on Edge path has negligible runtime overhead. The Android acceptance run must
compare the same Our World build and scripted gameplay workload with synchronization quiescent, actively uploading,
offline with a growing outbox, and reconnecting. Record at minimum:

- APK/install-size delta per selected ABI, excluding binaries for other platforms;
- steady-state and peak private memory, including SQLite page cache and IL2CPP allocations;
- idle and active CPU, Unity frame-time percentiles and main-thread stalls;
- database size versus entity/outbox count, compaction behavior and write amplification;
- bytes sent/received for equivalent domain operations, including synchronization envelope overhead;
- local read and durable-commit latency percentiles;
- reconnect duration and throughput for bounded pending-operation counts;
- battery and radio usage during idle online, prolonged offline and recovery workloads.

Expected behavior is better interactive latency for projected reads, a small SQLite commit cost for accepted local
actions, modest sync-envelope network overhead, and near-idle recovery work because probes back off exponentially.
Those are design expectations, not release claims, until the physical-device report exists. Any failed budget is
fixed in the shared Edge path rather than bypassed through a remote-only runtime branch.

### Current build evidence (2026-09-24)

- Automated regressions pass for HyperDrive Core (167), Edge Runtime (50), Edge SQLite (26), OASIS DNA (11),
  ONET synchronization contracts (9), hosted offline-session/JWT security (12), and OGEngineClient (12). The current
  configuration/profile, shared authentication, and public Edge lifecycle crash-boundary tests pass, and both the Edge and Remote-Only profiles build from the
  same project with their dependency contents verified. Our World now consumes the managed
  `OGEngineEdgeClient`; exporting the same routes through the NativeAOT C ABI for ODOOM/OQuake is the later
  native-game adoption milestone, not a claim that those games have already been migrated.
- The exact generated Unity Edge package compiles in Unity 2022.3.62f3 and passes its Android package smoke build.
- The Our World project compiles against that package in Unity batch mode.
- The MongoOASIS hosted-provider suite passes 24/24 against a real three-member MongoDB replica set. It proves
  atomic rollback at every injected transaction boundary, durable backfill/change capture, private/global audience
  filtering, ordered command execution, primary election replay and idempotency. A separate externally coordinated
  test passes after forcibly terminating the elected `mongod` primary between acknowledgement and replay.
- A real Our World Android ARM64 IL2CPP APK was produced and inspected. It contains `libil2cpp.so`, the ARM64
  `libe_sqlite3.so`, Java bytecode and the packaged `omniverse_host_config.json`; SQLite binaries for other ABIs are
  rejected by the build gate. The Unity linker contract explicitly preserves the OASIS Edge runtime/provider,
  `Microsoft.Data.Sqlite`, all required `SQLitePCLRaw` bootstrap/provider assemblies, Newtonsoft.Json and
  System.Text.Json. The package gate fails before Unity starts if any of those preservation entries disappears; this
  protects runtime-only reflection/bootstrap paths that an IL2CPP compile alone cannot prove. The final validation
  APK was 50,627,008 bytes before store processing (SHA-256
  `71616C71820D5BE66F656016B070AB6782F8571D718C26FE0CCB8D89A83216A3`). The release-profile
  run also proved that enabled Edge, the non-loopback HTTPS ONODE URL and the exact pinned public grant key were
  embedded in the APK. `Scripts/new_our_world_edge_release_config.ps1` composes that non-secret config from the
  public fragment emitted by `Scripts/new_edge_offline_grant_signing_config.ps1`; the signing secret is never copied
  into the Unity project or APK.
- This is packaging/build evidence, not physical-device runtime evidence. Flight-mode, process-kill, frame-time,
  memory, storage, radio and battery acceptance still require an Android device and production grant configuration.
  The latest local evidence, including the AOT-safe JSON changes and current APK hash, is recorded in
  `Docs/Devs/OUR_WORLD_OFFLINE_MVP_VERIFICATION.md`.

The release validator has two explicit profiles from the same codebase:

- `SqliteMvp` validates the current Our World SQLite-first deliverable without claiming a Holochain artifact.
- `HoloEnabled` additionally requires a clean sibling `OASIS-Holochain-hApp` checkout, a freshly tested Nix build,
  matching source/artifact hashes, and the HoloOASIS Unity package. It remains the default so a full release cannot
  silently omit Holochain evidence.

Run the current MVP gate with real Mongo evidence using:

```powershell
Scripts/validate_edge_runtime_release.ps1 -Configuration Release -Profile SqliteMvp `
  -HostedMongoSyncReport artifacts/edge-release-validation/hosted-mongo-sync.trx `
  -HostedMongoProcessKillReport artifacts/edge-release-validation/hosted-mongo-process-kill.trx
```

The gate emits the signed-off package set, API compatibility report, SBOM, Unity/Android logs, test reports,
checksums and `acceptance-report.json` under `artifacts/edge-release-validation`. Input TRX files may safely already
be in that directory; the validator holds their bytes before cleaning generated output.

### Domain-provider rollout after the SQLite MVP

Our World ships its first offline MVP on the already implemented Edge SQLite path. After the Android correctness
suite passes, HoloOASIS is the next milestone and the intended default domain provider. Promotion requires a rebuilt,
provenance-verified hApp, passing Nix/Tryorama tests, mobile conductor suspend/resume/restart support, IL2CPP/AOT
validation and the same disconnect/reconnect/idempotency suite used by SQLite.

SQLite is the implemented MVP/reference synchronization journal, not a permanent architectural dependency. The
permanent requirement is the provider-neutral `IHyperDriveSyncStateStore` invariant covering operations, receipts,
checkpoints, conflicts, snapshots and crash recovery. HoloOASIS will implement that contract and be tested both as
the domain provider over a SQLite journal and as the provider for both domain and journal state.

Holochain already uses SQLite internally. The qualification question is therefore whether its supported
conductor/zome transaction boundary can expose every HyperDrive journal invariant—not whether Holochain has durable
transactional storage at all. Proving that boundary would favor the single HoloOASIS domain+journal composition and
avoid a redundant application-owned SQLite database.

The physical-device performance report compares HoloOASIS, Edge SQLite and LocalFileOASIS using one dataset and
scripted workload across speed, memory, CPU, network, storage, battery, package footprint, offline restart and
reconnect convergence. LocalFileOASIS is initially a recovery/minimum-dependency baseline and cannot be called
production sync-equivalent until it implements the same atomic and idempotent contracts.

The final default is chosen from HoloOASIS-domain/SQLite-journal, HoloOASIS-domain/HoloOASIS-journal and
SQLite-domain/SQLite-journal using correctness first, followed by measured speed, memory, CPU, network, storage,
battery and package footprint. SQLite can be removed from the default composition if HoloOASIS proves the complete
journal contract with equal or better reliability and acceptable resource use.

The preferred end-state is HoloOASIS for local agent/domain state and, after qualification, the HyperDrive journal;
Holochain DHT/gossip for permitted peer convergence; and HyperDrive for hosted ONODE reconciliation, protected
authority and non-Holochain provider bridging. Both channels share immutable operation/version/origin identifiers so
the same action cannot circulate as a new mutation.

Our World classifies operations as agent/peer-convergent, hosted-authoritative or hybrid. Karma, inventory
consumption/rewards, NFT/GeoNFT ownership, exclusive claims and trades remain hosted-authoritative. A hybrid GeoNFT
collection may record/gossip the local discovery immediately but only the ONODE command result finalizes ownership
and reward. The product demonstration and performance report separately exercise Holochain-native, HyperDrive-only
and hybrid modes, including peers available without ONODE and ONODE available without peers.

The flagship demonstration must explain the boundary clearly: Holochain provides agent source chains, peer-to-peer
validation, DHT distribution and gossip; HyperDrive provides durable command/recovery semantics, ONODE authority,
cross-provider reconciliation and support for non-Holochain clients. Two offline phones can exchange permitted data
only when a local radio/network path still exists; with every radio disabled they operate locally and converge later.
The concise message is: **Holochain keeps people and communities connected peer-to-peer; HyperDrive keeps the entire
heterogeneous OASIS convergent.**

MongoOASIS is the initial hosted/reference HyperDrive provider, chosen because it is the existing economical and
practical OASIS persistence foundation—not because HyperDrive is Mongo-specific. Its implementation establishes the
hosted conformance baseline for transactions, receipts, checkpoints, snapshots, change capture and fan-out. Future
hosted HoloOASIS or other provider/composite implementations can replace or complement it after passing the same
applicable guarantees and advertising their actual capabilities through ONET.

GeoHotSpot, generic Holon, provider and NFT APIs are included in this milestone only where an actual call from the
current Our World project is found. API names alone do not justify adding speculative mobile contracts.

Quest definitions, GeoNFT placements and GeoNFT collection definitions are synchronized with a `global` audience:
they are shared world content and must remain visible to every authorized Our World avatar while offline. Avatar,
AvatarDetail, inventory, quest-progress command results and GeoNFT collection history remain avatar-scoped. The
local projection still keys each entity by its canonical entity type/id, so repeated capture of the same shared
definition converges rather than producing a second user-visible world object.

## Implementation sequence

1. **Implemented:** the release validation script inventories every public asynchronous gateway method, fails when
   a method is added or removed without classification, and verifies that each documented offline read still routes
   to its durable Edge projection. Bind each mutation call to its authoritative command as those calls are audited.
2. **Implemented:** a transport loss changes Edge to Offline once, immediately routes
   supported work to the local runtime, and cannot be overwritten by a stale in-flight response.
3. **Implemented for the audited Our World surface:** the required avatar, AvatarDetail/inventory/Karma, quest,
   GeoNFT and clan-membership projections are locally readable. Server-authoritative
   ownership, rewards, Karma and inventory effects are submitted as idempotent commands, not last-write-wins entity
   overwrites as those mutation calls enter the audited client surface.
4. **Implemented:** the three user notifications are driven by the shared Edge state machine and durable pending
   counts, not from HTTP error
   strings or Unity reachability alone.
5. **Automated runtime/store coverage implemented; physical acceptance pending:** tests cover bootstrap/synchronization
   state, durable restart, idempotency, duplicate delivery, conflicts and rejected commands. Android process-kill and
   radio-transition evidence still requires a device.
6. **Editor and Android build gates implemented and passing:** the exact Unity package and a release-profile ARM64
   IL2CPP APK are built and archive-inspected. Physical Android profiling under throttled, absent and restored
   networks remains the final product acceptance gate.

## Deferred full-API coverage

The later comprehensive milestone retains these work items:

- all Avatar and AvatarDetail mutations and overloads;
- generic Holon CRUD/query overload compatibility;
- Karma history plus authorized add/deduct command semantics;
- provider administration and capability management where offline operation is meaningful;
- complete generic NFT mint, update, ownership, transfer and trading workflows;
- first-class GeoHotSpot CRUD, query and conflict semantics;
- complete GeoNFT creation, editing, transfer and trading workflows;
- complete inventory add/remove/use/transfer/trade semantics;
- exhaustive Legacy versus `OASISHyperDrive2` manager/API regression matrices;
- long-running ONET partition, multi-device conflict and mobile resource soak tests.

These require explicit authority and conflict rules. They must not be implemented as silent REST fallbacks or simple
last-write-wins updates for ownership, consumption, rewards or currency-like state.

## Release evidence

The narrow milestone is complete only when automated evidence proves:

- the audited call inventory has no unclassified current Our World network operation;
- local commits survive app/process restart and pending counts remain accurate;
- hosted replay is authenticated, ordered and idempotent;
- reconnect converges projections and advances checkpoints atomically;
- rejected commands are surfaced without rolling back unrelated valid work;
- no bearer token or private signing key is persisted in Unity assets or `PlayerPrefs`;
- the exact packaged Unity dependencies compile in Unity 2022.3 and build an Android player;
- physical-device loss/recovery tests meet recorded frame, memory, battery, storage and latency budgets.
## HoloOASIS 0.7 implementation status

The Holochain 0.7 DNA/hApp in `OASIS-Holochain-hApp` now builds, packs and passes real-conductor Sweettest coverage for provider lookup, HyperDrive replay idempotency and public-DHT secret rejection. The C# HoloOASIS and Unity adapter also build against that contract.

This completes the desktop/runtime contract gate. Physical Android profiling plus two-device offline/gossip/online-resynchronization field tests remain required before HoloOASIS replaces Edge SQLite as the default Our World mobile provider; desktop conductor tests do not establish phone CPU, memory, storage, thermal or battery budgets.
