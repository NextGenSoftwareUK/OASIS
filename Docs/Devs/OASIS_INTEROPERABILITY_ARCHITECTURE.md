# OASIS Interoperability Architecture

## Overview
OASIS achieves multi-network interoperability by treating every blockchain, database, or storage system as a pluggable "provider" behind a consistent API surface. The .NET backend bootstraps providers at runtime from the `OASIS_DNA` configuration file, then routes all application requests through a set of managers that enforce auto-failover, replication, and load-balancing policies.

```
Client → ONODE Web API → Managers → Provider Manager → Selected Provider → Network/Database
```

Key concepts:
- **Providers** implement typed capabilities by inheriting from `IOASISProvider` and, where relevant, `IOASISStorageProvider`, `IOASISNETProvider`, `IOASISSmartContractProvider`, etc. (`OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Providers`)
- **Provider Manager** orchestrates registration, activation, failover lists, and load balancing for all providers (`ProviderManager.cs`)
- **Managers (AvatarManager, HolonManager, SearchManager, etc.)** consume the Provider Manager to execute domain-specific operations without worrying about the underlying network
- **OASIS DNA** supplies runtime configuration for provider credentials, replication rules, and failover strategy (`OASIS_DNA_mainnet.json`)
- **HyperDrive telemetry** feeds performance, latency, and cost metrics into provider selection routines (see `PerformanceMonitor` usage inside `ProviderManager`)

## Why This Architecture Exists
Developers needed one surface to talk to dozens of chains, databases, and messaging networks without re-writing business logic per stack. Each new integration was costing weeks and creating reliability gaps. The provider architecture solves four recurring pain points:

1. **Fragmented APIs:** Every chain and database exposes its own SDK and data model. OASIS wraps them behind the Holon/Avatar model so higher layers stay unchanged.
2. **Operational Fragility:** Single-provider solutions fail when RPC nodes go down or quotas are exceeded. Auto-failover and replication give every request multiple recovery paths.
3. **Vendor Lock-In:** By hot-swapping providers at runtime through configuration, teams can add/drop networks without shipping new binaries.
4. **Observability Debt:** HyperDrive metrics feed into routing decisions so latency/cost spikes trigger automatic rerouting instead of manual triage.

We chose a runtime configuration model (`OASIS_DNA`) so environments can promote providers gradually (dev → test → prod) without code forks. The Provider Manager centralizes policy enforcement, which keeps individual managers simple and focuses auditing on one layer.

## Design Principles
- **Abstraction over Duplication:** Managers manipulate Holons/Avatars, never raw network payloads. This keeps features consistent across chains.
- **Configurable, Not Hardcoded:** Everything about provider selection lives in DNA files or runtime APIs, allowing ops teams to react without redeploying.
- **Graceful Degradation:** Reads and writes should succeed even if a preferred chain is down. Failover lists and replication queues were built to uphold that SLA.
- **Progressive Enhancement:** Advanced routing (cost, latency-aware) is additive. If HyperDrive telemetry is missing, the system reverts to deterministic lists rather than failing.

## Why Providers Are Split by Interface
Storage, networking, smart contract compilation, and social messaging each carry unique lifecycle and security patterns. Splitting interfaces (`IOASISStorageProvider`, `IOASISNETProvider`, etc.) allows teams to:
- Grant least-privilege access (e.g., a storage provider cannot deploy contracts).
- Swap one capability (like smart contracts) without touching identity storage.
- Enable providers progressively—some deployments just need storage + IPFS, others need full smart contract access.

## Why Auto-Failover, Replication, and Load Balancing Matter
- **Failover:** Most blockchain nodes and hosted databases exhibit intermittent failures. The failover list means an outage is a latency blip, not a full incident.
- **Replication:** Compliance and archival workloads require immutable logs while UIs demand low latency. Replication lets MongoDB serve fast reads while Arbitrum records proofs.
- **Load Balancing:** Gas prices, throughput, and latency shift hourly. Intelligent selection prevents runaway costs and keeps UX responsive without engineers manually retuning endpoints.

## Operational Benefits
- **Reduced Time-to-Integrate:** Adding a new network usually means writing one provider class and updating DNA, not refactoring every service.
- **Structured Rollouts:** Deployments can activate a provider in read-only mode, observe metrics, then promote it to primary by editing config.
- **Auditability:** Because all provider decisions funnel through the Provider Manager, logging and audits capture why each call went to a given network.
- **Future Proofing:** The Holon abstraction preserves data independence. As new storage paradigms emerge, they can plug into the same schema without breaking clients.

## Trade-Offs & Considerations
- **Complexity Concentration:** Provider Manager is intentionally opinionated. Teams should invest in automated tests around provider activation and routing.
- **Consistency Lag:** When replication spans slow chains, write consistency becomes eventual. Critical workflows should read from the primary provider until confirmations arrive.
- **Secret Management:** DNA files carry sensitive credentials. Use environment overrides or secret stores in production deployments.
- **Telemetry Dependency:** Advanced load balancing is only as strong as the HyperDrive metrics pipeline. Ensure those collectors are monitored.

## Boot & Registration Flow
1. **Boot Loader** reads `OASIS_DNA` and sets up logging, error handling, and provider boot type (`OASISBootLoader.cs`).
2. Providers listed under `StorageProviders` are instantiated and registered with the Provider Manager. Example: MongoDB, Arbitrum, Ethereum, Solana, Pinata.
3. Default global provider, auto-failover, replication, and load-balancing lists are populated from `OASIS_DNA`.
4. ONODE Web API lazily resolves managers (e.g., `AvatarManager`) which in turn request an activated storage provider from the Provider Manager before serving requests (`Program.cs`).

## Request Handling
1. A client hits an endpoint on `NextGenSoftware.OASIS.API.ONODE.WebAPI`.
2. Middleware resolves the appropriate manager (Avatar, Holon, Search, Wallet, etc.).
3. The manager asks Provider Manager to set or activate the best provider for this call (respecting any overrides provided via query parameters).
4. Provider Manager:
   - Ensures the provider is registered and activated (`ActivateProvider`, `ActivateProviderAsync`).
   - Applies **auto-failover**: if a provider throws or times out, entries in `_providerAutoFailOverList` are tried in order.
   - Applies **auto-load balancing**: for read/write-heavy workloads, `SelectOptimalProviderForLoadBalancing` evaluates latency, throughput, cost, and regional metrics before dispatching to the target provider.
   - Applies **auto-replication**: where enabled, the same write is fanned out to additional providers listed in `_providersThatAreAutoReplicating`.
5. The provider implementation translates OASIS domain objects (Avatars, Holons, metadata) into network-specific operations (EVM transactions, Mongo CRUD, IPFS pins, etc.).

## Provider Types & Abstraction
- `IOASISProvider`: base metadata, activation lifecycle.
- `IOASISStorageProvider`: CRUD for avatars, holons, search APIs; exposes import/export for cross-silo sync.
- `IOASISNETProvider`: networking-style capabilities.
- `IOASISSmartContractProvider`: smart contract compilation/deployment hooks (used by the Smart Contract Generator).

Each concrete provider packages the native SDK and adapts it to the shared model. Example: `MongoDBOASIS` persists avatars and holons to MongoDB, while `EthereumOASIS` turns save/delete operations into on-chain calls.

## Failover, Replication, Load Balancing
- **Failover**: `_providerAutoFailOverList` and related lists are populated from DNA (`AutoFailOverProviders`). Managers call into Provider Manager methods like `SetAutoFailOverForProviders` to append new networks dynamically.
- **Replication**: `_providersThatAreAutoReplicating` determines which providers receive mirrored writes. Use `SetAutoReplicationForProviders` to add/remove entries at runtime.
- **Load Balancing**: multiple strategies implemented (round robin, weighted, least connections, geographic, cost-based, intelligent). Strategies use HyperDrive performance telemetry to route requests (`SelectPerformanceBasedProvider`, `SelectWeightedRoundRobinProvider`, etc.).

## Data Model (Holons & Avatars)
- **Avatar**: user identity object (credentials, profile) handled through `IOASISStorageProvider` methods such as `LoadAvatar`, `SaveAvatar`, and deletion routines.
- **Holon**: generic data container that allows nested structures, metadata queries, and cross-provider import/export. Methods like `SaveHolon`, `LoadHolon`, `LoadHolonsByMetaData` ensure the same schema works across MongoDB, Holochain, EVM, etc.

## Extending the Architecture
1. **Implement a new provider** by inheriting from the relevant interfaces and registering it through `ProviderManager.RegisterProvider` or OASIS DNA entries.
2. **Configure** credentials and boot behavior in `OASIS_DNA_[env].json`.
3. **Enable auto features** (replication, load balancing, failover) by adding the new provider type to the relevant lists (either config or runtime APIs).
4. Optionally update HyperDrive telemetry collectors to feed metrics for the new provider so it participates in intelligent routing.

## Interoperability Summary
- Single API (`/api/*`) surfaces multi-chain, multi-database operations.
- Provider abstraction ensures all managers work identically regardless of the backing network.
- OASIS DNA + Provider Manager deliver runtime control over where reads/writes land, how they fail over, and how they replicate.
- HyperDrive telemetry makes provider selection adaptive instead of static.
- Developers consume the same REST/GraphQL/websocket surfaces (or SDKs) while OASIS handles provider orchestration transparently.
