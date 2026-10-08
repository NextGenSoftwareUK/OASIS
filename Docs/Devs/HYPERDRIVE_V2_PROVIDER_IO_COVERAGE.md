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

### Four-provider real-runtime matrix

`Scripts/run_four_provider_hyperdrive_matrix.ps1` is the repeatable first provider matrix. It refuses to attach to pre-existing loopback services and owns and cleans up every runtime it starts:

| Provider | Isolated runtime | Executed evidence |
|---|---|---|
| MongoDBOASIS | Portable three-member MongoDB replica set | 43/43 transaction, replay, retry, election and abrupt-primary-termination tests |
| SQLLiteDBOASIS | Per-test temporary SQLite database | 3/3 activation and avatar/holon persistence round trips |
| IPFSOASIS | Pinned Kubo 0.43.1 daemon, offline and loopback-only | 1/1 activation and holon save/load round trip |
| EthereumOASIS | Ganache chain 31337 with deterministic development-only accounts | 3/3 contract deployment, activation validation, holon save/load and wrong-chain rejection |

The Ethereum run uses only free prefunded currency on a disposable local development chain. It never contacts Ethereum mainnet and cannot spend real gas. Evidence from the 2026-10-07 run is under `artifacts/four-provider-matrix`; `summary.json` records PASS and the TRX files retain individual assertions.

This matrix exposed two false-positive activation defects. IPFS previously treated construction of an HTTP client as successful activation without contacting a daemon. Ethereum did the same without validating the RPC endpoint, chain ID or contract bytecode, and its reflective Nethereum constructor lookup was incompatible with Nethereum 7. Both providers now fail activation unless the external system is actually reachable and correctly configured, allowing V2 failover to operate on truthful provider state.

### Expanded provider verification

The second expansion pass produced the following evidence on 2026-10-07:

| Provider | Result | Evidence and remaining boundary |
|---|---|---|
| Neo4jOASIS | PASS | Portable Neo4j Community 2026.08.1 on loopback; 5/5 activation, avatar and holon save/load, query and soft-delete tests passed with zero skips. |
| PolygonOASIS | PASS | Web3Core contract compiled and deployed to a disposable Ganache chain with chain ID 137; 2/2 activation, holon create/load/update/delete and unreachable-RPC rejection tests passed with zero skips. No real currency or public RPC was used. |
| ArbitrumOASIS Web3Core | PASS | Same contract deployed to a disposable Ganache chain with chain ID 42161; 2/2 activation, holon create/load and unreachable-RPC rejection tests passed with zero skips. It shares the now-proven mined-transaction CRUD implementation with Polygon. No real currency or public RPC was used. |
| RootstockOASIS | PASS | Web3Core contract deployed to a disposable Ganache chain with Rootstock mainnet chain ID 30; 2/2 activation, holon create/load/update/delete and unreachable-RPC rejection tests passed with zero skips. No real currency or public RPC was used. |
| SolanaOASIS | BUILD PASS; runtime pending | Activation now performs a bounded Solana RPC health request and cannot succeed merely because client objects were constructed. The provider builds successfully. A local Solana validator is not installed on the verification host, so transaction round trips are not claimed. |
| AzureCosmosDBOASIS | NOT VERIFIED | No Cosmos emulator or isolated Azure credentials are present. Construction of repositories is not accepted as runtime evidence. |
| AWSOASIS | PASS | Consolidated onto the official `AWSSDK.DynamoDBv2` implementation. Against DynamoDB Local, 2/2 activation, avatar CRUD, holon CRUD and search tests passed with zero skips. The former fabricated unsigned `/dynamodb/...` HTTP implementation is excluded from compilation. |
| GoogleCloudOASIS | NOT VERIFIED | The provider uses Google SDK clients, but no Firestore emulator/runtime is installed and the current multi-service activation path has not been proven against an isolated project. |

Both EVM providers now verify RPC reachability, chain identity and deployed bytecode before reporting active. The standalone Arbitrum provider has the same checks. Web3 mutations wait for successful mined receipts; holons use the correct contract collection; tuple results use Nethereum's typed struct/output mapping; and create/update/delete now mutate actual chain state. This prevents HyperDrive V2 from selecting a provider that only constructed a local client successfully or reporting success before persistence. `Web3CoreOASIS/package.json` no longer attempts to install Node as a project dependency, the Hardhat configuration no longer requires a production private key for local compilation, and all 148 Solidity sources compile on a clean dependency install.

The portable provider runner also validates cached ZIP archives before reuse and re-expands empty/corrupt runtime directories. This prevents an interrupted download from poisoning every later verification run.

These statuses are deliberately not rolled up as “all providers work.” V2 routing/failover/load-balancing/replication contracts are complete at the HyperDrive boundary, but a provider can participate only when its own activation and operations are truthful. Azure/Google/Solana still require their isolated runtimes before their provider rows can be promoted to real-runtime PASS.

### Railway OASISDNA mode configuration

Railway holds the complete DNA document in the `OASIS_DNA_JSON` service variable; the mode was not changed in a repository JSON file. The safe update procedure was:

1. read the existing variable from the exact Railway project, environment and WEB4 service;
2. parse it in memory;
3. set `OASIS.HyperDriveMode` to the exact value `OASISHyperDrive2`;
4. update `OASIS.OASISHyperDriveConfig` while preserving all unrelated keys and provider credentials;
5. serialize the complete document and pipe it directly to Railway's variable command through standard input;
6. verify the redeployed `/api/hyperdrive/mode`, `/config` and `/status` endpoints.

Secrets were neither printed nor committed. Changing `OASIS_DNA.json` locally would not change Railway because `OASIS_DNA_JSON` is the deployed source of truth. Likewise, changing only `OASIS.HyperDriveMode` is insufficient if `OASIS.OASISHyperDriveConfig` does not define the intended V2 failover, replication and load-balancing policy.

## Staging deployment evidence

Verified on 2026-10-07 against `https://web4-oasis-api-staging.up.railway.app`:

- Railway deployment `7519f239-6954-4125-b50c-8415f1673214` built the branch Docker context and started WEB4 with `HyperDriveMode = OASISHyperDrive2`.
- MongoDBOASIS activated as the default provider. Startup did not fall through to the configured SQLLiteDBOASIS failover provider.
- Authenticated `GET /api/hyperdrive/mode`, `/config`, and `/status` passed the checked-in `Scripts/verify_hyperdrive_web4_endpoints.ps1` contract. The effective configuration source was `OASIS.OASISHyperDriveConfig`; automatic failover, replication, and load balancing were all enabled; two providers were configured and MongoDBOASIS was active.
- Twenty concurrent authentications completed successfully through MongoDBOASIS with zero transport or OASIS errors. This exercises the deployed avatar read/authentication path under concurrent V2 routing.
- Controlled provider failure, latency selection, disabled-policy, explicit-provider, authoritative-not-found, mutation replication, and every-operation sync/async behavior remain covered by the disposable-provider test suite. Staging provider credentials were not intentionally broken because doing so would mutate shared environment configuration rather than produce isolated failure evidence.

Deployment also exposed and fixed two packaging/runtime defects before this evidence was accepted: broad `.railwayignore` patterns had removed production `TestData*` types, and Mongo startup did not accept an already-equivalent public-identity index under its legacy name. The latter now verifies the complete index invariant before accepting it; incompatible indexes still fail activation.

Production promotion was verified on 2026-10-07 after master merge `8c44e3d67` and Railway deployment `23b7ac3d-4665-4624-9e2f-cde58cf2644c`. MongoDBOASIS activated cleanly; authenticated mode/config/status returned `OASISHyperDrive2` with failover, replication, and load balancing enabled and two active providers; 20/20 concurrent production authentications succeeded with zero transport or OASIS errors.
