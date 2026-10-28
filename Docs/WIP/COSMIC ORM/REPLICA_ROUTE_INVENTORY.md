# Durable unfinished-route discovery

28October2026. Prerequisite for X10 automatic binding/operator reconciliation;X10 remains red.

Core adds IHolonReplicationRouteInventoryOutbox.ReadRouteHeadsAsync(pageSize,afterTargetId).
All3native outboxes implement it. It discovers the oldest unfinished obligation for
EACH persisted target, irrespective of which providers/routes are currently configured.
Pending,leased,deferred and reconciliation-blocked heads remain visible;completed rows
are skipped and later unfinished rows become that route's head. No claim or write occurs.

Pages contain1..256requested heads (or0when empty), validated operational inspections,
and an exact last-target continuation only when another route exists. The DTO contains
no business payload or claim token. Existing per-row payload/lifecycle/recovery validators
also validate the inventory read;error returns no partially successful page. Native
binary ordering is used (Mongo explicitly uses simple collation). Continuation is
not a cross-page snapshot under concurrent enrollment/completion;callers must rescan
as observations change. This is not a CQL/federation cursor or delivery acknowledgement.

SQLite uses a native minimum-unfinished-sequence lookup per target;Mongo uses a
read-only grouped aggregation;Neo uses minimum sequence per target in its read
transaction. No new table,index,identity row or migration was introduced.

Proof:
- SQLite3native cases:sync/async primary-enrollment pages plus mixed state/corruption.
- Mongo2native sync/async primary-enrollment pages.
- Neo2native sync/async primary-enrollment pages, retaining unrelated shared-store routes.
- Shared fixture probe verifies exact original operations/heads,one-row page continuation,
  no duplicate/missing owned routes,unchanged attempt counts,empty final continuation,
  request limits/inexact cursor rejection,and pre-cancellation. It creates no second fixture.
- SQLite mixed-state case proves case-sensitive target separation,live lease,blocked
  oldest head,deferred oldest head,and skipping a completed head to a later operation.
  PRAGMA data_version is unchanged by inventory reads. Corrupt payload digest fails
  the whole page with COSMIC_OUTBOX_CORRUPT and null result. This mixed-state native
  matrix is SQLite evidence;Mongo/Neo received the pending-page matrix,not equivalent
  corruption/concurrency certification from this milestone.
-4384fullCore tests pass,including5new page/request boundary cases;0skips/failures.
  BothCore targets and Shared/SQLite/Mongo/Neo/WEB4/native consumers build successfully.

The first mixed-state test omitted the existing required error code on its Retry
outcome. Its known owned1graph/1receipt/4obligations were reconciled read-only against
journal/source/targets/states/payload hashes and summarized before exact fixture cleanup.
Corrected test then passed. Failed TRX remains hash-named under OwnedRoutingCurrent/Archive;
raw reconciliation is route-inventory-authoring-reconciliation.json. No unknown outcome
was reset or replayed. All successful fixtures cleaned;owned native services stopped.

Evidence (raw files remain only under worktree TestResults):
- TestResults/OwnedRoutingCurrent/route-inventory-sqlite-final.trx: SHA256 188FB69D9948248894F302C465D34A12BC7129CA7F4C422076A9670DAE73216F.
- TestResults/OwnedRoutingCurrent/mongo-route-inventory.trx: SHA256 94017CEDF52D17F9FBD85CCE7AF9BEC1B94CDA64C6917A18133D9FAC8C19F001.
- TestResults/Neo4jCurrent/neo4j-cosmic-primary-outbox-current.trx: SHA256 9B65D4F58BEEE342309EB50F5A943D09A3FF54A35B82366A9216D31B27E688B5.
- TestResults/OwnedRoutingCurrent/route-inventory-core.trx: SHA256 4502E74BF6FF9632E2F2D0BF92CF974FFFE567AF756BD82F539FBC523F26CBE8.

Next:bind each primary enrollment to the expected native source identity inside its
own transaction,then publish immutable native source/target/policy routes and make the
worker consume inventory. Explicit reconciliation of old/unbound/changed-policy routes
remains required. The current worker still uses canonical type-only IDs;this discovery
capability alone neither changes routing nor resolves orphaned obligations automatically.
