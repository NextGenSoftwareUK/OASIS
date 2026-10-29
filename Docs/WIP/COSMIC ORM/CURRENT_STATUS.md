# Current COSMIC ORM / CQL status

Updated 29 October 2026. Full programme remains incomplete. Start here after compaction.

## Workspace and priorities

- Work/build/test only `C:/Source/OASIS-cosmic`; parent/Core branch `work/cosmic-cql`.
- First: HyperDrive V2 replication, failover and load balancing correctness.
- Then ORM/CQL across MongoDB, SQLite, Neo4j, IPFS, Azure Cosmos, AWS, Oracle, SOLID and ActivityPub. Preserve Solana demos.
- Work only on red [parity cells](CQL_PARITY_CHECKLIST.md). No green hardening without a reproduced bug.
- Entity-selected All/element/numeric depth stays paused; large POCO/migrations tooling deferred.
- V1: document gaps only. No paid provisioning, repeated signup requests or colleague build chatter.

## Current phase: safe V2 lineage migration

- Three native providers now expose explicit physical-source receipt verification without rewriting historical intents.
- Selected-record evidence binds original input, operation/actor, source/target identities, revisions and portable content; no business values returned.
- Shared read-only preflight compares that evidence with protected target heads. Missing/untracked records, lineage/version/deletion/content conflicts and insufficient evidence are distinct.
- NativeSnapshot is the target default. Neo targets require explicit ReadCommittedValidated; source and target reads are never a global snapshot.
- Retained Core4462/GraphQL1 compatibility; latest Shared1392/Neo619 controls pass. Native SQLite -> Mongo2, Neo/SQLite content/race3 and Mongo graph/head/receipt lifecycle2 pass.
- Reproduced/fixed false matches when current business content changed but revision tokens did not. All three V2 readers now validate the actual current body against the protected content. [Exact proof and limitations](V2_LINEAGE_MIGRATION.md).
- No migration writes, adoption/rebind or state clearing are enabled. Separate host-owned operator authorization is implemented; no migration HTTP endpoint is enabled. Older type-only V2 heads/obligations remain a deployment gate.

Complete-operation inventory now implemented on all3native providers: Core4462/Shared1404/GraphQL1, native SQLite2/Mongo1/Neo2 and preserved-failure reconciliation1 pass. Completed/later obligations and hidden lease changes are retained in redacted fingerprints. Neo exact-operation Unicode cursor fixed using its existing native range index; original failure reconciled without replay. The global route-head reader is now fixed too: Neo625/Shared1405/Core4462/GraphQL1 and native Neo2/SQLite2/Mongo1 pass; activation verifies the required target-order index. [Scope](V2_LINEAGE_MIGRATION.md).

Source-wide lineage inventory now passes Core4462/Shared1416/GraphQL1 and native SQLite2/Mongo1/Neo2. A later save is excluded from the captured window and visible in a new scan on all three sources. Earlier row state changes remain possible; fences and execution are still open.

Separate exact operator authorization now implemented in Shared (1430 tests pass). Empty host policy denies all; time-limited grants bind operator, stores, lineage, policies and reviewed preview digest. The read-only preview generator is now implemented; execution stays disabled.

Immutable preview now passes Shared1453/1453 plus native SQLite2/Mongo1/Neo2. It retains every observed route/state and binds selected-route receipt evidence to operator review. Missing original replica receipts, payload-to-receipt proof, replacement admission and native fences remain explicit findings. No partial preview on failed evidence.

Historical replica-receipt contract and shared existing-validator projection now implemented; all three native receivers are implemented. SQLite2cases retained; latest Mongo2/Neo replica-group6 pass, including original proof after later changes. Shared1462 controls retained. Preview integration is now implemented in v2 below.

Preview v2 now incorporates exact historical receiver evidence: Shared1473/1473 and native SQLite2/Mongo1/Neo-source2 pass. Committed/Absent/not-read remain distinct; scope and strict consistency enforced, evidence bound into review checksum. Payload/source equivalence, replacement admission and native fences remain open.

## Exact next step

1. Verify exact original frozen payload/source-receipt binding: selected graph, logical/source revisions and portable content. Incorporate that proof into the preview without relabeling historical bytes.
2. Validate replacement-target evidence and reauthorize the exact preview before mutation; implement native source/target write and delivery fences. A sequence window is not quiescence.
3. Implement per-store CAS, durable idempotent migration receipts, interrupted-cutover recovery and old-obligation disposition without rewriting historical evidence.
4. Complete remaining ordered-pair/concurrent-change/outage/cold-start acceptance, then resume broader priority-provider ORM/CQL parity.

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

- Parent Development c8a381dd2/Core0240771 integrated into task; current Core1709572. Core4462/DNA19/GraphQL1 and priority-provider/WEB4 compatibility builds pass. [Integration scope](DEVELOPMENT_SYNC_2026-10-28.md).
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
