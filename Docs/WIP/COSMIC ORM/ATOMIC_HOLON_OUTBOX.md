# Atomic primary holon outbox

Updated 19 October 2026. Locally implemented for SQLite, [MongoDB primary enrollment](MONGO_PRIMARY_OUTBOX.md) and [Neo4j primary enrollment](NEO4J_PRIMARY_OUTBOX.md); programme acceptance remains open.

## Invariant and ownership

For an explicitly enrolled conditional save/delete, the authoritative graph, exact operation receipt and one immutable delivery obligation per target must commit together. A failed insert into any target obligation or the receipt rolls back the graph and all earlier obligation inserts. Delivery is not enqueued after commit.

`IAtomicHolonReplicationStorageProvider` is a separate capability from conditional writes. `HolonReplicationEnrollment` freezes a source identity and distinct stable target identities, sorted with ordinal comparison. `CosmicWriteOptions` carries enrollment through HolonSavePlan and V2. The selected authority is pinned. V2 checks atomic-outbox capability before activation/dispatch and returns `COSMIC_ATOMIC_REPLICATION_UNSUPPORTED` when it is absent; it does not invoke plain writes, failover or best-effort replication instead. The original public constructor signatures remain available for existing binary consumers.

SQLite uses its existing IMMEDIATE graph transaction. Once the exact acknowledged graph is materialized, it captures the [versioned graph payload](HOSTED_COMMITTED_GRAPH_DELIVERY.md) and inserts `CosmicHolonOutbox` rows before the exact receipt and COMMIT. Each row contains sequence, operation/target identity, source/authority/actor, frozen payload, SHA256 and UTC creation time. The unique operation/target key prevents duplicate obligations. Replication enrollment is included in the operation fingerprint; an operation cannot be replayed with a changed source, changed target set or plain-write semantics. Reordering the same target set is equivalent. Existing unenrolled operation fingerprint bytes remain unchanged.

The direct explicit provider API is `ExecuteReplicatingHolonWrite` / `ExecuteReplicatingHolonWriteAsync`. SQLite also honors enrollment carried in a conditional request. Disagreeing explicit and embedded enrollment is rejected before I/O. Other shared conditional transaction owners reject required enrollment rather than silently dropping it.

## Manager example

```csharp
var enrollment = new HolonReplicationEnrollment("application-primary", new[] { "mongo-eu", "neo4j-eu" });
var options = new CosmicWriteOptions(operationId, expectedRevisions: null,
    replicationEnrollment: enrollment);
var result = await manager.SaveHolonAsync(holon, actorId, options,
    saveChildren: true, providerType: ProviderType.SQLLiteDBOASIS);
```

Existing accepted provider-specific revisions supply update conditions; new objects use create-only conditions. No-op saves create no obligations. Source/target identity strings are enrollment identifiers, not proof of authorization. Applications still need the authorized runtime target catalogue and host/tenant checks described below.

## Schema and recovery boundaries

Migration `20261019233000_AddCosmicHolonOutbox` creates the raw transaction-owned table. The existing EnsureCreated schema-upgrade owner uses the same declaration. Downgrade refuses to discard any retained obligation. The EF model is unchanged because this table is owned through native commands, like the existing receipt and replica-head tables.

An obligation persists independently of later edits, deletion of the source record and acknowledgement replay. Its payload is the original committed revision, including versioned soft/hard-delete tombstones. This is durable enrollment, not delivery completion. The exact original operation receipt remains the authority after an uncertain commit; do not repeat a user edit to reconstruct a payload.

## Verification and remaining work

Native tests cover sync/async manager save/publication, no-op behavior, source/target fingerprint binding, frozen selected child graphs, exact receipt replay, two competing clients, ordered logical versions, deletion tombstones, failure of a later target insert, failure of the receipt insert and migration downgrade protection. Final verification: **1,012 Core unit, 549 shared unit and 633 SQLite native tests pass**, zero failures/skips. The SQLite suite includes seventeen new atomic-enrollment cases, covering manager publication/rollback and pre-cancellation as well as transaction/replay/migration cases. Core builds pass for net10.0 and netstandard2.1. Full current hashes are in [verification](VERIFICATION.md).

Remaining: automatic hosting for the SQLite/MongoDB/Neo4j fenced workers; authorized target resolution and source/tenant/encryption context; bounded payload/queue limits, ordering and retention; actual restart/process/wire-loss recovery and operator reconciliation. The hosted receiver adapter is available but an explicit [bounded dispatcher](HOLON_OUTBOX_DISPATCHER.md) now consumes this table and native SQLite-to-MongoDB delivery passes; automatic host registration/scheduling remains open. Existing default/global auto-replication is not automatically converted to durable mode. Existing source revisions above version one require authorized baseline/backfill enrollment before delivery to an empty target. Legacy Hyperdrive remains documentation-only.

## Concurrent workstream boundary

The separate provider/Hyperdrive programme is tracked in [HyperDrive-Providers](../HyperDrive-Providers/README.md). Its current release gates and handoff were reviewed after the user identified the concurrent session. Both the main checkout and Core submodule are on Development. This COSMIC batch changes shared ORM contracts, V2 capability dispatch and SQLite transaction/schema code; it does not integrate or publish unrelated Oracle/OCI/provider work. Concurrent edits must be preserved, and dirty-worktree test evidence is not clean-source/deployment certification. No commit, branch creation, push or deployment was performed by this COSMIC work.
