# HyperDrive V2 provider-I/O coverage

**Verified:** 2026-10-07  
**Branch:** `codex/hyperdrive-v2-gaps`

## Routing contract

Provider-backed public manager APIs use one of two intentional boundaries:

1. Avatar, AvatarDetail, Holon, Search and Wallet persistence enter `OASISHyperDrive` through a typed request when `HyperDriveMode == OASISHyperDrive2`.
2. Feature managers persist through an injected `HolonManager`/`AvatarManager`; they do not select or mutate a provider themselves and therefore inherit boundary 1.

An explicit-provider request is request-scoped. It calls the selected registered provider directly through the router and does not change `ProviderManager.CurrentStorageProvider`. Automatic failover and balancing apply only to non-explicit routes. The dedicated Edge/hosted synchronization API is a separate transactional boundary: its provider transaction writes the canonical mutation, change feed and fan-out record atomically.

## Public operation matrix

The human-readable surface matrix below is backed by the exhaustive checked-in
[`HYPERDRIVE_V2_PUBLIC_MANAGER_METHOD_MANIFEST.csv`](HYPERDRIVE_V2_PUBLIC_MANAGER_METHOD_MANIFEST.csv).
It contains all **815** public method declarations under `API.Core/Managers`, including
overloads, and classifies **655** provider-backed/delegating methods by their intentional
boundary. `scripts/verify_hyperdrive_provider_io_manifest.ps1` reparses the source and fails
when a method is added, removed, or has its signature changed without a manifest review.

The WEB4 HTTP surface is independently catalogued in
[`HYPERDRIVE_WEB4_ROUTE_MANIFEST.csv`](HYPERDRIVE_WEB4_ROUTE_MANIFEST.csv). It currently
contains all **691** `[HttpGet|Post|Put|Patch|Delete]` controller actions and records whether
each action enters a mode-aware manager, delegates to another reviewed action/manager,
operates the HyperDrive/provider control plane, targets an explicit external/provider-native
boundary, or is local/non-storage. `Scripts/verify_web4_hyperdrive_route_manifest.ps1`
reparses the controller source and fails when the action surface drifts. This closes the
route-to-manager inventory gap without pretending that a Swagger request made with `{}` is
an end-to-end business assertion.

| Public API surface | Provider-backed operations | V2 path | Sync/async and explicit-provider disposition | Evidence |
|---|---|---|---|---|
| `AvatarManager` | load by ID/username/email/keys/tokens, load all, save, delete, AvatarDetail, karma, inventory | Direct typed `StorageOperationRequest`/avatar requests | Sync and async route through their corresponding V2 helpers; explicit provider remains request-scoped | `Managers/AvatarManager/AvatarManager-Load*.cs`, `AvatarManager-Save*.cs`, `AvatarManager-Delete*.cs`, `AvatarManager-Karma.cs`, `AvatarManager-Inventory.cs` |
| `HolonManager` | load by ID/provider key, load all, metadata queries, parent/child traversal, save, delete, settings | Direct typed `StorageOperationRequest` | Sync and async parity is exercised by provider-execution tests; explicit provider does not use automatic failover | `Managers/HolonManager/*.cs` |
| `SearchManager` | search holons/avatars | Direct typed search operation | Provider argument is carried in the request; empty result sets are terminal, successful results | `Managers/SearchManager.cs` |
| `WalletManager` | provider-wallet persistence stored with avatars plus provider-native wallet/token calls | Avatar persistence uses the V2 boundary; native chain calls deliberately execute on the explicitly requested wallet provider | Explicit wallet-provider selection is part of the wallet contract and does not change global storage state | `Managers/WalletManager*.cs` |
| `KeyManager` | avatar keys and wallet key material | Delegates avatar/provider-wallet storage to injected Avatar/Wallet managers | Both ID and username overloads use the injected runtime | `Managers/KeyManager*.cs` |
| `MessagingManager` | canonical messages and read state | Delegates to injected HolonManager | Async-only durable feature API; provider failures remain errors | `Managers/MessagingManager.cs` |
| `ChatManager` | sessions, messages, chat statistics | Delegates to injected HolonManager | Async-only feature API | `Managers/ChatManager.cs` |
| `CompetitionManager` | leaderboards, tournaments, enrollment | Delegates to injected HolonManager settings | Async-only feature API; load-before-create accepts authoritative not-found | `Managers/CompetitionManager.cs` |
| `EggsManager` | egg inventory and lifecycle | Delegates to injected HolonManager settings | Async-only feature API | `Managers/EggsManager.cs` |
| `GiftsManager` | canonical gifts and lifecycle/history | Delegates to injected HolonManager | Async-only feature API | `Managers/GiftsManager.cs` |
| `FilesManager` | canonical file metadata | Delegates to injected HolonManager; binary transport remains the selected file provider's responsibility | Async feature API | `Managers/FilesManager.cs` |
| `SeedsManager` | SEEDS records and state | Delegates to injected HolonManager or explicitly selected SEEDS provider for provider-native operations | Explicit provider-native calls are not storage load balancing | `Managers/SeedsManager*.cs` |
| `ClanManager`, `MissionManager`, `QuestManager`, `MapManager`, `GeoNFTManager`, `NFTManager` | domain holons and NFT/geo records | Delegate to Holon/Avatar managers for storage; blockchain/NFT execution is explicitly provider-targeted | Storage inherits V2; chain-side actions intentionally do not fail over as generic storage operations | corresponding manager sources |
| `ProviderManager` | registration, activation, health and routing policy | Local runtime control plane, not persisted domain data | Global mutation is limited to administrator configuration/boot, never a routed request | `Managers/OASIS HyperDrive/Provider Management/ProviderManager*.cs` |
| HyperDrive sync managers | outbox exchange, checkpoint, commands, change feed, fan-out | Dedicated transactional synchronization contracts | Provider transaction owns atomic enrollment; dispatcher owns leases, bounded retry and ordered completion | `Managers/OASIS HyperDrive/Synchronization/*.cs`; Mongo `MongoDBOASIS.HyperDriveSync.cs` |
| Analytics/AI/bridge orchestration | metrics, recommendations, chain bridge calls | Local analytics state or explicitly targeted external provider | Deliberately outside generic storage routing | `Managers/OASIS HyperDrive/*.cs`, `Managers/Bridge/*.cs` |

## Not-found and failure semantics

- A null `OASISResult` wrapper is a provider failure and is eligible for automatic failover.
- `IsError == true` is a provider failure and is eligible for automatic failover.
- A non-error result whose payload is null is the existing authoritative **not found** response and is terminal. This is required for safe load-before-create workflows.
- A non-error empty collection is an authoritative empty query and is terminal.
- Explicit-provider calls never silently fail over.

These rules are executable in `HyperDriveProviderExecutionTests`, including provider-error recovery, disabled failover, explicit-provider behavior, authoritative null payload, and empty collection behavior.

## Replication boundaries

- Ordinary WEB4 V2 mutations replicate inline. `EnableHostedSync` no longer causes those operations to claim false durable deferral.
- Edge synchronization mutations use `IHostedHyperDriveProvider.ApplyOperationsAsync`. MongoDB commits the entity mutation, change feed, device sequence/terminal operation and fan-out record in one transaction.
- `HostedHyperDriveFanOutDispatcher` provides exclusive leases, lease renewal, ordered batch handling and bounded retry/backoff. Failed work remains durable for a later worker/restart.
- Replica failures are warnings with structured attempt diagnostics; they never turn a successful primary mutation into a false primary failure.

## Static audit procedure

The matrix was checked by searching `Managers/**/*.cs` for direct provider method calls and classifying every result as:

- central V2 request routing;
- an explicit provider-native action;
- a feature-manager delegation to Avatar/Holon/Wallet;
- or local, non-provider state.

The former duplicate `ProviderManagerNew` and `ProviderConfigurator` implementation had no production references and was removed. `ProviderManager` is now the sole runtime policy owner.

Repeatable gate:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify_hyperdrive_provider_io_manifest.ps1
powershell -ExecutionPolicy Bypass -File scripts/verify_web4_hyperdrive_route_manifest.ps1
powershell -ExecutionPolicy Bypass -File scripts/verify_hyperdrive_v2_operation_contracts.ps1
```

Verified result on 2026-10-07: `815 public manager methods; 655 provider-backed/delegating methods intentionally classified`.
Verified WEB4 result on 2026-10-07: `691 controller actions; 597 non-local actions intentionally classified`.
Verified V2 router result on 2026-10-07: all `42` storage operations have synchronous/asynchronous handler parity and an executable provider-boundary contract.

## Executable evidence

- `HyperDriveProviderExecutionTests`: includes direct config-flag-to-routing and live-latency-to-next-selection contract tests.
- HyperDrive-filtered Core contract run on 2026-10-07: 298/298 passing, zero skipped. This includes disposable-provider assertions for Legacy failover, replication and load-balancing as characterization only, plus executable provider-boundary coverage for every V2 storage operation.
- Complete Core unit-test assembly on 2026-10-07: 309/309 passing, zero skipped; TRX captured locally at `artifacts/hyperdrive-dual-mode/hyperdrive-dual-mode-full.trx`.
- Hosted sync coordinator and fan-out subset: 16/16 passing.
- Real isolated three-member MongoDB replica-set evidence: 42/42 transaction/replay/retry tests plus 1/1 abrupt-primary-termination/idempotency test. The repeatable runner is `scripts/run_hosted_mongo_release_evidence.ps1`; TRX output is written beneath the selected artifacts directory.
- WEB4 Release build: succeeded with zero errors (866 pre-existing warnings reported by the compiler).
