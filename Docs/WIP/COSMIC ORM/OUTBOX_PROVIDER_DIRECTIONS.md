# Durable worker provider directions

Updated 19 October 2026. Native certification of selected explicit enrollment/worker paths; not provider-wide, automatic hosting or deployment acceptance.

| Source | Target | Evidence | Status |
|---|---|---|---|
| SQLite | MongoDB | Existing four-case SQLite/MongoDB suite, sync/async source enrollment | Verified checkpoint |
| MongoDB | SQLite | Same suite, native Mongo fenced outbox and durable completion inspection | Verified checkpoint |
| Neo4j | SQLite | Neo delivery suite: sync/async primary enrollment, dispatcher, native source completion | Verified |
| SQLite | Neo4j | Expanded six-case Neo delivery suite, including two reverse-direction cases | Verified |
| Neo4j | MongoDB | New selected four-case MongoDB/Neo4j suite | Verified |
| MongoDB | Neo4j | Same selected suite | Verified |

Each path uses actual atomic source enrollment, the source's `IHolonReplicationRecoveryOutbox`, the shared bounded dispatcher and an explicitly registered committed-graph target. The latest new cases exercise creation, updates, soft/hard deletion, source completion evidence and target receipt replay. MongoDB/Neo4j cases also check fresh-provider graph reads, exact audit/UTC/binary metadata and distinct native physical revision observations.

## Commands and ownership

- SQLite/MongoDB: `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1 -CosmicReplicaTransfers`.
- Both SQLite/Neo4j directions plus Neo lease/fault cases: `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableNeo4jTests.ps1 -CosmicOutboxDelivery` (six cases).
- Both MongoDB/Neo4j directions: `pwsh -NoProfile -File Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1 -CosmicNeo4jReplicas` (four cases).

The combined mode holds the existing Mongo lease/data directory and invokes the existing Neo runner while Mongo is owned and initialized. It then owns both environments until verification and cleanup finish. No parallel data directory, replacement container, runtime installation or download is needed. The Neo consumer's additional Mongo reference was restored using only the existing local NuGet cache.

## Failure evidence and remaining acceptance

The initial MongoDB/Neo4j case exposed [inconsistent public soft-delete visibility](SOFT_DELETE_READ_CONTRACT.md). Its successfully delivered source operations and native tombstones were preserved and archived by the explicitly reviewed one-time mode `-CosmicNeo4jReplicasReconcile`. That mode is evidence recovery for exact fixture IDs, not a routine reset or test setup. The normal mode always refuses a retained fixture.

The Mongo retained ID/key tombstone contract is now fixed at its native getter and linked-snapshot owners and the four MongoDB/Neo4j cases pass again with public tombstone reads asserted. Explicit discovery/CQL visibility remains open. Native hard-delete row counts distinguish physical absence from a public load error. Selected CRUD success does not certify every depth/traversal/mapping/history combination, topology or numeric metadata type. Pre-submission/process-loss recovery, authenticated reconciliation/resume, target/tenant/actor authorization, target-set versioning/backfill, automatic hosting, bounded queues/retention, broad provider certification, migration, placement, save deltas and CQL remain open.
