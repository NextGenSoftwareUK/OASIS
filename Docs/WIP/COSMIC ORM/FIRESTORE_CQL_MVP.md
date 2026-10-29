# Google Firestore CQL MVP

Updated 29 October 2026. Stage: adapter build + initial controls verified. Native/paired investor demo is not ready.

## Current implementation and evidence

- GoogleCloudOASIS implements the common CQL provider contract. `FirestoreCqlCompiler` intersects captured ID equality/membership with the exact trusted Google authority catalogue. Boolean AND/OR/constants and bounded paging are admitted, at most100 document IDs.
- The adapter uses native Firestore `GetAllSnapshotsAsync` on selected `holons/{id}` references. No collection scan or managed business predicate. The cached official SDK3.7.0 documentation specifies ordered snapshots including explicit absent documents; incomplete/foreign acknowledgements fail the query.
- Shipping canonical/history decoder is reused. Native identity/access, deletion visibility, response4MiB and result budgets are checked before publication. Cancellation/native errors publish no partial rows.
- Provider build passes; initial controls7/7 pass, zero skipped. These cover frozen selection/authority intersection, empty catalogue, five unsupported shapes, inactive execution and pre-cancellation. They do not prove successful native batch execution.
- Evidence: `TestResults/FirestoreCqlCurrent/firestore-cql-controls.trx`; raw evidence stays outside docs.

## Exact next step

1. Reuse cached official Firestore emulator1.22.0 JAR and Java21.0.12.101. Old worktree durability checkpoint is missing; do not replay its original journal or touch another checkout's historical state.
2. Create one fixed owned CQL fixture/project, proposed loopback18084/18085/18086, with persistent manifest/checkpoint/journals under `LocalState/COSMIC/FirestoreCql`. Inventory before allocation. Preserve original uncertain outcomes and export/checkpoint state before any teardown; never reseed a pending original.
3. Verify three native records and a two-record filtered query, missing/deleted/error semantics, no query writes and original seed reuse. Then Mongo-paired5/4 C#/HTTP/browser with provenance, fail-whole-query and exact fixture cleanup through the existing generic teaching flow.
4. Enrol the launcher only with verified scope. Then Cosmos blockchain, Polkadot and NEAR MVPs before deeper parity. Hosted Azure stays pending by user instruction.

## Reproduce initial controls

Build GoogleCloudOASIS then the maintained GoogleCloudOASIS.IntegrationTests project using one provider build lease and `--no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false`. Run test filter `FullyQualifiedName~FirestoreCqlTests` with `--logger 'trx;LogFileName=firestore-cql-controls.trx' --results-directory TestResults/FirestoreCqlCurrent`.

The consumer initially stopped at missing restored assets (NETSDK1004), before service/test execution. Offline restore from the existing `C:/Users/david/.nuget/packages` cache restored them and tests passed. No downloads, installations, new database or listener this stage. Runtime identity, size and hash are in the cleanup ledger.

No hosted Google, arbitrary metadata/order/graph, distributed snapshot or full parity claim. Unsupported shapes remain explicit errors.

[Current status](CURRENT_STATUS.md) / [breadth roadmap](CQL_DEMO_EXPANSION_ROADMAP.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
