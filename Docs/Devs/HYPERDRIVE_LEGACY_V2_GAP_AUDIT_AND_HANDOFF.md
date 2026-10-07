# HyperDrive Legacy/V2 Gap Audit and Implementation Handoff

**Audit date:** 2026-10-06  
**Repository/branch inspected:** `C:\Source\OASIS`, `Development`  
**Purpose:** Record the original gap audit and the verified implementation that closed it.

**Implementation status (2026-10-07):** Completed on `codex/hyperdrive-v2-gaps`. The checked-in coverage and evidence record is `Docs/Devs/HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md`.

**Exhaustive WEB4 follow-up (2026-10-07):** Work continues on
`codex/hyperdrive-dual-mode-route-matrix`. The HTTP action inventory is now a checked-in,
executable manifest rather than an estimate: 691 controller actions are classified in
`Docs/Devs/HYPERDRIVE_WEB4_ROUTE_MANIFEST.csv` and guarded by
`Scripts/verify_web4_hyperdrive_route_manifest.ps1`. Behavioral proof remains intentionally
layered at the shared manager/router boundary because hundreds of thin controller overloads
delegate to the same operations; route existence is not represented as storage-behavior proof.

**Implementation priority:** V2 is the only gap-closing target. Legacy behavior is retained
for compatibility and receives characterization tests/documentation only; newly discovered
Legacy limitations are recorded rather than repaired unless they prevent safe V2 operation or
mode isolation.

## Handoff prompt

Use this prompt in a new agent/session:

> Continue the HyperDrive Legacy/V2 work using `Docs/Devs/HYPERDRIVE_LEGACY_V2_GAP_AUDIT_AND_HANDOFF.md` as the source handoff. Read `AGENTS.md` and `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md` first. Verify every finding against the current Development branch, repair the configuration-authority and routing-coverage gaps at their root cause, add executable unit/integration tests, verify the authenticated WEB4 development HyperDrive endpoints, and document evidence. Do not change WEB5 version-aware `STARNETHolonId` load semantics and do not introduce fallback/shim paths.

## Executive conclusion

| Capability | Legacy | V2 | Audit conclusion |
|---|---|---|---|
| Auto-failover | Broadly implemented across Avatar/Holon operations with special-purpose failover lists | Implemented once in the central request router with quota checks and structured diagnostics | Legacy is the strongest live-capable implementation; V2 is structurally better but needs the configuration and coverage gaps below closed |
| Auto-replication | Implemented separately in selected Avatar, AvatarDetail, Holon, delete/save and wallet paths | Implemented centrally for mutation requests; can replicate inline or defer to hosted durable sync | Legacy is uneven and incomplete; V2 needs durable-pipeline proof before production activation |
| Auto-load-balancing | Partial, concentrated in selected Avatar/Holon save paths; frequently performs an additional write after the primary save | Central provider selection with multiple strategies | V2 contains the intended design, but configuration authority and real metric feedback are incomplete |

The inspected WEB4 operational DNA is configured with `HyperDriveMode: Legacy`. Therefore enabling individual V2-looking configuration fields does not make V2 the active routing path.

## Implemented resolution

- `OASIS.StorageProviders` is the sole effective Legacy authority; `OASIS.OASISHyperDriveConfig` is the sole effective V2 authority. Boot and mode changes atomically map the selected authority into `ProviderManager`.
- V2 configuration updates persist first, apply the runtime policy, and roll persistence back when runtime application is rejected.
- WEB4 `/mode`, `/config` and `/status` expose the effective source and actual runtime flags/lists; status counts registered and active providers.
- The unused duplicate `ProviderManagerNew`/`ProviderConfigurator` control plane was removed.
- Every routed provider outcome records latency/success/failure in the same `PerformanceMonitor` consumed by load-balancing selection.
- Failover distinguishes unavailable providers from authoritative misses: a null result wrapper or `IsError` fails over; a non-error null payload or empty collection is terminal.
- Ordinary WEB4 V2 mutations always replicate inline. They are never marked durably deferred without enrollment. Hosted Edge sync remains a separate transactionally enrolled path.
- Core tests: 307 discovered, 307 passed, zero skipped. Hosted sync/fan-out subset: 16/16. The real three-member Mongo replica-set gate passed 42/42 transaction tests plus 1/1 abrupt-primary/idempotency test. WEB4 build: zero errors.
- The checked-in public-manager manifest catalogs 815 public methods and intentionally classifies 655 provider-backed/delegating methods; its verifier fails on unreviewed source drift.
- WEB5 `STARNETHolonId` loading code was not changed.

## Important architecture invariants

1. `HyperDriveMode` decides whether manager methods use Legacy behavior or the V2 router.
2. WEB5 version-aware loads by `STARNETHolonId` are intentional. They identify a logical holon across versions and default to the latest version unless a version is requested. Do not replace them with direct version-specific Holon ID loads.
3. The normal default provider is the first configured provider in the applicable provider list; the inspected lists begin with `MongoDBOASIS`.
4. Provider routing must be request-scoped. V2 must not switch a singleton/global current provider merely to execute one request.
5. A successful primary mutation must not be reported as failed merely because a replica failed. Partial replication must be explicit and observable.
6. Do not hide a failed invariant with a retry, fallback, duplicate write, or alternate code path. Follow `AGENTS.md` and `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md`.

## Configuration observed during the audit

The ignored operational file below existed locally:

`ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`

Observed values:

```text
OASIS.HyperDriveMode                                      = Legacy
OASIS.StorageProviders.AutoFailOverEnabled                = true
OASIS.StorageProviders.AutoLoadBalanceEnabled             = true
OASIS.StorageProviders.AutoReplicationEnabled             = true
OASIS.OASISHyperDriveConfig.AutoFailoverEnabled           = true
OASIS.OASISHyperDriveConfig.AutoLoadBalancingEnabled      = true
OASIS.OASISHyperDriveConfig.AutoReplicationEnabled        = true
OASIS.OASISHyperDriveConfig.EnableHostedSync              = true
```

This file is ignored by Git. These values are evidence about the inspected workspace/configuration, not proof of the current Railway environment. The versioned default DNA uses Legacy mode and may use different feature flags.

## WEB4 endpoints and live verification

HyperDrive belongs to **WEB4**, not WEB5.

Development base URL:

```text
https://dev.api.web4.oasisomniverse.one
```

Verified routes:

```text
GET /api/hyperdrive/mode
GET /api/hyperdrive/config
GET /api/hyperdrive/status
```

On 2026-10-07 all three routes first returned HTTP `401` without a JWT, proving the expected authentication boundary. After the corrected Railway dependency pins deployed, an authorized development account captured and validated all three read-only responses at `2026-10-07T21:45:25Z` using `Scripts/verify_hyperdrive_web4_endpoints.ps1`. The token was neither printed nor persisted.

Authenticated development evidence:

| Runtime value | Captured value |
|---|---|
| Mode | `Legacy` |
| Effective source | `OASIS.StorageProviders` |
| Effective policy applied | `2026-10-07T21:45:07.4979993Z` |
| Auto-failover | `true` |
| Auto-replication | `false` |
| Auto-load-balancing | `true` |
| Registered / active providers | `14 / 1` |

The mode, config metadata and status response agreed on the effective mode/source and exposed the ordered failover, replication and load-balancing provider lists. The raw response artifact was captured locally at `artifacts/hyperdrive-v2-gap-evidence/web4-development-endpoints.json`.

Live verification procedure:

1. Authenticate against `POST https://dev.api.web4.oasisomniverse.one/api/avatar/authenticate` using an authorized test account.
2. Preserve the returned JWT outside source control and command output.
3. Call the three endpoints above with `Authorization: Bearer <JWT>`.
4. Capture `mode`, effective flags, provider lists, selected strategy, hosted-sync state and status/metrics.
5. Compare reported settings with the `ProviderManager` runtime state, not only `OASISHyperDriveConfigManager` output.
6. Perform controlled provider-failure tests using disposable test providers or an isolated environment. Do not intentionally break the shared development MongoDB service.

The read-only capture is automated by `Scripts/verify_hyperdrive_web4_endpoints.ps1`.
Set a short-lived JWT in `ONODE_JWT_TOKEN`; the script does not print or persist the token,
validates agreement between the effective mode/config/status responses, and writes the three
responses beneath `artifacts/hyperdrive-v2-gap-evidence/`.

Production base URL is documented elsewhere as:

```text
https://api.web4.oasisomniverse.one
```

Do not mutate production configuration as part of initial verification.

## Legacy implementation

### Boot/configuration wiring

`OASISBootLoader.Boot.cs` copies these values from `OASIS.StorageProviders` into `ProviderManager`:

- `IsAutoFailOverEnabled`
- `IsAutoFailOverLocalProvidersEnabled`
- `IsAutoLoadBalanceEnabled`
- `IsAutoReplicationEnabled`

`OASISBootLoader.Helpers.cs` populates the corresponding ordered provider lists from the same legacy DNA section.

Primary references:

- `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.Boot.cs`
- `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.Helpers.cs`
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.Failover.cs`
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.LoadBalance.cs`

### Legacy auto-failover

Confirmed behavior:

- Avatar and Holon managers contain explicit failover loops.
- There are separate ordered lists for ordinary operations, avatar login, email checks, username checks, OASIS system-account checks and local-provider failover.
- Holon saves generally fail over when the result is an error or null.
- Avatar behavior varies by overload and operation.

Known gaps:

- Logic is duplicated across managers and overloads.
- Failure predicates are inconsistent: `IsError`, `Result == null`, or both.
- A provider can return a non-error empty/not-found result without triggering another provider.
- Some Legacy paths temporarily change the global `CurrentStorageProvider`, which risks cross-request interference.
- Background recovery/retry work remains marked TODO in Avatar save code.
- Special authentication failover lists can differ from the general list, requiring separate validation.

### Legacy auto-replication

Confirmed behavior:

- Selected successful Avatar, AvatarDetail and Holon mutations are repeated across `AutoReplicationProviders`.
- Delete replication exists in selected Avatar/Holon paths.
- Wallet contains a separate HyperDrive save/replication path.
- Some Avatar operations expose a `waitForAutoReplicationResult` choice.

Known gaps:

- Replication is manager-specific rather than a guaranteed storage-layer invariant.
- Managers without explicit replication code do not automatically receive it.
- No universal Legacy retry/outbox mechanism guarantees eventual delivery.
- Reads do not reconcile conflicting replica versions.
- Sync, async and error/warning behavior varies among operations.
- A long replication list may substantially increase request latency when replication is inline.

### Legacy auto-load-balancing

Confirmed behavior:

- Provider selection algorithms exist in `ProviderManager.LoadBalance.cs`.
- Strategies include RoundRobin, WeightedRoundRobin, LeastConnections, Geographic, CostBased and Performance.
- Selected Avatar and Holon save paths invoke the selector.

Critical semantic limitation:

Legacy save code commonly performs the normal primary save first and then saves again to the selected load-balanced provider. That is an extra distribution/replication write, not true selection of the primary provider before the request.

Known gaps:

- Read operations are not generally load-balanced.
- Load-balancing references are concentrated in Avatar/Holon saves and provider-management code.
- Several strategies use default values when metrics, cost or geographic data are unavailable.
- The same data can be written twice when both replication and load-balancing are enabled.
- Error semantics for the second load-balanced write are not uniformly exposed.

## V2 implementation

Primary router:

`OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/OASISHyperDrive.cs`

The central V2 sequence is:

1. Check subscription quota.
2. Select an allowed provider.
3. Route the typed request to that provider.
4. If the provider returned no result wrapper or an error and failover is enabled, try the ordered failover providers.
5. If an ordinary WEB4 mutation succeeds and replication is enabled, replicate inline. Hosted Edge synchronization owns its separate durable transaction/fan-out path.
6. Record usage and attach structured diagnostics.

### V2 auto-failover

Implemented:

- Ordered attempts from the ProviderManager failover list.
- Primary provider is skipped during secondary attempts.
- Explicit-provider routes do not silently fail over.
- Quota enforcement and diagnostic attempt records.
- Predictive failover overrides without changing the global current provider.

Resolved semantics:

- A missing result wrapper or explicit provider error triggers failover.
- A non-error null payload is an authoritative not-found result; a non-error empty collection is an authoritative empty query. Neither fails over.
- `MaxRetryAttempts` does not retry ordinary non-idempotent WEB4 mutations. Durable hosted fan-out owns bounded retry/backoff after transactional enrollment.

### V2 auto-load-balancing

Implemented:

- Explicit preferred provider takes precedence.
- Disabled load balancing uses the current provider.
- Candidate filtering uses subscription constraints.
- Cost and intelligent selection exist.
- ProviderManager supplies RoundRobin, WeightedRoundRobin, LeastConnections, Geographic, CostBased and Performance strategies.
- Deterministic tie-breaking is present in several selectors.

Resolved behavior:

- Every synchronous and asynchronous provider execution records success/failure and latency in `ProviderManager.PerformanceMonitor`, the same instance used by load-balancing selectors.
- Cost and geographic values remain explicitly declared operator inputs; latency, error rate, uptime and overall score are live measured inputs.

### V2 auto-replication

Implemented:

- Successful mutation requests fan out to configured replica providers.
- The primary provider is skipped.
- Each attempt is represented in structured diagnostics.
- Partial failures are warnings and do not convert a successful primary mutation into a false total failure.
- `ReplicatorManager` delegates to the authoritative V2 pipeline.
- Both synchronous and asynchronous router variants exist.

Hosted-sync behavior:

- Ordinary WEB4 V2 mutations replicate inline regardless of `EnableHostedSync`; no unenrolled operation claims durable deferral.
- Edge sync calls use the hosted provider contract. MongoDB atomically writes the mutation, change feed, terminal operation/device sequence and fan-out record.
- Dispatcher tests verify leases, renewal, ordered batches and bounded retry/backoff; coordinator tests verify atomic acknowledgement/checkpoint commits and unchanged durable state on transport failure.

## Highest-priority defect: conflicting configuration authorities

There are currently two sets of flags and lists:

### Legacy/shared provider configuration

`OASIS.StorageProviders` contains:

- `AutoFailOverEnabled`
- `AutoReplicationEnabled`
- `AutoLoadBalanceEnabled`
- `AutoFailOverProviders`
- `AutoReplicationProviders`
- `AutoLoadBalanceProviders`

These are copied into the active `ProviderManager` by the boot loader.

### V2-specific configuration

`OASIS.OASISHyperDriveConfig` contains:

- `AutoFailoverEnabled`
- `AutoReplicationEnabled`
- `AutoLoadBalancingEnabled`
- `AutoFailoverProviders`
- `AutoReplicationProviders`
- `LoadBalancingProviders`

`OASISHyperDriveConfigManager` loads, updates and reports this object.

### Actual routing authority

`OASISHyperDrive` checks `ProviderManager.IsAutoFailOverEnabled`, `IsAutoReplicationEnabled`, `IsAutoLoadBalanceEnabled`, and the ProviderManager lists. During boot those are populated from `OASIS.StorageProviders`, not visibly from the parallel V2 flags/lists.

Consequences:

- WEB4 may report V2 configuration that does not control routing.
- Editing `/api/hyperdrive/config` may persist values without changing effective behavior.
- The two sections can contradict one another.
- Operators cannot safely infer runtime state from the configuration endpoint.

Required root-cause fix:

1. Define exactly one effective runtime configuration authority per mode.
2. Prefer one normalized runtime policy object consumed by both boot and routing.
3. If backwards-compatible DNA fields must remain, migrate/map them explicitly at boot and reject contradictory values or report the resolved source.
4. Make `/api/hyperdrive/config`, `/mode`, and `/status` return effective runtime values and their source.
5. Updating configuration must atomically update persistence and effective router state, or explicitly require/reveal a restart.
6. Add tests proving that every exposed flag/list changes actual routing behavior.

Do not fix this by checking both flag sets opportunistically. That creates two authorities and ambiguous precedence.

## Dormant duplicate provider-manager architecture

These files define another registry/configuration implementation:

- `Provider Management/ProviderManagerNew.cs`
- `Provider Management/ProviderConfigurator.cs`

No production references to `ProviderManagerNew` were found outside its own implementation during the audit. It has separate default-enabled flags and provider lists.

Required decision:

- Either integrate it as the single replacement through an intentional migration with tests, or remove/archive it.
- Do not leave it appearing production-ready while the runtime uses the original `ProviderManager`.

## V2 routing coverage

V2 mode checks/routing calls were found in 18 manager source files, primarily covering:

- Avatar load/save/delete/Karma
- Holon load/save/delete/load-all/metadata/parent traversal
- Search
- Wallet
- Shared OASIS manager helpers

There are approximately 90 manager-named source files, although many do not directly persist data and may delegate to Avatar/Holon. Do not interpret `18/90` as a feature coverage percentage.

Required coverage audit:

1. Enumerate every public manager method that performs provider I/O.
2. Record whether it routes through V2, delegates to a covered method, deliberately remains local, or bypasses V2 unintentionally.
3. Add a checked-in matrix with evidence.
4. Route missing provider I/O through the same typed V2 request boundary.
5. Verify sync and async overload parity.
6. Verify explicit-provider overloads do not unexpectedly fail over, balance or mutate global state.

## Existing tests and current test blocker

Relevant tests exist in:

`OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/HyperDrive/HyperDriveProviderExecutionTests.cs`

They include coverage for:

- Legacy Avatar/Holon replication
- Legacy AvatarDetail failover
- V2 failover enabled/disabled
- Ordered failover diagnostics
- V2 use-current-provider behavior when load balancing is disabled
- Ordered automatic replication
- Partial replication failure
- Replication disabled behavior
- Durable hosted replication deferral
- ReplicatorManager delegation
- Explicit-operation quota checks

At original audit time, tests could not be executed reliably. `dotnet test` exited without discovering output, and a direct build revealed missing NuGet artifacts including:

```text
xunit.analyzers.dll
xunit.analyzers.fixes.dll
Microsoft.TestPlatform test-host assemblies
```

The build ended with `CS0006` for missing xUnit analyzer assemblies. This restore/cache problem was repaired in the isolated implementation worktree; the current Core suite discovers and passes 307/307 tests.

Completed repair:

1. Repair/restore the NuGet dependency cache or lock-file inputs without committing machine-specific paths.
2. Run the focused HyperDrive tests and record discovered/passed/failed counts.
3. Run the full Core unit-test project.
4. Add integration tests with two deterministic in-memory/fake providers for failover, balancing and replication.
5. Add an integration test for the durable hosted replication path using its real command store boundary.

## Recommended implementation order

### Phase 1: make behavior observable and configuration truthful

- Establish the single effective configuration authority.
- Wire configuration updates into runtime state.
- Make status/config endpoints expose effective values and their source.
- Resolve or remove `ProviderManagerNew`/`ProviderConfigurator` duplication.
- Add configuration-to-routing contract tests.

### Phase 2: prove V2 primitives

- Repair test execution.
- Test failover for errors, null payloads and intentional not-found results.
- Feed route outcomes into `PerformanceMonitor`.
- Verify each load-balancing strategy with deterministic metrics.
- Verify inline replication diagnostics and partial failures.
- Verify hosted durable replication enrolment and eventual completion.

### Phase 3: close manager coverage

- Produce the provider-I/O coverage matrix.
- Move uncovered operations to the common typed V2 request boundary.
- Ensure sync/async and explicit-provider semantic parity.
- Eliminate global provider mutation from request-scoped routing.

### Phase 4: controlled WEB4 rollout

- Deploy to WEB4 development only.
- Authenticate and verify `/api/hyperdrive/mode`, `/config`, and `/status`.
- Start with deterministic test providers and hosted sync disabled.
- Test failover, distribution and replication separately.
- Enable hosted sync only after durable-pipeline evidence passes.
- Switch development to V2, run regression/soak tests, then follow the documented Development-to-master promotion process.

## Acceptance criteria

The gaps are closed only when all of the following are true:

- One documented configuration source controls effective V2 flags and ordered lists.
- WEB4 config/status endpoints report the actual effective runtime values.
- Changing an enabled flag demonstrably changes routing behavior in tests.
- All provider-backed public operations are catalogued and intentionally covered.
- V2 failover does not mutate global provider state.
- Not-found failover semantics are explicit and tested by operation type.
- Load-balancing decisions use recorded live metrics or explicitly declared static inputs.
- Replication never duplicates the primary provider.
- Partial replica failure is observable without falsifying primary success.
- Hosted replication durably enrolls each successful primary mutation before acknowledging deferral.
- Restart, retry and idempotency behavior are tested for hosted replication.
- Unit tests execute with non-zero discovery and pass.
- Authenticated WEB4 development endpoint results are captured as release evidence.
- WEB5 version-aware `STARNETHolonId` behavior remains unchanged.
- No unrelated user worktree changes are overwritten.

## Principal file map

| Concern | File |
|---|---|
| Mode switch and shared manager behavior | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASISManager.cs` |
| V2 central router | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/OASISHyperDrive.cs` |
| Active provider manager | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| Provider lists/failover configuration | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.Failover.cs` |
| Load-balancing algorithms | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.LoadBalance.cs` |
| Explicit replication manager | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/ReplicatorManager.cs` |
| V2 config manager | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/OASISHyperDriveConfigManager.cs` |
| V2 config schema | `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Configuration/OASISHyperDriveConfig.cs` |
| Boot flag wiring | `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.Boot.cs` |
| Boot provider-list wiring | `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.Helpers.cs` |
| Legacy Holon operations | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/HolonManager/` |
| Legacy Avatar operations | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AvatarManager/` |
| WEB4 HyperDrive API | `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/HyperDriveController*.cs` |
| Main behavioral tests | `OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/HyperDrive/HyperDriveProviderExecutionTests.cs` |
| Config-manager tests | `OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/HyperDrive/OASISHyperDriveConfigManagerTests.cs` |

## Final verification notes

- Authenticated WEB4 development mode/config/status evidence was captured successfully on 2026-10-07 after deployment commit `2a0da2228` repaired the Railway dependency pins.
- No provider was deliberately disabled on the shared development environment.
- The ignored local operational DNA cannot prove Railway environment-variable or mounted-file values.
- This audit made no production configuration changes.
