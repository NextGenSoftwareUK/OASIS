# STAR API: remote vs native transport — architecture notes

The versioned Edge Runtime ABI used by ODOOM and OQuake is documented separately in
[OGENGINE_EDGE_NATIVE_GAMES_MILESTONE.md](OGENGINE_EDGE_NATIVE_GAMES_MILESTONE.md), including durable gameplay,
settings, notifications, and release validation.

**Purpose:** Capture the decision space for **in-process OASIS** (`star_transport: native`) versus the shipped **HTTP client** (`remote`), so future work does not re-derive size, AOT, and split-build trade-offs from chat history.

**Status (repo):**

- **Remote:** Default. `OASIS Omniverse/OGEngineClient` (NativeAOT `star_api`) talks to WEB5/WEB4 over HTTP.
- **Native flag:** Games and `ogengine_config_t` expose `transport` / `oasis_dna_path`; the **default** `star_api` build returns **`InitFailed`** with an explicit message for `native` (no silent fallback) — see policy in `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md`.
- **Edge Runtime:** the managed, Unity-compatible Edge Runtime, durable SQLite store, HyperDrive sync protocol and Edge Native Integrated Endpoint are wrapped by the platform-neutral `NextGenSoftware.OGEngine.Client.Edge` assembly. Our World consumes that OGEngineClient assembly through its thin Unity binding. NativeAOT C-ABI routing for ODOOM/OQuake remains to be migrated.
- **DNA / legacy local-provider plumbing:** `AutoFailOverLocalProviders` + `AutoFailOverLocalProvidersEnabled` on `StorageProviderSettings`; `ProviderManager` local list + `ActivateNextLocalAutoFailOverStorageProvider()`; `OASISBootLoader` loads from DNA. This is not a substitute for the durable Edge journal/reconciliation protocol.

---

## What the “current” client is

| Aspect | Shipped `OGEngineClient` (`star_api`) |
|--------|--------------------------------------|
| TFM | `net10.0` |
| Publish | **NativeAOT** (`PublishAot`), `IlcTrimMode` **copy** (stability over minimal size) |
| References | **OASIS Common** + prebuilt **API.Contracts** DLL (AOT-friendly reference pattern) |
| Does **not** reference | **API.Core**, **OASISBootLoader**, provider projects |

It is intentionally a **thin HTTP + JSON** layer suitable for **OQuake / ODOOM** and small native interop surface.

---

## What “full native” implies

**OASISBootLoader** (`NextGenSoftware.OASIS.OASISBootLoader`) targets **net8.0** and **project-references** **API.Core**, **DNA**, and **many provider assemblies** (storage, network, blockchain, cloud, etc.). That is a **large transitive dependency graph** (third-party SDKs, DB drivers, optional chains, logging).

Bringing that **into the same NativeAOT binary** as today’s `star_api` means:

1. **Binary size:** Expect **large growth** (often **tens of MB** toward **100+ MB** depending on RID and how much ILC can trim) versus the current slim DLL — not a “small bump” if the **full** BootLoader graph is linked.
2. **NativeAOT / trimming:** Many providers and dependencies assume **reflection**, **dynamic code**, or patterns **not** guaranteed AOT-safe. A monolithic “BootLoader inside `star_api`” build typically needs **dedicated ILC spikes**, **descriptor roots**, and possibly **excluding** unsafe providers from the native build.
3. **Runtime cost:** For **remote-only** users, cost stays low **only if** native stacks are **not** booted until `transport == native`. Once native runs, cost is comparable to a **small integrated OASIS endpoint** (DNA load, provider registration/activation, managers, optional replication).

---

## Overhead summary

| Concern | Remote-only (default) | Native path |
|--------|------------------------|-------------|
| **CPU / RAM at idle** | Minimal (HTTP client) | Full OASIS boot + active provider(s) |
| **Disk (approx.)** | Current `star_api` native output | **Much larger** if full BootLoader + providers are compiled in |
| **Build / maintainability** | Established AOT pipeline | New matrix: AOT vs JIT, provider subsets, TFM alignment (net8 vs net9) |

---

## Agreed target architecture

The standard OGEngineClient always composes Edge Runtime, its durable local store and HyperDrive synchronization. All
supported operations use that one local-first path. Connectivity changes only synchronization state:

- **Online:** local execution plus prompt synchronization with a hosted ONODE.
- **Offline:** the same local execution path, with durable pending operations.
- **Reconnecting:** local operation continues while HyperDrive resumes exchange.
- **Synchronized:** local and hosted state have converged.

Games and applications do not switch between `remote` and `native`, and a failed hosted request does not invoke a
parallel fallback implementation. Edge is already the execution path; hosted transport loss changes its state and
leaves its durable journal pending. HTTPS versus ONET remains an internal synchronization transport choice with
identical protocol semantics.

`OGEngine.Shared` owns reusable DTO mapping, validation, error codes and state transitions. Native games use the
stable C ABI; Unity uses a managed adapter over the same shared contracts and Edge components. The Full OASIS
BootLoader and full provider graph are not pulled into mobile/game clients.

The shared assembly also owns the canonical WEB4 authentication protocol and is included in both release profiles.
ODOOM/OQuake's NativeAOT client and Our World's managed Unity client therefore use the same endpoint normalization,
single-flight request, nested-envelope parsing, JWT/avatar extraction and safe error mapping. Host-specific behavior
starts only after that result: native games initialize WEB5 and warm caches, while Unity presents `Beaming In...` and
hands the authenticated identity to the Edge session lifecycle.

---

## Packaging decision

- The standard shipped OGEngineClient includes Edge Runtime and is the baseline for Our World, ODOOM, OQuake and
  future clients.
- Edge remains a separately testable internal package/component because Unity/IL2CPP and native games have different
  composition roots; that boundary does not make Edge optional at runtime.
- Compose the existing Edge Runtime, durable store and hosted sync transport rather than the Full BootLoader graph.
- Let Unity reference the managed OGEngineClient package/API and compile it through IL2CPP. It need not load the
  Windows-style NativeAOT binary: managed, C ABI and future web bindings are runtime-specific bindings over the same
  OGEngineClient behavior owner, not separate clients.
- Retire the standard client's user-facing `remote`/`native` selection once migration is complete. Configuration may
  select HTTPS or ONET synchronization transport, but never online versus offline behavior.
- A special remote-only build may exist for a proven constrained or unsupported target, but it is a distinct artifact,
  not a fallback inside the standard client.
- Drive connectivity transitions through Edge Runtime's state machine and durable outbox, not legacy provider
  activation calls or process-memory OGEngine queues.

Offline sync is enabled by default but can be explicitly disabled through
`OASIS.OASISHyperDriveConfig.OfflineSyncEnabled` and the host application's settings UI. Disabling is a lifecycle
recomposition into remote-only operation, not a response to transport failure. It is rejected while the durable
outbox is non-empty unless the user first synchronizes; local state is preserved by default.

The release pipeline produces **OGEngineClient Edge** as the standard artifact and **OGEngineClient Remote-Only** as
an exceptional minimum-footprint artifact without Edge/SQLite binaries. Both derive from the same shared contracts
and C ABI. Capability metadata makes the difference explicit; the Remote-Only artifact cannot enable offline sync.

### Measured artifact snapshot (2026-09-23)

| Artifact | Local raw size | Interpretation |
|---|---:|---|
| Existing Windows NativeAOT OGEngine DLLs | 5.53-6.97 MiB | Slim remote client; build outputs differ |
| Entire manifested Unity Edge package | 22.81 MiB | Includes native SQLite binaries for every supported desktop/mobile platform |
| Android ARM64 Edge dependencies | about 3.92 MiB | 2.23 MiB managed set plus 1.69 MiB ARM64 SQLite before compression/stripping/deduplication |
| OASIS-owned managed Edge assemblies | about 0.29 MiB | Contracts, synchronization, store, runtime, ONET and endpoint assemblies |

These are disk artifacts only. Physical-device RAM, database growth, CPU, radio and battery measurements remain a
release requirement.

### Public component boundary

`OASISEdgeAPI` remains a supported low-level facade for applications that only require the Edge Runtime, durable
local store and HyperDrive protocol. `OGEngineEdgeClient` composes that facade and is the standard integration for
OASIS products. It adds the stable product contract on which authentication, avatars, quests, inventory, NFTs,
Karma, status notifications, configuration and language bindings converge. First-party games must not bypass
OGEngineClient and independently recreate those rules.

The same source tree produces both profiles. Edge inclusion is a build capability; `OfflineSyncEnabled` is a runtime
preference available only when that capability exists. The Edge build defaults it to enabled and accepts explicit
OASIS DNA or host-UI changes. The Remote-Only build excludes the Edge/SQLite assemblies and reports the capability as
unavailable. Network loss never changes either setting.

---

## Related code and docs

| Area | Location |
|------|-----------|
| Slim client | `OASIS Omniverse/OGEngineClient/` |
| C ABI / config | `ogengine.h`, `ogengine_config_t.transport`, `oasis_dna_path` |
| Games | `oquake_ogengine_integration.c`, `uzdoom_ogengine_integration.cpp`, `oasisstar.json` (`star_transport`, `oasis_dna_path`) |
| DNA | `NextGenSoftware.OASIS.API.DNA` — `AutoFailOverLocalProviders*`, default `OASIS_DNA.json` samples |
| Provider lists + failover | `ProviderManager` — `GetProviderAutoFailOverLocalList`, `ActivateNextLocalAutoFailOverStorageProvider` |
| Boot load | `OASISBootLoader.LoadProviderLists` |
| Integrated native STAR (historical / server-style) | `Native EndPoint/.../STARAPI.cs` and related — compare when designing the native host |

---

## Changelog

| Date | Note |
|------|------|
| 2026-03-27 | Initial doc: overhead, split-build recommendation, pointers to implemented DNA/ProviderManager and explicit native `InitFailed` in default `star_api`. |
