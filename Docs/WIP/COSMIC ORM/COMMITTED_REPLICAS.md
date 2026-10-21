# Committed holon replicas — receiver contract and verification

Updated 19 October 2026. Locally implemented receivers: MongoDB, canonical Neo4j and SQLite. Selected explicit outbox/worker paths now have native evidence for all six directed MongoDB/SQLite/Neo4j combinations with sync/async source writes; see the [direction matrix](OUTBOX_PROVIDER_DIRECTIONS.md). This does not certify automatic/default CRUD enrollment, every graph/history combination or deployment.

## Explicit application contract

Core exposes `ICommittedHolonReplicaStorageProvider` and `CommittedHolonReplicaRequest`. Callers supply the original stable source operation ID, a stable source installation identity, source provider authority, actor, committed operation kind, and committed selected graph/depth flags. This is a committed-replica intent, separate from a user edit. Receipt replay is scoped to immutable content/source intent; changing an operation's source, audit, lineage, topology or business content rejects reuse.

```csharp
var request = new CommittedHolonReplicaRequest(
    sourceOperationId, sourceInstallationId, sourceProviderType, actorId,
    ConditionalHolonOperation.Save, committedRoots,
    saveChildren: true, recursive: true, maxChildDepth: 0);
var applied = await replicaProvider.ApplyCommittedHolonReplicaAsync(request);
```

`committedRoots` must contain actual committed identities, positive logical versions, coherent previous-version IDs and source physical revision observations. Deleted events require committed versioned tombstones. The source identity is an explicit caller assertion, not authentication. Transport authorization, tenant binding and source enrollment belong to the delivery integration gates; this raw provider surface does not establish them.

The shared `CommittedHolonReplicaPlan` captures detached selected snapshots and binds the full committed logical content/audit. Provider-native keys/revision observations and the target's own provider metadata namespace are local observations. The receiver preserves logical version IDs/numbers/lineage and origin audit, owns its native key and physical revision, and retains its existing local metadata namespace. It does not run edit stamping at a replica, even with target history enabled. It does not mutate submitted committed source objects.

The existing conditional receipt format and public conditional-plan overloads remain available. `IAtomicHolonWritePlan` lets the same receipt topology/integrity owner validate both explicit intents; replica acknowledgements additionally must preserve committed logical content. This is not a second graph mutation path.

## Neo4j transaction and durable head ownership

The existing `SaveHolonSnapshotsAsync` transaction owner handles graph, native relationships, optional history, replica head and exact operation receipt together. Receipt lookup precedes state comparison. Exact replay after intervening changes or physical deletion returns the original acknowledgement without another mutation/commit. Commit uncertainty exposes `Neo4jReplicaReconciliationException` with operation/source identity and original cause; the installed driver exposes no native transaction ID, so none is invented.

A unique `CosmicReplicaHead.HolonId` record retains the source identity, last committed logical snapshot, target physical revision, and hard-deletion state. Its snapshot has a SHA256 integrity digest. The digest detects accidental corruption; it is not authentication against a native database writer. Activation now verifies **four** identity guarantees: holon, operation receipt, composite historical version, and replica head. Missing guarantees are provisioned/verified; duplicate data is not silently repaired.

Each selected native holon is locked in stable identity order, then its replica head is locked. All lineage/local-state comparisons precede graph preparation/archive writes. Existing targets without a replica head are rejected rather than silently adopted. A first event must be logical version one on an absent record. Subsequent events must advance exactly from the retained logical head and match the enrolled source. Missing intermediate events, a different source, raw local modification, unexpected local deletion/recreation or corrupted head produces an explicit error and rolls back the graph/head/receipt claim.

The retained physical anchor detects a local raw write even if it kept the same logical version ID. Hard deletion removes selected native rows/history according to the existing native contract but retains replica-head tombstone/lineage and the operation receipt. Old creates cannot silently resurrect deleted identity. An old exact receipt replay still returns its original acknowledgement without resurrecting data.

## Verified checkpoint

- Shared storage: **502 unit tests pass**, including twelve new replica-plan/receipt cases; the total includes concurrent contributors' cases.
- Neo4j: **117 unit and 163 native cases pass**, zero failures/skips. Four replica theory cases cover sync/async with target history off/on. The fourth schema uniqueness race verifies replica-head identity ownership.
- Core: **1,001 unit cases pass**. Both net10.0 and netstandard2.1 builds pass.

The four native replica sequences use real SQLite committed roots/children and real Neo4j transactions. They verify logical/audit preservation, out-of-order rejection and later ordered application, provider reconstruction and exact replay, changed-intent rejection, root-only soft/hard deletion with unchanged child snapshots, durable hard-delete heads and stale-create rejection, raw local-write conflicts (including unchanged logical IDs), duplicate competing workers, concurrent ordered events, corrupted returned acknowledgement rollback, and retained-head checksum rejection.

They also delegate a real commit and inject an application acknowledgement exception. Independent native receipt/graph inspection precedes exact replay, with one real commit total. This is not an actual wire-drop or process-kill test. Failed fixtures would be preserved; successful synthetic native rows/heads/receipts/history and the owned fixed-path SQLite source database are removed after normal lifecycle shutdown. See [verification](VERIFICATION.md#committed-replica-receiver-checkpoint--19-october-2026).

## Remaining gates

All three initial providers now implement the receiver as described below; MongoDB cross-provider native certification remains open. The hosted idempotent-target adapter/payload contract, direct CRUD atomic outbox enrollment, durable per-target scheduling/diagnostics, process-crash recovery and operator reconciliation remain open. No existing global replication loop is redirected to this capability implicitly.

Existing/versioned target adoption and bulk migration need an explicit authorized baseline/checkpoint contract; the current receiver deliberately rejects an unproven initial version greater than one. Primary hard-delete/recreate semantics, source handover, coherent historical graph restoration, encryption context, source/tenant authorization and retention/compaction also require implementation/certification. Source hard-delete followed by a new version-one creation is a new lineage and is rejected against the retained target tombstone. Legacy Hyperdrive remains documentation-only. Deltas and CQL remain later roadmap stages.

## SQLite receiver mapping prerequisite

SQLite now preserves complete common-holon persistent values outside its relational subset through a nonoverlapping canonical extension. Seventeen new cases pass within a 597-case native suite, and the four existing SQLite-to-Neo4j sequences now include custom keys, extra parents and origin metadata. This removes the mapping loss that would otherwise invalidate a faithful SQLite receiver. The receiver transaction/head implementation remains pending. See [SQLite mapping](SQLITE_PERSISTENT_VALUES.md).

## SQLite receiver now implemented

The [SQLite receiver](SQLITE_COMMITTED_REPLICAS.md) uses the existing IMMEDIATE graph transaction for replica heads, exact receipts and current records, with explicit commit/rollback/cleanup outcomes. Audit null/default presence and nested metadata codecs now preserve committed source content. Current verification: 616 SQLite native, 165 Neo4j native, 510 shared and 75 MongoDB unit cases pass. Six native cross-provider sequences cover both SQLite-to-Neo4j and Neo4j-to-SQLite. Earlier counts in this document are historical checkpoints. MongoDB cross-provider certification, outbox/hosted adapter/history and later roadmap stages remain open.

## MongoDB receiver now implemented

The [MongoDB receiver](MONGO_COMMITTED_REPLICAS.md) uses the existing snapshot/majority graph transaction for current rows, retained heads and exact receipts. Embedded audit precision and dropped portable fields are repaired, and native body validation precedes every replica mutation. Current Mongo evidence: 112 unit cases, four selected conditional/replica sequences and 56 full native cases pass. Two new sync/async sequences cover actual separate MongoDB source/target commits, ordering/replay/local conflicts, auxiliary-write rollback, retained tombstones and uncertain-commit reconciliation without another mutation. Cross-provider receiver directions and durable primary CRUD enrollment remain open; no existing global replication loop is implicitly redirected.

## SQLite/MongoDB cross-provider receiver checkpoint

All four sync/async and source/target direction combinations pass using actual source commits and fresh provider contexts. Native source and target values are compared to immutable acknowledgements before replay/deletion checks. This uncovered and corrected Mongo metadata precision/tree/binary codec loss, CLR implementation tags in acknowledgement comparison, and a provider conflict-code inconsistency. Original frozen intent fingerprints and existing receipt formats remain unchanged. See [codec evidence and remaining gates](MONGO_METADATA_CODEC.md). Neo4j/MongoDB directions, hosted adapter, primary atomic outbox and broader roadmap stages remain open.

## Hosted adapter checkpoint

The explicit [hosted committed graph delivery](HOSTED_COMMITTED_GRAPH_DELIVERY.md) payload and receiver adapter are implemented locally. Full shared verification passes 548 cases and all four actual SQLite/MongoDB native combinations pass through the adapter, with direct receipt replay and independent reader checks. This supersedes older statements above that the payload adapter and SQLite/MongoDB directions are entirely open. Ordinary CRUD registration, atomic primary/outbox enrollment, MongoDB/Neo4j directions and crash/security/operations gates remain open.
