# MongoDB atomic primary holon outbox

Updated 19 October 2026. Local explicit enrollment; MongoDB [delivery leasing/inspection](MONGO_OUTBOX_DELIVERY.md) is now implemented; automatic host scheduling remains open.

## Ownership and admission

MongoDBOASIS now implements `IAtomicHolonReplicationStorageProvider`. Explicit sync/async calls and enrollment carried in a conditional request enter the same existing graph transaction owner. Manager `CosmicWriteOptions` already propagates that enrollment through V2's capability dispatch. No ordinary user edit is replayed to build delivery payloads, and no post-commit enqueue is introduced.

`ConditionalHolonPlan.CreateReplicating` admits an explicitly capable owner, freezes selection flags and binds source/target enrollment into the fingerprint. Plain `Create` still refuses required enrollment for owners that cannot enroll it, and existing unenrolled fingerprint bytes remain unchanged. Explicit and embedded enrollment must agree; reordering the same target set is equivalent, while changing source/targets or replaying as a plain write is rejected.

## One native transaction

The existing snapshot/majority+journal transaction compares native revisions, prepares exact graph acknowledgements, captures the frozen selected graph and checks it against actual BSON round trips before mutation. It then persists current rows, a sequence range, every target obligation and the exact receipt before the original commit. Any failed counter, later target insert or receipt insert rolls everything back. Commit-uncertainty handling continues to retain the original Mongo session/transaction identity; no new transaction retry loop was added.

`CosmicHolonOutbox` retains schema version, operation/source/authority/actor, target, frozen payload/SHA256, precise UTC ticks and initial delivery/lease/retry/recovery fields. Deterministic operation/target native ids and a unique compound index prevent duplicate enrollment. Global sequence ids identify individual obligations, matching the existing inspection contract; all targets of one operation receive distinct consecutive positions.

`CosmicHolonOutboxSequence` allocates the entire target range inside that same transaction. Before inserting obligations, the owner rejects a counter behind retained sequences; it does not silently repair or fast-forward corrupted metadata. Native unique sequence and operation/target indexes, plus a target/order index, are initialized by the existing activation owner before publishing the provider. Conflicting/duplicate existing data fails activation instead of being discarded.

The shared sequence document serializes enrolled writes and can become a throughput contention point. Mongo write conflicts are explicit (`COSMIC_CONCURRENT_WRITE`) rather than hidden retries. Finer queue partitions require a declared replacement for globally unique inspection/ordering semantics and native proof; no claim of unlimited write scalability is made here.

## Caller and replay behavior

A receipt replay returns the original acknowledgement without allocating another sequence range or adding obligations. Frozen payloads survive later source edits and versioned soft/hard deletion. Metadata dates/binary and child graph lineage are checked against fresh source reads in the native cases.

Manager tests reject an enrolled target insert after native preparation and verify no generated identity/version is published to the caller. Retrying the same operation after the injected failure commits once and publishes the exact payload identity/version; no-op saves add nothing. Reusing the committed operation id for a later edit rejects while retaining the caller's pending change.

## Primary-enrollment checkpoint (historical)

These counts describe the enrollment-only checkpoint. Current delivery/store verification is in [MongoDB delivery](MONGO_OUTBOX_DELIVERY.md).

- Mongo unit suite: **134 passed**, including four new enrollment admission cases.
- Shared suite: **594 passed** at the latest local run, including five new capable-plan binding cases and concurrent provider-session CAS coverage.
- Complete Mongo native suite: **58 passed**, zero failures/skips. Two new sync/async sequences cover atomic source graph/targets/receipt, precise frozen content, embedded enrollment replay, source/target/plain replay mismatches, later-target/counter/receipt failure rollback, exact retry, ordered updates/tombstones, manager publication/failure/no-op and regressed-counter refusal.

Reproduce using the existing `Run-ReusableUsageRecoveryTests.ps1 -MongoTransactions` environment (60 expected) or `-CosmicConditionalWrites` (eight expected), with bounded producer builds. Current final hashes are in [verification](VERIFICATION.md). Earlier passes before the final per-obligation sequence/index checks are superseded by the final full native rerun.

## Remaining acceptance

MongoDB now exposes a fenced [recovery-capable consumer](MONGO_OUTBOX_DELIVERY.md); actual MongoDB-to-SQLite worker delivery is verified. Enrollment support does not mean MongoDB host delivery or default/global auto-replication is now durable. Neo4j enrollment, authenticated target/source/tenant/encryption context, native process/submission recovery journaling, operator reconciliation, queue/payload bounds, retention and migration/baseline enrollment remain open. Live managed-cloud/sharded topology and availability/latency claims require separate certification.

The concurrent Oracle/OCI provider session was notified of the additive plan method and native Mongo27029 ownership. Its backend/CAS files and dependency pins were preserved. No COSMIC commit/push/deployment was performed.
