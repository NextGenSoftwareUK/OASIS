# Durable COSMIC CRUD delivery — implementation contract and gates

Updated 19 October 2026. Working design and acceptance contract; **generic direct CRUD outbox enrollment is not implemented**. This extends the active COSMIC roadmap rather than redefining completion around hosted sync. Hyperdrive changes target V2; legacy findings remain documentation-only.

## Current authoritative boundaries

- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/OASISHyperDrive.cs` replicates successful ordinary mutations after primary completion. Conditional command replication is explicitly rejected. The post-commit loop is not an atomic durable-delivery guarantee.
- `.../Synchronization/IHostedHyperDriveFanOutStore` (defined in `SyncInterfaces.cs`) and `HostedHyperDriveFanOutDispatcher.cs` supply durable leased hosted fan-out. `OASISProviderFanOutHandler.cs` consumes configured idempotent targets.
- Mongo's `MongoDBOASIS.HyperDriveSync.cs` and domain/change-capture partials implement hosted sync transactions and capture. These are not evidence that every direct CRUD route across all providers enrolls an obligation in the primary mutation transaction.
- `IHyperDriveIdempotentReplicationTarget` requires mutation plus operation receipt to commit atomically. At the initial inspection checkpoint there were no generic implementations in the initial three canonical adapters. The explicit committed-replica receiver below now exists in Neo4j, but is not yet wired through the hosted target adapter. Domain-specific application remains a different contract.
- SQLite, MongoDB and Neo4j conditional writes have primary-operation receipts. Those receipts alone are not delivery queues or target receipts.

Adding an enqueue call after primary Save/Delete returns would leave a crash window. Replaying a user edit command at a replica would also create a different logical revision/audit history. Neither approach satisfies the invariant.

## Required native commit invariant

A requested guaranteed-delivery mutation must commit the selected primary graph, its original acknowledgement/receipt, and immutable delivery obligations in the **same authoritative native transaction**. Before dispatch, unsupported primary/target capabilities or invalid configuration must fail explicitly. A failed transaction must leave none of those effects; an uncertain commit must expose its stable operation identity for receipt reconciliation and must not fail over/replay the edit command.

The event must contain the committed operation/actor/authority, operation kind, exact selected graph and relationship selection, each committed logical head and previous head, persistent audit values, and immutable target identities. Hard deletion needs a durable tombstone/event even after physical rows disappear. Do not reconstruct the event later from mutable caller objects, a new database read, or the then-current replication configuration.

Provider-local physical revisions and native keys are observations, not portable logical identity. Target-specific observations must not contaminate the stable source intent fingerprint. Snapshotting targets for each obligation is necessary: later configuration changes must not silently erase a previously accepted delivery obligation.

## Replica application before queue enrollment

1. Define an explicit committed-replica intent separate from a user edit. Do not infer copy-versus-edit from version hints. Preserve committed logical revision IDs, lineage and origin audit; rotate only target-owned physical revisions/native identities as appropriate.
2. Apply the selected graph and target operation receipt atomically using the existing provider mutation owner. Receipt lookup must precede comparison; exact replay after an intervening local write must return the original acknowledgement without rewriting it.
3. Bind receipt identity to source authority, stable source operation, actor, operation kind, selected graph and committed content. Changed content under the same identity must fail. Checksums provide integrity, not authentication.
4. Compare source logical lineage under native ownership. Out-of-order delivery and concurrent local edits must yield explicit conflicts or defined deferral, never silent last-writer-wins. Target-native tokens cannot be preconditions chosen by the source, and receipt fingerprints cannot depend on a worker's changing pre-read token.
5. Preserve target provider observations while retaining source logical/audit identity. History-enabled targets must not stamp a second edit revision or duplicate history on replay. Historical snapshot/restore semantics remain an explicit certification gate.
6. Define destination encryption context at the existing storage boundary. Persist committed storage snapshots, not plaintext preparation objects; reject unsupported encryption/key mappings rather than guessing or silently bypassing protection.

The existing hosted target interface/dispatcher should be reused where their semantics fit. A generic full-holon graph adapter must explicitly validate its payload contract; domain-specific hosted entity handlers cannot be treated as universal holon targets. Avoid a second competing ordinary write implementation.

## Durable dispatch ownership

- Persist independent target obligations with stable identities. Claim, renew, complete, defer and release must verify lease ownership atomically. Process restart must not erase work.
- Preserve ordering per affected logical record/graph stream, including tombstones. An unrelated failed stream should not indefinitely block unrelated work; cross-stream scheduling must still preserve shared-record ordering.
- Target completion requires a durable acknowledgement or validated exact receipt replay. An uncertain target response cannot be promoted to success even if contradictory flags say otherwise.
- Continue independent targets after a non-caller-cancellation failure. Each receives a detached copy of the accepted event. Caller cancellation stops subsequent attempts. Retain target identities, native causes and structured outcomes for reconciliation.
- Retry only the documented idempotent delivery protocol, with bounded backoff and visible conflict/reconciliation state. Never retry the primary user mutation to discover whether it committed.
- Queue/receipt/tombstone retention and compaction require proof that no pending delivery/replay window still needs them. Authorization, tenant scope and actor binding must survive restart and worker identity changes.

## Handler correction completed in this checkpoint

The existing V2 hosted fan-out handler previously passed one mutable item to every target, stopped on a target exception, and discarded native failure causes. It now snapshots accepted intent and supplies a fresh copy per target; visits later targets after independent failures; propagates caller cancellation; preserves single/aggregate native causes and successful cleanup warnings; and refuses contradictory success carrying native reconciliation evidence.

Result metadata `hyperDriveFanOutTargetOutcomes` contains a JSON array of `TargetId`, `IsApplied`, `RequiresReconciliation`, `ErrorCode` and `Message`. Missing/negative acknowledgements remain incomplete. This metadata is returned by the handler; durable persistence and operator exposure through every dispatcher/store boundary remain acceptance work.

Ten new unit cases cover these invariants (thirteen handler cases including the original three). The full current core suite passes 1,001 cases, zero failures/skips; both net10.0 and netstandard2.1 builds pass. Other contributors' cases are included in the full count. No native CRUD-to-outbox guarantee is claimed by those unit results.

## Required verification before declaring C08 complete

- Native atomic mutation/receipt/obligation tests for MongoDB, SQLite and Neo4j, sync/async single/batch Save and soft/hard Delete through V2 manager routes.
- Pre-commit failure/rollback leaves no graph change, receipt or obligation. Commit-before-response loss requires independent receipt reconciliation; committed obligations survive provider/process reconstruction.
- Real process termination/restart between primary commit and dispatch, and between target commit and queue acknowledgement; exact target receipt replay performs no second mutation/history stamp.
- Multi-target failure/recovery, payload mutation isolation, lease loss/renewal, worker competition, completion-response loss, independent stream progress and ordered edit/delete delivery.
- Conflicting target local edits, source lineage gaps, migrated history, absent/deleted records, selected-depth graph boundaries and explicit parent reassignment/unlink.
- Durable per-target diagnostics/reconciliation, authorization/tenant isolation, encryption context, cancellation and retention/compaction boundaries.
- Unsupported capability admission before writes, and configuration changes that preserve previously accepted obligations.

These gates remain open except the handler invariants explicitly verified above. Availability must be described by measured operation-specific SLOs and recovery behavior; failover alone does not establish absolute uptime or cross-provider atomicity. Save deltas and CQL remain later roadmap stages.

## Committed-replica receiver prerequisite implemented for Neo4j

[Committed replica application](COMMITTED_REPLICAS.md) now shares the native graph/receipt owner, preserves source logical/audit identity, and retains per-record physical/lineage anchors through hard deletion. Native SQLite-to-Neo4j sequences verify ordering, replay, local conflicts and acknowledgement uncertainty with history on/off. The primary-mutation outbox and hosted generic target adapter remain open; this prerequisite does not close C08.

## SQLite receiver prerequisite completed locally

The explicit committed-replica capability now has SQLite and Neo4j implementations, tested with actual commits in both directions. SQLite's graph/head/receipt ownership, local-token anchors, retained tombstones, cancellation/cleanup and acknowledgement-loss behavior are documented in [SQLite receiver](SQLITE_COMMITTED_REPLICAS.md). MongoDB cross-provider certification, the hosted adapter and primary atomic outbox enrollment are still required; C08 is not complete.

## MongoDB receiver prerequisite completed locally

MongoDB now applies committed replicas through its existing native graph/receipt transaction, with retained source/physical heads and actual-BSON acknowledgement validation. Native auxiliary-write rejection rolls back graph/head/receipt; independent causal majority inspection precedes exact replay of uncertain commits. See [MongoDB receiver](MONGO_COMMITTED_REPLICAS.md). These receiver guarantees do not add a primary outbox, authorized transport or durable hosted adapter. C08 remains incomplete.
