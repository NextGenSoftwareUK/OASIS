# COSMIC ORM implementation roadmap

User priority28October: first prove HyperDriveV2 replication, failover and load balancing; then resume COSMIC ORM/CQL parity across MongoDB, SQLite, Neo4j, IPFS, Azure Cosmos, AWS, Oracle, SOLID and ActivityPub. Prove these priority providers before broadening implementation to others. Existing Solana demos remain preserved. Hosted Azure remains explicitly deferred pending a genuinely free authenticated account.
Latest user steering: MVP breadth first, depth afterward. Finish AWS, then Ethereum, Polygon, Arbitrum, Google, Cosmos blockchain, Polkadot, NEAR and other suitable existing popular providers. Prioritize useful native queries and Mongo-paired demo presets over advanced operators, deep migration or full parity.
Current execution state: [CURRENT_STATUS](CURRENT_STATUS.md). Full objective remains active; no completion claim.

| Order | Work | Acceptance owner |
|---|---|---|
| 1 | HyperDriveV2 replication correctness, failover and load balancing | X10 red; [SQLite bound receiver milestone](REPLICA_STORAGE_BINDING.md), All3local receivers and durable inventory verified;atomic source binding verified;[bound source/factory gates verified](SOURCE_OUTBOX_BINDING.md);[atomic complete route descriptors verified](DURABLE_ROUTE_DESCRIPTORS.md);snapshot-first worker verified in recorded native scope;cold/offline new-write binding,operator/V2 lineage migration and physical replacement gates remain |
| 2 | COSMIC ORM/CQL parity across the nine priority providers | Independent provider capability/acceptance evidence; work only on red checklist cells. Hosted Azure native proof pending; emulator proof stays labelled. Deep LINQ remains deferred until HyperDrive gates pass. |
| 3 | Reconcile remaining ORM correctness, child graph/parameter, history and replication requirements | Roadmap requirements preserved in historical plan; current source + proving tests |
| 4 | Complete migration/placement/delta/history and operational acceptance still missing | Independent full-scope audit; do not infer success from focused tests |
| 5 | Broader EF/NHibernate parity and final programme audit | Every declared requirement independently verified |

Investor-demo rows are already green. Large POCO-without-IHolon mapping and migrations tooling stay deferred until the demo/current priority set is complete.
Only HyperDrive V2 receives implementation changes; legacy gaps are documentation-only.
Keep change tracking, save ownership, concurrency, conditional writes, rollback/versioning and per-save deltas in the full objective; prior implementations do not replace final acceptance.

[Feature checklist](CQL_PARITY_CHECKLIST.md) is the authoritative remaining feature inventory.
[Full original roadmap and history](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_history_2026-10-27.md) preserves all earlier requirements and checkpoints.

PostgreSQL breadth milestone: control8/native1/restart1/paired1 and browser5/4 pass;
[scope and retained evidence](POSTGRESQL_CQL_MVP.md). Full parity remains open.

SQL Server breadth milestone: control9/native/restart/paired and browser5/4 pass;
[scope and evidence](SQLSERVER_CQL_MVP.md). Wider parity stays deferred.
