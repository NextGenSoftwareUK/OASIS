# Three-database demo hub integration

27October2026. Hub routing verified; full native launch remains blocked by a
retained older fixture. Preserve its data; do not treat discovery as native proof.

## Implemented

- The Databases choice delegates Run-CosmicCqlV1Demo.ps1, which owns its build
  lease, native fixture, signed HTTP/SDK checks, interactive finish and cleanup.
- ShowPlan returns the MongoDB/SQLite/Neo4j group, correct runner and5088/demo URL
  without entering a build/service path. VerifyOnly selects bounded execution.
- Database fixture lifecycle rejects the protocol ArchiveCompletedDemo option.
- The joint runner reuses the existing LocalState/COSMIC/mongo-data environment
  instead of allocating another Mongo directory under disposable TestResults.
- Test-CosmicCqlDemoHubRouting.ps1 passes8profiles/16plans/3archive rejections and
  verifies that discovery leaves relevant listeners unchanged. No new dependencies.

```powershell
# Safe discovery; no services start.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Databases -ShowPlan
& Scripts/TestHosts/Test-CosmicCqlDemoHubRouting.ps1

# Run after retained-fixture reconciliation, not as an overwrite/reset operation.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Databases -VerifyOnly
# Interactive alternative: http://127.0.0.1:5088/demo ; use Finish demo.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Databases
```

## Actual native preflight findings

The installed Mongo8.3.11 runtime and retained stopped oasis-neo4j container are
present. Native preflight refused existing cosmic_cql_v1_web4_storage/usage
Mongo databases. An explicit namespace experiment then encountered146retained
Neo4j Holons. Neither attempt created new demo records; services shut down.

A copied ownership snapshot exists at
LocalState/COSMIC/Neo4jCurrent/cql-v1-web4-fixture.json, SHA256
892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5.
It declares157IDs across five groups and610requests at before-http-request, with a SQLite path in
the original C:/Source/OASIS checkout. The read-only graph audit now matches all146live graph IDs to declared identities;11declared IDs are absent. Mongo/SQLite operation outcomes remain unverified. No cleanup/replay is authorized by a
matching count alone. Original checkout files remain untouched.

The namespace experiment was removed from source because it did not solve graph
isolation. No new database/clone was created. The empty scratch directory
LocalState/COSMIC/CqlV1Demo/cosmic_cql_demo_hub remains inventoried after automatic
review rejected deletion. An initial misplaced ShowPlan dispatcher was corrected;
subsequent discovery tests confirm no listener changes.

## Next

Perform a read-only audit of retained IDs, versions and durable operation outcomes
against the snapshot before designing exact reconciliation. Preserve SQLite,
receipts, protected keys and unknown outcomes. Never reset the shared namespace,
replay source mutations or silently substitute a fresh environment to obtain green.
Cloud teaching-host integration is now native/interactive-HTTP verified. Broad demo/parity
acceptance remains incomplete; prior three-database evidence is historical proof,
not proof of a successful current launch.

Raw routing/native preflight evidence stays under TestResults. Hashes/resources
and the empty scratch-directory lifecycle are in ENVIRONMENT_AND_CLEANUP_LEDGER.md.

## Read-only retained graph identity audit
Inspect-CqlDatabaseRetainedGraph.ps1 owns one lease and retained-container lifetime, submits only fixed read-only queries, archives previous report by SHA256 and preserves all source data. The first completed audit found146live Holons, all within157declared IDs: primary10/missing1 (index9), scalar137/missing0, REST3/missing3, transport3/missing3, recovery4/missing4. Seven CosmicWriteReceipt nodes all belong to declared sharing/scalar operations; no OasisRecordVersion Holon nodes were observed. Counts and IDs do not prove the outcome or content of those operations.
Canonical versions remain unverified: the provider owns Version inside SnapshotJson, not a parallel h.Version property. Two attempts to decode canonical JSON through cypher-shell plain output failed because its string transport was not the assumed JSON/CSV representation; neither changed graph data and both shut down the owned service. Removed that parser rather than adding format guesses. The next audit must use the existing typed Neo4j driver and canonical deserializer, then inspect retained Mongo write receipts/usage outcomes. The original SQLite reference remains outside this worktree and untouched. No cleanup, reset or replay has been performed or authorized by this report.


Final read-only identity audit succeeded. Evidence TestResults/Neo4jCurrent/cql-retained-graph-inspection.json SHA256 FA026125603891A3FBC1DFC6527F12AA034C16433BB457B42BA912376B6EC832; initial identity report3C0B9F7180FEBD3B7D13F397787C0CAA7211E34EBAFBB8DFB31139AC866A0E31 retained by hash (its null native Version column is not canonical version evidence). Final report explicitly marks canonicalVersionsVerified=false and cleanupAuthorized=false. Container inspected exited and17687listener absent after teardown; script parser check passes.

## Typed canonical graph audit
Run Scripts/TestHosts/Inspect-CqlDatabaseRetainedGraph.ps1 -Canonical. The existing integration assembly uses a read-mode typed Neo4j session and OasisJson.DeserializeStored, without provider activation or writes. [Proving test](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlRetainedGraphInspectionTests.cs) checks the exact source journal, all157declared IDs, canonical/native identity equality, bounded complete inventories, receipt digest/graph validation through ConditionalHolonReceipt.Read, native operation/fingerprint envelope binding, absence of unfinished claims and current-version dominance. Scalar seed markers are provider:recordId, not operation IDs; receipt admission uses only sharingOperations.
Native inspection1/1 passes.146canonical records:144version0 direct seeds,1version1,1version7. Seven receipts acknowledge eight roots (including versions1through7 for one record); all root IDs are declared and observed current versions are not behind. Eleven declared identities remain absent. JSON TestResults/Neo4jCurrent/cql-retained-canonical-inspection.json SHA256 603441A27681EF5DB1F41A27E2D41FAF944D48433392D97E271E022DB65CC97B; TRX TestResults/Neo4jCurrent/cql-retained-canonical-inspection.trx SHA256 142BE4E329C37649DCF6FAE1EB871FAB036F0E60F5F9C74584FDEFE7EF151D15. The initial typed test exposed seed-marker/operation distinction before graph reads (failed TRX0EB37224 retained by content hash); corrected source passes. No original request fingerprint was recomputed from a preserved request payload; envelope agreement alone does not prove full original intent. Mongo usage/receipt outcomes, original SQLite and full safe reconciliation remain open. Container inspected exited,17687listener absent, source journal unchanged.


## Retained Mongo and local SQLite inventory
Run Scripts/TestHosts/Inspect-CqlDatabaseRetainedStorage.ps1. [Proving inspection](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlRetainedStorageInspectionTests.cs) uses existing Mongo data/configuration and read-only local SQLite with no provider activation/schema initialization. It found146records in each; exact ID sets match the typed graph audit, with144version0/1version1/1version7 in all three. Seven durable write receipts exist in each backend. Mongo validates canonical receipt digests and native operation/intent binding. SQLite validates its outer digest through the existing fingerprint owner, RequestHash/intent/operation/authority binding and canonical acknowledged graph; local file hash remains7E1C6FBF19A56E00923CDC34B478FC8AE4833FDBA96F1D20E936FA57586992B2. Original outside-worktree file is not inspected; copied-file provenance remains open.
Mongo usage has267settled rows:244succeeded/23failed.207bind to journal service/actor/idempotency-key identities;60(all recorded outsider actor) do not.10journal-derived identities have no observed usage row, which is not evidence that an operation never ran. Offline comparison against all declared actor identities for every journal key also found no match for the60. Report explicitly sets usageJournalBindingsVerified=false and cleanupAuthorized=false. These unexplained settled records prevent exact namespace cleanup. The test reports a complete inventory; it does not turn unresolved evidence green. The runner emits a warning when usage bindings remain unresolved.
Final inventory1/1passes with those explicit unresolved findings. JSON TestResults/Neo4jCurrent/cql-retained-storage-inspection.json SHA2563FE06F356F954E81AD0853409264DA06E15EB2CB35E480A05B4CE0720C1FC61D; TRX TestResults/Neo4jCurrent/cql-retained-storage-inspection.trx SHA2561A7666DE10D9AE4E95E66D69CC17EC86867EE034DC0407BD17D97DD66E15E20A. Earlier inventory evidence archived by content hash. Owned Mongo terminated,27029listener absent; SQLite bytes/source manifest unchanged. No reset, replay, receipt deletion or settlement was submitted. Next inspect retained usage receipt/attempt/endpoint evidence for the60unbound operations and establish SQLite-copy provenance before designing scoped reconciliation. Full native grouped demo remains red.


## Usage identity reconciliation supersedes the60unbound finding
The preceding unresolved-usage finding is superseded by a verified journal parser correction. The manifest has three explicit ownership forms: actor/operation ordinary request keys; actor/key read-policy/federation/cursor keys; and completed-process operationIds with a preceding actor/root ownership entry. No federation/cursor production code was changed or hardened. The inspection now rejects ambiguous simultaneous key formats, verifies the completed three-provider/three-page protected-key summary, and checks each observed usage actor/service against the corresponding journal identity.
Native inventory1/1passes. All267usage rows bind:264recomputed service/actor/request-key hashes and3completed-process IDs. The57named-key entries and3process IDs explain the former60unbound rows; no new or replayed operations were required. All267have settled timestamps, with244succeeded/23failed. Endpoints:rows146,scalar39,federation33,join18,composition15,projection13,grouped3. Ten ordinary journal keys still have no observed usage row; this remains an observation, not proof of nonexecution. Process ownership is summary-bound; original request intent is not reconstructed.
Evidence TestResults/Neo4jCurrent/cql-retained-storage-inspection.json SHA25604E63031A136624CB35CBC7230535C43E1A0698948E47C640C547571FC4E394C; TRX TestResults/Neo4jCurrent/cql-retained-storage-inspection.trx SHA256A7BF99F82842C92FBACFC7933F8CF43A70D0F9E8E7ADB870F2CBD2C461972627. Report retains bounded endpoint/attempt/receipt-count/provider evidence without native preparations or payloads. Local SQLite hash unchanged, source journal unchanged, owned Mongo stopped and27029listener absent. CleanupAuthorized remains false: next inventory every collection in the two owned Mongo databases, verify remaining scope boundaries and implement exact reconciled teardown. Preserve original/copy SQLite evidence; do not drop a namespace based only on matching record counts.


## Complete Mongo collection boundary proof
The read-only inspector now enumerates both owned databases completely, with explicit32collection/1000document/16MiB-per-collection budgets and SHA256 over sorted self-delimiting BSON documents. It retains names/counts/byte totals/digests, not raw documents. Storage contains exactly Avatar0,AvatarDetail0,CosmicHolonOutbox0,CosmicOperationFence7,CosmicWriteReceipt7,Holon146; usage contains only subscription_usage_outbox_v1 with267records. Seven fence IDs exactly equal the seven receipt IDs, each with positive generation. Existing canonical/receipt/usage bindings and local SQLite byte preservation still pass.
Native inspection1/1passes: JSON TestResults/Neo4jCurrent/cql-retained-storage-inspection.json SHA256405C3E91DCDD2785E82A9BAC2D39B3724AD14DD3AC75A5E74BFE9A197324ADF3, TRXC9E6D0FF3C8EA4967DED27E04903177F1B3B368247C1826769923076AE070CC3. Five standalone boundary rejection controls pass for unexpected collection, nonempty Avatar, extra fence, extra usage and missing Holon collection: TestResults/Neo4jCurrent/cql-retained-boundary-guards.trx SHA256A9162E970C4712F734FD91D85DD9CA09EAC0514C1D474673E0291451CD607BAF. These controls do not prove arbitrary foreign data is absent at a later time: reconciliation must revalidate full current fingerprints immediately before mutation. Owned Mongo stopped/27029listener absent; no teardown performed.
Next implement a separate explicit reconciliation mode that pins the verified source/evidence identities, rechecks exact current Mongo and graph contents, archives required original fixture evidence outside disposable TestResults, journals every destructive phase and removes only the owned fixture. Preserve the original checkout and local SQLite copy as evidence. Unknown or changed contents must reject; no namespace reset/new fixture may hide a failed outcome. Full combined demo launch remains red until reconciliation and a fresh native launch pass.

