# Google Firestore CQL MVP

Updated 29 October 2026. Stage: local native + Mongo-paired C#/HTTP/browser MVP verified. Hosted Google and full parity remain unverified.

## Current implementation and evidence

- GoogleCloudOASIS implements the common CQL provider contract. `FirestoreCqlCompiler` intersects captured ID equality/membership with the exact trusted Google authority catalogue. Boolean AND/OR/constants and bounded paging are admitted, at most100 document IDs.
- The adapter uses native Firestore `GetAllSnapshotsAsync` on selected `holons/{id}` references. No collection scan or managed business predicate. The cached official SDK3.7.0 documentation specifies ordered snapshots including explicit absent documents; incomplete/foreign acknowledgements fail the query.
- Shipping canonical/history decoder is reused. Native identity/access, deletion visibility, response4MiB and result budgets are checked before publication. Cancellation/native errors publish no partial rows.
- Provider build passes; initial controls7/7 pass, zero skipped. These cover frozen selection/authority intersection, empty catalogue, five unsupported shapes, inactive execution and pre-cancellation. They do not prove successful native batch execution.
- Evidence: `TestResults/FirestoreCqlCurrent/firestore-cql-controls.trx`; raw evidence stays outside docs.

## Exact next step

Continue Cosmos blockchain, Polkadot and NEAR MVPs before deeper parity. Hosted Azure stays pending by user instruction. Broader content/order/graph/RPC-failure parity remains open.

## Reproduce initial controls

Build GoogleCloudOASIS then the maintained GoogleCloudOASIS.IntegrationTests project using one provider build lease and `--no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false`. Run test filter `FullyQualifiedName~FirestoreCqlTests` with `--logger 'trx;LogFileName=firestore-cql-controls.trx' --results-directory TestResults/FirestoreCqlCurrent`.

The consumer initially stopped at missing restored assets (NETSDK1004), before service/test execution. Offline restore from the existing `C:/Users/david/.nuget/packages` cache restored them and tests passed. No downloads, installations, new database or listener this stage. Runtime identity, size and hash are in the cleanup ledger.

No hosted Google, arbitrary metadata/order/graph, distributed snapshot or full parity claim. Unsupported shapes remain explicit errors.

[Current status](CURRENT_STATUS.md) / [breadth roadmap](CQL_DEMO_EXPANSION_ROADMAP.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Native proof and retained state

Native acceptance1/1 passes: three native records, filtered Espresso/Latte subset, explicit missing-ID empty result and unchanged source document revisions after queries. A complete stop/start import run passes1/1 with original seed/checkpoint hashes unchanged. Evidence `TestResults/FirestoreCqlCurrent/firestore-cql-native.trx`.

`LocalState/COSMIC/FirestoreCql` retains one original seed (actor/three IDs) and immutable native checkpoint (total10122bytes at first measurement). The runner verifies each export-file hash and seed hash before import, never overwrites a checkpoint and refuses an unacknowledged seed/export or pending original. Failed native execution without an acknowledged checkpoint preserves the original live emulator for reconciliation. Successful runs stop the owned process. Historical Google project/checkpoint is untouched; no runtime download or duplicate installation.

## Paired preset

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Google -Federated

# Bounded native + C#/HTTP verification:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Google -Federated -VerifyOnly
```

Open http://127.0.0.1:5100/ after startup. All returns Firestore3 + Mongo2 =5 physical rows; subset returns2 +2 =4. The generic C# identity-membership walkthrough executes through HolonManager, and HTTP calls the same walkthrough. Server-owned actor/catalogues cannot be replaced by browser input.

Paired SDK/HTTP acceptance1/1 passes, including provenance/unrelated-row exclusion, forged actor/unknown preset rejection and source deactivation503 with no partial Mongo rows. Evidence `TestResults/FirestoreCqlCurrent/firestore-mongo-federation.trx`. Mongo fixture ownership is journaled and exact native contents are checked before dropping its temporary database; Firestore seed/checkpoint remain reusable.

No hosted Google Cloud, arbitrary metadata query, shared-snapshot, scaling or full parity claim. Local Firestore is clearly labelled in the page. Cached emulator runtime/Java and existing Mongo runtime/data are reused.

Actual browser all5/subset4 and both source labels verified. Screenshot `TestResults/FirestoreCqlCurrent/firestore-mongo-browser.jpg`; final interactive1/1 duration1m3s includes UI verification/shutdown, not query latency. Owned emulator/Mongo services stopped; exact temporary Mongo DB validated/dropped; pending/ready markers removed. Original seed/checkpoint remain unchanged and reusable. Launcher discovery11paired profiles/22plans,9pair rejections,8single/16plans passed with unchanged listeners. No historical Google state was reset.
