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

- Native SQLite/Mongo/Neo source and receiver reads verify exact physical bindings, original receipts, frozen payloads and current protected heads. Changed current bodies cannot pass on unchanged tokens alone.
- Bounded source-wide inventory retains completed/later obligations; a sequence cutoff excludes later appends but does not freeze existing state or prove quiescence.
- Preview v3 binds original source save -> frozen queued graph/revisions/content -> exact historical receiver intent, with explicit consistency and complete coverage checks. No partial result on failed evidence.
- Separate host-owned, time-limited administrative grants bind the exact operator, stores, route, policy and reviewed preview digest. Owning a record grants no migration permission.
- Latest Shared1497/1497; native SQLite2/Mongo1/Neo-source2 pass combined production preview cases. Native creates cover one record/four obligations; unit graph proof covers two records. Broader native graph/delete/race scope remains open.
- Missing proof and receiver absence remain findings. Replacement admission, native fences, CAS/idempotent receipts and cutover remain red. No migration writes/state clearing or HTTP execution endpoint enabled.
- Older type-only V2 heads/obligations remain a deployment gate. [Evidence, commands and limits](V2_LINEAGE_MIGRATION.md).

First SQLite native record barrier now passes Shared1506/1506 and native2 race/restart cases. It blocks affected V2 save/delete/replica replay and new claims; unrelated records and historical reads remain available. Mongo/Neo, paired authorization/quiescence and migration mutation remain open.

## Exact next step

1. Implement Mongo/Neo equivalents of the verified SQLite record barrier, then pair the physical-store fences under exact preview/operator admission. Held is not quiescent: reconcile existing in-flight/unknown outcomes; no automatic expiry or post-mutation cancellation.
2. Validate replacement physical identity/content under fences; compare-and-swap exact graph/head/receipt/obligation observations with durable per-store migration receipts.
3. Implement idempotent cutover/recovery and old-obligation disposition without changing historical evidence; expand native graph/delete/race and ordered-pair acceptance alongside it.
4. Complete remaining outage/cold-start acceptance before resuming broader priority-provider ORM/CQL parity.

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

- Parent Development c8a381dd2/Core0240771 integrated into task; current Coree5bcf46. Core4462/DNA19/GraphQL1 and priority-provider/WEB4 compatibility builds pass. [Integration scope](DEVELOPMENT_SYNC_2026-10-28.md).
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
