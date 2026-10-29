# Current COSMIC ORM / CQL status

Updated 29 October 2026. Full programme remains incomplete. Start here after compaction.

## Workspace and priorities

- Work/build/test only `C:/Source/OASIS-cosmic`; parent/Core branch `work/cosmic-cql`.
- User priority: breadth first across the demo providers. Defer deeper migration/parity work while closing missing provider demos; retain verified HyperDrive V2 correctness work.
- Then ORM/CQL across MongoDB, SQLite, Neo4j, IPFS, Azure Cosmos, AWS, Oracle, SOLID and ActivityPub. Preserve Solana demos.
- Work only on red [parity cells](CQL_PARITY_CHECKLIST.md). No green hardening without a reproduced bug.
- Entity-selected All/element/numeric depth stays paused; large POCO/migrations tooling deferred.
- V1: document gaps only. No paid provisioning, repeated signup requests or colleague build chatter.

## Current phase: safe V2 lineage migration

- Latest: Shared **1599/1599**; native Mongo three-store **2/2** (empty/foreign SQLite replacement), Neo + SQLite **2/2** (existing replica, process-crash recovery). Programme incomplete.
- Preview v3 binds exact source receipt, frozen graph/transport and historical receiver intent. Time-limited host approval binds operator, stores, route and reviewed digest.
- SQLite/Mongo/Neo native record barriers block affected V2 mutations/replays/new claims while preserving unrelated writes/history. Held does not prove every in-flight delivery has stopped.
- Acquire-only coordinator reads all native receipts before acquiring absent scopes. Durable hash-verified original evidence precedes acquisition; no automatic expiry, compensation or replacement of evidence.
- Actual coordinator process exit after first native commit and fresh-process recovery pass over Neo + SQLite. WEB4 deployment/server/power restart, other crash boundaries and real transport loss remain unverified.
- Read-only delivery review scans a fresh bounded lineage window, verifies graph membership and historical acknowledgements, and retains lease/recovery digests, later affected work and other routes. It never claims/finishes/clears/relabels work.
- Replacement review classifies empty, matching original/new lineage, untracked data, foreign ownership and version/deletion/content conflicts. It checks exact native identity/selection/consistency and held owners before/after. Matching or empty observations grant no adoption/overwrite permission.
- SQLite matching-receiver lineage adoption now mutates heads atomically with a dedicated receipt; native1/1, SQLite fence2/2 and Shared1598 pass. No business-content transfer/source cutover or HTTP execution endpoint enabled. Native business CAS, durable final decision, historical-receipt admission and cutover/recovery remain red. Older type-only V2 routes and broader graph/multi-version/delete/provider-pair scope remain gates. [Detailed proof/history](V2_LINEAGE_MIGRATION.md).

Native mutation-start capability passes SQLite2/2, Mongo2/2 and Neo2/2. Exact prepared-start coordinator now passes native Mongo/Mongo/SQLite1/1, Neo/SQLite fresh1/1 and original nonempty-head failure recovery1/1; Neo multi-route regressions2/2. Shared1598/Core4462/DNA19/GraphQL1 retained. Nonempty-head canonical recovery bug fixed. Mongo adoption now passes3/3 including unchanged native V2 source lineage, absent receipt namespace, drift/rollback/reconnect/history. Mongo fence controls2/2 retained. No global cutover enabled; deeper migration gaps are deferred for demo breadth.

## Exact next step

1. MVP breadth first, depth afterward: inspect AWS DynamoDB native query capability and add its bounded multi-record CQL + Mongo federated C#/HTTP/browser demo admission, tests and docs. Reuse existing free/local environment; no paid provisioning.
2. After AWS: Ethereum, Polygon, Arbitrum, Google, Cosmos blockchain, Polkadot and NEAR, in that order; then other suitable existing popular providers. Each MVP needs a genuinely native bounded filtered multi-record query and a Mongo-paired demo with explicit limits; no full-parity gate. Reconcile existing Mongo/SQLite/Neo/IPFS/Azure emulator/Oracle/SOLID/ActivityPub/Solana demos; hosted Azure stays pending.
3. Park deeper migration work: SQLite and Mongo matching-replica adoption now have scoped native proofs; Neo adoption, empty-target transfer, source/outbox CAS, final decision and interrupted cutover remain red. No global cutover/100% claim.

## HyperDrive verification already retained

- Effective avatar routing, candidate-list isolation and preferred-authority error preservation have controlled/native coverage.
- Mongo/SQLite and Neo/SQLite each have 42 strategy reads; real Mongo outage -> SQLite failover covers sync/async/explicit reads.
- All three native providers persist destination catalogues and complete source/target/policy descriptors with graph/receipt/outbox commits.
- Cold writes, receiver metadata outages, hosted restart/idempotence and Neo -> SQLite native write-lock recovery have scoped proofs.
- Shipping ten-attempt native SQLite -> Mongo exhaustion retains terminal blocking after recovery; other ordered pairs remain open.
- [Routing acceptance](V2_ROUTING_ACCEPTANCE.md), [durable routes](DURABLE_ROUTE_DESCRIPTORS.md), [migration gates](V2_LINEAGE_MIGRATION.md) own detailed evidence. No universal uptime claim.

## Demo breadth and remaining limits

| Provider scope | Verified demo scope / remaining work |
|---|---|
| SQLite/Mongo/Neo | Original 12 investor rows green; grouped demos 681 SDK queries/138 HTTP requests/30 save checks. Full EF/NHibernate parity is red. |
| Oracle + Mongo | Native bounded membership, 5 rows/4 subset; C#/HTTP/browser. No OCI-hosting claim. [Proof](CQL_ORACLE_DEMO_ADMISSION.md). |
| Cosmos + Mongo | Emulator C#/HTTP/browser 5/4. Hosted Azure pending by user instruction: no free subscription/auth; do not provision or repeat signup requests. [Gate](CQL_HOSTED_AZURE_FREE_ADMISSION.md). |
| IPFS/SOLID/ActivityPub + Mongo | Native 3/2 bounded-key queries, paired SDK/HTTP/browser 5/4. Arbitrary content queries remain open. [Scope](CQL_PROTOCOL_MULTI_RECORD.md). |
| Solana + Mongo | Retained local validator + explicit SQLite index, C#/HTTP/browser. No hosted-chain/reseed claim. Original missing historical chunk has no known backup; preserve retained state. [Proof](CQL_MONGO_SOLANA_NATIVE_VERIFICATION.md). |
| AWS + Mongo | Native DynamoDB CQL admission and C#/HTTP/browser presets remain red. Storage support alone is insufficient. [Roadmap](CQL_DEMO_EXPANSION_ROADMAP.md). |

Three-database save demos cover dirty/no-op ownership, generated IDs, concurrency and per-save deltas within checklist scopes. Full history/value authorization, broad child/provider parity, placement, hosted durability/scaling and migration remain open.

## Git, resources and commands

- Parent Development c8a381dd2/Core0240771 integrated into task; current Core276b780. Core4462/DNA19/GraphQL1 and priority-provider/WEB4 compatibility builds pass. [Integration scope](DEVELOPMENT_SYNC_2026-10-28.md).
- Last outgoing Development batch: parent858edd828/Corebd86a37. Later work remains on the pushed task branch; no entire-branch deployment while the lineage gate is open.
- Commit named files only; Core push/remote verification first, then matching parent gitlink + deployment manifest. Fetch before scoped daily integration; never force push.
- Use one build lease and `/m:2 /nr:false /p:UseSharedCompilation=false`; reuse SDK10.0.401/cache/services. No new install/restore for these milestones.
- Logs/TRX/JSON go to worktree TestResults; persistent identities/pending journals to LocalState/COSMIC. Preserve failed/unknown outcomes; never replay to hide them.
- Latest successful fixture journals/files removed, owned Mongo/Neo services stopped. Reinspect live state before reuse. [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

```powershell
Scripts/TestHosts/Run-CosmicMongoDestinationTests.ps1 -MigrationPreflight
Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-neo -Operation { Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicSourceReceipt }
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Oracle -Federated -ShowPlan
```

[Manual](CQL_MANUAL.md) / [architecture](CQL_ARCHITECTURE.md) / [launcher](CQL_DEMO_LAUNCHER.md) / [previous status](Archive/CURRENT_STATUS_PRE_RECEIPT_PREFLIGHT_2026-10-29.md).
