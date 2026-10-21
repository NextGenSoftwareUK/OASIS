# Soft-delete read contract

Updated 19 October 2026. The selected MongoDB identity-read discrepancy is locally fixed and passes 140 unit and 64 native cases. Broad discovery/query visibility and historical-graph certification remain open.

## Identity lookup invariant

Loading a current holon by its exact logical ID or native key resolves the retained current record, including its tombstone. Soft deletion changes lifecycle state and logical version; it does not make the retained identity cease to exist. Hard deletion removes the current record. This is the existing SQLite/Neo4j identity-read behavior and is now also MongoDB's behavior. Consumers must inspect lifecycle state rather than infer hard absence from soft deletion.

MongoDB's four native repository GUID/key getters formerly filtered `DeletedDate == DateTime.MinValue`. Its provider-linked graph-read cursor applied the same filter, so its root disappeared when that hint was enabled. Both filters are removed at their existing owners. There is one identity lookup path, no extra query, failed-read retry, legacy fallback or new parallel API. The snapshot cursor supplies both root and provider-linked descendants, including retained tombstones. Duplicate logical identities still fail visibly.

The repository getter also resolves the previous record for ordinary single-record save preparation. Keeping the retained record visible prevents classifying a tombstone as an absent identity before creation-audit ownership is resolved. No public signatures or binary constructor contracts change.

## Evidence and compatibility

Four native GUID/key × sync/async cases exercise a fully selected soft-deleted root/child/grandchild graph. Each tests both embedded/provider-linked hints, child loading on/off, recursion on/off and depths 0/1/2. Logical versions, exact tombstone audit and creation audit remain intact. An exact current version hit returns that tombstone; an unavailable old version does not return latest. Hard deletion is verified by native zero-row counts and identity misses.

The full Mongo native suite passes 64 cases and its unit suite 140. The four-case targeted MongoDB/Neo4j worker regression also passes with public tombstone reads after actual delivery asserted; final evidence is recorded in VERIFICATION.md. This behavior change affects callers that previously treated Mongo identity-load absence as proof of soft deletion. Active-only discovery should use its declared query policy; identity lookup returns the lifecycle state.

## Historical finding and preserved evidence

The first selected Neo4j-to-MongoDB worker test completed creation, update and soft-delete, then incorrectly expected the old public Mongo load to expose its tombstone. Its native version3 records, receipts and Completed obligations were archived before exact owned fixture removal. The failed report and reviewed archive are retained in verification. Native delivery correctness and the public read-contract correction are distinct evidence checkpoints.

## Remaining closure

Define explicit active/deleted visibility for WEB4 discovery and CQL, then apply it consistently to all/parent/metadata/paged queries and every provider. Identity-root traversal includes the retained graph selected by its documented current/embedded hydration policy; this checkpoint does not certify stale embedded membership, unlink/reparent history or every mixed active/deleted graph case. Declare exact historical record and graph visibility independently. Add native conformance before claiming uniform discovery, historical graph behavior or provider-wide coverage. Legacy Hyperdrive remains documentation-only.
