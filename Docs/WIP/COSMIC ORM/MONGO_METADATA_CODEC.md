# MongoDB metadata codec and cross-provider value ownership

Updated 19 October 2026. Working implementation/evidence for COSMIC ORM/V2. Legacy Hyperdrive remains documentation-only.

## Native value invariants

Metadata dates, nested JSON trees and binary data must survive the actual database representation, rather than only the in-memory acknowledgement. The codec now writes Newtonsoft JObject/JArray/JValue trees as native BSON values and byte arrays as binary. Existing arbitrary complex CLR objects keep their documented JSON-text representation; strings containing JSON are not parsed or guessed to be objects.

DateTime metadata keeps its native BSON date field for indexing/querying. The containing storage record optionally owns MetaDataExactUtcTicks: an array of entries with exactly Path and Ticks. Paths use escaped JSON-pointer segments, including dictionary keys containing slash/tilde and array indexes. The companion is outside the business metadata dictionary; no business key is reserved or overwritten. Provider entities restore it after the driver has read all members; embedded canonical Core records use the same helper through their existing class-map serializer wrapper.

The companion is emitted for precision or driver-boundary information not safely reconstructed from the native date alone. Reads require an existing date projection at each unique path, valid path escaping, valid Int64 ticks, and matching native milliseconds. Missing, duplicated or conflicting paths fail instead of creating fields or silently changing values. Later metadata edits generate a new companion. No precision is invented for old metadata without one.

Audit precision validation now compares the installed driver's signed native millisecond projection rather than assuming mathematical truncation relative to .NET ticks. This corrects pre-1970 dates. Capture also retains the explicit value at the driver DateTime.MaxValue boundary, including an exactly millisecond-aligned value that the driver can otherwise decode as DateTime.MaxValue.

## Portable acknowledgement comparison

The shared replica validator previously compared CLR collection implementation tags as business content. A source JSON tree can legitimately become dictionaries/arrays after native BSON decoding. Acknowledgement comparison now normalizes recognized dictionary/list/array implementation envelopes while preserving all values, array order, missing fields, custom object types, logical lineage and audit.

The request fingerprint construction and existing receipt format are unchanged. Exact replay remains bound to the original frozen source intent and its serialization. Equivalent acknowledgement containers do not authorize rebinding an operation to a different supplied request representation. Native keys/revisions and target-local metadata retain their existing ownership exclusions.

Five shared tests verify portable JSON/CLR containers (including arrays of dictionaries and ObservableCollection versus List), exact original-intent binding, changed values/order/missing fields, and retained custom type identity.

## Native cross-provider verification

The reusable runner now has CosmicReplicaTransfers: four actual SQLite/MongoDB source/target sequences covering both directions and sync/async. Each compares committed source receipts with fresh source-provider reads, target acknowledgements with fresh target reads, full selected graph metadata/audit, provider-local observations, ordered delivery, exact replay, soft/hard deletion and retained tombstones. It reuses one fixed SQLite database and the owned Mongo replica-set environment. Failed fixtures are retained.

The first SQLite-to-Mongo sequence rejected the target's different CLR container representation before any target mutation. Independent read-only source inspection proved two version-one source rows and one source receipt; majority reads proved the target collections empty before exact cleanup. No source/target mutation was replayed for that inspection.

After the logical comparison correction, the same direction completed all business assertions, then a test cleanup path check failed on mixed separators. Directory normalization and validation of every resolved cleanup target now precede removal. Independent read-only reconciliation verified that exact source hash, its unchanged child and five coherent receipts through hard deletion, with the target synthetic Mongo database already absent; only that owned fixture was then removed. A later Mongo-to-SQLite run reached the correct out-of-order rejection and exposed a conflict-code inconsistency. Mongo now uses the established COSMIC_REPLICA_LINEAGE_CONFLICT code shared by SQLite and Neo4j. Its exact retained source/target fixture was independently inspected before cleanup, without replaying mutations.

All four native SQLite/MongoDB direction/sync combinations now pass. The complete Mongo/SQLite V2 filter includes these four cases, so its inventory is now sixteen cases; the previous twelve-case result is historical. Broader current regression verification is recorded in VERIFICATION.md.

## Remaining acceptance gates

- The four native combinations and their exact retained-fixture reconciliations pass. The broader Mongo native regression passes 56 cases and V2 Mongo/SQLite routing passes 16 cases, with zero failures or skips.
- Neo4j/MongoDB native receiver directions, generic hosted payload adapter and atomic primary CRUD/outbox enrollment remain open.
- Exact timestamp query/projection translation must select the companion as well as the native date; native date predicates alone still have millisecond resolution.
- Arbitrary custom node/CLR value implementations, multidimensional arrays, decimal/large-number fidelity through untyped canonical JSON, and explicit metadata schema/type handling need further certification. No universal typed-codec claim is made by date/tree/binary tests.
- Source/tenant authorization, encrypted delivery context, retention and real process/wire-loss recovery remain open.
- Source inspection shows the canonical JSON binder uses namespace-based type admission. Explicit DTO registration and literal reserved type-marker handling require security certification; this is an identified review gate, not a demonstrated exploit or a completed security audit.

## Current evidence

130 Mongo unit cases and 523 shared cases pass, including seventeen metadata precision cases plus a literal-business-key/JSON-text case and five portable-comparison cases. The full SQLite native suite passes 616 cases and all six existing Neo4j/SQLite replica sequences pass after the shared comparison change. Four actual SQLite/MongoDB sequences pass. Existing warnings remain; no provider runtime was installed or duplicated. Successful owned fixtures were removed after shutdown, with exact paths checked before removal.
