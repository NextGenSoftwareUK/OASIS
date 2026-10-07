# OASIS Edge Runtime and HyperDrive Offline Synchronization

Status: canonical architecture and implementation tracker, based on the source tree as inspected on 2026-09-23.

## HyperDrive execution modes and the unified V2 pipeline

`OASIS.HyperDriveMode` remains the explicit compatibility boundary. `Legacy` keeps the established manager execution paths unchanged. `V2` uses the same `ProviderManager` enable flags and ordered provider lists for load balancing, failover, and replication, while avoiding request-time mutation of the global current provider.

When hosted synchronization is disabled, successful V2 mutations replicate directly through the configured `AutoReplicationProviders` list, matching Legacy policy. When `OASISHyperDriveConfig.EnableHostedSync` is enabled, V2 defers mutation replication to the durable hosted pipeline: the authoritative provider persists the change, domain capture records it in the ordered outbox, and the fan-out worker selects compatible targets from the same `ProviderManager` replication list. Offline game commands enter that same ordered change and fan-out pipeline. This prevents ordinary manager writes and offline commands from running separate replication systems.

The existing flags remain authoritative in V2: `AutoLoadBalanceEnabled` controls provider selection, `AutoFailOverEnabled` controls traversal of `AutoFailOverProviders`, and `AutoReplicationEnabled` controls both direct replication and durable hosted fan-out. Provider list order is preserved.

## OGEngineClient native boundary

The native boundary is additive and versioned. `ogengine_configure_edge` accepts
`ogengine_edge_config_t` version 1 before legacy `ogengine_init`; `ogengine_get_edge_status` returns
connectivity, synchronization state, pending durable operations, and the last successful sync time.
The legacy init struct remains byte-for-byte unchanged and is guarded by layout tests. Edge-enabled
authentication is composed inside OGEngineClient, not separately in each game.

Our World uses that same client boundary for durable GeoHotSpot triggers, quest progress, GeoNFT collection and
active quest/objective selection. Reads for GeoHotSpots and tracker state use synchronized projections. When Edge
is enabled, a projection or journal failure remains a structured visible error; it never silently falls through to
REST. REST is retained only by the explicitly configured remote-only deployment profile. Hosted ONODE remains the
authority for reward, cooldown, collection, Karma and XP decisions when the queued commands synchronize.
The Our World Quests popup and persistent tracker are two UI views over this single command boundary: both persist
the exact quest id and objective id, and tracker restoration reads those ids from AvatarDetail. Display labels are
never used as persistence keys.

The deployment scripts treat the two Edge exports as required contract symbols. A library/header
mismatch therefore fails during packaging instead of becoming an optional runtime fallback.
`PublishAot` is deliberately declared only by the top-level native library project and is not passed
as a global command-line property, because global propagation attempts to AOT-publish Unity-compatible
netstandard dependencies. Stable device identity is generated once and atomically persisted beside
the Edge database when a host does not provide an explicit identity; it is not a credential.
The Windows release path discovers MSVC/Windows SDK environmental tools, completes NativeAOT publish,
and runs a freestanding linked ABI probe against the produced DLL. Offline saved-session restore is
authorized only by a valid device-bound signed grant plus durable avatar projections; a network error
does not weaken authentication or turn arbitrary cached data into a session.

## Version terminology

The version numbers describe different layers and must not be conflated:

- **HyperDrive Runtime v1 (`Legacy`)** — the original provider routing/replication behavior retained for controlled
  compatibility and rollback.
- **HyperDrive Runtime v2 (`OASISHyperDrive2`)** — the current provider execution, deterministic routing, failover,
  load-balancing, quota and replication-policy implementation being completed and migrated across managers.
- **HyperDrive Sync Protocol v3** — the transport-neutral durable synchronization envelope used by Edge and hosted
  ONODE over HTTPS or ONET. Version 3 adds immutable entity version identities, causal previous-version links,
  operation sequencing/idempotency, opaque checkpoints, authoritative paged snapshots and structured conflicts and
  command results.

“Sync Protocol v3” does not mean a HyperDrive Runtime v3 exists. Edge Runtime composes HyperDrive Runtime v2 behavior
with Sync Protocol v3. Both HTTPS and ONET carry the same v3 synchronization semantics.

Local validation snapshot (2026-10-03): DNA 12/12, Core 227/227, Edge SQLite 31/31,
Edge Runtime 64/64, ONET synchronization 15/15, HyperDrive AI 7/7, Holo Edge 9/9, HoloNET Client/ORM 70/70 and hosted
offline-session security 13/13 passed. The broader Full-runtime preservation gate also passes: ONODE Core
164/164, ONODE Core integration 42/42, ONODE WebAPI 83/83, Full Native Integrated Endpoint, both Edge Native
target frameworks and STAR CLI build successfully, and STAR DNA passes 1/1. The existing STAR CLI, CLI library
and STAR runtime projects named `UnitTests` currently contain no discoverable test methods; their successful build
is therefore recorded as build evidence, not misreported as test coverage. Run
`Scripts/validate_full_runtime_regression.ps1`; it emits and parses the four non-empty TRX reports, enforces their
minimum executed-test baselines, and builds Full Native, Edge Native and STAR CLI.
Dependency audits of Full ONODE WebAPI, Edge Native and STAR CLI report no known vulnerable direct or transitive
NuGet packages from the configured package sources. Redundant framework-provided `System.*` package references were
removed from the touched OASIS projects. ONODE.Client now consumes the `net10.0` shared-framework
`System.Text.Json` assembly rather than carrying a redundant package reference through the STAR CLI graph; the
dependency gate rejects that reference if it returns. The active EVM provider projects were migrated together from their mixed
Nethereum 4.x versions to Nethereum 7.0.0; the receipt-log API change is handled at the provider call sites and the
Full ONODE build/regression gate verifies the unified graph. This removes the former `NU1608` conflict between
Nethereum's logging dependency and the Full Runtime's `Microsoft.Extensions.Logging.Abstractions` 10.x graph rather
than suppressing it or forcing an unsupported package combination. Run
`Scripts/validate_dependency_security.ps1` to enforce the single Nethereum version across active provider projects
and fail on vulnerable direct or transitive packages in Full ONODE, Edge Native or STAR CLI.
Release TRX evidence is fail-closed: every required report must have identical total, executed and passed counts,
and every other VSTest outcome counter must be zero. Skipped, inconclusive, warning, not-runnable, disconnected,
pending and partially executed suites therefore cannot satisfy the release gate. The strengthened SQLite profile
passed 555/555 tests and the Holo-enabled profile passed 651/651 tests on 2026-10-05.
MongoDBOASIS, the migration tool, Full ONODE WebAPI, both Native Integrated Endpoint compositions,
STAR CLI, HoloNET Client/ORM and HoloOASIS.Unity build successfully. The SQLite profile's seven Edge NuGet packages
and the Holo-enabled profile's nine Edge NuGet packages are
packable, the Unity package passes editor and Android-player validation, and the synchronized Our World
project compiles with Unity 2022.3. The Holochain 0.7 hApp builds and packs with the pinned Holonix toolchain,
and its Holochain Sweettest suite passes against a real conductor. A portable three-node MongoDB 7 replica set now
passes 32/32 hosted transaction, concurrency, rollback, retry and election tests plus the separately coordinated
abrupt-primary-process-loss replay test 1/1. `Scripts/run_hosted_mongo_release_evidence.ps1` reproduces that evidence
on Windows without installing a service; the Linux CI job remains an independent Docker-based proof. Feeding those
TRX reports into `Scripts/validate_edge_runtime_release.ps1` passes the complete local release gate, including all
managed suites, provenance checks, the complete profile-specific NuGet set, SPDX SBOM, checksums, Unity editor compilation, Android
player compilation and Our World integration. These results deliberately do not claim the remaining physical-device
proofs: HoloOASIS still requires Android/iOS resource profiling, suspend/resume qualification and two-device field tests.

Implemented release-candidate foundation (physical-device and external-network qualification still required):

- shared synchronization contracts, client coordinator, hosted processor and HTTPS transport in Core;
- agreed OGEngine target: the standard OGEngineClient always includes Edge Runtime and uses one durable local-first
  path. Online/offline/reconnecting/synchronized are automatic runtime states; HTTPS or ONET is only the hosted sync
  transport. Any exceptional remote-only artifact is not an in-client fallback or the standard product;
- atomic Edge SQLite entity/outbox/checkpoint/conflict/inbox persistence targeting .NET Standard 2.1;
- MongoDBOASIS hosted transactional sync store with operation deduplication, version conflicts and ordered delta feed;
- authenticated ONODE `POST /api/hyperdrive/sync/exchange` endpoint;
- Unity-compatible Edge Runtime composition with explicit connectivity and synchronization state;
- network-unavailable exchanges return the runtime to explicit offline/pending local mode without rolling back
  local success; connection failures, client-side HTTP timeouts, and gateway/service-unavailable HTTP responses
  share that classification, while authentication/protocol errors remain visible online errors and explicit caller
  cancellation is never converted into an outage. Status includes the durable queued-operation count. A later
  online signal drains the outbox, and a bounded `HostedServiceRecoveryInterval` probe also detects hosted-service
  recovery and resynchronizes when the platform stayed online and therefore emitted no connectivity transition.
  Repeated failures back off exponentially up to `MaximumHostedServiceRecoveryInterval` to limit radio, CPU and
  battery use during a prolonged outage;
- Edge Native Integrated Endpoint package/facade;
- versioned ONET request/response boundary and authenticated TCP wire channel with length-prefixed frames,
  persisted ECDSA identity signing, correlation, cancellation, source/operation validation, structured remote
  errors, authenticated-avatar resolution, and a transport adapter for the shared sync processor;
- durable MongoDB ONET-node-to-avatar bindings established by an authenticated REST call and a canonical
  ECDSA proof bound to avatar, device and the SHA-256 node identity; Full ONODE startup composition is
  controlled by `OASIS.ONET.EnableHyperDriveSyncHost` and fails startup when its configured provider lacks
  either hosted synchronization or durable peer-binding support;
- Unity-compatible Edge peer registration through `IHyperDrivePeerBindingTransport` and
  `IEdgeNodeIdentity`; the host supplies Keychain/Keystore-backed signing and the runtime explicitly refuses
  to persist private keys in SQLite or ordinary files;
- a fail-closed offline-session boundary in Edge Runtime and the Edge Native Integrated Endpoint. Cached grants
  must remain bound to the configured avatar and device, carry UTC validity and explicit scopes, pass a hosted
  signature validator both when cached and whenever resumed, and be persisted by an injected secure-platform
  store. The contract explicitly forbids files, application configuration and Unity `PlayerPrefs`; no default
  insecure implementation exists;
- shared desktop host adapters now live in Edge Runtime: `DesktopEdgeConnectivityMonitor` observes operating-system
  network availability while hosted responses remain authoritative, and `DesktopPlatformSecureSessionStore`
  selects Windows Credential Manager, macOS Keychain or Linux Secret Service/libsecret. This is explicit platform
  dispatch, never a downgrade to file storage. Unity Windows and NativeAOT game hosts share the Windows
  implementation, and a real Credential Manager save/load/delete test guards that native contract. Unity mobile
  uses Android Keystore or Apple Keychain; the Apple binding includes iOS and tvOS. tvOS still requires a qualified
  SQLite native payload and player build before it can be marked release-supported;
- explicit synchronization protocol version 3 across HTTPS and ONET, including immutable client-generated
  entity version identifiers, authoritative snapshot cursors and refusal of mismatched protocol payloads;
- both hosted and Edge protocol boundaries reject malformed JSON entity payloads, and the client refuses remote
  command records because only entity upserts and tombstones are valid in the pull feed;
- opaque server-issued MongoDB pull checkpoints, preventing clients from forging a feed sequence or using a
  checkpoint belonging to another avatar;
- durable per-avatar/device pull cursors and last-seen timestamps advanced on every hosted pull. The hosted
  compactor uses these records and the configured offline window to compute the retention watermark;
- a MongoDB fan-out outbox inserted in the same transaction as every accepted entity version and change-feed
  record, plus a server change-sequence order key, an exclusive cross-replica ordered-dispatch lease,
  per-item expiring leases, lease heartbeats for long provider calls, durable completion/failure state and
  bounded retries. A deferred head mutation is an ordering barrier; later claims are released and cannot
  overtake it;
- Full ONODE background composition for that dispatcher and an explicit
  `IHyperDriveIdempotentReplicationTarget` boundary. Every configured secondary provider must persist the
  operation id atomically with its mutation and replay it idempotently; startup fails visibly when a configured
  target lacks that contract. Edge SQLite implements the boundary by committing the mutation and operation-id
  inbox record in one SQLite transaction. HoloOASIS implements the client boundary and the sibling
  `OASIS-Holochain-hApp` source now defines an immutable mutation entry plus operation/entity indexes in one
  zome call. Its pinned Holochain 0.7 build, integrity tests and real-conductor Sweettest suite pass, and the
  checked-in provider artifact has been replaced. Android lifecycle/resource and two-device gossip tests remain
  release gates before HoloOASIS becomes the mobile default. Other secondary providers still need native atomic implementations;
- explicit `OASIS.OASISHyperDriveConfig.EnableHostedSync` composition: disabled hosts reject sync exchanges,
  while enabled hosts fail startup unless the default provider exposes both the authoritative sync store and
  durable fan-out outbox. The worker no longer silently becomes inactive when a configured hosted provider is
  incompatible;
- durable conflict discovery and explicit server-wins, retry-local and manual-merge resolution. Server-wins
  is permitted only after the winning version has actually reached the local store;
- causal inbound application in Edge SQLite: a remote entity version advances local state only when its
  `PreviousVersionId` matches the current local version, it is an idempotent replay, or the same exchange
  recorded an explicit conflict whose server version matches that change. An out-of-order update therefore
  cannot resurrect a newer local tombstone or silently overwrite a newer offline edit, and the pull checkpoint
  is rolled back with the rejected exchange;
- HyperDrive sync protocol v3 snapshot cursors and Edge SQLite snapshot staging: snapshot pages are stored
  durably across restarts without changing live entities or the pull checkpoint; only the final ordered page
  atomically replaces the live entity set, clears staging state and advances the checkpoint. Hosted Mongo
  materializes immutable, avatar/device-scoped snapshots at a transactional change watermark. Page width is
  fixed in the snapshot header so configuration changes cannot skip records. Expired snapshots are explicitly
  replaced, and Edge discards only the superseded staging set. A duplicate entity identity across snapshot pages
  rejects and rolls back the page rather than masking broken pagination with an upsert;
- transactionally safe hosted retention-watermark compaction: the slowest active device defines the safe
  sequence, while an absence of active devices permits compaction through the current sequence. The same Mongo
  transaction records the monotonic retention floor before deleting history. New devices and returning devices
  below that floor receive an authoritative snapshot, so pruning can never produce a partial initial state;
- restored `netstandard2.1` build paths for HoloNET Client, HoloNET ORM, HoloOASIS and HoloOASIS.Unity;
  the Unity package now references the real provider and has no machine-specific UnityEngine DLL dependency;
- deterministic HyperDrive provider scheduling foundation: round-robin no longer depends on wall-clock
  milliseconds, weighted scheduling no longer uses random choice, observable metric ties use stable provider
  ordering, and "Intelligent" selection no longer blocks synchronously on the placeholder AI engine;
- concurrency-safe HyperDrive AI telemetry and optimization: shared history/provider scores are synchronized,
  recommendation analysis uses stable snapshots, invalid telemetry returns a structured `OASISResult` error, and
  the engine no longer hides failures behind neutral scores or empty recommendation lists. Provider mutations that
  already completed remain successful but carry a telemetry warning if post-operation recording fails;
- structured preventive failover: null input is rejected, duplicate providers are processed once, an empty set is
  an explicit no-op, and any provider failure returns `HYPERDRIVE_PREVENTIVE_FAILOVER_FAILED` with its detailed
  errors. The HTTP boundary cannot report success after an internal failover failure, and the prediction loop
  observes the same result rather than discarding it;
- fail-closed HyperDrive DNA configuration persistence: update/reset commits the new live configuration only after
  `OASISDNAManager.SaveDNA()` succeeds. Persistence errors retain the previous configuration and propagate as an
  `OASISResult<bool>` error; they cannot be reduced to console output followed by false success;
- structured node-local persistence for quota and ONET peer-cache state: a missing file is a successful empty read,
  while corrupt/inaccessible state is an explicit error. Enforced quota loads therefore fail closed. Atomic writes
  use per-write temporary files; a post-operation quota write failure rolls back only the unpersisted counter and is
  attached to the already successful provider result as a warning so callers neither receive false durability nor
  retry a mutation that actually completed;
- deterministic Unity package construction: generated tar entry timestamps and the gzip header timestamp are
  canonicalized, and the release gate performs a second clean build of the selected profile and requires an exact
  SHA-256 match before accepting its provenance;
- direct HyperDrive v2 storage-provider execution for migrated async paths and synchronous Holon ID/provider-key,
  parent, metadata and load-all queries plus single/batch save and ID delete paths. The synchronous router invokes
  provider synchronous contracts directly (there is
  no `Task.Result`/sync-over-async bridge),
  preserving child traversal and version options without recursively invoking managers, mutating the global
  current provider, silently bypassing subscription policy, or falling back to the legacy pipeline on error;
- explicit all-provider/partial-provider failover and replication diagnostics. The legacy node-local quota counter
  is an explicit opt-in and records only successful routed operations with awaited persistence; hosted quotas remain
  the responsibility of authenticated subscription authority. V2 storage mutations are classified against the
  replication quota rather than the request quota, and the failover quota is checked before any secondary provider
  is invoked and recorded only after a secondary succeeds. Explicit load-balance, failover and replication entry
  points enforce the same quotas, so callers cannot bypass subscription policy by choosing a lower-level route;
- deterministic unit tests for protocol validation, transport errors, restart durability, rollback, replay and connectivity recovery.

The active `IOASISStorageProvider` router surface and runtime-scoped manager composition described below are migrated,
and the deterministic/real-database fault-injection and full automated release gates pass. HoloOASIS physical mobile
profiling, device lifecycle/field tests and external-network endurance gates remain mandatory before this status becomes
production-ready. The
Our World package integration now compiles through Unity 2022.3, securely resumes an offline grant before showing
login, keeps an animated `Beaming In...` state for the entire active login operation, reports a friendly hosted-network
failure only after the operation finishes, displays Edge connectivity/synchronization/pending state in the HUD, and
serializes mobile suspend/resume against endpoint disposal.

This document supersedes unqualified claims in older documentation that HyperDrive already guarantees zero downtime.
The implemented Edge path now has a durable SQLite outbox, restart-safe checkpoints, idempotent hosted exchange,
conflict records and snapshot rebase, but production guarantees still depend on platform-secure host integration,
physical-device qualification and external live-network/endurance gates documented below.

## Decision

Mobile and constrained clients use an **OASIS Edge Runtime**. “ONODE Lite” remains a useful product description, but the implementation must not be a trimmed copy of ONODE. It is a separate composition root which references shared OASIS components:

- OASIS Core object model and managers;
- HyperDrive routing and synchronization engine;
- a provider-neutral durable synchronization-state contract, currently implemented by SQLite for the MVP;
- LocalFileOASIS for recovery/export and a minimum-dependency comparison provider;
- HoloOASIS as the intended default domain provider after its Unity/mobile conductor is complete, proven and measured;
- HostedOASISOASIS, an authenticated remote `IOASISStorageProvider` backed by the hosted sync API;
- secure device identity/session storage and mobile lifecycle integration.

It excludes ASP.NET controllers, the ONODE WebAPI host, server-only email/subscription services, MongoDB drivers, and providers that the application did not select at build time.

The full hosted ONODE and the Edge Runtime share HyperDrive synchronization contracts, conflict rules and identifiers through the lightweight `NextGenSoftware.OASIS.HyperDrive.Synchronization` assembly. Full Core references that assembly, while Edge references it directly; the Unity/IL2CPP dependency closure must not contain `NextGenSoftware.OASIS.API.Core` or its server-only package graph. They differ in their composition roots and installed providers.

## Runtime products and Native Integrated Endpoint distributions

OASIS should publish two runtime compositions without copying or forking Core code:

| Distribution | Composition | Intended use |
|---|---|---|
| OASIS Full Runtime | Core + completed HyperDrive v2 + full ONODE services + the configured server/provider catalogue | hosted REST APIs, servers, desktop/community ONODEs and powerful native applications |
| OASIS Edge Runtime | Core + completed HyperDrive v2 + durable sync + selected mobile providers, without the WebAPI host or server-only dependencies | Unity, Android, iOS, constrained/embedded and offline-first applications |

The STAR ODK and STAR CLI are the primary existing consumers of the Full Native Integrated Endpoint. Their native workflows, OAPP templates and runtime-version reporting remain Full Runtime scenarios; Edge work must not silently reduce their provider or ONODE capabilities.

The Native Integrated Endpoint is the in-process facade over a runtime composition. It therefore has two supported distributions:

- `NextGenSoftware.OASIS.API.Native.Integrated.EndPoint` remains the full/runtime-compatible product for backward compatibility;
- `NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge` exposes the same supported facade shape over OASIS Edge Runtime;
- the Edge endpoint also exposes `OASISEdgeOnetAPI`, which composes authenticated ONET startup, signed capability
  leases, typed offline entity operations, offline-session grants, conflicts and explicit synchronization without
  referencing Full ONODE;
- shared endpoint contracts and manager facades live in one shared project/package;
- the Edge package must not reference full ONODE, ASP.NET, MongoDB or the all-provider BootLoader;
- applications choose a distribution at build/deployment time, not by catching a full-runtime load failure and silently switching.

The hosted REST APIs and full Native Integrated Endpoint use OASIS Full Runtime. The Edge endpoint uses OASIS Edge Runtime. Both execute the same Core and HyperDrive v2 behaviour; the provider catalogue and hosting surface differ.

### ONET node profiles

ONET participants may run either node profile:

- **Full ONODE** — full ONODE services and APIs with an operator-selected provider capability set;
- **Edge ONODE** — OASIS Edge Runtime plus ONET identity/transport, local storage and hosted/peer synchronization, within constrained-device resource limits.

“Full” means the complete ONODE hosting/runtime capability, not that every provider must be installed, registered or active. A Full ONODE operator chooses the providers the node supports. Its signed ONET capability advertisement must describe only providers that are installed, configured, healthy and permitted for remote use. HyperDrive routes only to those advertised capabilities.

Edge ONODE is likewise a node profile over shared components, not a second network protocol. It participates in ONET using the same identity, security and synchronization contracts, while advertising its smaller capability set and resource constraints.

The lightweight implementation is split into `NextGenSoftware.OASIS.ONET`, which owns the transport-neutral
request/response endpoint and version-3 HyperDrive client transport, and
`NextGenSoftware.OASIS.Edge.ONET.Runtime`, which composes that transport with `OASISEdgeRuntime`. Startup first
proves and persists the authenticated ONET-node-to-avatar binding through an explicitly supplied bootstrap
transport, rejects a channel/identity mismatch, and only then starts connectivity-triggered synchronization.
Neither assembly references Full ONODE, ASP.NET, MongoDB or the all-provider BootLoader. Full ONODE consumes the
same shared ONET endpoint instead of maintaining a second copy of the wire protocol.

Provider composition must become modular:

- provider implementations are independent packages/modules;
- runtime manifests list included provider modules and exact versions;
- OASIS DNA enables, prioritizes and configures only installed providers;
- startup fails clearly when DNA requests a provider absent from the runtime manifest;
- unused provider dependencies are not carried in the process or release artifact;
- ONET capability advertisements derive from the successfully activated provider registry, never from a hard-coded catalogue.

Older OASIS Runtime releases must be inventoried from their actual tags/assets and mapped to the source commit and dependency graph before compatibility claims are made. Existing release descriptions that promise offline operation are historical intent unless the corresponding artifact passes the new durable-sync acceptance suite.

### Release artifacts

The release pipeline produces reproducible, version-aligned artifacts rather than hand-copied DLL folders:

- Full Runtime archives for supported desktop/server runtime identifiers;
- Edge Runtime Unity package plus Android/iOS/desktop-compatible managed/native dependencies;
- Full and Edge Native Integrated Endpoint NuGet packages;
- dependency lock/manifest containing every source SHA and package version;
- checksums and SBOM;
- API compatibility report between Full and Edge endpoint facades;
- test/acceptance report identifying the exact commit and artifact hashes.

The Edge Runtime receives its own GitHub release only after the Edge acceptance criteria in this document pass. Release creation is a controlled production action, not part of an ordinary development build.

## Runtime topology

```text
Unity / Our World
       |
OASIS Edge Runtime
       +-- IHyperDriveSyncStateStore: provider-neutral journal/recovery invariant
       |      +-- EdgeSQLiteSyncStateStore: implemented MVP/reference store
       |      +-- HoloOASISSyncStateStore: candidate after atomic-equivalence qualification
       +-- HoloOASIS: intended default domain/agent/peer provider after mobile qualification
       +-- LocalFileOASIS: recovery/export/minimum-dependency comparison role
       +-- HostedOASISOASIS: HTTPS or ONET transport
                         |
                    Hosted ONODE
                         +-- validation/auth/business rules
                         +-- HyperDrive
                         +-- MongoDB/Holochain/other configured providers
```

The phone does not run MongoDB and does not receive database credentials. MongoOASIS remains a hosted provider. A MongoDB client cannot make a remote database available offline, and embedding a database server in a phone would add unacceptable platform, security, memory, storage and battery costs.

### MongoOASIS is the first hosted reference provider

MongoOASIS is the first complete hosted implementation of the HyperDrive synchronization contracts because MongoDB
is already the principal practical OASIS persistence foundation and provided the quickest, simplest and most
economical path to an end-to-end implementation. It proves transactional operation ingestion, immutable idempotency
receipts, per-device checkpoints, snapshots, change-feed capture, retention, migration/backfill and durable fan-out.

MongoDB is not the definition of HyperDrive and is not a permanent protocol dependency. HyperDrive depends on
provider-neutral hosted contracts for operation application, feed/snapshot generation, change capture, receipts,
checkpoints, retention and fan-out. MongoOASIS is currently the most complete reference implementation and therefore
the initial hosted production provider.

An authoritative hosted provider implements the single composite `IHostedHyperDriveProvider` contract. That
interface inherits the smaller sync, peer-binding, command, fan-out, maintenance, domain-mutation, change-capture and
backfill contracts. The smaller interfaces let each hosted service depend only on the capability it uses; provider
authors normally declare only the composite interface. MongoOASIS follows this model.

Other providers may implement the whole hosted contract or an explicitly declared subset. HoloOASIS and future SQL,
graph, blockchain or composite providers must pass the applicable conformance and fault-injection suites before
advertising a capability. A provider suitable for immutable replication may not be suitable for high-volume device
checkpoints or authoritative snapshots. ONET capability advertisements and HyperDrive routing policy must report
those distinctions rather than treating every provider as interchangeable.

The hosted ONODE currently fails startup when hosted synchronization is enabled but its selected authoritative
provider does not implement the required complete boundary. That fail-fast rule remains until another qualified
provider or an explicitly designed composite capability supplies the same guarantees.

## Provider roles

The device should not continuously replicate every write to three equal local databases.

| Provider | Mobile role |
|---|---|
| SQLiteOASIS | Implemented reference sync journal; authoritative MVP domain store and qualified alternative domain provider |
| HoloOASIS | Intended default agent-centric domain/peer provider and candidate sync-journal implementation after qualification |
| LocalFileOASIS | Recovery/export and minimum-dependency comparison provider; not sync-equivalent until it implements the required atomic/idempotent contracts |
| HostedOASISOASIS | Remote synchronization boundary; never owns local durability |

This role separation avoids multiplied writes, battery drain and unnecessary local-local conflicts.

### Provider rollout and promotion policy

The agreed delivery order is correctness-first:

1. Complete the Our World MVP using Edge SQLite, including physical Android flight-mode, restart, reconnect and
   recovery tests. The existing 26/26 Edge SQLite suite proves the store implementation but is not a substitute for
   device qualification.
2. Finish the HoloOASIS hApp and mobile conductor lifecycle, then run the identical correctness and performance suite.
3. Promote HoloOASIS to the default domain provider only after it passes provenance, Nix/Tryorama, IL2CPP/AOT,
   Android/iOS lifecycle, idempotency, restart, convergence and resource-budget gates.
4. Implement the complete `IHyperDriveSyncStateStore` contract for HoloOASIS and compare it with the existing SQLite
   implementation. SQLite remains underneath HoloOASIS only if that mixed composition wins the correctness/resource
   comparison; it is not an architectural requirement.

Provider selection and the offline-sync feature toggle are separate controls. `OfflineSyncEnabled=false` disables
the Edge journal and synchronization feature. With offline sync enabled, DNA selects the qualified domain provider;
the standard initial ordering is SQLite during MVP and changes to HoloOASIS after its promotion gate passes.

The permanent requirement is the journal invariant, not its storage technology. Every journal implementation must
atomically preserve local mutation/outbox state, monotonic device sequence, idempotency receipts, acknowledgements,
checkpoints, tombstones, conflicts, snapshot staging and crash recovery. Edge Runtime currently constructs the
concrete `EdgeSQLiteSyncStateStore`; provider-neutral constructor/repository injection is required before another
journal can be selected and must be covered by the same fault-injection suite.

Holochain itself uses SQLite internally for conductor persistence. Therefore the comparison is not “SQLite versus a
non-transactional Holochain filesystem.” It is whether Holochain's supported conductor/zome APIs let HoloOASIS expose
the complete HyperDrive journal transaction boundary without maintaining a second application-owned SQLite database.
Internal SQLite use alone does not prove that an application can atomically couple source-chain/DHT actions with
HyperDrive sequence, receipt and checkpoint records; the HoloOASIS journal implementation and fault-injection tests
must demonstrate that public contract. If they do, the single HoloOASIS domain+journal composition is preferred for
simplicity and avoids redundant application-level storage.

### Comparative provider benchmark

HoloOASIS, Edge SQLite and LocalFileOASIS must run the same versioned workload and dataset. Reports record raw values,
device/OS/build hashes and percentiles rather than subjective labels. The matrix includes:

- cold/warm startup and provider activation time;
- read, durable write and delete latency plus operations per second;
- idle/active/peak memory and CPU, Unity main-thread stalls and frame-time percentiles;
- storage per 1,000 and 10,000 entities, write amplification, compaction and restart recovery time;
- network bytes per operation, offline outbox growth and post-reconnect convergence time;
- idle-online, prolonged-offline and synchronization battery/radio consumption;
- Android/iOS package footprint per ABI and IL2CPP/AOT compatibility;
- duplicate delivery, abrupt termination, conflict and multi-device convergence correctness.

The comparison includes at least these complete compositions:

- HoloOASIS domain provider plus SQLite journal;
- HoloOASIS domain provider plus HoloOASIS journal;
- SQLite domain provider plus SQLite journal;
- LocalFileOASIS as a benchmark baseline, and as a full composition only after contract qualification.

LocalFileOASIS is a useful minimum-dependency baseline, but benchmark participation does not imply production sync
equivalence. It must implement atomic mutation plus operation-receipt persistence and the same recovery invariants
before it can be selected as a production synchronization provider.

### Preferred HoloOASIS + HyperDrive topology

The target is a cooperative hybrid, not two synchronization engines racing to own the same mutation:

```text
Our World / OGEngineClient
          |
    OASIS Edge Runtime
          |
   HoloOASIS local state
 source chain + qualified journal
          |
    +-----+----------------+
    |                      |
Holochain DHT/gossip   HyperDrive coordinator
    |                      |
peer convergence       Hosted ONODE + other providers
```

HoloOASIS is the preferred local domain/agent provider and, if its journal implementation passes qualification, the
single local mutation and synchronization-state store. Holochain owns permitted agent-to-agent DHT/gossip
convergence. HyperDrive owns connectivity state, hosted reconciliation, authority enforcement, provider abstraction
and bridging to non-Holochain providers. The hosted ONODE remains authoritative for protected global effects.

There is one logical operation identity across both systems. Every mutation envelope carries at least:

- immutable `OperationId`, `EntityId`, `VersionId` and optional `PreviousVersionId`;
- `OriginNodeId` and `OriginProvider`;
- an explicit `AuthorityClass`;
- canonical payload/type version and authenticated actor/device identity.

An operation observed through Holochain gossip and later seen through HyperDrive—or the reverse—is acknowledged as
the same operation, never reapplied as a new mutation. Capture/projection code records origin and operation receipt
atomically so Holochain-to-HyperDrive-to-Holochain feedback cannot create an infinite loop or duplicate reward.

Authority policy is versioned per entity/command type:

| Authority class | Examples | Offline/peer behavior |
|---|---|---|
| Agent/peer convergent | user-created Holons, permitted profile/content edits, presence/social state, discoveries, peer messages, public non-ownership metadata | Commit locally through HoloOASIS and gossip directly under validation rules |
| Hosted authoritative | Karma changes, inventory consumption, quest rewards, NFT/GeoNFT ownership, exclusive claims, purchases, entitlements, trades, security-sensitive avatar changes | Record pending command locally; only an authenticated ONODE result finalizes the protected effect |
| Hybrid | a local discovery that requests a protected collection/reward | Gossip the permitted event; HyperDrive submits the protected command using the same operation identity and distributes its final result |

For a GeoNFT collection, HoloOASIS first records the local discovery/event. HyperDrive queues a collection command
with the same operation id. ONODE validates location, availability, ownership and reward policy, then returns a
success or rejection command result. HoloOASIS records that authoritative outcome and gossip may distribute the
final state without minting, collecting or rewarding twice.

Qualification compares three explicit modes rather than an ambiguous mixed result:

1. **Holochain-native:** HoloOASIS local source-chain commit plus DHT/gossip, with hosted HyperDrive disabled for the
   entity types under test.
2. **HyperDrive baseline:** Edge journal plus hosted ONODE synchronization and configured providers, without
   Holochain gossip for the test records.
3. **Hybrid:** HoloOASIS local/domain operation plus gossip and HyperDrive hosted/provider reconciliation under the
   shared identity and authority policy.

The test report measures local-commit, peer-visibility and hosted/global-convergence latency separately. It also
tests peer-reachable/ONODE-unreachable and ONODE-reachable/no-peer partitions, duplicate delivery, feedback-loop
suppression, conflicting multi-device edits and protected-command rejection.

### Capability boundary and product message

Holochain and HyperDrive both contribute offline resilience and eventual convergence, but they are not substitutes:

| Scenario or responsibility | Primary owner |
|---|---|
| Agent-owned source-chain history and local-first domain data | Holochain/HoloOASIS |
| Direct peer sharing, decentralized validation, DHT distribution and gossip convergence | Holochain |
| Community operation while the hosted ONODE is unavailable | Holochain peers, for policy-permitted data |
| Durable commands, ordering, idempotency receipts, acknowledgements and checkpoints | HyperDrive journal protocol |
| Hosted-authoritative Karma, rewards, inventory consumption, ownership, purchases and entitlements | HyperDrive + ONODE |
| Device snapshot/bootstrap and deterministic recovery | HyperDrive synchronization protocol |
| MongoDB/Holochain/SQLite/LocalFile and future-provider reconciliation | HyperDrive |
| Provider routing, replication policy and clients that are not Holochain participants | HyperDrive |
| Peer-reachable but ONODE-unreachable partition | Holochain continues permitted peer convergence; HyperDrive retains protected/global work pending |
| ONODE-reachable but no Holochain peers | HyperDrive continues hosted/global reconciliation |

Two phones without internet can exchange Holochain data only when they still have a usable communication path, such
as local Wi-Fi, a hotspot, supported mesh/direct networking, or later connectivity to reachable peers. Flight mode
with every radio disabled cannot transmit data; it supports local operation and later convergence, not communication
without a physical/network link.

The accurate product message is: **Holochain keeps agents and communities convergent peer-to-peer; HyperDrive keeps
the heterogeneous OASIS convergent across authority boundaries and providers.** These are the selected roles within
OASIS, not a claim that no other technology could ever implement similar capabilities.

## Current implementation audit

Before this work the source implemented only:

- ordered provider failover through `ProviderManager`;
- immediate auto-replication by calling each configured provider during a save;
- warnings when a replica fails;
- provider health/routing/analytics scaffolding;
- ONET discovery, node registration, routing and security scaffolding.

Those legacy facilities did **not** implement the offline guarantees required here. The new Edge SQLite
coordinator, hosted Mongo sync store and transport-neutral protocol now implement the core offline journal,
replay, acknowledgement, tombstone, explicit-conflict, atomic local commit and bounded delta-exchange
invariants. Edge Runtime and the Edge Native Integrated Endpoint also expose a typed entity repository whose
save/delete calls use that same atomic state/outbox boundary; stable caller-supplied operation IDs make an
identical interrupted local retry idempotent, while reuse for different immutable content is rejected.
The audit found a critical integration boundary that the earlier protocol work did not satisfy: hosted sync
collections were separate from normal `Avatar`, `AvatarDetail` and `Holon` collections, creating two sources of
truth. That boundary is now implemented for the supported Holon-family types through
`IHostedHyperDriveDomainMutationStore` and `IHostedHyperDriveDomainChangeCaptureStore`; unsupported types fail
explicitly rather than landing only in a shadow collection. `HyperDriveEntityTypes` supplies stable, versioned
wire identities for avatar, holon,
quest, quest-progress, inventory and GeoNFT data. A custom entity type remains a valid private sync document but
cannot silently claim a full-runtime projection without a registered codec.
The Mongo provider now implements authoritative Holon-family codecs for `oasis.holon.v1`, `star.quest.v1`,
`star.inventory-item.v1`, `star.geonft.v1` and `star.geonft-collection.v1`. Each typed codec validates the persisted
`HolonType`, so a client cannot label an unrelated Holon as a quest or GeoNFT. Their upsert or soft delete,
immutable operation receipt, sync entity, change record, fan-out record, operation result and device sequence all
commit in one Mongo transaction. Replaying identical content returns the original domain result; reusing an
operation id for different immutable content fails. Avatar/profile and command-state types are explicitly rejected with
`HOSTED_DOMAIN_CODEC_NOT_REGISTERED` until their domain-specific codecs exist, rather than being falsely accepted
into only the sync shadow collection. The replica-set rollback matrix includes the domain-write boundary and the
real `Holon` and domain-receipt collections.
Quest and GeoNFT definitions have an explicit global audience and are included in every authenticated Edge feed and
snapshot; general Holons and inventory definitions remain creator-scoped. A filtered pull advances to the transaction's
global sequence watermark after exhausting its visible rows, so private changes owned by other avatars cannot cause
permanent rescanning or checkpoint starvation. The reverse `Holon` path is also implemented as MongoDB change-stream capture. It uses update lookup plus required
pre-images, a durable resume token, a leased single active worker and a transaction containing the projected sync
entity/change and capture checkpoint. A later capture of an Edge-originated write observes the already-projected
version and advances its source checkpoint without emitting a duplicate change. Malformed or unowned source
documents are written to `HyperDriveDomainCaptureDeadLetters` and surfaced as an error count; they are never
silently skipped. A fresh empty database is initialized at an explicit replica-set operation time. An existing
non-empty domain is initialized by the online backfill before capture starts, preventing a deployment from pretending
that historical state was captured.

When `EnableHostedSync` starts against an existing Mongo deployment, the domain-capture worker automatically runs
the same online, lease-protected migration before beginning change capture. Multiple ONODE replicas coordinate through
the migration lease; replicas that do not own it wait until the owning replica completes. Invalid historical documents,
missing replica-set support or failure to enable pre-images still fail visibly and prevent capture from starting.

The migration can also be run explicitly before deployment when operators want to separate it from application startup:

```powershell
$env:OASIS_MONGO_REPLICA_SET_CONNECTION = '<replica-set connection string>'
dotnet run --project Tools/NextGenSoftware.OASIS.HyperDrive.Migration --configuration Release -- `
  --database '<OASIS database name>' --batch-size 250
```

The connection string is read only from the named environment variable and is never echoed. The migration enables
MongoDB pre-images, captures a replica-set operation-time watermark, pages existing Holons into sync entities and
initial change records, persists hard-delete identity mappings, and only then initializes capture at that watermark.
Writes may continue during the scan: later change-stream replay converges them from the watermark. The migration is
single-writer leased and its per-Holon initial changes are idempotent, so a repaired dead-letter run can be retried.
It exits non-zero and leaves capture uninitialized while any source document is quarantined.
Remaining production work is to complete all HyperDrive manager migration and validate/profile the mobile
application on physical target devices. The Holochain hApp and portable release matrix are now automated; they do
not substitute for the Android/iOS hardware gates.
Edge connectivity has three explicit states: `Offline`, `Connecting`, and `Online`. A device network signal moves
the runtime only to `Connecting`; `Online` is published after the hosted service successfully answers an exchange.
This prevents the HUD and clients from briefly claiming that OASIS is online during an outage or recovery probe.
`ReplicatorManager` is now a runtime-scoped SDK façade over the authoritative v2 replication pipeline rather than
commented scaffolding. It validates that configured targets are registered in its injected runtime, preserves their
declared order, exposes the structured replication diagnostic, and never mutates the process-global runtime when an
isolated Full/Edge/Native runtime is supplied. Hosted synchronization continues to use the durable Mongo outbox and
ordered, leased, idempotent provider fan-out path described here; the façade does not create a competing replication
implementation.
Two focused manager tests and the complete Core suite (227/227) protect runtime isolation, configured target
validation and delegation to v2. The aggregate `SqliteMvp` gate passes 458/458; evidence is
`artifacts/current-goal-replicator-manager-release/acceptance-report.json`. The Edge archive remains byte-identical
to the preceding conflict-status release at SHA-256
`B4A8F94316CF79986480D5CBEF05896E08B653D666A6E0A9E07462F6B097D3C7`.
After adding the hosted provider facade and mandatory ONET partition soak, the complete `HoloEnabled` gate passes
551/551 tests plus Unity Editor, ARM64 Android-player and real Our World integration compilation. Its evidence is
`artifacts/current-goal-command-restart-release/acceptance-report.json`; the reproducible UPM archive SHA-256 is
`65D7E0018C224B71FB6FF625855F4099E190E2FC1723530FB28C06C1B047ED1D`.

Settings-backed manager writes use detached Holon snapshots. This prevents a provider cache that returns entity
references from observing uncommitted metadata when a save is rejected. Social and video manager projections are
also bound to their injected runtime rather than the process-global singleton; failed projection writes are exposed
as warnings, and a rejected initial video-call write removes the transient in-memory call.
Chat, gifts, competition and karma projections are likewise runtime-scoped. Chat creation supplies an explicit audit
avatar and removes transient sessions/messages when their authoritative save is rejected. Karma is committed before
its five seasonal leaderboard projections, projection failures are visible warnings rather than hidden console output,
and restart-safe history/statistics load both in-memory dictionary and provider JSON-array metadata shapes.
Chat sessions and messages are now durable read models rather than process-memory-only objects. Session Holons carry
an explicit entity discriminator, stable session identity, participant indexes and lifecycle timestamps; message
Holons carry the matching session index and message contract. Active-session and history reads rebuild their caches
through the injected Holon/HyperDrive runtime after restart. Ending a session updates a detached Holon snapshot and
rolls the cache back when persistence is rejected, preventing provider-returned object aliases from leaking an
uncommitted termination. Chat competition projections are executed and returned as structured warnings after the
canonical message commit; the former unused method with a console-only catch no longer hides projection failures.
Focused restart/rejection coverage passes 3/3 and the complete Core suite passes 235/235.
Durable karma settings are authoritative for totals as well as history and statistics; avatar details are projections
updated only after ledger acceptance. A rejected ledger mutation leaves the avatar projection untouched, while a
post-commit avatar projection failure is an explicit warning. Authentication temporarily scopes its login-provider
list to the injected runtime under a concurrency lock and restores it in `finally`, including failed login attempts.
Search routing and all-provider enumeration now use only the manager's injected `ProviderManager`. The legacy path
also selects the actual failover-list entry (rather than retrying the originally requested provider) and restores the
runtime's original provider in `finally` for single-, multi-provider and exceptional searches.
All active Avatar, Holon, Wallet and Key manager partial operations now use their injected runtime provider manager as well;
only the intentional singleton factory/default-constructor entry points retain `ProviderManager.Instance`. This
prevents a Full ONODE, Edge ONODE, Native Integrated Endpoint or test runtime in the same process from selecting,
replicating through, or mutating another runtime's providers. Legacy Holon hard-delete replication also now selects
the configured replica entry (`type.Value`) rather than repeatedly deleting through the requested primary.

Consequently, existing auto-replication is **write-through replication**, not offline eventual synchronization.

## Required invariants

1. A locally successful mutation and its outbound sync operation commit atomically in the same local transaction.
2. Every operation has a globally unique immutable `OperationId` and a monotonically increasing per-device sequence.
3. Replaying an operation any number of times has the same server result as applying it once.
4. The device removes/compacts an operation only after a durable server acknowledgement.
5. The server advances a device checkpoint only in the same transaction as accepting the associated operations.
6. Pull changes and the device pull checkpoint commit atomically locally.
7. Deletes synchronize as tombstones; absence is not a delete signal.
8. Conflicts are explicit results. They are never silently overwritten by provider order or wall-clock time alone.
9. Provider transport failure never changes a committed local success into data loss.
10. ONET and HTTPS carry the same synchronization protocol and do not define different conflict semantics.
11. A hosted operation is acknowledged as accepted only if its authoritative OASIS domain mutation and durable
    operation receipt commit atomically; the sync shadow collection is not a substitute for the domain write.
12. A mutation committed through an ordinary hosted OASIS API is captured into the sync change feed exactly once,
    with its capture checkpoint committed atomically with that projection.

## Synchronization protocol

The Edge Runtime records an operation locally, then periodically exchanges bounded batches with hosted ONODE:

1. commit entity mutation and outbox record together;
2. read the next ordered pending batch;
3. send device id, last server checkpoint and operations;
4. hosted ONODE authenticates the avatar/device and deduplicates by `OperationId`;
5. ONODE validates base versions and applies accepted built-in operations through the registered domain codec and
   authoritative provider transaction; an unknown/unregistered domain type is rejected rather than acknowledged;
6. hosted HyperDrive performs its configured provider replication;
7. response contains per-operation acknowledgements/conflicts plus remote changes after the supplied checkpoint;
8. the device atomically applies remote changes, records conflicts, marks acknowledgements and advances its checkpoint.

Hosted provider fan-out retries use bounded exponential backoff and durable attempt/lease state. Edge outbound
operations remain durably pending across process restarts. A platform adapter implements
`IEdgeConnectivityMonitor`; after `StartConnectivityMonitoringAsync`, recovery events immediately run bounded
synchronization, while an offline transition during an in-flight exchange cannot be overwritten by that stale
run's completion. Hosts may also call `SetConnectivityAsync` directly. Mobile lifecycle/connectivity adapters
remain responsible for supplying authoritative platform signals. A retry is protocol behaviour, not an
alternative implementation path.

## Versions and conflicts

Existing holons already expose `Version`, `VersionId`, `PreviousVersionId`, `ModifiedDate`, `DeletedDate` and provider keys. The synchronization protocol uses the stable OASIS `Id`, `VersionId` and `PreviousVersionId`; provider-specific keys never become cross-provider identity.

Initial conflict policy:

- non-overlapping metadata/property changes may be merged only by a registered deterministic resolver;
- quest rewards, inventory grants, purchases, karma and other transactional effects require domain-specific idempotent commands and must not use last-write-wins;
- ordinary profile/content edits may use an explicit server-wins, client-wins or manual policy configured by entity type;
- an unknown entity type defaults to conflict/manual review, not silent overwrite;
- deletes are tombstones with retention long enough for every supported offline window.

## Authentication while offline

A previously authenticated avatar may continue offline using a securely stored, device-bound local session and cached authorization scope. A new login, password verification, entitlement purchase or other server-authoritative action cannot be invented offline. Offline actions outside the cached scope remain pending until ONODE validates them.

`EdgeOfflineSessionManager` enforces those client-side rules and is exposed by both `OASISEdgeRuntime` and
`OASISEdgeAPI` when a secure store and signature validator are configured together. The authenticated hosted endpoint
`POST /api/hyperdrive/sync/offline-session-grant` derives the avatar from the JWT, binds the grant to the submitted
device id, rejects scopes outside the server allow-list, caps its lifetime and signs it with a dedicated ECDSA P-256
identity. `HttpHyperDriveSyncTransport` obtains the grant and `OASISEdgeRuntime.AcquireOfflineSessionAsync` validates
and stores it through the platform-secure boundary.

Enable issuance with `OASIS.OfflineSessionGrants.Enabled`, an explicit `AllowedScopes` list and
`MaximumLifetimeMinutes`. `SigningPrivateKeyEnvironmentVariable` names the environment secret containing a base64
PKCS#8 P-256 private key (default `OASIS_OFFLINE_GRANT_SIGNING_PRIVATE_KEY`); the private key is never stored in DNA.
`SigningPublicKey` contains the corresponding base64 SubjectPublicKeyInfo and is pinned into the Edge application
release. The ONODE derives the public key from the private key at startup and compares the two in constant time, so a
mis-keyed deployment fails before serving traffic. An enabled ONODE fails startup when its key or scope policy is invalid.
Unity packages include concrete iOS Keychain, Android Keystore and Windows Credential Manager adapters. Existing JWT
values in editable host configuration are not accepted as offline authorization.

`hyperdrive.sync` is a deliberately narrow scope. After an offline restart the Edge transport attaches the validated
grant only to `POST /api/hyperdrive/sync/exchange`; it is not an authentication token for avatar, admin, STAR or any
other REST endpoint. The hosted exchange validates signature, expiry, exact scope, avatar and request device before it
activates a storage provider. This lets the recovery loop automatically drain the durable outbox when the host returns
without retaining a JWT or asking the player to log in again. Rotating the signing key invalidates grants issued by
the previous key; normal expiry bounds every capability independently.

Generate a matched P-256 signing pair and public deployment fragments with:

```powershell
./Scripts/new_edge_offline_grant_signing_config.ps1 -OutputDirectory '<secure output directory>'
```

The generated `offline-grant-signing.secrets.env` contains the private PKCS#8 key and is git-ignored; load it
through the host secret manager under the emitted environment-variable name. Merge only the generated ONODE
public fragment into `OASIS` DNA and the Edge public fragment into the Unity release configuration. The script
cryptographically verifies the pair before writing either public fragment. Production release automation must
never generate a replacement implicitly because doing so would invalidate every outstanding device grant.

## ONET relationship

ONET supplies discovery, secure peer communication and alternative routes. HyperDrive owns storage routing and synchronization semantics. The sync exchange contract is transport-neutral:

- HTTPS is the first production transport;
- ONET carries the same version-3 exchange DTO through `oasis.hyperdrive.sync.exchange.v3`; its request/response
  layer does not equate network delivery acknowledgement with application success and derives avatar scope
  from authenticated node identity rather than the submitted payload;
- HoloNET may carry the same batches later.

This prevents ONET from becoming a second synchronization engine.

## Delivery sequence

1. **Implemented:** shared HyperDrive sync contracts, journal interfaces, coordinator and deterministic tests.
2. **Implemented:** SQLite atomic entity/outbox/checkpoint implementation for Edge Runtime.
3. **Partially implemented:** hosted ONODE sync endpoint with operation deduplication, version validation and delta
   feed. The explicit domain-mutation and reverse change-capture contracts plus stable versioned entity names are
   implemented. Atomic Mongo projection and replay receipts are implemented for the base Holon codec, including a
   replica-set rollback boundary. Durable leased Mongo change capture is implemented for normal Holon saves,
   including resume checkpoints, pre-image hard-delete recovery, loop suppression and dead letters. The online,
   operation-time-watermarked existing-data backfill tool is implemented and included in the release gate. The gate
   runs its direct CLI contract tests (argument rejection, environment selection, batch propagation, structured
   success and structured failure) and retains `hyperdrive-migration.trx`; compiling the executable alone is not
   accepted as migration evidence.
   Typed quest, inventory-definition, NFT, NFT-collection, GeoNFT, GeoNFT-collection and GeoHotSpot Holon codecs plus
   global-definition audience delivery are implemented. Quest-progress, inventory-grant and GeoNFT-collection commands are durably accepted in
   the same MongoDB transaction as their device acknowledgement, then executed in command-sequence order through the
   existing QuestManager, AvatarManager and NFT loading paths. A global fenced worker lease prevents ONODE instances
   from overtaking the queue; item leases are renewed during long manager calls, expired work is reclaimable, and
   transient infrastructure failures use the documented exponential retry schedule. Terminal success or domain
   rejection is written atomically as a private `oasis.command-result.v1` change. The Edge API queues command intent
   without speculatively changing local entity state and exposes the eventual typed outcome by operation id.

   Canonical command payloads are JSON: `star.quest-progress.v1` carries `gameSource`, optional
   `activeObjectiveId`, and the existing progress delta fields; `star.inventory-item.v1` carries the Core
   `InventoryItem` shape; `star.geonft-collection.v1` carries `CollectGeoNFTRequest` and targets the source GeoNFT id.
   In all three cases the authenticated avatar,
   target entity id and idempotency operation id come from the synchronization envelope, overriding client identity
   fields. Quest reward grants derive stable child operation ids, so replay after a partial worker failure cannot
   duplicate inventory effects. Avatar and AvatarDetail now use separate private, whitelisted Edge projections with
   their own watermarked backfills, required pre-images, fenced leases and resumable change-stream checkpoints.
   Projection writes/change records/checkpoints are transactional, and rejected source records are quarantined with
   their checkpoint in the same transaction. Direct Edge overwrites of Avatar, AvatarDetail and quest-progress
   projections are rejected as server-authoritative; those states change only through authenticated domain APIs or
   the supported idempotent command path.

   Portable command DTOs live in the shared synchronization contracts, so Unity callers do not reference ONODE or
   the full Core object graph. EdgeEntityRepository provides dedicated quest-progress, inventory-grant and
   GeoNFT-collection queue methods plus typed command-outcome loading. The Avatar projection contract is an explicit
   whitelist: password hashes, JWT/refresh/reset/verification tokens, provider wallets/usernames/storage keys and
   biometric-provider identifiers cannot be represented in the Edge payload. The matching projection mapper and
   durable capture/backfill wiring live in MongoOASIS.
4. **Implemented:** authenticated HTTPS transport implementing the same exchange, peer-binding and offline-grant
   contracts consumed by Edge Runtime. `HostedOASISSyncProvider` is the provider-shaped facade for Full Runtime
   consumers that require registration through `ProviderManager`. It is intentionally registered as a Network
   provider rather than pretending the ordered synchronization protocol exposes direct `IOASISStorageProvider`
   Avatar/Holon CRUD semantics. Activation is explicit and inactive calls return a structured error.
5. **Implemented:** Edge Runtime composition project targeting Unity-compatible .NET Standard 2.1, including
   typed save/load/delete operations surfaced by the Edge Native Integrated Endpoint. Tests cover durable
   restart, explicit tombstones, identical-operation replay, mismatched-operation rejection and concurrent
   contiguous device sequencing. The Edge Native Integrated Endpoint now also composes directly from an
   authenticated `HttpClient`, exposes runtime status changes and ONET identity binding, and has an endpoint-level
   test proving a locally queued entity is acknowledged through the hosted synchronization API without a Full ONODE.
   Explicit suspend/resume lifecycle methods stop hosted recovery work while suspended, preserve offline writes,
   ignore platform connectivity callbacks during suspension, and drain the durable outbox on resume when the
   platform is online. A final durable pending-count check prevents an in-flight concurrent local write from being
   hidden behind a stale `Synchronized` status; the bounded run immediately consumes that new operation. Async
   disposal cancels and joins an in-flight exchange before closing runtime resources. The deterministic UPM build
   now packages the portable endpoint plus verified SQLite 2.1.13 native artifacts for Windows, Linux, macOS,
   Android and iOS, rejects Full Core/server dependency leakage, emits per-file SHA-256 provenance, and compiles
   the result in Unity 2022.3.62f3. The Android acceptance build compiles the Java Keystore bridge and packages
   architecture-bound SQLite libraries for ARMv7, ARM64, x86 and x86_64; its successful player-build log is a
   required, hashed release artifact. `OASISEdgeUnityHost` owns authenticated transport, reachability transitions,
   application suspend/resume, status propagation and deterministic shutdown. It also has a distinct offline boot
   path which opens the local runtime without a bearer token, validates the cached server-signed grant against the
   pinned ONODE public key, and delays connectivity monitoring until a fresh hosted session is attached. This avoids
   requiring a working network merely to enter authorized local mode.
6. **Partially implemented for the current Our World read surface:** Our World distinguishes authentication network/timeout failures and preserves
   the `Beaming in...` transition while authentication is in flight. The reusable Unity Edge composition includes
   a fail-closed `IEdgeSecureSessionStore`: Windows Credential Manager is exercised by the Unity editor acceptance
   test, Android encrypts with a non-exportable Android Keystore AES-GCM key, and iOS stores a
   `kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly` Keychain item. Plain files and `PlayerPrefs` are never used.
   Our World has a deterministic package sync/install script and a local UPM reference. The release gate compiles
   the actual Hub against the exact manifested package after checking that no bearer is persisted in StreamingAssets.
   Online login keeps the bearer in memory, acquires a signed device-bound grant and secures it in the platform
   credential store before completing beam-in. Startup resumes only a valid pinned-key grant; otherwise it presents
   login. The prior short-lived PlayerPrefs API-response cache has been removed. Avatar, AvatarDetail/inventory,
   quest and GeoNFT collection screens read typed/durable Edge projections whenever the runtime is in local mode.
   The always-visible status strip reports Online/Offline, synchronization state, pending durable operations and
   the last Edge error code. The authoritative Karma total and ordered Edge-safe history are read from the private
   AvatarDetail projection while offline. Provider identity and external links are deliberately not projected.
   Device UI preferences apply immediately from local `PlayerPrefs`, but persistence is no longer an online-only
   REST side channel. Edge-enabled builds queue the typed, avatar-scoped `oasis.avatar-preferences.v1` command in
   the same SQLite outbox while offline. The hosted executor validates it, writes the deterministic `omniverse`
   settings Holon with the command operation receipt, and publishes the confirmed private projection. A crash and
   replay of the same operation therefore cannot create a second logical update. The remote-only profile uses the
   single authenticated `/api/settings/omniverse-preferences` route backed by that same settings Holon.

   Cached projections cover avatar/profile, inventory, quest definitions, generic NFT/GeoNFT definitions and collections,
   GeoHotSpot definitions, and the Karma total/history. The audited Our World gateway surface is release-gated: its
   current gameplay mutations for quest progress and active selection, inventory create/update/use/remove/transfer,
   GeoNFT collection, GeoHotSpot triggers and avatar preferences route through durable commands whenever Edge is
   enabled. Disconnect, restart, reconnect and duplicate-delivery coverage protects those command families. Any new
   public asynchronous gateway method fails release validation until it is explicitly classified. Clan/social live
   fields remain online-only today and are shown as such rather than synthesized offline. Our World's cross-game
   asset view merges the generic NFT and GeoNFT streams by stable id.
   `OGEngineClient.GetGeoHotSpotAsync` and Our World's dedicated map/presentation loader read the first-class,
   versioned GeoHotSpot projection in Edge mode. The REST endpoint is used only in the explicitly remote-only
   profile; missing Edge data remains a visible error rather than triggering a hidden network fallback. This path is
   compiled against the manifested package in the real Our World checkout, and an editor regression test protects
   the OGEngineClient/durable-projection routing. GeoHotSpot trigger submission queues through OGEngineClient and
   synchronizes to the same ONODE Core authority used by WEB5 REST. That authority owns authored-rule validation,
   spawn limits/cooldowns, durable reservation/replay and protected reward effects; Edge clients do not speculate
   those effects before the authoritative result synchronizes back.
   The permanent status strip is supplemented by state-driven Offline, reconnecting/synchronizing and synchronized
   toast notifications. Edge callbacks enqueue those notifications and Unity renders them on its main thread. The
   offline initialization path reloads the unsettled-operation count before publishing status, so a process restart
   cannot briefly display an empty tracker while durable commands remain in SQLite. Parameterized reconstruction
   tests lock this invariant for quest progress, inventory grants, GeoNFT collection and GeoHotSpot triggers. The
   release gate also classifies every public asynchronous gateway method and verifies the required projection routes,
   so a newly added live network call cannot silently escape the documented offline audit.
7. **Implemented; physical qualification pending:** HoloOASIS.Unity is a real, compiled adapter rather than a commented placeholder.
   HoloNET Client, HoloNET ORM, OASIS Common/DNA/Core, HoloOASIS and HoloOASIS.Unity have an actual
   `netstandard2.1` build path, and the release gate builds that exact target. Server-only subscription middleware,
   Mongo usage-ledger storage and telemetry registration are excluded from the Unity target rather than pulled into
   a mobile player. `HoloOASISUnityHost` serializes conductor start/suspend/resume/stop with provider
   activation/deactivation through the explicit `IHolochainConductorLifecycle` platform boundary; invalid or failed
   conductor startup is surfaced as an `OASISResult` and cannot leave an active provider behind. Unit tests protect
   startup failure and endpoint validation, and the Holo-enabled release gate runs them.

   The portable dependency gate now inspects the generated `.deps.json`, not only direct project references, and
   rejects transitive ASP.NET, MongoDB, SQL Server and OpenTelemetry dependencies. The portable Core excludes its
   SQL Server bridge repository; `System.Reflection.Metadata` is declared directly instead of being received
   accidentally through `Microsoft.Data.SqlClient`.

   Android now has a production integration against Holochain's official
   `org.holochain.androidserviceruntime:service` foreground-service runtime and a passing ARM64 Unity IL2CPP player
   build. This was not just a Java binding task. The current official Holochain 0.7 runtime intentionally exposes its
   admin operations through same-package Android IPC rather than an admin WebSocket. `setupApp` installs/enables the
   hApp and returns an app-interface port plus an authentication token. The existing HoloNET/HoloOASIS activation
   path historically required an admin WebSocket and did not send the Holochain 0.7 app-interface authentication
   token after connecting. That boundary is now repaired: HoloNET serializes the Holochain 0.7 `authenticate` envelope, defers both its connected
   event and automatic app-info request until authentication succeeds, clears authentication state on reconnect, and
   has exact wire-contract tests. HoloOASIS accepts a service-provisioned authenticated app client with no admin
   client, while its deactivation result now requires every client that actually exists to disconnect successfully.
   `HoloOASISUnityHost` validates the app token and installed-app id and composes that client without enabling
   HoloNET conductor process ownership; HoloNET obtains the agent key and DNA hash from authenticated `app_info`,
   matching the official service's port/token result. The managed Android lifecycle and real IPC/foreground-service
   adapter are implemented. The adapter starts the same-package service, installs/enables the packaged hApp,
   receives only the authenticated app-interface port/token, refreshes that session on resume and never creates a
   dummy admin WebSocket. Eight deterministic HoloOASIS.Unity tests cover host failure/cleanup, service-session
   validation, repository binding and Android start/resume semantics. The Maven coordinate currently documented by the upstream service,
   `service:0.0.19`, is not accepted for this profile: inspection of its published bytecode shows the pre-0.7
   `signalUrl`/`iceUrls` network contract, whereas upstream 0.3.0 source uses Holochain 0.7 and the iroh
   `bootstrapUrl`/`relayUrl` contract. The Holo-enabled release must therefore build a pinned upstream 0.3.0 source
   commit (or consume a later Maven artifact whose bytecode proves the same 0.7 contract); it must never silently
   resolve 0.0.19. `Scripts/build_holochain_android_runtime.ps1` now builds ARM64 service/client AARs from pinned
   upstream commit `a7b5bb12a64d837694a6939701823d61803654bf`, compiles the Kotlin Unity bridge against those locally built
   artifacts through an exclusive dependency rule, verifies the ARM64 conductor library and emits a hash manifest.
   The pinned upstream 0.3.0 tree had two stale handwritten parcel boundaries (removed `signalUrl`/`iceUrls` fields
   and missing `AwaitingRestore`/`Unrecoverable` app states); a reviewed compatibility patch is applied at build
   time. A second reviewed patch targets Kotlin 1.6.21/coroutines 1.6.4 because Unity 2022.3 ships Android Gradle
   Plugin 7.1.2, while newer Kotlin metadata causes Unity's D8/R8 stage to fail. The source validator requires the
   exact patched-file hashes and rejects any additional dirty path. Both patch hashes, every patched-source hash and
   the exact AAR/JAR runtime dependency closure are recorded in the manifest rather than silently substituting a
   different source or binary.
   The resulting ARM64 service AAR is 35.80 MiB compressed; its native ARM64 payload is approximately 108.5 MiB
   before APK/AAB compression and split delivery. The client AAR is 0.79 MiB and the OASIS bridge AAR is 0.01 MiB.
   These are artifact measurements, not memory/CPU/battery results. The `HoloEnabled` UPM profile now packages that
   verified closure, the hApp, the managed JNI bridge and an Android API 27+/ARM64-only build guard. Unity 2022.3
   compiles the exact package and produces a 56.46 MiB ARM64 IL2CPP APK containing `libil2cpp.so`,
   `libholochain_conductor_runtime_ffi.so` and `libholochain_conductor_runtime_types_ffi.so`; validation rejects a
   missing required library or any non-ARM64 native ABI. The UPM archive is 61.8 MiB. A loopback dummy admin endpoint
   or unauthenticated connection is explicitly not acceptable.

   The full HoloOASIS/Core assembly graph is intentionally not copied into the mobile package: doing so introduces
   server dependencies and breaks the Edge footprint invariant. The service boundary is in OASIS Edge Runtime. The
   new `HoloOASIS.Edge` assembly maps idempotent HyperDrive mutations to the exact hApp wire contract and its runtime
   host atomically owns foreground-service setup, authenticated HoloNET connection and cleanup. HoloNET's lightweight
   build is approximately 0.38 MiB rather than the former 53+ MiB embedded build, uses managed Ed25519, and matches a
   Rust-generated Holochain 0.7 canonical MessagePack/SHA-512 signing vector. Package gates reject desktop conductor
   executables, Sodium, the obsolete Windows serialization wrapper and framework-facade collisions. Nine Holo Edge
   tests cover mapping, rejection, cleanup and lifecycle invariants; all 70 HoloNET tests pass.

   The separately durable SQLite-to-Holo projection queue is implemented. Each non-command local mutation inserts
   one row per configured local target in the same SQLite transaction as the entity and hosted outbox write. Hosted
   acknowledgement cannot remove local-provider work. The local coordinator drains one target sequentially by device
   sequence, records explicit failure metadata without an inner retry loop, and acknowledges only after the Holo hApp
   accepts the idempotent operation. Restart, independent acknowledgement, command exclusion, failure ordering and
   late target attachment are covered by SQLite and Edge Runtime tests. OGEngineClient supplies the Holo target before
   store creation, while the Unity host starts Holo before OGEngineClient, suspends Edge before Holo, resumes Holo
   before Edge, rolls back failed two-part transitions, and disposes both through one lifecycle owner.

   Physical installation/lifecycle instrumentation and two-device convergence tests remain external release gates.

   iOS cannot be claimed from the Android result: its precompiled/interpreted WASM and App Store execution
   constraints require a separately qualified native runtime. Measured Android memory/CPU/storage/battery profiling,
   hardware suspend/resume/process-death testing and two-device offline gossip/reconnect tests remain release gates
   before HoloOASIS becomes the mobile default.
8. **Implemented; external network/mobile qualification pending:** authenticated ONET framed transport, Full ONODE sync host and lightweight Edge ONODE
   identity-binding/synchronization lifecycle are composed over the same shared protocol. Edge and Full nodes now
   sign canonical, expiring capability advertisements; the registry verifies source identity, signature, lifetime,
   stale replay and same-time equivocation. Full-node provider capabilities are derived only from registered,
   activated, policy-healthy providers explicitly allowed by `RemotelyAdvertisedProviderTypes`. Ten-minute leases
   renew every five minutes, fail closed on expiry, and expose/log renewal failures. The authenticated registry
   now distributes signed leases through a query endpoint; clients independently re-verify every lease and Edge
   ONODE selects its current Full ONODE sync host from the `hyperdrive-sync-v3` service advertisement on every
   exchange, with deterministic issue-time/node-id ordering and no static-host fallback. Edge can use multiple
   registries with an explicit acknowledgement/query quorum: it publishes the same signed lease to every registry,
   reconciles each node to its newest signed lease, rejects same-time equivocation, and surfaces partial-quorum
   failures as warnings. Full ONODE registries also pull signed leases from DNA-configured peers on a bounded interval,
   enforce a response quorum, retain newer local leases when an older peer replays state, reject equivocation, and turn
   transport exceptions into structured reconciliation errors. DNA loading and live operator updates both reject the
   local node ID in the peer registry set, so a quorum that counts only peer responses cannot be configured against
   an impossible topology. A deterministic repeated-partition test proves partial-quorum operation, recovery to a
   newer signed lease, post-recovery equivocation rejection, and preservation of the last valid lease. Multi-partition
   convergence and adversarial state transitions are therefore covered. A mandatory deterministic 500-cycle soak
   rotates partitions across five registries, repeatedly crosses the quorum boundary, renews leases throughout the
   run, proves the newest valid lease survives every failure and rejects post-soak equivocation without corrupting
   retained state. Real-network long-duration soak and mobile transport/resource profiling remain external release
   gates because an in-memory test cannot establish radio, operating-system or battery behaviour.
9. **Implemented; external endurance qualification pending:** deterministic restart, rollback, replay, duplicate and conflict tests exist. Edge
   SQLite exposes seven stable diagnostic transaction boundaries, and the release suite injects failure at each
   boundary to prove rollback of entity/outbox, replicated entity/inbox, acknowledgement, inbound change and
   checkpoint state after reopening the database. The hosted Mongo suite injects failure at six stable transaction
   boundaries against a real three-member replica set, proves that every sync collection remains empty after rollback,
   and then retries the identical operation successfully. The release workflow also proves replay across both a forced
   primary election and abrupt termination of the active primary process; the latter is externally coordinated so the
   failure is a real database-process loss rather than an in-process simulation.
   Persisted Edge upgrade tests also construct the pre-version-protocol SQLite schema directly: completed rows are
   migrated using their durable result version, while pending rows fail closed and roll back the schema alteration
   because an immutable client version cannot be reconstructed safely.
   The release validator defines every dependency assertion before its first invocation. For the `HoloEnabled`
   profile, the release inspector now requires and parses the HoloNET authentication, HoloOASIS Unity lifecycle and
   Holo Edge integration TRX reports (70, 8 and 9 tests respectively); merely generating those reports is no longer
   sufficient. Its minimum baselines also track the current DNA, Core HyperDrive, Edge SQLite and Edge Runtime suites
   (12, 293, 31 and 65),
   preventing removal of the new durability and partition tests from producing a misleading green release.
   These deterministic and real-database gates establish the transactional invariants required by the release.
   Long-duration real-network soak, physical-device suspend/resume/process-death runs and measured mobile resource
   profiles remain external qualification evidence; they do not represent missing transaction or recovery code.

## HyperDrive v2 completion and migration gate

`OASISHyperDrive2` is selected through `OASIS.HyperDriveMode`. Async single Holon load by OASIS ID or provider
key, typed/untyped parent, metadata and all-holon collection queries, async search including deterministic
all-provider and explicit-additional-provider aggregation,
typed/untyped single save, batch save, hard delete by OASIS ID/provider key, and Avatar/AvatarDetail ID,
username and email load/save operations, plus Avatar deletion operations, now execute providers
directly without mutating the process-wide current-provider selection. Routing tests lock the provider key and
all provider-bound recursion/version/child options. Soft delete composes the migrated load and save operations.
The router now covers the remaining active `IOASISStorageProvider` surface as well: provider-key Avatar deletion,
positive/negative karma mutations, bulk import, per-avatar exports by id/username/email, and full export in both
synchronous and asynchronous modes. AvatarManager's live karma entry points select those routes in v2 mode rather
than changing the process-wide current provider.
The shared `OASISManager` constructor follows the same rule: in v2 it registers and activates the manager's
provider without selecting it as the process-wide current provider. Legacy mode retains that global selection
only for compatibility. Activation failures are surfaced immediately rather than being hidden behind a timed
fire-and-forget task. An isolated provider-manager test locks this constructor invariant without mutating the
singleton used by other tests. A second instance claiming an already registered provider type is rejected: allowing
it would subscribe the manager to one instance while HyperDrive routed operations to another.
Injected Avatar, Holon, Search and Wallet manager constructors now pass their runtime registry through the base
composition boundary as well. Supplying a concrete provider to an isolated Edge/Full runtime therefore registers
and activates it only in that runtime; it can no longer leak into `ProviderManager.Instance`. A regression test
uses a non-null provider and proves both registries retain their independent identities.
`FilesManager` is the first composed manager family migrated across that boundary: its upload, download, metadata,
listing and deletion paths use a Holon manager bound to the same injected runtime instead of
`HolonManager.Instance`. Its isolated-runtime upload test proves the selected provider receives the write while the
process singleton remains unchanged.
`HolonManager` search now follows that same boundary. All typed and untyped synchronous/asynchronous search overloads
use a `SearchManager` composed from the Holon manager's DNA and provider registry, forward the requested provider and
child/depth/error/version options, and preserve structured provider failures. Regression tests prove an isolated
runtime receives the search without consulting the process singleton and that an unavailable provider cannot be
reported as an empty successful result.
`ClanManager` now follows the same composition rule for every clan load, list, create, update, delete and inventory
operation. Its Holon and Avatar managers share the supplied runtime registry, and an isolated-runtime load test
proves clan reads cannot escape to the process singleton. Avatar inventory-to-clan operations now reuse that same
runtime-scoped ClanManager (and the originating AvatarManager) instead of `ClanManager.Instance`; an unavailable
injected clan provider remains an error and cannot be replaced by data from the Full Runtime singleton.
`MessagingManager` is also runtime-scoped. Every message is one canonical provider-backed holon indexed for its
sender and recipient; inboxes, sent views and conversations derive from that same entity rather than two
independently committed avatar aggregates. Read receipts update the canonical record, while notifications and
statistics are explicit projections whose failures are surfaced after the message commit. Provider outages cannot
be interpreted as empty inboxes, and rejected sends/read transitions cannot leak through provider-returned object
aliases. Restart, rejected-write and alias-isolation tests cover these invariants. Notification aggregate changes
are likewise cloned and enter the cache only after their durable write succeeds. Settings Holon creation verifies
the provider save and uses the avatar ID supplied to the settings API, so background and Edge operations do not
depend on process-global login state.
`BridgeManager` now builds its blockchain-provider map from the supplied runtime registry as well. An isolated
Ethereum-provider test proves Edge/Full bridge discovery does not inspect or mutate the process singleton. NFT
bridge execution uses an immutable provider map built from that same registry; it no longer jumps back to
`ProviderManager.Instance` after ordinary bridge discovery completed. Unknown provider names and providers absent
from the runtime fail explicitly before any transfer begins. A regression test proves the injected NFT source is
the provider invoked by the operation.
`KeyManager` now composes its Avatar and Wallet managers from that same runtime instead of exposing the global
`AvatarManager.Instance` or `WalletManager.Instance`. AvatarManager likewise supplies itself to a runtime-scoped
WalletManager, breaking the former singleton escape without creating a recursive manager graph. Its per-avatar key
and usage collections are initialized at construction, removing the null-state path in active key statistics.
Synchronous isolated-provider tests prove public- and private-key wallet lookup use only the supplied runtime and
never invoke an asynchronous or process-singleton provider path.
`StatsManager` now composes its Avatar and Holon managers from the supplied runtime and subscribes to cache
invalidation on that runtime's Holon manager. Avatar, settings and aggregate system-stat reads therefore cannot
cross into the process singleton. Configuration and event-subscription exceptions are no longer silently ignored;
an isolated-provider test locks the runtime boundary at the initial avatar read. Avatar search itself now composes
the runtime-scoped Search manager, closing the nested singleton escape used by system statistics. System count
aggregation preserves provider errors instead of converting an outage into authoritative zero-avatar, zero-karma,
zero-gift, zero-message or zero-active-user values; a provider-failure test locks that fail-closed contract. Its
karma, gift, chat, key and leaderboard aggregates likewise read only this runtime's durable settings: a genuinely
missing category receives the documented empty shape, while a failed read remains an error and never consults a
process-global manager. The shared settings loader now distinguishes successful not-found from provider failure;
only the former may create a settings Holon, and a regression test proves an outage cannot trigger an overwrite.
When routing and every configured failover fail, HyperDrive now includes the primary provider's error code and
message in the aggregate failure. The earlier implementation discarded the primary failure before building the
response, leaving clients with only a generic exhaustion message when no secondary provider was configured.
Karma history used by `StatsManager` is read from the runtime's durable karma settings rather than the singleton
Karma manager's process-memory list. Comprehensive avatar statistics now fail as a whole when any required durable
aggregate cannot load instead of returning a successful dashboard populated with null or partial sections.
Avatar sanitization also accepts providers that return no wallet collection (and null wallet entries) while still
clearing every available private key; provider DTO shape differences can no longer crash a stats or profile read.
`KarmaManager` now composes Provider, Avatar and Holon managers from one runtime. Basic add/deduct operations load
the durable total and transaction history, serialize same-avatar mutations through an async lock, persist the new
total and history before updating their memory cache, reject non-positive amounts, and return an error when storage
rejects the commit. History reads reload the durable state rather than trusting the process cache. Regression tests
prove a rejected write neither reports success nor appears in history, and that committed history survives manager
reconstruction (the relevant restart boundary for Full and Edge runtime composition).
`SettingsManager` now composes Avatar and Holon managers from the same runtime registry. Preference reads use the
same category schema written by updates instead of incorrectly reading the parent dictionary, and provider load/save
failures are returned as errors rather than being logged and reported as success. Missing stored preferences and
subscriptions still produce their defined defaults, but an unavailable or rejecting provider is never mistaken for
missing data. Aggregate settings reads fail when any constituent read fails, so callers cannot receive a partially
defaulted object presented as authoritative. The v2 `SaveAvatarAsync` path was
also corrected to use asynchronous load and routing contracts end to end; tests forbid synchronous provider calls
from this path and prove rejected settings reads and writes remain rejected operations.
`ProviderManager.ResolveStorageProvider[Async]` is the canonical escape hatch for provider-specific extension
interfaces: it resolves explicit/default providers and activates them without changing process-global selection,
and rejects `ProviderType.All` because a single-provider extension cannot safely imply fan-out. WalletManager now
uses this path for v2 local wallet reads and writes. Its former forced-`LocalFileOASIS` synchronous path is removed;
the legacy overload walks configured failover providers and restores its original current provider, while v2 uses
the configured default without global mutation. The synchronous wallet timeout now uses its configured seconds
value exactly once, and non-local wallet saves persist public wallet data on the avatar instead of silently doing
nothing. Focused tests prove explicit/default resolution and v2 SQLite-style wallet load/save leave the current
provider unchanged. Wallet key import/generation now resolves a lazy KeyManager bound to the same Avatar, Wallet and
Provider managers, breaking the former Wallet-to-Key singleton path without a recursive constructor graph. Cross-chain
wallet operations likewise use a lazy BridgeManager built from the runtime's provider registry rather than
`BridgeManager.Instance`; an isolated wallet-import test proves provider failures originate from the injected runtime.
The username/email wallet entry points now preserve asynchronous execution through both avatar identity resolution
and the local wallet-provider call; they no longer enter the synchronous ID overload after an awaited lookup. The
asynchronous save timeout path also awaits the completed provider task instead of consuming `Task.Result`. Focused
tests cover username and email loads and saves and forbid the corresponding synchronous local-provider methods.
The Free subscription provider policy now classifies the two zero-cost Edge stores, `SQLLiteDBOASIS` and
`LocalFileOASIS`, as permitted providers. This closes a real automatic-routing failure where an otherwise healthy
offline Edge runtime rejected its current local store before any provider call; both providers have direct routing
coverage.
ProviderManager load-balancing strategy and performance weights now come from that manager's injected OASISDNA rather
than the process-global HyperDrive configuration singleton. A deterministic test proves `Auto` honors the isolated
runtime's configured round-robin strategy.
Performance observations are runtime-scoped as well: every `ProviderManager` owns its `PerformanceMonitor`, and its
HyperDrive router, analytics, predictive failover and load-balancing components consume that same monitor. Full ONODE
administration, GraphQL, gRPC and ONET capability advertisement resolve the monitor from the owning provider manager
instead of the process-global singleton. A regression test records metrics in one isolated runtime and proves they do
not affect provider metrics in another runtime.
AvatarManager's provider-specific AvatarDetail save and Avatar delete-by-id/username/email helpers now use the
same non-mutating resolution path in v2. Their provider calls treat a null `OASISResult` as an explicit warning
instead of dereferencing it, and tests prove both requested-provider routing and null-result handling without
changing the current provider.
AvatarManager, HolonManager and SearchManager now construct their v2 router with the same `ProviderManager`
instance supplied to the manager, and read HyperDrive mode from that manager's DNA. They no longer instantiate a
router bound implicitly to `ProviderManager.Instance` or decide mode from stale global DNA. This is required for
an Edge runtime and a full runtime to coexist safely in one process. Dedicated tests exercise synchronous Avatar,
Holon and Search entry points against isolated provider registries and prove that the requested provider executes
without replacing the registry's current provider.
Legacy Avatar karma operations and Avatar/AvatarDetail auto-replication now use that same injected registry rather
than `ProviderManager.Instance`, so an Edge runtime cannot activate or enumerate providers in a co-hosted full
runtime. The AvatarDetail background-replication branch also dispatches the AvatarDetail worker contract (it
previously started the Avatar worker with incompatible parameters and therefore performed no replication).
Regression tests cover both runtime isolation and a real AvatarDetail replication write.
AvatarDetail provider saves also perform custom-property projection through a HolonManager bound to the same
runtime, removing the final singleton dependency from that replication write path.
Egg discovery and hatching now persist one JSON collection through the injected runtime's durable settings
aggregate. The former implementation created a new unrelated holon on every save, read through a string key that
was never written, kept hatch state only in memory and swallowed provider failures. Reads now distinguish an empty
collection from an unavailable provider, rejected writes cannot be reported as successful discoveries, and
restart tests prove both discovered and hatched state are recovered.
Competition leaderboards are now season-period-scoped durable aggregates rather than process-only dictionaries.
Score mutation loads and commits under a per-board async lock, rank reads reload provider state, rejected writes do
not leak through provider-cached object aliases, and Egg discovery projects its score into all five seasonal boards
with explicit warnings for partial projection failure. Tournament reads and enrollment no longer fabricate a new
sample tournament or return unconditional success: the catalog is provider-backed, tournament creation/update has
a validated save API, enrollment reloads under a per-tournament lock, enforces registration dates/status/capacity,
is idempotent, and commits the participant list before reporting success. Restart and rejected-write tests lock
these leaderboard and tournament invariants in.
Gifts are now stored as one canonical provider-backed holon per gift rather than duplicated process-memory sender
and recipient collections. Recipient lists, sender/recipient history and statistics are derived from those records;
receive/open transitions update the same durable entity, are idempotent, and never report success after a rejected
write. Projection failures are surfaced as warnings after the canonical commit. Metadata lookup internals now use
the HolonManager's injected provider registry instead of the process singleton, which keeps gift queries and every
other metadata query inside its Edge or Full runtime boundary. Restart and rejected-write tests cover the full gift
lifecycle and derived transaction history.
The ONODE ONET unit-test project directly references the split Contracts, HyperDrive Synchronization and Core
projects whose public types its request/response host tests compile against. This makes the release gate independent
of accidental transitive-reference behavior; the nine authenticated correlation, error propagation, provider
capability and hosted-sync transport tests now build and pass from the repository graph.
The hosted WebAPI authentication gate also compiles against the split result contract and explicit text-encoding
namespace; its twelve JWT middleware and signed offline-session-grant tests pass. Invalid bearer tokens remain
diagnostic middleware state rather than a premature response mutation, while endpoint authorization retains the
final allow/deny decision.
The STAR CLI executable and reusable CLI library now reference the split Contracts assembly directly. This is
required because both expose `OASISResult<T>` in their own compiled surfaces and cannot rely on transitive type
forwarding through Core or STAR. Release builds now preserve the existing full Native Integrated Endpoint and STAR
ODK/CLI products alongside the additive Edge runtime.
Unity package validation now synchronously imports its generated Android smoke-test scene and resolves the
canonical asset path before invoking `BuildPipeline.BuildPlayer`. Previously the validator raced Unity's asset
database and failed with an "incorrect path for a scene file" error despite using a project-relative path. A fresh
UPM archive now passes managed compilation, secure-session storage validation and the Android player build.
The manifested package also synchronizes into the real Our World/OASIS Hub project and passes a clean Unity batch
compilation. Release smoke packaging produces exactly the seven intended NuGets: Contracts, HyperDrive
Synchronization, ONET, Edge Runtime, Edge ONET Runtime, Edge Native Integrated Endpoint and HoloOASIS Unity.
The HoloOASIS hApp remains a hard provenance gate: release validation requires a clean sibling
`OASIS-Holochain-hApp` checkout, its pinned Nix toolchain, passing npm tests/build, and a generated manifest binding
the bundled `oasis.happ` to source commit, source digest and artifact digest. A binary without that evidence is not
accepted as a release artifact.
The Edge ONET composition root now owns the complete authenticated lifecycle: peer binding, signed capability
publication and renewal, capability-based full-node discovery, connectivity monitoring, bounded synchronization,
hosted-service recovery, suspend/resume and ordered asynchronous disposal. An initial hosted sync outage leaves the
runtime explicitly offline but started, retaining local operation and capability renewal while its recovery loop
waits for the service to return. Recovery-loop cancellation is awaited before suspend, monitor detachment, restart
or database disposal, and ONET capability renewal is awaited before the endpoint is closed. Lifecycle tests cover
offline start, automatic recovery, rapid suspend/resume behavior and disposal during in-flight work.
Legacy Avatar and AvatarDetail auto-replication no longer starts untracked background `Thread` workers. Legacy
operations now finish their configured replication set before returning, including when the compatibility
`waitForAutoReplicationResult` argument is false, so provider failures remain observable and process shutdown cannot
discard acknowledged work. HyperDrive v2 remains the non-blocking path because its asynchronous behavior is backed
by the transactional local outbox rather than process memory. A regression test proves AvatarDetail replication has
completed on return and stays inside the injected Full/Edge runtime registry.
Holon legacy single/batch save, failover, replication, load-balancing and provider-restoration paths now obtain
their policy and providers from the HolonManager's injected registry as well. The private provider execution and
replication helpers no longer activate or restore providers through `ProviderManager.Instance`. A dedicated legacy
replication test proves both the primary and replica in an isolated runtime execute while the process singleton is
untouched; this preserves Full Runtime behavior without allowing a co-hosted Edge runtime to cross registries.
Avatar JWT issuance, password policy, refresh-token retention and Holon metadata encryption now also read the
manager's runtime DNA. An isolated runtime signs only with its explicitly configured key and fails closed when that
key is absent; it can no longer copy the singleton DNA/key into itself or mutate the singleton provider manager.
Tests lock both isolated-key selection and missing-key failure.
HyperDrive quota configuration and its analytics/failure-prediction provider inventory are likewise read from the
router's injected provider manager. The old persisted counter is node-local and has no authenticated subscription
identity, so it is now disabled unless `SubscriptionConfig.EnforceLocalQuota` is explicitly enabled. Hosted APIs
must use their authoritative subscription service; they can no longer exhaust one process-global allowance and
then reject every avatar and isolated Edge runtime. An explicit zero-request local-quota test locks the opt-in path.
The provider contract now includes AvatarDetail deletion by id, username and email. Its portable default performs
a real soft delete by loading the detail, setting its audit tombstone state and saving it; hard deletion fails
explicitly unless a provider implements native hard-delete semantics. AvatarManager and both v2 routers expose all
six synchronous/asynchronous deletion forms without changing the selected global provider, with routing and
contract-behaviour tests.
The synchronous router now has direct provider execution and coverage proving it neither calls async provider
methods nor changes global provider state. Holon ID/provider-key/parent/metadata/load-all queries, single/batch
save and ID delete have migrated to it. Synchronous Avatar/AvatarDetail ID, username, email and collection
loads, saves, and Avatar deletes have also migrated. Synchronous single-provider and deterministic aggregated
Search and provider-key Holon delete have migrated as well. Other manager families still require migration and
compatibility tests. V2 therefore remains an
unfinished opt-in implementation rather than a tested replacement for legacy HyperDrive.

The Core regression suite passes 233/233 after the wallet async-boundary and local Free-plan corrections. This is
host-side coverage of the manager and routing invariants; it does not replace the physical Android/iOS qualification
matrix described below.

Every active `ProviderManager` load-balancing decision now publishes a structured
`ProviderSelectionDiagnostic`. It records the requested and effective strategies, selected provider, deterministic
candidate ordering, and the observed latency, reliability, connection, geographic-latency and cost inputs without
inventing observations for providers that have no metrics. The hosted HyperDrive status response exposes the most
recent decision from that same runtime instance. Focused tests protect both decision/diagnostic agreement and the
explicit reason returned when load balancing is disabled.

The `SqliteMvp` release gate incorporating this diagnostic contract passes 454/454 tests, including Core 222/222,
the retained real MongoDB replica-set suite 37/37 and abrupt-primary recovery 1/1. Its acceptance report is
`artifacts/current-goal-provider-selection-release/acceptance-report.json`. The Edge package is byte-identical to
the preceding inventory-statistics release because these changes belong to Full Core and hosted ONODE, not the
lightweight Unity dependency graph; its SHA-256 remains
`807F085B51230EFE07DC2D4F4C5BF79AE69B5AE3FD115AEDFE83016F15CC74A2`.

Automatic and explicit v2 failover now emit `HyperDriveFailoverDiagnostic` evidence from the execution path itself.
The record preserves the primary failure, every ordered provider attempt, error codes/messages, quota blocking,
exhaustion and the provider that recovered the request. Explicit failover candidates are attempted exactly as
configured and do not acquire a second predictive override inside an already-active failover sequence. The same
diagnostic is attached as JSON to the returned `OASISResult.MetaData`, retained on the injected `ProviderManager`,
and exposed by hosted ONODE's HyperDrive status response. This makes request-level evidence available even though
managers construct a short-lived `OASISHyperDrive` router. Focused tests cover recovery and both automatic and
explicit quota-denial paths; the complete Core suite passes 223/223.

The aggregate `SqliteMvp` release gate containing both provider-selection and failover diagnostics passes 455/455
tests. Its machine-readable evidence is
`artifacts/current-goal-failover-diagnostics-release/acceptance-report.json`. The Unity package remains
byte-identical because the diagnostic implementation is confined to Full Core and hosted ONODE; its SHA-256 is
`807F085B51230EFE07DC2D4F4C5BF79AE69B5AE3FD115AEDFE83016F15CC74A2`.

V2 replication now publishes the same level of structured evidence. `HyperDriveReplicationDiagnostic` records the
primary provider, explicit versus automatic execution, configured attempt order, every provider result and error,
success/failure totals, quota denial, and whether the mutation was deliberately handed to the durable hosted
pipeline. The diagnostic is retained by the injected runtime, attached to `OASISResult.MetaData`, and exposed by
hosted ONODE status. A disabled replication policy emits no diagnostic, preventing “disabled” from being confused
with an empty or successful replication run. Focused coverage includes ordered success, partial failure without
invalidating the committed primary mutation, explicit quota denial, disabled policy and durable-hosted deferral;
the complete Core suite passes 224/224.

The corresponding aggregate `SqliteMvp` gate passes 456/456 tests. Evidence is retained at
`artifacts/current-goal-replication-diagnostics-release/acceptance-report.json`; the package SHA-256 remains
`807F085B51230EFE07DC2D4F4C5BF79AE69B5AE3FD115AEDFE83016F15CC74A2` because the Unity dependency graph did not
change.

Edge conflict observability is also structured. `EdgeRuntimeStatus` now reports the durable unresolved-conflict
count and the latest conflict's operation/entity identities, local/server versions, code, message and timestamp.
Payload JSON is deliberately excluded from this status surface. Synchronization updates the status from the same
SQLite conflict transaction it uses to decide whether work remains pending; direct conflict reads and successful
resolution refresh it as well. The existing divergence/manual-merge acceptance test now proves the status moves
from one identified conflict to zero when the durable resolution commits. The complete Edge Runtime suite remains
green at 59/59, and the Edge Native Integrated Endpoint compiles with the expanded status contract.
The aggregate `SqliteMvp` release gate passes 456/456 with Unity Editor, Android-player and real Our World
integration compilation. Evidence is
`artifacts/current-goal-conflict-diagnostics-release/acceptance-report.json`; the expanded Edge package SHA-256 is
`B4A8F94316CF79986480D5CBEF05896E08B653D666A6E0A9E07462F6B097D3C7`.

Use `Scripts/set_hyperdrive_mode.ps1 -Mode OASISHyperDrive2 -Path <OASIS_DNA.json>` to opt a deployment into
v2. The command validates the JSON, requires exactly one mode property, preserves the rest of the file text,
creates a mode-labelled backup by default, and verifies the value after writing. Use `-WhatIf` for a dry run.
Unknown mode strings are rejected both by this tool and by `OASISDNAManager` at runtime.

The v2 completion work must:

1. inventory every legacy failover, replication and load-balancing entry point;
2. route all supported managers through one shared v2 execution pipeline;
3. replace random or placeholder decisions with observable health, latency, policy and deterministic tie-breaking;
4. integrate durable synchronization below manager-specific APIs;
5. expose structured diagnostics explaining every provider selection, failover and conflict;
6. keep legacy mode only during a measured compatibility period and remove it after the migration gate passes.

V2 becomes the default only when all of the following test suites pass:

| Suite | Required evidence |
|---|---|
| Provider selection | deterministic priority, health, latency, cost and tie-breaking tests |
| Failover | primary failure, partial failure, all-provider failure, recovery and switch-back policy |
| Replication | success, partial acknowledgement, duplicate delivery and provider recovery |
| Durable sync | offline write/delete, restart, reconnect, replay, pull and checkpoint recovery |
| Conflicts | stale base version, concurrent devices, tombstone/update race and domain-command conflict |
| Idempotency | repeated operation and repeated batch never duplicate effects |
| Crash safety | termination at every local/server transaction boundary |
| Concurrency | simultaneous sync, save, login and provider-health transitions |
| Mobile lifecycle | suspend/resume, connectivity changes, bounded batches and cancellation |
| Compatibility | v1/v2 result and persisted-state comparison for every supported manager operation |
| Full ONODE | hosted provider fan-out and failed-provider recovery |
| Edge Runtime | Unity-compatible build plus device-to-hosted-ONODE end-to-end tests |
| ONET transport | the same protocol/semantics over ONET once HTTPS is proven |

No test may depend on random provider choice, wall-clock sleeps or a live external service unless it is explicitly classified as an opt-in live integration test. Deterministic fakes and an injectable clock/network state are required for the normal CI suite.

## Acceptance criteria

- A device can create/update/delete supported entities offline, restart, and retain both state and pending operations.
- Reconnecting eventually produces the same hosted state without duplicate effects.
- Killing either side at every transaction boundary cannot lose an acknowledged operation or apply a command twice.
- Conflicts are visible and deterministic.
- REST-only mode remains lightweight and online-only.
- Edge mode includes only selected providers and stays within measured mobile CPU, memory, storage and battery budgets.
- Hosted ONODE continues to use the same Core/HyperDrive managers and may replicate accepted changes to its full provider set.

## Automated release gate

The gate requires dedicated `hosted-command-executor.trx` and `web5-geohotspot-authority.trx` evidence in addition
to the general Edge and hosted-sync suites. This prevents a package from passing merely because trigger evidence can
be queued locally: malformed evidence must fail before provider access, and WEB5 REST eligibility/trigger handling
must remain bound to the same durable ONODE Core authority used by the hosted command worker.

Run `Scripts/validate_edge_runtime_release.ps1` from the repository root. It runs the deterministic Core,
DNA-mode validation, Edge SQLite, Edge Runtime/Edge ONET and ONET protocol suites serially; builds MongoDBOASIS and the Full ONODE WebAPI;
builds the `netstandard2.1` HoloNET/HoloOASIS Unity dependency chain; rejects ASP.NET/MongoDB dependencies in
Edge projects; builds the existing Full Native Integrated Endpoint and STAR CLI as non-regression gates; and
packs lightweight OASIS contracts, portable HyperDrive synchronization, shared ONET, Edge Runtime, Edge ONET Runtime,
Edge Native Integrated Endpoint and HoloOASIS.Unity. It also creates the versioned Unity package with desktop,
Android and iOS SQLite assets and compiles that package using Unity 2022.3 before accepting it. On Windows the
Unity acceptance method also proves an OS-protected offline-session save/load/delete round trip. The matching
`edge-runtime-validation.yml` workflow runs for relevant pull requests and pushes and publishes the packages
as CI artifacts. The gate also emits `SHA256SUMS.txt`, an SPDX 2.3 dependency SBOM, an exported-public-API
comparison between the Full and Edge Native Endpoint assemblies, and a commit/configuration acceptance report
containing both endpoint assembly hashes plus the Unity archive, build-manifest and Unity compilation-log hashes.
The same gate clean-publishes both OGEngineClient NativeAOT deployment profiles on Windows, compiles and runs the
C++ ABI smoke probe against each, requires the generated export contract, rejects unsafe trim/dynamic-code call-site
warnings, and packages `Edge` and `RemoteOnly` archives. The inspector verifies each archived DLL against its native
report and enforces that SQLite exists only in the Edge profile. These archives, reports and hashes are part of the
acceptance report and `SHA256SUMS.txt`; a managed-only green build cannot certify a native-game release.
Nineteen SQLite-profile and twenty-two Holo-profile TRX reports (DNA, Core HyperDrive, Edge store, Edge runtime, ONET synchronization, ONET peer binding,
HyperDrive persistence, HyperDrive AI optimization, HyperDrive predictive failover, hosted sync API, hosted offline-session grant security,
quest/reward idempotency, HyperDrive migration, hosted command execution, WEB5 GeoHotSpot authority,
hosted Mongo transaction rollback/retry, abrupt Mongo primary loss, HoloNET authentication,
HoloOASIS Unity and HoloOASIS Edge) are parsed by the inspector for the Holo-enabled profile; the three Holo-specific
reports are omitted from the SQLite-only profile;
the Mongo evidence is produced by a separate Linux replica-set job and copied into the release evidence rather than
substituting a mocked store. Missing, empty, failed, errored, timed-out or aborted reports fail the gate,
and their counts and hashes are embedded in the acceptance report and checksum set. Per-suite minimum executed-test
baselines prevent a filter or accidental test removal from producing a misleading green release. Passing this gate is necessary but does not replace device profiling, extended replica-set election/partition soak tests, IL2CPP/AOT tests, or the full compatibility matrix.

The CI packaging job verifies that the OASIS checkout is clean before it downloads cross-job evidence or starts
the release build. On trusted pushes and manual runs, GitHub OIDC and Sigstore then sign one SLSA provenance
attestation covering every file produced in `artifacts/edge-release-validation`. The serialized Sigstore bundle is
preserved as `github-slsa-provenance.sigstore.json` in the uploaded evidence artifact, while GitHub stores the
corresponding attestation against the repository. Verify a downloaded release subject with
`gh attestation verify <file> --repo NextGenSoftwareUK/OASIS`. Pull-request runs deliberately cannot mint release
provenance: they execute the same validation gate, but only a trusted push or explicitly dispatched workflow can
produce the signed attestation.

Physical qualification uses the separate, strict
`Scripts/our-world-device-acceptance-evidence.template.json` contract. The validator requires two Android devices,
one iOS device, 13 named lifecycle/convergence cases, hashes every raw evidence file, and checks recorded frame,
p95 CPU, memory, database-growth, reconnect, synchronization-drain, battery and network measurements against the explicit budgets supplied for the
run. Supplying `-PhysicalDeviceEvidence` packages those records into the release evidence; adding
`-RequirePhysicalDeviceEvidence` makes their absence fatal. This provides a reproducible gate without treating an
emulator, an Android build, or an unverified `PASS` label as physical-device certification.
The aggregate release gate also runs the validator's deterministic positive, tamper, budget and completeness
contract tests before building release artifacts.

The iOS/tvOS secure-session bridge preserves the same durable-session invariant as the other platforms. Keychain
saves use `SecItemUpdate`, adding only when the credential does not yet exist; they never delete an acknowledged
grant before replacing it. Native loads return an OSStatus separately from the allocated credential value, so Unity
treats only `errSecItemNotFound` as an empty session and reports every other Keychain failure through
`OASISResult`. Package validation locks both sides of that native/managed ABI and rejects the former destructive-save
or error-as-not-found contracts.

The gate also requires the sibling `OASIS-Holochain-hApp` repository and verifies that the packaged `oasis.happ`
and its build manifest cryptographically match the current Rust/TypeScript hApp source tree. Rebuild and run the
Tryorama suite with `Scripts/build_holooasis_happ.ps1`; that command uses the pinned Nix environment, copies the
tested artifact into HoloOASIS, and writes its provenance manifest. The hApp source must be clean and the manifest
records its exact Git commit as well as source-tree and artifact hashes. CI builds it on Ubuntu with Nix before the
Windows packaging job, which consumes only that verified artifact and manifest. A missing toolchain, manifest, commit/source mismatch,
or changed binary now fails the release before any NuGet is packed. This deliberately prevents a C#-green build
from publishing an old zome contract.
## HoloOASIS Holochain 0.7 reference implementation

The canonical mobile/edge hApp is the sibling repository `OASIS-Holochain-hApp`. It uses HDK 0.7/HDI 0.8 and exposes the wire functions consumed by `HoloOASIS` and HoloNET:

- Avatar, AvatarDetail and Holon CRUD plus ID, username, email, parent, provider-key, custom-key, metadata and all-record indexes.
- Immutable `HyperDriveMutation` entries indexed by operation ID and logical entity. Replaying an operation returns its existing record.
- Forward-compatible flattened domain fields so additive OASIS model changes can be preserved without an immediate DNA migration.

The public-DHT boundary is explicit: passwords, JWTs, refresh/reset tokens and verification tokens are omitted by the C# provider and rejected by DNA validation. HoloOASIS is a replicated data provider, not an authentication-secret vault.

The release evidence is layered: Rust invariant tests; Holochain 0.7 Sweettest tests that load the packed DNA into a real conductor; and builds of both HoloOASIS and HoloOASIS.Unity against HoloNET. Tryorama 0.19 targets Holochain 0.6 and is not a valid 0.7 release gate.

Do not invoke `nix develop path:.` from a tree containing `target` or `node_modules`: a path flake snapshots those directories into `/nix/store`. Automation must evaluate a clean Git source or a flake-only environment and then execute Cargo in the working tree.

## Our World durable read ownership

When Edge is enabled, Our World renders inventory from the private avatar-detail projection, quests from Quest
projections, and playable GeoNFTs from the synchronized GeoNFT entity set. `AvatarDetail.Inventory` is the canonical
avatar-scoped inventory view; it includes portable category names and the display/lifecycle fields required by a
Unity client. GeoNFT synchronization preserves both WEB4 playable records and WEB5 provenance wrappers, which the
client merges locally by the wrapped WEB4 id. These reads fail visibly if their projection is missing or malformed.
They do not make an opportunistic REST request, because that would make behavior depend on connectivity and conceal
an incomplete synchronization snapshot. REST remains a separate, explicitly selected remote-only profile.

GeoNFT collection availability is a separate private projection keyed by the avatar id. Before a normal hosted
pull, the authoritative Mongo provider evaluates every active WEB4 GeoNFT against the complete avatar collection
history using the same `GeoNFTCollectionPolicy` as the REST API. The content-addressed projection is emitted only
when status changes and is visible only to that avatar. Edge may advance a cooldown after its authoritative UTC
expiry, because `NextCollectAtUtc` is populated only when cooldown is the sole blocker; it never guesses away
global/player limits or exclusive ownership. A missing avatar or malformed history fails synchronization visibly.
The projection is server-authoritative, so a direct Edge mutation is rejected. When the final active GeoNFT is
removed, the provider atomically emits a private delete tombstone rather than leaving stale eligibility on the
device. The actual collection command is still revalidated transactionally by the hosted authority after reconnect.

The 2026-10-03 GeoNFT availability release evidence passes the complete `SqliteMvp` aggregate gate at 448/448:
Core 217/217, hosted Mongo replica-set transactions 36/36, abrupt-primary recovery 1/1, and all existing DNA, Edge
SQLite, Edge Runtime, ONET, hosted API, migration, security, AI, failover and OGEngineClient suites. Unity Editor,
Android-player and Our World integration compilation also pass. The authoritative report is
`artifacts/current-goal-geonft-availability-release/acceptance-report.json`.

Inventory use and removal are stable-ID avatar-gameplay commands, not direct API calls. A transfer is also an
avatar-gameplay command, but its hosted application is deliberately provider-transactional: Mongo removes the whole
stable-identity stack from the sender, inserts it for the recipient, advances both avatar versions and writes the
same immutable operation receipt to both avatar details in one transaction. This prevents the legacy two-save loss
window and makes reconnect replay exactly-once. The local pending projection removes the sender's item immediately;
it never invents the recipient's private projection.

Inventory updates are full-state, stable-ID avatar-gameplay commands. The Edge pending view applies the same
validation as the hosted provider, and Mongo persists the updated supported item fields and immutable receipt in one
transaction. This prevents an online-only PUT from bypassing the durable journal and makes duplicate delivery a
no-op. Inventory creation and higher-level trading semantics remain separate authority contracts rather than being
silently approximated by update or transfer.

## Current aggregate release evidence (2026-10-03)

The current `SqliteMvp` release gate passes 488/488 tests across 18 suites with zero failures. Its authoritative report
is `artifacts/current-goal-clan-state-sqlite-release/acceptance-report.json` (SHA-256
`DEF4E925FED4720B97C6E9B77FF22DBCD1CA7F27C97896483A1589F93CC64D69`). The corresponding `HoloEnabled` gate
passes 581/581 across 21 suites with zero failures; its report is
`artifacts/current-goal-clan-state-holo-release/acceptance-report.json` (SHA-256
`180D9233885D1F533330290CA0A4AED2DF72FB5EC5E2FFE655E3E56E4AEE4196`). Both reports include the 250-test Core
suite, durable hosted Mongo synchronization and abrupt-primary recovery, the two OGEngine NativeAOT profiles with
113 required exports and compiled C++ smoke tests, deterministic Unity package construction, Unity editor and
Android-player compilation, and real Our World package integration compilation. The Holo-enabled report additionally
binds the hApp manifest to source commit `c0eb1603e1855a933e75961c038e5f41fd4a5007`.

These host-side gates do not claim physical-device certification. Two Android devices, one iOS device and the
required lifecycle, convergence and resource-budget measurements remain governed by the strict physical acceptance
evidence contract described above.

The next manager-migration increment preserves an explicitly supplied provider through the child-manager
composition used by Files, Settings and Messaging. In a multi-provider v2 runtime those operations can no longer
silently re-resolve `ProviderType.Default` to a different provider. Files also propagates provider list failures
instead of presenting an outage as an empty file collection, and metadata updates use detached Holon snapshots so
a rejected save cannot mutate a provider-owned cached object by alias. Bridge audit composition now preserves the
same explicit provider and fails construction visibly instead of retaining a partially initialized manager. Clan
writes now carry their explicit owner audit identity instead of consulting the process-global logged-in avatar, and
membership changes use detached Clan snapshots so a rejected save cannot leak through a provider object alias. The
KeyManager's internally composed AvatarManager also retains the explicitly selected provider for default key
lookups. WalletManager now preserves that same provider scope for its internally composed AvatarManager, preventing
default identity lookup from escaping to the runtime's general provider selection before a wallet operation. The
complete Core suite passes 242/242. That exact suite is embedded in both current aggregate acceptance reports above,
so the provider-scope invariants are covered by the packaged SQLite and Holo release provenance.

Clan inventory transfer now clones the loaded AvatarDetail collection before removing items and rejects a requested
quantity unless every item is present before persistence. Rejected sender writes therefore cannot mutate a
provider-owned object alias, and partial quantities cannot be committed accidentally. Two focused regressions pass
and the complete Core suite passes 244/244 at
`artifacts/current-goal-clan-inventory-invariants/core-clan-inventory-full.trx`. This legacy manager path is retained
for compatibility, but is not used by the offline-capable OGEngine contract. OGEngineClient now emits the canonical
`TransferInventoryToClan` command with client-stable operation and destination-item identities. Edge validates and
journals it locally; hosted Mongo applies the AvatarDetail decrement, canonical `OASIS.Clan.State.v1` treasury
addition and both receipts in one transaction. Replay is idempotent, and asymmetric receipts fail visibly. The new
`POST api/avatar/inventory/send-to-clan-atomic` endpoint requires both identities; the legacy endpoint remains
separate so it cannot pretend a server-generated ID makes an old client's retry idempotent.

Encrypted Holon metadata loads now fail closed in every synchronous/asynchronous ID and provider-key path for both
legacy routing and HyperDrive v2. Decryption returns a structured result rather than swallowing every exception;
invalid ciphertext or missing configuration clears the returned entity and exposes a specific error code, while
valid ciphertext still restores its metadata. The focused four-case legacy/v2 matrix passes, and the complete Core
suite passes 248/248 at `artifacts/current-goal-holon-decryption-failclosed/core-current-full.trx`. The current SQLite
and Holo aggregate reports above include this exact regression matrix and package it with the complete release gates.

Clan persistence no longer assumes that a storage provider retains runtime-derived CLR fields. The manager encodes
owner identity, membership and the complete shared inventory into the versioned `OASIS.Clan.State.v1` metadata
contract before the base Holon crosses the provider boundary, then hydrates that state on every Clan load path.
Missing or malformed state on a provider-returned base Holon is a structured `CLAN_STATE_INVALID` failure, not an
empty treasury or membership list. The base-Holon round-trip and fail-closed cases are included in the seven-test
focused result at `artifacts/current-goal-clan-portable-persistence/core-clan-persistence.trx`; the complete Core suite
passes 250/250 at `artifacts/current-goal-clan-portable-persistence/core-current-full.trx`. Both cases are included in
the current SQLite and Holo aggregate package reports above.

Atomic Clan transfer evidence is retained in
`artifacts/current-goal-clan-atomic-mongo/hosted-mongo-sync.trx` (38/38 real replica-set integration),
`artifacts/current-goal-clan-atomic-mongo/hosted-mongo-process-kill.trx`, and the focused client/API reports under
`artifacts/current-goal-clan-atomic-client/`. The OGEngine lifecycle case proves an offline command is reflected in
the pending inventory, survives client restart, synchronizes the same source/clan/destination identities and drains
without applying the quantity twice. The aggregate release evidence immediately below incorporates and supersedes
these focused reports for package-promotion provenance.

## Atomic gameplay receipt hardening and current release proof (2026-10-04)

Hosted Mongo now applies the same symmetric receipt invariant to avatar-to-avatar inventory transfer that the Clan
path uses: both authoritative aggregates must contain the same operation identity and canonical payload, or neither
may contain it. Asymmetric state is a structured rejection and is never heuristically repaired. Avatar receipt
metadata is parsed at the provider boundary; malformed or null ledgers return
`AVATAR_GAMEPLAY_RECEIPT_INVALID` through `OASISResult<HyperDriveAvatarDetailProjection>` rather than throwing past
the hosted executor. Transaction coverage also proves that a colliding Clan destination-item identity commits
neither the source decrement nor a receipt. The current real three-member MongoDB 7 suite passes 41/41, plus the
separately coordinated abrupt-primary termination/election case, under
`artifacts/current-goal-clan-atomic-mongo-v2/`.

The OGEngine offline GeoHotSpot read boundary now requests opaque persisted JSON as `JsonElement`, which is directly
supported by the generated AOT-safe Edge serializer. Newtonsoft `JObject` construction remains inside OGEngine after
that boundary and is not part of the Unity-facing Edge serialization contract. The complete managed OGEngine suite
passes 68/68 runnable tests at `artifacts/current-goal-full-regression/ogengine-client-full.trx`; the Edge-enabled and
remote-only builds both compile from the same source tree.

Fresh aggregate evidence supersedes the earlier package reports for this increment:

- `SqliteMvp`: 495/495 across 18 suites at
  `artifacts/current-goal-clan-atomic-v2-sqlite-release/acceptance-report.json`, report SHA-256
  `73B4E9948692F98E4DD048B56F0E8755B14731AC9DE71B12BE22572E6FCA979A`, deterministic Unity package SHA-256
  `6B146D26D513C6EAB1F15D23ECA6C5C082BD1AFB50333A216DEB833D5A1FB898`.
- `HoloEnabled`: 588/588 across 21 suites at
  `artifacts/current-goal-clan-atomic-v2-holo-release/acceptance-report.json`, report SHA-256
  `DCBBD116F6C809AEEF08EB9321DDA1B2083041F9100DD80FCBFCC368107B774A`, deterministic Unity package SHA-256
  `27EE6D0F5535F14ECD9ED907B49E3588B096830C6A28F8A341904A29F45DDA68`, with hApp provenance bound to source
  commit `c0eb1603e1855a933e75961c038e5f41fd4a5007`.

Both aggregates retain Mongo transaction/process-loss evidence, API compatibility, SBOM, hashes, Unity Editor and
Android-player compilation, real Our World integration compilation, and 113-export NativeAOT Edge and RemoteOnly
smoke-tested artifacts. They remain host-side release proof; completed physical Android/iOS acceptance evidence is
still required before claiming device certification.

## Durable hosted-authoritative Karma commands (2026-10-04)

Karma add/deduct now uses the existing `oasis.avatar-gameplay.v1` command stream. Its portable payload carries the
stable Karma/source enum names, claimed amount, audit description and UTC occurrence time without introducing a
dependency from OGEngineClient or the Unity package to the full Core runtime. The local deterministic reducer gives
the player an immediate pending total and Edge-safe history entry. Hosted Core remains authoritative: Mongo parses
the named enums and recomputes the amount through the portable `HyperDriveKarmaPolicy`. An exhaustive Core parity
test locks every positive and negative policy entry to `KarmaManager`'s existing public weighting. A mismatch, undefined
type, insufficient balance, malformed receipt or reused operation identity is a structured rejection rather than a
fallback write.

On acceptance, total, Akashic history and the canonical payload receipt are persisted in the same Mongo transaction;
replay returns the already committed projection without adding Karma twice. OGEngineClient exposes the same managed
API for Edge-enabled and remote-only profiles, routes Edge-enabled calls through the journal, updates its HUD cache,
and uses the existing authenticated WEB4 Karma endpoint when offline support is disabled. The real replica-set suite
passes 42/42 plus abrupt-primary termination evidence under `artifacts/current-goal-karma-mongo-v2/`; the focused
portable reducer and OGEngine lifecycle suites pass 12/12 and 7/7 respectively. The post-change aggregate evidence
passes 508/508 for `SqliteMvp` and 601/601 for `HoloEnabled`, at
`artifacts/current-goal-karma-v2-sqlite-release/acceptance-report.json` and
`artifacts/current-goal-karma-v2-holo-release/acceptance-report.json`. Their report SHA-256 values are respectively
`DC9E102F83B12111CBB06CCE453F3ED941B57C83DDB6A123A8A342F21DD730D9` and
`8B89687F4255939D9CD14F39EC18B854C57D70703B1F463AE9D86B6A1F516C48`; both include NativeAOT Edge/RemoteOnly,
reproducible Unity packaging, Editor and Android compilation, real Our World integration and retained Mongo
transaction/process-loss evidence.

Karma remains non-transferable reputation. The old `KarmaManager.TransferKarmaAsync` implementation performed two
independent mutations followed by a compensating write, which was neither atomic nor consistent with WEB4's explicit
product rule. It now returns `KARMA_TRANSFER_NOT_SUPPORTED`, and the misleading MCP transfer tool is no longer
advertised. Authorized, typed add/deduct operations are the only supported Karma balance mutations; governance of
future weighting versions is a separate hosted-authority concern.

The hosted weighting surface no longer reports success for an operation it did not perform. Reads expose the
authoritative Core integer weight. Vote/set REST actions return
`KARMA_WEIGHTING_GOVERNANCE_NOT_AVAILABLE`, and the placeholder GraphQL mutations plus MCP vote/set tools are not
published until durable versioned governance is implemented. The aggregate gate requires the dedicated 2/2
`karma-weighting-policy.trx` report. Current SQLite evidence passes 511/511 across 19 suites at
`artifacts/current-goal-karma-policy-sqlite-release/acceptance-report.json` (report SHA-256
`BB8FE0CD3FB485B163D6999591374805B9F4DCEF702892DC4EF7F3C57B88CBDB`). Earlier Holo test/package stages passed but
their aggregate reports were rejected after the shared source commit changed underneath them; the later
stable-source Holo report below supersedes those partial runs.

## Immutable Karma policy identity

Durable Karma commands identify the calculation contract independently from the enclosing
`oasis.avatar-gameplay.v1` stream. The current and only accepted value is `oasis.karma-policy.v1`. Both the portable
Edge reducer and the hosted transactional provider validate it before any state mutation or receipt write. This is
an invariant, not a retry or compatibility fallback: an unrecognized version is rejected and remains visible as a
synchronization error until software that implements that policy is deployed.

Commands created before the policy field was introduced map deterministically to version one through the command
model's default initializer. New OGEngineClient commands always serialize the version explicitly. Consequently,
offline commands retain the exact weighting semantics under which they were accepted, while old v3 journal entries
remain readable without maintaining a second reducer path. Core serialization/reducer evidence passes 262/262 at
`artifacts/current-goal-karma-policy-version-core/core-hyperdrive-policy-version.trx`; real Mongo atomicity evidence
passes 42/42 plus abrupt-primary recovery under `artifacts/current-goal-karma-policy-version-mongo/`.

The full `SqliteMvp` gate passes 514/514 across 19 suites at
`artifacts/current-goal-karma-policy-version-sqlite-release-stable/acceptance-report.json` (SHA-256
`2706767B3365D431112D737D64F25B36E1B6F038FB991BD20B2822DEA3951CE4`). NativeAOT staging is deliberately kept
under a short, deterministic, evidence-directory-keyed path: Windows MSBuild otherwise reaches the legacy
260-character path ceiling for the Native Integrated Endpoint intermediate assembly. Durable reports and archives
are copied back into the requested release evidence directory, preserving isolated provenance without making build
success depend on the caller's directory-name length.

The corresponding stable-source `HoloEnabled` gate passes 610/610 across 22 suites at
`artifacts/current-goal-karma-policy-version-holo-release-stable/acceptance-report.json` (SHA-256
`B074A4B8C022BC83869EFE4AB60FA2980DAFFE3141FD3CC1CD2015978B682C4F`, Unity package SHA-256
`DB33F8946ED4066D03EBF33FD635DCF0662AD4461EEFD0680D25E2570BB0E290`). It adds HoloNET authentication,
HoloOASIS Unity and Edge tests, Holo release packages and hApp provenance to the complete SQLite gate while using
the same source commit `306d7acce25c232ca767a77b33c877b0dd3afec9`.

Generic typed Holon v2 load/save routing now preserves the complete provider diagnostic contract when converting an
`OASISResult<IHolon>` to `OASISResult<T>`: structured error code, detailed message, inner diagnostics, stack traces,
metadata, operation flags and all result/error/warning/load/save/delete counts. The shared result-copy helper owns
that invariant so other typed manager adapters cannot silently discard provider failures. Focused coverage passes
5/5 and the complete Core suite passes 263/263 under `artifacts/current-goal-generic-holon-result-contract/`; the
aggregate Core floor is raised accordingly.

The same diagnostic invariant now holds across the provider-execution boundary itself and the asynchronous typed
provider-key overload. HyperDrive's generic provider-result conversion no longer reduces a provider failure to only
`IsError`, message and exception; it preserves the structured code, detailed message, warning/error counts, inner
diagnostics, metadata and operation flags before the manager performs its typed Holon mapping. The focused router and
manager contract passes 6/6 and the complete Core suite passes 264/264 at
`artifacts/current-goal-generic-holon-result-contract-v2/`; the release floor is 264.

The same diagnostic-envelope invariant now covers generic holon search. SearchManager routes against its injected
HyperDrive v2 runtime, and HolonManager preserves provider warnings, detailed diagnostics, inner messages, metadata
and operation counts when it projects `ISearchResults` into typed or untyped holon collections. Null provider
payloads remain explicit failures rather than empty successes. The focused search matrix passes 4/4 and the full
Core suite passes 265/265 under `artifacts/current-goal-holon-search-result-contract-v2/`; the release floor is 265.

Metadata-based holon reads now preserve the same contract. One shared selector projects collection results into a
single typed or untyped holon for all eight scalar/dictionary and sync/async overloads, preserving every diagnostic
field and failing explicitly when the provider result object itself is missing. An empty successful query is a
visible `No holon found` warning, not a false loaded result. The common typed collection mapper now delegates to the
complete result copier as well. Focused coverage passes 4/4 and Core passes 267/267 under
`artifacts/current-goal-holon-metadata-result-contract-v2/`; the release floor is 267.

The v2 Holon delete boundary now owns one consistent semantic invariant: every successful soft or hard delete, by
OASIS id or provider key and through sync or async APIs, sets `IsDeleted`. A hard delete is no longer mislabeled as a
save, and typed async results use the common full-envelope projection. The focused diagnostic/delete-state test
passes and the complete Core suite passes 268/268 under
`artifacts/current-goal-holon-delete-result-contract-v2/`; the release floor is 268.

Avatar and AvatarDetail deletion follows the same rule across all twelve v2 ID/username/email and sync/async entry
points. The manager marks a true, non-error provider result `IsDeleted` without replacing any provider diagnostics.
The complete route matrix passes and Core passes 269/269 under
`artifacts/current-goal-avatar-delete-contract-v2/`; the release floor is 269.

Synchronous `SaveAvatar` no longer bypasses HyperDrive v2. It applies the same credential, token-retention and audit
preparation as asynchronous save, routes through the injected v2 runtime, preserves provider diagnostics, and marks
only a real returned Avatar as saved. Focused coverage passes and Core passes 270/270 under
`artifacts/current-goal-avatar-save-contract-v2/`; the release floor is 270.

Derived Avatar collection views are also result-envelope preserving. Flat and grouped avatar-name APIs project only
the payload while retaining the provider's warning/error diagnostics, metadata and counters; missing successful
payloads are explicit contract errors rather than false successes. During that audit, legacy AvatarDetail username
failover was found to cross the wrong provider boundary (email lookup). Both sync and async paths now remain on the
username provider contract, protected by a regression that forbids email lookup. The focused pair and complete
272/272 Core suite are retained at `artifacts/current-goal-avatar-query-contracts/`; the release floor is 272.

The corresponding stable-source `SqliteMvp` aggregate passes 524/524 across 19 suites at
`artifacts/current-goal-avatar-query-sqlite-release-7d5b00c6/acceptance-report.json` (report SHA-256
`3732838EA2B742B4CB406A5F46B4A600F1D60E88C5123DB9101CB6C29375F371`). It was produced from commit
`7d5b00c642c5a77f5feecd7360f1ccd0eff11220` and includes successful Unity, Android, Our World, package
reproducibility, NativeAOT Edge/RemoteOnly, dependency-security, API-compatibility and SBOM gates.

The paired stable-source `HoloEnabled` aggregate passes 620/620 across 22 suites at
`artifacts/current-goal-avatar-query-holo-release-f104e0b0/acceptance-report.json` (report SHA-256
`0F2CD956528EDC986260C7D4E160A071CF3FADC5F6E0393D8B4396BC294259D2`). It validates source commit
`f104e0b0b27207e9bec34625564d300d7302dbff` against hApp source commit
`c0eb1603e1855a933e75961c038e5f41fd4a5007`, including HoloNET authentication, HoloOASIS Edge/Unity lifecycle,
hApp provenance, Unity/Android/Our World compilation, reproducible packaging and both NativeAOT profiles. The
Holo-enabled UPM archive SHA-256 is `494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`.

Provider implementations that inherit the default Avatar verification/reset/refresh-token or public/private-key
lookups now receive the same v2 boundary guarantees as indexed providers. One shared selector preserves all provider
diagnostics, rejects a successful collection envelope without its payload, awaits the asynchronous collection API
directly, and keeps synchronous lookup entirely on the synchronous provider contract. The focused 2/2 regression
and complete 274/274 Core suite are retained at `artifacts/current-goal-provider-default-avatar-lookups/`; the
release floor is 274.

The corresponding stable-source `SqliteMvp` aggregate passes 526/526 across 19 suites at
`artifacts/current-goal-provider-default-avatar-lookups-sqlite-release/acceptance-report.json` (report SHA-256
`92E7FBAC0E22D609240B66FAE9C1F93DCE8EA7AA4D55F292510C470343370466`). It validates commit
`f104e0b0b27207e9bec34625564d300d7302dbff`; the reproducible UPM archive SHA-256 is
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`.

The paired stable-source `HoloEnabled` aggregate passes 622/622 across 22 suites at
`artifacts/current-goal-provider-default-avatar-lookups-holo-release/acceptance-report.json` (report SHA-256
`0473CC27BD06E36C66C5B461295C89B4DFFAED24DF4060DFB3EE7534D5804B2B`). The hApp source commit remains
`c0eb1603e1855a933e75961c038e5f41fd4a5007`; the reproducible Holo-enabled UPM SHA-256 is
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`.

The default provider AvatarDetail deletion contract no longer implements synchronous methods by blocking on async.
ID, username and email routes stay on their matching sync/async provider boundary, preserve authoritative save and
failure diagnostics, avoid the former second lookup for username/email deletion, and fail explicitly for missing
results, missing payloads or unsupported hard deletion. The focused 2/2 regression and complete 276/276 Core suite
are retained at `artifacts/current-goal-provider-default-avatar-detail-delete/`; the release floor is 276.

The corresponding stable-source `SqliteMvp` aggregate passes 528/528 across 19 suites at
`artifacts/current-goal-provider-default-avatar-detail-delete-sqlite-release/acceptance-report.json` (report
SHA-256 `BBCDCB2DC67E533CC6A4E20E4467A77BF54D161F16B9E34D6CB5CC18F848E71C`). It validates commit
`f104e0b0b27207e9bec34625564d300d7302dbff`; the reproducible UPM SHA-256 remains
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`.

The paired stable-source `HoloEnabled` aggregate passes 624/624 across 22 suites at
`artifacts/current-goal-provider-default-avatar-detail-delete-holo-release/acceptance-report.json` (report
SHA-256 `E118497CB9E12463E423EF72D905AB19052F568AAA1FD3DAEFEBB47EF7F2480C`). It validates the same OASIS commit and
hApp source commit `c0eb1603e1855a933e75961c038e5f41fd4a5007`; the reproducible Holo-enabled UPM SHA-256 remains
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`.

The default provider visibility projection for generic Holon collections now has one invariant across parent,
single-metadata, multi-metadata and all-Holon sync/async overloads. It preserves the authoritative provider result,
materializes owned/public filtering at the call boundary and fails explicitly when the provider omits either its
result envelope or required collection payload. Focused regression coverage passes 2/2 and the full Core suite
passes 278/278 under `artifacts/current-goal-provider-default-holon-visibility/`; the release floor is 278.

The stable-source `SqliteMvp` aggregate passes 530/530 across 19 suites at
`artifacts/current-goal-provider-default-holon-visibility-sqlite-release-stable/acceptance-report.json` (report
SHA-256 `E6EE3845CA169F9DE9765E7F9F7D28911B7BB77FDFC2EFF21DC9895B01803E64`). It validates commit
`f104e0b0b27207e9bec34625564d300d7302dbff`; the reproducible UPM SHA-256 is
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`. The gate also corrected the
OGEngineClient test host's disposal race: only listener termination after the fixture has explicitly entered
disposal is accepted, while unexpected listener failures still propagate.

The paired stable-source `HoloEnabled` aggregate passes 626/626 across 22 suites at
`artifacts/current-goal-provider-default-holon-visibility-holo-release/acceptance-report.json` (report SHA-256
`133BD627F162F60E710BF430F6D88496255EFF9D14C3CA4B7BC69FD6284062DF`). It validates the same OASIS commit and
hApp source commit `c0eb1603e1855a933e75961c038e5f41fd4a5007`; the reproducible Holo-enabled UPM SHA-256 remains
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`.

Legacy Holon failover/provider-selection helpers now keep ID and provider-key loads on their declared provider
boundary. Sync routes call the synchronous provider directly on the caller thread; async routes await their provider
task once after the timeout race. Focused regression coverage passes 2/2 and the full Core suite passes 280/280
under `artifacts/current-goal-legacy-holon-provider-boundary/`; the release floor is 280.

The stable-source `SqliteMvp` aggregate passes 532/532 across 19 suites at
`artifacts/current-goal-legacy-holon-provider-boundary-sqlite-release/acceptance-report.json` (report SHA-256
`5DEE418508769F856F3D613B0CCDB9FE74C776527BE728F2EC1804CBF07E9FC5`). It validates commit
`f104e0b0b27207e9bec34625564d300d7302dbff`; the reproducible UPM SHA-256 remains
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`.

The paired stable-source `HoloEnabled` aggregate passes 628/628 across 22 suites at
`artifacts/current-goal-legacy-holon-provider-boundary-holo-release/acceptance-report.json` (report SHA-256
`C6807E2AC5F69FC5B660670F1E39376B4D52DE2591A1F3782F71C94710019DBF`). It validates the same OASIS commit and
hApp source commit `c0eb1603e1855a933e75961c038e5f41fd4a5007`; the reproducible Holo-enabled UPM SHA-256 remains
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`.

Legacy SearchManager now preserves provider execution semantics as well: sync search calls remain synchronous and
thread-affine, while async searches await their provider task exactly once after the timeout race. A provider that
violates the async or result-envelope contract is reported explicitly rather than dereferenced. The focused 2/2
regression and complete 282/282 Core suite are retained under
`artifacts/current-goal-legacy-search-provider-boundary/`; the release floor is 282.

The corresponding stable-source `SqliteMvp` aggregate passes 534/534 across 19 suites at
`artifacts/current-goal-legacy-search-provider-boundary-sqlite-release/acceptance-report.json` (report SHA-256
`AFD640223FA4A706341E52A42BD0FE3DA2BD5D22F151F141036A85AB1E2BA3CD`). It validates commit
`f104e0b0b27207e9bec34625564d300d7302dbff`; the reproducible UPM SHA-256 remains
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`.

The paired stable-source `HoloEnabled` aggregate passes 630/630 across 22 suites at
`artifacts/current-goal-legacy-search-provider-boundary-holo-release/acceptance-report.json` (report SHA-256
`FB9818C71D7C754C1A58D954E739E7973CF96C087F00D44952BC3C6C2B926E4B`). It validates the same OASIS commit and
hApp source commit `c0eb1603e1855a933e75961c038e5f41fd4a5007`; the reproducible Holo-enabled UPM SHA-256 remains
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`.

Provider lifecycle dispatch now obeys the same provider boundary. Sync activation/deactivation stays on the caller
thread and invokes only the sync provider contract; async activation/deactivation awaits only the async provider
contract after its bounded timeout race. Null provider/task/result violations are surfaced as explicit result errors.
The focused lifecycle regression passes 4/4 and the complete Core suite passes 286/286 under
`artifacts/current-goal-legacy-provider-lifecycle-boundary/`; the release floor is 286.

The corresponding stable-source `SqliteMvp` aggregate passes 538/538 across 19 suites at
`artifacts/current-goal-legacy-provider-lifecycle-boundary-sqlite-release/acceptance-report.json` (report SHA-256
`EC2DFDFDCFA3153DC89125A017998A03B2355D9A311D28B8804B7B5A4967F041`). It validates commit
`f104e0b0b27207e9bec34625564d300d7302dbff`; the reproducible UPM SHA-256 remains
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`.

The paired stable-source `HoloEnabled` aggregate passes 634/634 across 22 suites at
`artifacts/current-goal-legacy-provider-lifecycle-boundary-holo-release/acceptance-report.json` (report SHA-256
`E6F937E56286ADE255150557D8F8CC0EBE5365E948C305377F8C58D2D011EB9C`). It validates the same OASIS commit and
hApp source commit `c0eb1603e1855a933e75961c038e5f41fd4a5007`; the reproducible Holo-enabled UPM SHA-256 remains
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`.

Synchronous Avatar and local-wallet save helpers now preserve the provider thread boundary too. They call the sync
provider contract directly instead of wrapping it in `Task.Run` and synchronously waiting, while retaining explicit
null/error result handling. Focused regression coverage passes 2/2 and the complete Core suite remains 286/286 under
`artifacts/current-goal-sync-avatar-wallet-save-boundary/`.

The corresponding stable-source `SqliteMvp` aggregate passes 538/538 across 19 suites at
`artifacts/current-goal-sync-avatar-wallet-save-boundary-sqlite-release/acceptance-report.json` (report SHA-256
`5C504A2ACD9DA144E074D68AE6D310825A3B0829C00EC07976E0F6AF359E9464`). It validates commit
`f104e0b0b27207e9bec34625564d300d7302dbff`; the reproducible UPM SHA-256 remains
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`.

The paired stable-source `HoloEnabled` aggregate passes 634/634 across 22 suites at
`artifacts/current-goal-sync-avatar-wallet-save-boundary-holo-release/acceptance-report.json` (report SHA-256
`17E4C65FFABDAC0AD5F305F2F3D1C774B44F979DE21BD1B9034229CBD622ED3D`). It validates the same OASIS commit and
hApp source commit `c0eb1603e1855a933e75961c038e5f41fd4a5007`; the reproducible Holo-enabled UPM SHA-256 remains
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`.

The synchronous email boundary used by registration and password reset now invokes the actual configured transport;
it is no longer a full-runtime no-op and callers no longer manufacture a worker thread around `SendAsync`. Missing
initialization or credentials fail explicitly, while the async transport does not capture a caller context. Focused
coverage passes 1/1 and the complete Core suite passes 287/287 under `artifacts/current-goal-email-sync-contract/`;
the release floor is 287.
The stable-source `SqliteMvp` aggregate containing this correction passes 539/539 tests across 19 suites at
`artifacts/current-goal-email-sync-contract-sqlite-release/acceptance-report.json` (report SHA-256
`05A69285446C024783240052B0D1696DA9A20916BAB72F026494906B44264561`). It validates OASIS commit
`f104e0b0b27207e9bec34625564d300d7302dbff`; the reproducible UPM package SHA-256 remains
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`.
The paired stable-source `HoloEnabled` aggregate passes 636/636 tests across 22 suites at
`artifacts/current-goal-legacy-sync-avatar-key-holo-release/acceptance-report.json` (report SHA-256
`E855B88F0F7DFD87FBA3784A25E37444DB5685B3C44DF63D6C6A414BD2F33177`). It validates the same OASIS
commit plus HoloOASIS hApp source `c0eb1603e1855a933e75961c038e5f41fd4a5007`; its reproducible UPM package
SHA-256 remains `494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`.

The synchronous Avatar replication helpers now resolve the selected provider through the same v2/legacy policy as
their async counterparts and invoke `SaveAvatarDetail`, `DeleteAvatar`, `DeleteAvatarByEmail` or
`DeleteAvatarByUsername` directly. They no longer block on asynchronous provider work, execute on the caller thread,
and report missing resolution/results through the existing replication warning policy. Focused save/delete boundary
coverage passes 1/1 and the complete Core suite passes 289/289 under
`artifacts/current-goal-sync-avatar-replication/`; the release floor is 289. The Core `netstandard2.1` build also
passes, preserving Unity compatibility.

The synchronous inventory query surface (`GetAvatarInventory`, both `AvatarHasItem` forms,
`SearchAvatarInventory` and `GetAvatarInventoryItem`) now remains synchronous through the AvatarDetail provider
boundary. It no longer blocks on the asynchronous inventory methods, preserves explicit load errors, and performs
the query against the synchronously loaded inventory. Focused coverage exercises all five entry points and forbids
`LoadAvatarDetailAsync`; it passes 1/1 and the complete Core suite passes 290/290 under
`artifacts/current-goal-sync-inventory-reads/`. The Core `netstandard2.1` build also passes and the release floor is
290. Synchronous inventory removal now follows the same boundary: it loads and saves AvatarDetail through the
synchronous provider contracts, reports null/error results explicitly, and never blocks on either asynchronous
contract. Its focused regression passes 1/1, the complete Core suite passes 291/291 under
`artifacts/current-goal-sync-inventory-remove/`, the `netstandard2.1` build passes, and the enforced release floor is
291. Inventory addition now uses one shared mutation engine across sync and async entry points, preserving metadata
promotion, functional stack identity, NFT/GeoNFT uniqueness and the durable operation ledger without duplicating
business rules. The synchronous entry point performs only synchronous AvatarDetail load/save calls and accepts the
same optional operation id for idempotent replay. Its focused boundary-and-replay regression passes 1/1; the complete
Core suite passes 292/292 under `artifacts/current-goal-sync-inventory-add/`, the `netstandard2.1` build passes, and
the enforced release floor is 292. The remaining legacy synchronous Avatar provider loads no longer block on their
async counterparts: ID, username and email loads for both Avatar and AvatarDetail, plus both complete collection
loads, use the matching synchronous provider contract. Focused coverage exercises all eight entry points and the
username failover invariant across sync and async paths (2/2); the complete Core suite passes 293/293 under
`artifacts/current-goal-legacy-sync-avatar-loads/`, the `netstandard2.1` build passes, and the enforced release floor
is 293.
The paired stable-source `HoloEnabled` aggregate passes 635/635 tests across 22 suites at
`artifacts/current-goal-email-sync-contract-holo-release/acceptance-report.json` (report SHA-256
`D13B654E0A2B14078830F49E45F36ACC27124E63ED502E37D8822DECCC40E5F7`). It validates the same OASIS
commit plus HoloOASIS hApp source `c0eb1603e1855a933e75961c038e5f41fd4a5007`; its reproducible UPM package
SHA-256 remains `494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`.

Legacy-mode synchronous Avatar token/key lookups now invoke their matching synchronous provider contracts directly;
they no longer block on the asynchronous lookup path. The shared boundary covers verification, reset and refresh
tokens plus public, provider and private keys, preserves the provider's complete `OASISResult<IAvatar>`, and returns
an explicit error when provider activation or the provider result is missing. Focused public-key boundary coverage
passes 1/1 and the complete Core suite passes 288/288 under
`artifacts/current-goal-legacy-sync-avatar-key/`; the release floor is 288.
The stable-source `SqliteMvp` aggregate containing this boundary repair passes 540/540 tests across 19 suites at
`artifacts/current-goal-legacy-sync-avatar-key-sqlite-release/acceptance-report.json` (report SHA-256
`571559A33A3FCD0BDF56C41E92E7BC7C172D4E14F63055E57E1720D547CB1DA5`). It validates OASIS commit
`f104e0b0b27207e9bec34625564d300d7302dbff`; the reproducible UPM package SHA-256 remains
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`.

The current paired release evidence includes the shared synchronous/asynchronous inventory mutation engine and all
eight corrected synchronous Avatar/AvatarDetail provider load contracts. `SqliteMvp` passes 545/545 tests across 19
suites at `artifacts/current-goal-sync-avatar-loads-sqlite-release/acceptance-report.json` (report SHA-256
`2644B0947F8F61981C30A1815F90F91EE265D9C563147D25F3FB6C714433DC2B`), while `HoloEnabled` passes 641/641 tests
across 22 suites at `artifacts/current-goal-sync-avatar-loads-holo-release/acceptance-report.json` (report SHA-256
`1D277979A8A4F718AE42CEFE7E429774D00073F7B5FE64DC27EF78CCA8C4173F`). Both have zero failures and validate OASIS
commit `f104e0b0b27207e9bec34625564d300d7302dbff`. The Holo profile binds hApp source commit
`c0eb1603e1855a933e75961c038e5f41fd4a5007` using manifest SHA-256
`46B0F9986430FEEE28FFEB430613F471AA25174C6E2BF3DE6B03E6082E73B23C`. The reproducible UPM SHA-256 values are
respectively `4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE` and
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`; both 113-symbol native profiles, the compiled
native smoke tests, Unity compilation, Android player validation and the Our World Edge integration compile pass.

Registration preparation now has one synchronous implementation shared by the sync and task-returning APIs. This
removes the synchronous registration path's task wait without creating a second set of business rules. It also fixes
the identity invariant at its source: the email uniqueness query receives the supplied email and the username
uniqueness query receives the supplied username. Focused coverage passes 1/1, the complete Core suite passes 294/294
under `artifacts/current-goal-registration-preparation/`, the `netstandard2.1` build passes, and the release gate now
requires at least 294 Core tests.
The complete rebuilt `SqliteMvp` gate passes 546/546 tests across 19 suites at
`artifacts/current-goal-registration-sqlite-release/acceptance-report.json` (report SHA-256
`FB951E908DA51748E5E09176E9BDA2AF3D026182CB45A5E4DCBAB0BC51A91A92`). It validates OASIS commit
`f104e0b0b27207e9bec34625564d300d7302dbff`, both 113-export native profiles, compiled native smoke tests, Unity and
Android player validation, and the Our World integration compile. Its byte-reproducible UPM SHA-256 remains
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`.
The paired rebuilt `HoloEnabled` gate passes 642/642 tests across 22 suites at
`artifacts/current-goal-registration-holo-release/acceptance-report.json` (report SHA-256
`9FA75F5D0F098E4C4895DD701B9374AD17E1348C18FBE7536A677EC0FF099CBC`). It validates the same OASIS commit and
binds hApp source commit `c0eb1603e1855a933e75961c038e5f41fd4a5007` through build-manifest SHA-256
`46B0F9986430FEEE28FFEB430613F471AA25174C6E2BF3DE6B03E6082E73B23C`. Its byte-reproducible UPM SHA-256 is
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`; both native profiles, compiled smoke tests,
Unity compilation, Android player validation and the Our World integration compile pass.

Quest progress operation identity is now content-bound rather than merely presence-bound. The durable quest ledger
stores a deterministic SHA-256 fingerprint over avatar, quest, normalized game source and every progress field.
Exact retries replay the original rewards once; an operation-id/content mismatch returns
`QUEST_PROGRESS_OPERATION_CONFLICT`, and an unverifiable pre-fingerprint row returns
`QUEST_PROGRESS_REPLAY_FINGERPRINT_MISSING` without mutating the quest. The focused suite passes 4/4 at
`artifacts/current-goal-quest-progress-fingerprint/quest-progress-fingerprint.trx`, and the aggregate release
inspector enforces a four-test minimum for `quest-idempotency.trx`.

The corresponding Full Runtime compatibility run passes ONODE Core 163/163, ONODE Core integration 43/43, ONODE
WebAPI 98/98 and STAR DNA 1/1, and builds the Full Native Integrated Endpoint, both Edge Native targets and STAR CLI.
Evidence is under `artifacts/current-goal-quest-progress-full-runtime/`; the four report hashes are recorded by the
artifact directory and the ONODE Core report SHA-256 is
`B2A067064DD074DA620712F60D61F75B5638AC9FB308CFE4FDFE23F04CA221C9`.

Fresh paired package evidence includes the content-bound quest-operation ledger. `SqliteMvp` passes 549/549 tests
across 19 suites at `artifacts/current-goal-quest-fingerprint-sqlite-release/acceptance-report.json` (report SHA-256
`E8E2EB5E20C2B06638967B00DC1D767B917849FABFAFB2C6D4493A2F878CAEBE`, deterministic UPM SHA-256
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`). `HoloEnabled` passes 645/645 tests
across 22 suites at `artifacts/current-goal-quest-fingerprint-holo-release/acceptance-report.json` (report SHA-256
`8893AB92E5FC9F85DD6ADB930A2A6D285BC82E4776E86291BBE5859A4EC64BC7`, deterministic UPM SHA-256
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`). Both pass their native, Unity,
Android-player and Our World integration gates; the Holo report binds hApp commit
`c0eb1603e1855a933e75961c038e5f41fd4a5007` through manifest SHA-256
`46B0F9986430FEEE28FFEB430613F471AA25174C6E2BF3DE6B03E6082E73B23C`.

### Synchronous durable Karma execution

Synchronous callers are a first-class provider boundary, not wrappers over the asynchronous pipeline.
`HolonManager` exposes synchronous setting load/save operations using the deterministic settings-holon identity and
detached writes. `CompetitionManager.UpdateAvatarScore` uses those operations for the durable leaderboard and
avatar-stat projections. `KarmaManager.GetKarma`, `AddKarma`, `DeductKarma`, `AddKarmaToAvatar`, and
`RemoveKarmaFromAvatar` use the synchronous settings and competition contracts from end to end; consequently the
legacy synchronous `AvatarDetail` APIs contain no task wait. Sync and async mutations share semaphore instances and
pure parsing/ranking/record-construction rules, preserving ordering without maintaining a second ledger.

Missing data must be explicit: only a true absent settings holon may be created. Provider outage/error results are
propagated and cannot cause a replacement write. Tests also require provider-returned settings objects to remain
unchanged until a detached save succeeds. The 2026-10-05 evidence is 36/36 synchronous-boundary tests, 21/21
Karma/competition regressions, and 298/298 complete Core tests under
`artifacts/current-goal-sync-provider-boundary/`; the release inspector enforces the 298-test Core floor.

Fresh aggregate evidence includes this boundary. `SqliteMvp` passes 553/553 tests across 19 suites at
`artifacts/current-goal-sync-karma-sqlite-release/acceptance-report.json` (report SHA-256
`37B7344216EE041691631D8E83340CD52D8CE5B858EF4B7C7DC9EF8477994913`, deterministic UPM SHA-256
`4E23F30EAC31CC75C1C7EB659F80DA3A321D1CA5B62B16842550D7A7F6C66FAE`). `HoloEnabled` passes 649/649 tests
across 22 suites at `artifacts/current-goal-sync-karma-holo-release/acceptance-report.json` (report SHA-256
`432E9B84F0B3776E190D7A92CDA59041C412F3AC55DAE736B5117F4F97328B80`, deterministic UPM SHA-256
`494F427CBCFAFDC425814D07B677CD7B15286D412173269EB96297F6832DFDC3`). Both pass the native profiles, Unity,
Android-player and Our World integration gates. The Holo manifest binds hApp commit
`c0eb1603e1855a933e75961c038e5f41fd4a5007` with SHA-256
`46B0F9986430FEEE28FFEB430613F471AA25174C6E2BF3DE6B03E6082E73B23C`.

The independent Full Runtime regression remains green after the change: ONODE Core 163/163, integration 43/43,
WebAPI 98/98 and STAR DNA 1/1, together with successful Full Native Endpoint, Edge Native and STAR CLI builds.
Evidence is retained under `artifacts/current-goal-sync-karma-full-runtime/`.

`AvatarManager` now joins that same invariant when `HyperDriveMode` is explicitly `V2`: its sync and async Karma
APIs use the durable `KarmaManager` boundary rather than directly invoking provider-specific Karma methods. A lazy,
runtime-injected manager preserves the caller's provider graph without constructing a second singleton runtime.
`Legacy` remains the DNA default and its existing provider calls are unchanged. Mode-boundary coverage is 4/4,
complete Core coverage is 300/300, and fresh aggregate coverage is 555/555 (`SqliteMvp`) plus 651/651
(`HoloEnabled`) under `artifacts/current-goal-avatar-manager-karma*`.
