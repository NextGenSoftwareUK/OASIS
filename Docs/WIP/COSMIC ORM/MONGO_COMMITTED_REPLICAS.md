# MongoDB committed-replica receiver

Updated 19 October 2026. Local implementation for COSMIC ORM/V2; legacy Hyperdrive remains documentation-only. This receiver does not enroll primary CRUD into an outbox.

## Ownership and protocol

MongoDB now implements ICommittedHolonReplicaStorageProvider. The detached CommittedHolonReplicaPlan enters the existing SaveGraphsAtomicAsync / ImportAtomicAsync owner used by ordinary graph and conditional writes. One snapshot-read, majority/journal-write native transaction owns all selected rows, durable CosmicReplicaHead documents and the original exact CosmicWriteReceipt. There is no secondary graph-write implementation and no automatic primary replay.

Receipt lookup and immutable fingerprint validation precede state comparison. The fingerprint binds source installation/provider, actor, operation, selected topology/depth and committed logical/audit content. Exact receipt replay returns the original selected acknowledgement after intervening changes or physical deletion without another mutation/commit. Receipt/read-only session cleanup failures retain successful acknowledgement plus warnings.

The head collection uses Mongo's native unique _id as the holon identity. Existing activation already enforces the unique public identity index for canonical Holon records. A head retains SourceId, SourceAuthority, SnapshotJson, SnapshotDigest, target ProviderRevisionId and HardDeleted. The SHA256 digest detects accidental corruption; it is not authentication.

All selected lineage/local-state checks run before writes. Initial enrollment requires an absent record and logical version one. Later delivery must advance exactly one version from the same source, match its previous logical version ID and the retained target physical revision. Existing unenrolled data, skipped events, another source, raw local edits and corrupt heads fail explicitly. Hard deletion retains its head to reject stale recreation. Competing transactions are arbitrated by native row/index/head conflicts; callers do not get a process-local lock or silent retries.

Replica preparation preserves source logical versions, origin audit, committed tombstones and business fields. It replaces only target-owned identity/revision/metadata observations. Incoming target native keys and metadata cannot select a local native identity or overwrite its namespace. Source objects remain unchanged. Save, soft delete and hard delete use the same owner; each selected acknowledgement is validated against its actual BSON body before any mutation or receipt. Exact receipts and heads commit with that body.

Commit dispatch captures the original native session/transaction identity before awaiting commit. Uncertain commit results expose MongoTransactionReconciliationException and COSMIC_WRITE_RECONCILIATION_REQUIRED, and do not dispatch abort. Source operation identity is supplied in the immutable request and stored receipt; no invented native transaction identity is used.

## Persistence prerequisites repaired

- Provider entities and embedded canonical Core audit records retain sub-millisecond creation/modification/deletion precision. Native BSON date projections remain queryable; optional exact UTC ticks are validated against those projections. The Core serializer delegates class maps, discriminator handling, ID and member-query metadata to the existing driver serializers. Nested Holon and derived InventoryItem cases are covered.
- CustomKey, ParentHolonId2, ParentHolonId3, ParentHolonIds and declared ChildrenIds now survive the native entity mapping. Missing legacy ChildrenIds has the common DTO's empty-list default. Key/ID collections are copied across DTO boundaries. Nodes maps to the common storage DTO List shape instead of introducing an ObservableCollection implementation tag on read.
- Single-save admission now captures one detached graph before choosing the existing native single-record or multi-record transaction owner. Both sync and async routes consume that same capture; they do not traverse caller Children during validation or routing. Ordinary graph assembly uses SelectedChildIds captured by HolonGraphWritePlan rather than rereading caller membership. Single-record operations retain native standalone Mongo support. Selected descendant snapshots and excluded native frontiers keep their existing ownership.

## Verification

Current unit suite: 112 passed, no failures/skips. New cases include sixteen embedded precision cases, two canonical portable-value round trips and four pre-I/O replica admission cases and four single-record cyclic-metadata admission cases. The portable-value tests validate admitted common DTOs through actual BSON serialization against the unchanged shared replica content validator; they do not substitute for a cross-provider native delivery test.

Two native sync/async committed-replica sequences use actual commits in a separate MongoDB source database and target database. They verify selected graph/audit precision, origin metadata, immutable source objects, target identity ownership, provider reconstruction, exact replay, changed intent/source, out-of-order and concurrent delivery, excluded child frontiers, local physical-token conflicts, checksum rejection, soft/hard deletion and retained tombstones. Native validators reject head and receipt writes separately and independent reads prove graph/head/receipt rollback before exact application after the validator is removed.

A native commit write-concern acknowledgement fault retains the original native evidence and sends no abort. An independent causal majority read proves the original receipt, row and head before exact replay; command monitoring proves no additional insert/update/delete/commit on replay. This is not an OS process-kill or actual wire-drop certification.

A native regression probe returns a child only from its first Children getter call; both sync/async saves read it exactly once and persist/acknowledge that child. The first probe failed against the old single-save route (four getter reads); independent majority reads verified its exact owned source root and empty target/auxiliary collections before fixture cleanup. That failure exposed and drove the single-admission routing repair, not an assertion relaxation.

The bounded full Mongo native suite passes 56 cases. The selected conditional/replica filter now expects four cases (two existing conditional, two committed-replica); the runner count was updated only after all four were observed passing. Successful owned source/target databases are removed after provider shutdown; failed fixtures remain preserved. No runtime installation or duplicate environment was created.

## Open acceptance gates

Four actual SQLite-to-MongoDB and MongoDB-to-SQLite sync/async round trips now pass; Neo4j/MongoDB directions remain to certify. Metadata DateTime precision, native JSON trees/binary and equivalent collection implementations are covered in the later codec checkpoint; arbitrary custom node/CLR implementations and other typed-value combinations still need explicit portability coverage. Current native date filters have millisecond resolution; exact sub-millisecond range/order semantics require tick-aware CQL translation.

SourceId is an assertion, not authentication. Source/tenant binding, authorized baseline/version-greater-than-one enrollment, source handover, encrypted delivery context, native process/wire-loss recovery and an operator reconciliation surface remain open. No implicit adoption of preexisting target data is introduced. Hosted payload adapter, primary mutation/outbox atomic enrollment, worker lease/order/restart behavior, retention and historical graph semantics remain roadmap work. A raw receiver capability does not establish universal replication, settlement finality or uptime guarantees.

## Input error classification and V2 regression follow-up

Broad V2 verification initially reported four assertion failures because its old serialization test required a BSON exception for unsupported delegates. Single-holon capture now rejects that input before BSON. The shared graph planner now uses the input projection deserializer for freshly captured JSON; persisted receipt/head readers retain their stored-corruption wrappers. A new shared case requires the precise input JsonSerializationException and unchanged source identity. The V2 test requires that exact exception/reason for holons, retains the existing BSON/NotSupportedException requirement for avatar/detail routes, and still proves no native mutation, no fallback to SQLite and byte-identical retained data. The shared suite passes 518 cases, including concurrent contributors' additions; no broad assertion or skip was substituted.

Strict input capture now rejects cyclic persistent dictionaries/lists before projection or storage access, as well as rejecting runtime delegates without traversing their reflection object graph. Stored-record serialization keeps its established contract; the new SerializeInput capture uses error-on-cycle settings. Shared tests prove the existing bytes are unchanged for valid acyclic input, including repeated noncyclic values. Four direct-provider sync/async cases prove cyclic metadata returns the precise payload error before an unactivated repository can be touched, with no caller identity publication. These guards prevent the new single-record capture from turning unsupported metadata into a truncated successful write.

Final strict-capture verification: 1,001 core, 518 shared, 112 Mongo unit, 56 Mongo native, 616 SQLite native and all twelve V2 routing cases pass; both core target frameworks build. See [verification hashes](VERIFICATION.md#final-strict-capture-and-v2-checkpoint--19-october-2026).

## Metadata and portable comparison follow-up

See [metadata codec and native cross-provider work](MONGO_METADATA_CODEC.md). Exact metadata DateTime companions, native Newtonsoft JSON trees and binary values are implemented; shared acknowledgement comparison recognizes equivalent CLR/JSON collection implementations without changing original request fingerprints. All four SQLite/MongoDB directions/modes now pass with fresh source/target reads; earlier same-provider evidence remains separately scoped. Numeric/schema/type-security and the later outbox/adapter gates remain open.

## Primary enrollment follow-on

The separate [MongoDB primary outbox](MONGO_PRIMARY_OUTBOX.md) now commits graph, sequence range, frozen per-target obligations and exact receipt together. Full Mongo native58/unit134 and shared594 cases pass. This supersedes older statements that MongoDB primary enrollment is entirely open; MongoDB fenced delivery/inspection, crash/authorization/retention and broader programme acceptance remain open. Receiver and enrollment capabilities do not imply automatic/default durable delivery.
