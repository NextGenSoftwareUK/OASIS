# Session-held native transaction ledger

2026-11-28 gpt-cosmic, claimO03, reusework/gpt-q50 and existing isolated CI. Main148/168, extension4/13; fullO03/O02/O04/O05 remainOPEN.

## Invariant and implemented kernel

One fixed actor/authority/native SDK owner carries every provisional operation. Only an acknowledged native commit may return IsSaved; errors after commit invocation remain uncertain, preserve operation IDs and cannot be replayed/rolled back by the kernel. Cancellation before dispatch does not fabricate a commit. Operations/rows/payload/time are bounded and concurrent operations are refused. Provisional row snapshots are immutable and detached. Native query plans retain captured read policy; provider re-admission compares that immutable policy through public SamePolicy (comparison is not a grant).

Coreb9881e33 adds the kernel; StageAsync is internal and consumes trusted private conditional storage intent. This is not yet the public CosmicSession.SaveChanges enlistment path and must not bypass its authorization/save-plan/lifecycle preparation. No new network endpoint or public raw ORM write API is exposed. Existing provider conditional storage interfaces remain trusted host primitives.

## MongoDB implementation, native0/3 pending

Shipping MongoDBOASIS opens one snapshot/majority-journal native transaction. Source reads use the existing native compiler/canonical guarded row decoder under that exact session/deadline and captured repository; graph expansion is explicitly not in this kernel cohort. Native unit-of-work receipt/part/unique-key work is factored from commit ownership; ordinary calls retain their own commit/abort, held calls never commit/abort a successful stage. Already committed operation IDs are refused inside a new held transaction. No AsyncLocal override, managed data replay, synthetic transaction, alternate data source or failover.

The focused native gate has Mongo held create/read-own-write/outside invisibility/sequential revision update/frozen provisional snapshot/commit/rollback/foreign policy/cancel-before-commit (9groups,13timings,2ownedIDs,2staged operations acknowledged in1commit; rolled-back operation not counted as a successful write). It also reuses the changed Mongo ordinary mixed-UOW journey26groups. Expected2native cases, actualWEB4 build and exactowned cleanup. SQLite/Neo4j have no new held-owner admission yet. No native qualification or full row credit until CI passes.

## Next

Qualify/repair the exact Mongo owner first, then implement the same owner on SQLite/Neo4j. Wire trusted owned-write preparation and authorization into the public session, route all supported CQL read families through its held owner, defer lifecycle/accepted tracking publication until final commit, and qualify the combined behavior before fullO03 credit. Preserve native isolation differences; savepoints/distributed transactions are separate rows and never silently emulated.

## Development and cleanup

Qualified Development8143052a/Coree31af3c1 published2026-11-28T14:28:26.219Z from native19bb49f6; CQL46153501347 and XML46153501315 passed. Railway780254fe observedQUEUED; no new hosted acceptance. This transaction source is feature-only. Next normal own Dev>=14:58:26UTC, verified batches only.

No local build/runtime/database/install/worktree/archive, sharedAtlas/PIPER call, queue535/543 change or staging/production rollout. Active owned source backup LocalState/COSMIC/GPT_O03_ACTIVE_SOURCE_DRAFTS_2026-11-28.json is retained until tracked source is qualified; it is not disposable build output. Scoped indexes removed in finally; CI artifacts2day retention. No ACTIONS-FOR-DAVID entries added.
