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

Local validation snapshot (2026-09-23): DNA 11/11, Core HyperDrive 167/167, Edge SQLite 26/26,
Edge Runtime 46/46, ONET synchronization 9/9 and hosted offline-session security 12/12 passed.
MongoDBOASIS, the migration tool, Full ONODE WebAPI, both Native Integrated Endpoint compositions,
STAR CLI, HoloNET Client/ORM and HoloOASIS.Unity build successfully. All seven Edge NuGet packages are
packable, the Unity package passes editor and Android-player validation, and the synchronized Our World
project compiles with Unity 2022.3. The Holochain 0.7 hApp builds and packs with the pinned Holonix toolchain,
and its Holochain Sweettest suite passes against a real conductor. These results deliberately do not claim the
remaining external release proofs: the real three-node MongoDB transaction/primary-loss jobs require a
replica-set environment, while HoloOASIS still requires Android resource profiling and two-device field tests.

Implemented foundation (not yet a production release):

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

The remaining HyperDrive manager migration, HoloOASIS mobile runtime profiling,
fault-injection and full release gates below remain mandatory before this status becomes production-ready. The
Our World package integration now compiles through Unity 2022.3, securely resumes an offline grant before showing
login, keeps an animated `Beaming In...` state for the entire active login operation, reports a friendly hosted-network
failure only after the operation finishes, displays Edge connectivity/synchronization/pending state in the HUD, and
serializes mobile suspend/resume against endpoint disposal.

This document supersedes unqualified claims in older documentation that HyperDrive already guarantees zero downtime.
The implemented Edge path now has a durable SQLite outbox, restart-safe checkpoints, idempotent hosted exchange,
conflict records and snapshot rebase, but production guarantees still depend on completing the remaining migration,
platform security, device qualification and live fault-injection gates documented below.

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
Remaining production work is to complete all HyperDrive manager migration, validate and profile the mobile
application on physical target devices, validate the
Holochain hApp with its unavailable external toolchain, and pass the full release matrix below.
Edge connectivity has three explicit states: `Offline`, `Connecting`, and `Online`. A device network signal moves
the runtime only to `Connecting`; `Online` is published after the hosted service successfully answers an exchange.
This prevents the HUD and clients from briefly claiming that OASIS is online during an outage or recovery probe.
`ReplicatorManager` remains mostly a stub; hosted acceptance instead uses the durable Mongo outbox and ordered,
leased, idempotent provider fan-out path described here.

Settings-backed manager writes use detached Holon snapshots. This prevents a provider cache that returns entity
references from observing uncommitted metadata when a save is rejected. Social and video manager projections are
also bound to their injected runtime rather than the process-global singleton; failed projection writes are exposed
as warnings, and a rejected initial video-call write removes the transient in-memory call.
Chat, gifts, competition and karma projections are likewise runtime-scoped. Chat creation supplies an explicit audit
avatar and removes transient sessions/messages when their authoritative save is rejected. Karma is committed before
its five seasonal leaderboard projections, projection failures are visible warnings rather than hidden console output,
and restart-safe history/statistics load both in-memory dictionary and provider JSON-array metadata shapes.
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
   operation-time-watermarked existing-data backfill tool is implemented and included in the release build gate.
   Typed quest, inventory-definition, GeoNFT and GeoNFT-collection Holon codecs plus global-definition audience
   delivery are implemented. Quest-progress, inventory-grant and GeoNFT-collection commands are durably accepted in
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
4. **Implemented as the transport-neutral hosted boundary:** authenticated HTTPS transport implementing the same
   exchange and peer-binding contracts consumed by Edge Runtime. A provider-shaped facade remains to be added
   only for consumers that require registration through `ProviderManager`.
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
   the last Edge error code. The authoritative Karma total is read from the private AvatarDetail projection while
   offline; Karma history remains online-only. Device UI preferences remain local `PlayerPrefs` state by design and
   synchronize to the settings API only while online; neither path silently serves an unrelated API-response cache.

   This is not yet equivalent to complete offline support for every workflow used by Our World. Cached projections
   currently cover avatar/profile, inventory, quest definitions, GeoNFT collections and the Karma total. Durable
   command contracts exist for quest progress, inventory grants and GeoNFT collection, but every live Our World
   mutation call still has to be mapped to one of those commands and exercised through disconnect, restart,
   reconnect and duplicate-delivery tests. Clan/social reads, Karma history and global settings synchronization are
   online-only today. Generic NFT and GeoHotSpot records are not yet first-class versioned Edge projections.
   The permanent status strip is supplemented by state-driven Offline, reconnecting/synchronizing and synchronized
   toast notifications. Edge callbacks enqueue those notifications and Unity renders them on its main thread. The
   release gate also classifies every public asynchronous gateway method and verifies the required projection routes,
   so a newly added live network call cannot silently escape the documented offline audit.
7. **Partially implemented:** HoloOASIS.Unity and its HoloNET/HoloOASIS dependency chain now compile for
   `netstandard2.1`; device conductor lifecycle, IL2CPP/AOT validation and measured mobile profiling remain.
8. **Partially implemented:** authenticated ONET framed transport, Full ONODE sync host and lightweight Edge ONODE
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
   transport exceptions into structured reconciliation errors. Multi-partition convergence is therefore implemented;
   adversarial network-partition soak testing and mobile transport/resource profiling remain.
9. **Partially implemented:** deterministic restart, rollback, replay, duplicate and conflict tests exist. Edge
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
`ClanManager` now follows the same composition rule for every clan load, list, create, update, delete and inventory
operation. Its Holon and Avatar managers share the supplied runtime registry, and an isolated-runtime load test
proves clan reads cannot escape to the process singleton.
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
`KeyManager` now composes its Avatar manager from that same runtime instead of exposing the global
`AvatarManager.Instance`. Its per-avatar key and usage collections are initialized at construction, removing the
null-state path in active key statistics. A synchronous isolated-provider test proves wallet/public-key lookup
uses only the supplied runtime and never invokes an asynchronous or singleton provider path.
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
provider unchanged.
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
Eight named TRX reports (DNA, Core HyperDrive, Edge store, Edge runtime,
ONET, hosted offline-session grant security, hosted Mongo transaction rollback/retry, and abrupt Mongo primary loss) are parsed by the inspector;
the Mongo evidence is produced by a separate Linux replica-set job and copied into the release evidence rather than
substituting a mocked store. Missing, empty, failed, errored, timed-out or aborted reports fail the gate,
and their counts and hashes are embedded in the acceptance report and checksum set. Per-suite minimum executed-test
baselines prevent a filter or accidental test removal from producing a misleading green release. Passing this gate is necessary but does not replace device profiling, extended replica-set election/partition soak tests, IL2CPP/AOT tests, or the full compatibility matrix.

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
