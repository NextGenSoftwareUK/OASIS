# ONET / ONODE Session Notes — 2026-07-13

## What Was Built

### Phase 1 — OASISDNA Config & Discovery Wiring (commit 8ef79ef56)
- Added `ONETConfig` class to `OASISDNA.cs` with fields: `BootstrapServers`, `NetworkType`, `NodeId`, `NodePublicKey`, `NodePrivateKey`, `TcpPort`, `EnableMDNS`, `AutoRegisterOnBootstrap`
- Added `ONET` section to `OASIS_DNA.json` (default: Internal mode, bootstrap server `https://api.web4.oasisomniverse.one`, port 38470)
- `ONETDiscovery` constructor now reads `BootstrapServers` and `NodeId` from DNA
- Removed orphaned `using MongoDB.Driver;` from STAR CLI `Program.cs`

### Phase 2 — NodeId Keypair, Bootstrap Registration, Singleton Controllers, ONETManager Injection (commit f6dfde955)
- `ONETManager.InitializeAsync`: generates ECDSA-P256 keypair + SHA-256 NodeId on first run, persists to DNA, registers public key with `ONETProtocol`, POSTs to all bootstrap servers
- Added `POST /api/v1/onet/nodes/register` endpoint to `ONETController` — community ONODEs call this on startup so the bootstrap server can verify their future ECDSA-signed requests
- `ONETController.GetOnetManagerStaticAsync` exposed as `internal static` so `ONODEController` shares the same singleton (no duplicate discovery loops)
- `ONODEController` refactored from `Task.Run(...).Result` deadlock pattern to static cached Task singleton
- `ONODEManager` now accepts injected `ONETManager`; `StartNodeAsync` calls `StartNetworkAsync`, `StopNodeAsync` calls `StopNetworkAsync`, peers/stats delegate to live ONET state
- Fixed operator-precedence bug across all 12 ONODEController action methods (`await await Task<T>.Method()` → `await (await Task<T>).Method()`)

### Phase 3 — Tests (commit f93bc4364)
- **`ONETManagerUnitTests.cs`** (13 tests): construction, DNA-priority over constructor param, HoloNET-without-provider error, keypair generation, idempotent re-init, unreachable bootstrap resilience, stats/start/stop
- **`ONODEManagerUnitTests.cs`** (17 tests): lifecycle (start/stop/restart, double-start error), DNA passthrough, config CRUD, real memory metric, logs, peers without ONET, onet_-prefixed stats, ONET delegation
- **`ONODEONETChainIntegrationTests`** (6 tests in `ONETIntegrationTests.cs`): full ONODE→ONET chain — StartNode starts ONET, StopNode stops ONET, stats merge, peers delegate, keypair persisted to DNA, networkType in stats
- **ONODE WebAPI TestHarness `Program.cs`**: replaced "Hello World" stub with full HTTP harness covering all ONODE and ONET endpoints; set `ONODE_BASE_URL` env var to point at any running instance
- Core unit test csproj: added FluentAssertions, fixed project reference paths

### Phase 5 — Hardening (commit 7e9e12bce)
- **Thread-safe peer tracking**: `_connectedNodes` changed to `ConcurrentDictionary<string, ONETNode>`
- **HttpClient timeout**: reduced 15s → 5s; max cold-path with 3 retries now ~21s not 45s+
- **TcpPort wired from DNA**: `ONETProtocol.ListenPort` now reads `ONETConfig.TcpPort`
- **`RestartNodeAsync` graceful**: skips `StopNodeAsync` when node is already stopped
- **API key auth**: `POST /onet/nodes/register` checks `X-ONET-API-Key` vs `ONETConfig.ONETApiKey`
- **`AutoRegisterOnBootstrap: false`** default in `OASIS_DNA.json` — no spurious bootstrap POSTs on new installs
- **GitHub Actions CI**: `.github/workflows/onet-onode-tests.yml` — triggers on ONODE/DNA path changes; runs unit + integration + WebAPI unit tests

### Phase 6 — CVE fix, peer persistence, auth tests (commit cfb81852f)
- **SQLite CVE cleared**: `SQLitePCLRaw.lib.e_sqlite3` bumped 2.1.11 → 2.1.12 (GHSA-2m69-gcr7-jv3q HIGH)
- **Peer persistence**: `PersistPeers()` / `LoadPersistedPeers()` — saves to `onet-peers.json` on stop, restores on start; warm restart with no rediscovery delay
- **`GetOASISDNAAsync` and `RegisterNodePublicKey` made `virtual`** — enables test subclass overrides
- **`protected ONETManager(OASISDNA?)` constructor** — test subclasses avoid full network init chain
- **7 API key auth unit tests**: `ONETControllerRegisterNodeTests` — open, correct key, wrong key, missing header, null/empty body

### Phase 7 — Controller lifecycle tests, two-manager demo, CPU fix (current)
- **CPU metric seeded at startup**: `_lastCpuSample`/`_lastCpuTime` set in `StartNodeAsync` so first `GetNodeMetricsAsync` returns a real delta not 0
- **Two-ONETManager discovery demo**: `RunONETTwoManagerDiscoveryAsync` in Core TestHarness — two in-process ONETManagers connect, broadcast, and report stats
- **ONETController lifecycle tests** (9 tests in `ONETControllerLifecycleTests`): singleton caching, all GET/POST endpoints, null-body 400 cases
- **ONODEController lifecycle tests** (12 tests in `ONODEControllerLifecycleTests`): singleton caching, all lifecycle endpoints, start/stop/restart, null-body 400 cases

### Phase 4 — Improvements (commit b7d4cca15)
- **Bootstrap retry**: 3 attempts with 2s→4s→8s exponential backoff in `RegisterWithBootstrapServersAsync`
- **Real CPU %**: `GetNodeMetricsAsync` now computes `ΔTotalProcessorTime / (Δwall × ProcessorCount) × 100` instead of hardcoded 0
- **HoloNET test harness**: `RunONETHoloNETModeAsync` added to `HoloOASIS.TestHarness/Program.cs` — exercises ONETManager in HoloNET mode; logs clearly when conductor isn't running rather than crashing

---

## Key Architecture Decisions

| Decision | Rationale |
|---|---|
| DNA takes priority over constructor `networkType` | Single source of truth; operator sets DNA, not code |
| Static cached Task singleton for both controllers | Avoids per-request reconstruction and `Task.Run(...).Result` deadlock |
| `ONODEController` shares `ONETManager` via `ONETController.GetOnetManagerStaticAsync` | Prevents two competing discovery loops |
| `P2PNetworkType.HoloNET` requires HoloOASIS provider | Conductor URL comes from `StorageProviders.HoloOASIS` DNA section, not ONET config |
| Bootstrap server self-registers on startup | Adds own public key to own registry so ECDSA-signed self-requests are verifiable |
| Bootstrap failures are non-fatal (warn only, with retry) | Community nodes shouldn't crash if bootstrap is temporarily unreachable |

---

## Files Changed (key files)

```
OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASISDNA.cs
OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json
ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/ONETDiscovery.cs
ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/ONETManager.cs
ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/ONODEManager.cs
ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/ONETController.cs
ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/ONODEController.cs
ONODE/NextGenSoftware.OASIS.API.ONODE.Core.UnitTests/.../ONETManagerUnitTests.cs  (new)
ONODE/NextGenSoftware.OASIS.API.ONODE.Core.UnitTests/.../ONODEManagerUnitTests.cs  (new)
ONODE/NextGenSoftware.OASIS.API.ONODE.Core.IntegrationTests/ONETIntegrationTests.cs
ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.TestHarness/Program.cs
Providers/Network/TestProjects/.../HoloOASIS.TestHarness/Program.cs
STAR ODK/NextGenSoftware.OASIS.STAR.CLI/Program.cs
```

---

## Outstanding / TODO

### Requires Your Action
- **System DNA key bytes** — `DNALoader` (external project, not in this repo) still has placeholder zero bytes. Run the DNA Generator (System mode → option 2), paste the byte arrays in, rebuild OASIS-Security, then generate `OASIS_DNA_SYSTEM.json`. Until done, system DNA is not secure in production.

### Future Work
- **HoloNET mode live test** — `NetworkType = "HoloNET"` path is wired and has a test harness scenario but needs a live Holochain conductor to exercise end-to-end. Run `Providers/Network/TestProjects/HoloOASIS.TestHarness` against a running conductor to verify.
- **Live multi-ONODE test across processes** — the in-process two-manager demo in `RunONETTwoManagerDiscoveryAsync` exercises the manager layer. A true cross-process test (two separate ONODE WebAPI processes talking to each other via the bootstrap server) still hasn't been done.

---

## How to Run Tests

```bash
# Unit tests
dotnet test "ONODE/NextGenSoftware.OASIS.API.ONODE.Core.UnitTests/NextGenSoftware.OASIS.API.ONODE.Core.Tests/NextGenSoftware.OASIS.API.ONODE.Core.Tests.csproj"

# Integration tests
dotnet test "ONODE/NextGenSoftware.OASIS.API.ONODE.Core.IntegrationTests/NextGenSoftware.OASIS.API.ONODE.Core.IntegrationTests.csproj"

# WebAPI test harness (requires running ONODE WebAPI)
set ONODE_BASE_URL=http://localhost:5000
dotnet run --project "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.TestHarness/NextGenSoftware.OASIS.API.ONODE.WebAPI.TestHarness.csproj"

# HoloNET mode test harness (requires running Holochain conductor)
dotnet run --project "Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.HoloOASIS.TestHarness/NextGenSoftware.OASIS.API.Providers.HoloOASIS.TestHarness.csproj"

# ONODE Core hands-on demo (two-node PING/PONG + NFT minting)
dotnet run --project "ONODE/NextGenSoftware.OASIS.API.ONODE.Core.TestHarness/NextGenSoftware.OASIS.API.ONODE.Core.TestHarness.csproj"
```

---

## STAR CLI ONET/ONODE Commands (already existed pre-session)

```bash
star onet   # ONET P2P network management commands
star onode  # ONODE management commands
```

These call the ONODE WebAPI REST endpoints. The backend wiring was what this session fixed.

## Bootstrap / Web4 API

- Bootstrap server: `https://api.web4.oasisomniverse.one`
- Register endpoint: `POST /api/v1/onet/nodes/register` `{ nodeId, publicKey, nodeAddress? }`
- Community ONODEs call this on startup; the server stores the public key for future ECDSA header verification
