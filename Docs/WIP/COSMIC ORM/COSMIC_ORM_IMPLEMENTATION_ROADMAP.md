# COSMIC ORM implementation roadmap

Solr:48controls/native/restart/paired/browser5/4 pass; bounded canonical RealTimeGet. [Scope](SOLR_CQL_MVP.md).
Typesense:38controls/native/restart/paired/browser5/4 pass; native created_at volatile. [Scope](TYPESENSE_CQL_MVP.md).
Meilisearch:39controls/native/restart/paired/browser5/4 pass; bounded ID scope. [Proof](MEILISEARCH_CQL_MVP.md).
Weaviate: 38 controls/native/restart/paired/browser5/4 pass in the bounded identity demo scope. [Scope](WEAVIATE_CQL_MVP.md).
Qdrant: 38 controls/native/restart/paired/browser5/4 pass in the bounded identity demo scope. [Scope](QDRANT_CQL_MVP.md).
InfluxDB: 34 controls/native/restart/paired/browser5/4 pass in the bounded identity demo scope. [Scope](INFLUXDB_CQL_MVP.md).
ClickHouse: 28 controls/native/restart/paired/browser5/4 pass in the bounded identity demo scope. [Scope](CLICKHOUSE_CQL_MVP.md).
Couchbase: 31 controls + native/restart/paired/browser5/4 pass in the bounded identity demo scope. [Proof](COUCHBASE_CQL_MVP.md).
User priority28October: first prove HyperDriveV2 replication, failover and load balancing; then resume COSMIC ORM/CQL parity across MongoDB, SQLite, Neo4j, IPFS, Azure Cosmos, AWS, Oracle, SOLID and ActivityPub. Prove these priority providers before broadening implementation to others. Existing Solana demos remain preserved. Hosted Azure remains explicitly deferred pending a genuinely free authenticated account.
Latest user steering: MVP breadth across ALL providers first, depth afterward. Work through red identities in the all-provider inventory, with independent native queries and Mongo-paired presets; local/emulated scopes stay labelled. Hosted Azure remains pending by user instruction.
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

DuckDB breadth milestone: control14/native/reopen/paired and browser5/4 pass;
[scope and evidence](DUCKDB_CQL_MVP.md). Wider parity remains deferred.

Upstash hosted breadth milestone: control17/native/repeat/paired/browser5/4 pass;
[scope and expiry](UPSTASH_CQL_MVP.md). Existing free scratch expires October31.

Litestream Linux breadth milestone: native/live file replication/restore/paired and
browser5/4 pass; [scope/resources](LITESTREAM_CQL_MVP.md). Windows fsync stays red.

Memcached breadth milestone: native volatile generations/paired/browser5/4/CAS pass;
[scope/lifecycle](MEMCACHED_CQL_MVP.md). Wider atomicity/durability remain open.

Valkey breadth milestone: native9.0.4/restart/paired/browser5/4 and hub repeat pass; [scope/evidence](VALKEY_CQL_MVP.md). Full ORM/cluster parity remains open.

KeyDB breadth milestone: controls8/native6.3.4/restart/paired/browser5/4 and hub pass; [scope/evidence](KEYDB_CQL_MVP.md). Full parity/cluster/registration remain separate.

Dragonfly breadth milestone: controls8/native completed snapshot/restart/paired/browser5/4 and hub pass; [scope/evidence](DRAGONFLY_CQL_MVP.md). AOF/crash/cluster/full parity remain separate.

Garnet: controls8/lifecycle6/empty-checkpoint1 and explicitly admitted generation2 native/restart/paired/browser5/4/hub repeat pass; original failed fixture/loss remain separate, unchanged/refused. [Scope/evidence](GARNET_CQL_MVP.md).
