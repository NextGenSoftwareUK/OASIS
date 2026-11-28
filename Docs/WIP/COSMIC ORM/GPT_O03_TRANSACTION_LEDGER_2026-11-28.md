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


### 2026-11-28 — enum collision corrected; SQLite owner added, qualification pending
- First Mongo/kernel gates46155646543 and46155646448 failed compilation before native tests: inherited OASISProvider.ProviderType shadowed the enum type in the new owner. Explicit NativeProviderType aliases fix the root naming collision in both new owners; no runtime fallback or deployed change. CI owned Mongo cleanup completed; no native acceptance from those failed runs.
- SQLite uses an independent DataContext on the exact captured configured file/named shared-memory source, one enlisted serializable IMMEDIATE transaction, existing guarded CQL read owner and existing native conditional/receipt/unique-key code. Private temporary/in-memory connections that cannot share source identity are explicitly refused. Existing UOW now separates owned/held commit/abort; held operations never commit/abort their successful stage.
- SQLite write cancellation uses the existing native progress/work hook and remaining transaction deadline. Native autocommit state recognizes an already aborted transaction before rollback; finalization does not depend on the parent provider remaining active. No global/AsyncLocal context replacement, nested native owner or managed row replay.
- Mongo/SQLite existing9group/13timing held journey and26group changed ordinary-UOW journey run as4native cases plus WEB4/cleanup. Native0/3qualified pending; public session owned-write authorization/lifecycle and all-query-family enlistment stillOPEN. Main148/168 extension4/13 unchanged. Coreb9881e33 unchanged.


### 2026-11-28 — Mongo/SQLite held kernel native-qualified2/3
- Exact83f63a7cb89fae8f3b5eb70cc4ac34a5dcf2265a/Coreb9881e3397666a2393fbac835afa16a1daacc68c passed46156584432:4native cases (held9groups/13timings and ordinaryUOW26groups perstore), actualWEB4 and exactcleanup. Independent source review passed. No managed query/transaction emulation.
- Artifact15267694307,4,387B/6entries,SHA256c342c16498dea31d3f0c8abbbed859326afc9cb0c0e798f8564d52f2aa073f7d verified in RAM and released; compact receipt retained. Native Mongo create/update/commit observations22/5/4ms and SQLite9/3/25ms in this isolated run are observations, not benchmarks or hosted SLA.
- Public session owned-write authorization/tracking/lifecycle integration remainsOPEN; O03 is not credited. Main148/168 extension4/13. NextNeo owner and native unique-claim staging/rollback/release cohort. Initialf493 enum compile failure remains recorded, corrected by83f63a7c.
- Qualified Development8143052a/Coree31af3c1 Railway780254fe-86bd-476d-bd47-bb424904a87c isSUCCESS; metadata verified, not a new hosted database/browser walkthrough. No transaction kernel was deployed.


### 2026-11-28 — Neo4j owner and native unique claims, native3 qualification pending
- Neo4j reuses the same Cypher compiler, canonical row decoder and staged conditional/receipt/unique-key pipeline under one explicit write transaction. Ordinary read/UOW paths retain their own lifecycle. A held query/stage never commits the encompassing transaction. No read-pool lease is held for the transaction lifetime, so outside reads are not blocked on the provider lifecycle gate; each native stage binds the captured driver under the short lifecycle gate.
- Unique-key constraint moves from per-UOW DDL to the existing writable activation schema owner, verified by shape (9required guarantees). No data rewrite or backfill; missing/bad constraints fail activation. Mongo held/ordinary UOW snapshot transaction explicitly selects primary reads. Duplicate Neo parent receipts fail as invalid. Known commit followed by cleanup failure remains acknowledged with a warning, never falsely uncertain.
- Native held journey now10groups/18timings/2ownedIDs/3acknowledged mutation operations across2commits perstore, including unique claims, rollback absence and native hard-delete claim release. Existing changed ordinaryUOW26groups perstore retained;6native cases plus actualWEB4/cleanup expected. Full public-session owned-write/lifecycle/all-query-family enlistment remainsOPEN; no O03credit. Main148/168 extension4/13.
- Preserves peer Development33142f96/Core4b4ee0aa clan mutation-actor fixes, map work and WEB6 pin in mergedCore250c5936 and parent merge. Next exact combined source must qualify before Dev. Last own successfulDev8143052a/Coree31 at14:28:26, Railway780254fe SUCCESS; no transaction source deployed.


### 2026-11-28 — native held kernel3/3 and paired CQL accepted
- Exact84b488cd4150f7d253243fe1917b95c87abf200d/Core250c5936e8bbd1b08505995f3e61b4e4501c7c06 passed46158243226 (held3+changedordinaryUOW3,6cases) and46158243287 (compiler20/native15), both actualWEB4/exactcleanup. Independent source review found no blockers. Native held10groups/18timings/2IDs/3mutations across2commits perstore includes unique claim rollback and deletion release.
- All26artifact entries inspected inRAM. Held15268399647(5,940B),SHAd345c38d8eb2b13525a6fb5c220d428edcec2ca2c2be57573ad27aa70e483694; paired15268773316(18,509B),SHA06736906e93a9dc772635651a6fcfb98b534d3b7493437328c63d6bbc6e7f796. Buffersreleased, compact receiptretained,0sharedDBcalls.
- Kernel3/3 is not fullO03: public session authorized writes/accepted tracking/lifecycle/allreadfamilies remainOPEN. Main148/168 extension4/13 unchanged. NextCore24404c3b private authority-read seam is source-only and excluded from this acceptedCore250 batch.


### 2026-11-28 — private authority-read seam, qualification pending
- Native commands now expose a trusted bounded canonical point read under the same held SDK owner. Its immutable plan selects exactly one logical identity with a two-row duplicate guard, no caller visibility grant, no default-provider routing or separate snapshot. Kernel internal authority lookup enforces transaction budgets/state and never returns this private snapshot through a user endpoint. Public user-query policy remains unchanged.
- Existing manager owned-write authorization accepts an explicit authority-reader callback for this owner; normal calls retain their existing authoritative path. Public session preparation/enlistment/lifecycle publication remainsOPEN. The seam alone does not authorize writes or complete O03.
- New representative native checks read an uncommitted create for authorization, read a private record only inside the trusted authorization seam while the foreign public query remainsempty and managerWrite permission remainsdenied, and distinguish a missing/rolled-back record. Held11groups/21timings perstore;6native+actualWEB4/cleanup plus pairedCQL expected. Core24404c3b; qualificationpending. Main148/168 extension4/13 unchanged.
- Accepted prior kernel84b488cd/Core250c5936 was publishedDev3f23c388 at2026-11-28T15:22:04.945Z after native6+paired20/15; Railwaypending. This new authority seam is excluded from that deployed pin. No sharedAtlas/PIPER call or new local build/environment.


### 2026-11-28 — one owned-write preparation/publication owner, qualification pending
- Existing ordinary SaveAndDeleteOwnedHolonsAsync now reuses PrepareOwnedUnitOfWorkAsync and its single private publisher. The extracted preparation keeps current authority/actor/rights/revision/model/unique-key/audit/replication intent checks. An explicit held authority reader feeds the same save/delete checks; delete snapshots are decrypted privately and never accepted into global tracking before commit.
- Prepared plans own their capture leases until disposed, publish once only from IsSaved committed acknowledgement, preserve planned generated IDs for filtering, and keep later local edits under the existing HolonSavePlan publisher. The public session transaction state/provisional tracking/multiple flush lifecycle still remainOPEN. No managed transaction emulation or parallel legacy preparation.
- New actual native prepared-write journey perstore: generated identity remains unpublished beforecommit, provisional publication isrefused, committed receipt publishes accepted tracking, foreignwrite preparation isdenied, rollback leaves callerunpublished.6groups/10timings/2IDs/1committed mutation. With kernel/authority and changed ordinaryUOW cohorts,9native cases/actualWEB4/cleanup expected; pairedCQL required. Main148/168 extension4/13 unchanged.
