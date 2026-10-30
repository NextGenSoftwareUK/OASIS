# Current COSMIC ORM / CQL status

<<<<<<< HEAD
Updated 30 October 2026. Full programme remains incomplete. Read this first after compaction.
=======
Updated30October2026 UTC. Scoped sandbox release candidate; full programme remains incomplete.
>>>>>>> origin/work/cosmic-cql-sandbox-integration

## Current phase and exact next step
MVP breadth first across **all providers**, then deepen. Work only on red rows;
no green/federation-cursor hardening without a reproduced bug. Legacy HyperDrive
issues are documentation-only. Hosted Azure Cosmos stays pending by user instruction.

**Immediate next: [sandbox CQL host admission](SANDBOX_CQL_ADMISSION.md). Client11controls; federation startup fix52tests; free profile3controls; temporary free subscription lifecycle21controls+7native pass. Hosted read-only probe1passes: opening marker present/no legacy operations/zero mutations; no reset needed. Live host still503 pending configuration/deployment. Integrate the verified scoped batch, then verify hosted rows/scalars/federation and visitor isolation.**
**Next: [approved10-provider content-query demo set](CONTENT_QUERY_DEMO_ROADMAP.md): name/description, metadata equality and AND/OR plus [WEB4 sandbox integration](SANDBOX_PROVIDER_DEMO_ROADMAP.md). QuestDB65controls/native1+restart1 pass; paired deferred for this priority; [LanceDB67save/table/Arrow/CQL controls pass](LANCEDB_CQL_MVP.md), free remote native admission pending; [Pinecone58save/index/CQL controls pass](PINECONE_CQL_MVP.md), free hosted native admission pending. [LanceDB protocol audit](LANCEDB_CQL_MVP.md). [Chroma50controls/native/restart/paired/browser5/4 pass](CHROMA_CQL_MVP.md). Milvus2.6.26 TLS startup fixed; upstream embedded-client shutdown defect established, same source/pending retained. [50controls pass; native red](MILVUS_CQL_MVP.md). Solr [48controls/native/restart/paired/browser5/4 pass](SOLR_CQL_MVP.md). Typesense [38controls/native/restart/paired/browser5/4 pass; native timestamp gap](TYPESENSE_CQL_MVP.md). Meilisearch [39controls/native/restart/paired/browser5/4 pass](MEILISEARCH_CQL_MVP.md). Weaviate [38controls/native/restart/paired/browser5/4 pass](WEAVIATE_CQL_MVP.md). Qdrant [38controls/native/restart/paired/browser5/4 pass](QDRANT_CQL_MVP.md). InfluxDB2 [34controls/native/restart/paired/browser5/4 pass](INFLUXDB_CQL_MVP.md). ClickHouse [28controls/native/restart/paired/browser5/4 pass](CLICKHOUSE_CQL_MVP.md). Couchbase [31controls/native/restart/paired/browser5/4 pass](COUCHBASE_CQL_MVP.md). OpenSearch [40controls/native/restart/paired/browser5/4 pass](OPENSEARCH_CQL_MVP.md). Elasticsearch [32controls/native/restart/paired/browser5/4 pass](ELASTICSEARCH_CQL_MVP.md). ScyllaDB [23controls/native/restart/paired/browser5/4 pass](SCYLLADB_CQL_MVP.md). Cassandra [23controls/native/restart/paired/browser5/4 pass](CASSANDRA_CQL_MVP.md). TimescaleDB [native/restart/paired/browser5/4 verified](TIMESCALEDB_CQL_MVP.md).**
Firebase50controls/native/export-restore/paired/browser5/4/hub pass [scope](FIREBASE_CQL_MVP.md). Supabase REST paired verified. [Proof](SUPABASE_CQL_MVP.md). PocketBase paired verified [scope](POCKETBASE_CQL_MVP.md).
Garnet generation2 native/restart/paired/browser5/4/hub repeat verified. Source2 retained,
services stopped; controls8/lifecycle6/empty-ABI1 pass. Original failure stays separate.
[Scope and initial loss](GARNET_CQL_MVP.md).
Dragonfly2.0.2 controls8/native snapshot/restart/paired/browser5/4 and hub pass; source
retained, services stopped and Mongo fixture removed. [Scope](DRAGONFLY_CQL_MVP.md).
Litestream Linux native/live LTX/restore/paired/browser/hub pass; original source
retained, owned services stopped; Windows fsync stays red. [Scope](LITESTREAM_CQL_MVP.md).
Upstash hosted/native/repeat/paired pass; free scratch expires2026-10-31, no reseed. [Scope](UPSTASH_CQL_MVP.md).
DuckDB control14/native/reopen/SDK/HTTP/browser5/4 pass; original source retained,
owned processes stopped and temporary fixture removed ([scope](DUCKDB_CQL_MVP.md)).
SQL Server control9/native/restart/SDK/HTTP/browser5/4 pass; original LocalDB source
retained, owned services stopped and Mongo fixture removed ([scope](SQLSERVER_CQL_MVP.md)). PostgreSQL control8, native/restart
1+1 and Mongo-paired C#/HTTP/browser5/4 pass ([scope](POSTGRESQL_CQL_MVP.md)).
Owned services are stopped and temporary Mongo data removed; source retained.
Redis native3/2/missing, ciphertext restart, SDK/HTTP/browser5/4 and launcher pass;
DB11/source/keys retained, temporary Mongo fixture removed and services stopped.
[Redis proof/commands](REDIS_CQL_MVP.md). Aliases/central registration are not certified.
Hedera controls/public runtime audit pass; existing routed-read funding guard is
not met by indexed testnet balance. No paid queries/key export performed; retain
historical admission guards. [Hedera gate](HEDERA_CQL_MVP.md). Sui full demo verified.
Aptos native/restart/paired/browser5/4 is verified; main-hub enrollment remains
review-gated. [Direct commands/gate](APTOS_CQL_MVP.md). All owned nodes are stopped.
LocalFile native/restart/SDK/HTTP/browser5/4 is verified and stopped/cleaned.
[Proof](LOCALFILE_CQL_MVP.md). TRON original source/fence recovery and S3 local launch
policy gate remain open; both stay red. [TRON](TRON_CQL_MVP.md), [S3](AWS_S3_CQL_MVP.md).
## Scoped demo proofs
| Provider scope | Current proof |
|---|---|
| Mongo/SQLite/Neo4j | Initial investor rows green; full parity remains open. [Checklist](CQL_PARITY_CHECKLIST.md). |
| Garnet generation2 + Mongo | Explicit new source/checkpoint/restart/SDK/HTTP/browser5/4; original loss separate. [Guide](GARNET_CQL_MVP.md). |
| Dragonfly / KeyDB / Valkey + Mongo | Independent encrypted native/restart/SDK/HTTP/browser5/4. [Dragonfly snapshot scope](DRAGONFLY_CQL_MVP.md), [KeyDB](KEYDB_CQL_MVP.md), [Valkey](VALKEY_CQL_MVP.md). |
| Memcached + Mongo | Volatile native/SDK/HTTP/browser5/4, values/CAS preserved; no persistence. [Guide](MEMCACHED_CQL_MVP.md). |
| Litestream + Mongo | Linux live file replication/restore + SDK/HTTP/browser5/4; Windows fsync red. [Guide](LITESTREAM_CQL_MVP.md). |
| Upstash + Mongo | Hosted free scratch native/repeat/SDK/HTTP/browser5/4; expires Oct31. [Guide](UPSTASH_CQL_MVP.md). |
| PostgreSQL / SQL Server / DuckDB + Mongo | Native/reopen and SDK/HTTP/browser5/4; [PostgreSQL](POSTGRESQL_CQL_MVP.md), [SQL Server](SQLSERVER_CQL_MVP.md), [DuckDB](DUCKDB_CQL_MVP.md). |
| Redis + Mongo | Encrypted DB11/native/restart/paired5/4, direct registration only. [Guide](REDIS_CQL_MVP.md). |
| Sui + Mongo | Dedicated encrypted native/restart/paired5/4, original Storage preserved. [Guide](SUI_CQL_MVP.md). |
| LocalFile + Mongo | Native files/restart/paired5/4; plaintext scope. [Guide](LOCALFILE_CQL_MVP.md). |
| Oracle + Mongo | Native/SDK/HTTP/browser5/4. [Scope](CQL_ORACLE_DEMO_ADMISSION.md). |
| Azure Cosmos + Mongo | Emulator5/4 only; hosted pending, no free subscription. [Gate](CQL_HOSTED_AZURE_FREE_ADMISSION.md). |
| IPFS/SOLID/ActivityPub + Mongo | Native3/2 and paired 5/4; arbitrary content predicates open. [Scope](CQL_PROTOCOL_MULTI_RECORD.md). |
| Solana + Mongo | Retained validator/index and SDK/HTTP/browser. Preserve missing-history gate. [Proof](CQL_MONGO_SOLANA_NATIVE_VERIFICATION.md). |
| AWS + Mongo | DynamoDB Local native3/2/paired 5/4. [Guide](AWS_CQL_MVP.md). |
| Ethereum/Polygon/Arbitrum/Base + Mongo | Local EVM31338 native3/2/paired 5/4. No public-chain/finality claim. [Ethereum](ETHEREUM_CQL_MVP.md), [Polygon](POLYGON_CQL_MVP.md), [Arbitrum](ARBITRUM_CQL_MVP.md), [Base](BASE_CQL_MVP.md). |
| Google Firestore + Mongo | Emulator native/restart/paired 5/4. [Guide](FIRESTORE_CQL_MVP.md). |
| Cosmos blockchain + Mongo | Native encrypted contract/restart/paired 5/4. Original four signed writes retained. [Guide](COSMOS_BLOCKCHAIN_CQL_MVP.md). |
| Polkadot + Mongo | Native encrypted contract/restart/paired 5/4. Original three signed writes retained. [Guide](POLKADOT_CQL_MVP.md). |
| NEAR + Mongo | Original encrypted records/restart/paired 5/4; historical write gate remains open. [Guide](NEAR_CQL_MVP.md). |
| Azure Blob + Mongo | Azurite native/restart/paired5/4; no hosted Azure claim. [Guide](AZURE_BLOB_CQL_MVP.md). |
| Avalanche + Mongo | Chain-identity/factory root fix; controls16/family37, native/restart/paired 5/4. Original empty-state continuation preserved contract/IDs. [Guide](AVALANCHE_CQL_MVP.md). |

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md): 226 concrete identities, 62 scoped MVP
proof links, 33 active central cases, 11 extra project names; linked scopes only, no full-parity claim.

## Deferred correctness and parity gates

- Save ownership/change tracking, concurrency and committed deltas have scoped proofs;
  see the strict [parity checklist](CQL_PARITY_CHECKLIST.md) and implementation roadmap.
- Effective avatar routing, failure isolation, native strategy reads, durable route
  descriptors/outbox and selected delivery failures have retained proofs; no universal
  uptime claim. [Routing](V2_ROUTING_ACCEPTANCE.md), [routes](DURABLE_ROUTE_DESCRIPTORS.md).
- SQLite/Mongo matching-lineage adoption and selected coordinator/crash scopes pass.
  Neo adoption, empty-target transfer, historical admission, source/outbox CAS,
  durable final decision/cutover and broader crash recovery remain red. No global
  cutover enabled. [Detailed gates](V2_LINEAGE_MIGRATION.md).
- Deep LINQ All/element/numeric terminals, POCO mapping, migrations and broader
  child/history/provider parity remain deferred for breadth. Keep original acceptance.

## Workspace, integration and resource rules

<<<<<<< HEAD
- Work/build/test only C:/Source/OASIS-cosmic; parent/Core branch work/cosmic-cql.
- Development835653f31 integrated; merged Cored9f5ec1 pushed/remote-verified. Core4552,
  WEB4schema/policy4 and affected builds/pins pass. [Proof](DEVELOPMENT_SYNC_2026-10-30.md).
- Verify task remote before integration (GitHub500 incident30Oct14:22). Development deployment is a
  separate verified scoped batch; never sweep deferred work into deployment to sync.
- Named git adds only; one build lease/batch, /m:2 /nr:false /p:UseSharedCompilation=false.
- Reuse cached SDKs/services. Persistent identities/stores/keys/journals stay under
  LocalState/COSMIC or their retained protected location; logs/TRX only TestResults.
- Preserve unknown outcomes; never replay/reseed/reset. Record all resources and
  exact teardown in [the ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md). Free-only services.
=======
- Work/build/test C:/Source/OASIS-cosmic; parent/Core currently work/cosmic-cql-sandbox-integration. Broader work/cosmic-cql preserved.
- Full ORM/CQL roadmap remains active/incomplete. Eight-provider demo breadth precedes remaining EF/NHibernate depth.
- Work only on red checklist cells. Freeze green features/cursors unless a bug is reproduced.
- HyperDriveV2 implementation only; document legacy gaps. Large POCO/migrations tooling stays deferred.
- [Checklist](CQL_PARITY_CHECKLIST.md), [demo priorities](CQL_DEMO_EXPANSION_ROADMAP.md), [full roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md).

## Immediate next step

[Scoped sandbox release](CQL_SANDBOX_RELEASE_2026-10-30.md): Core52, WEB4controls25,
native7pass. Existing hosted opening guard ready (read-only); publish scoped pins,
explicit free profile staged/read-back verified. Exact Railway build efd2c5c1-288b-4b58-8b8c-bee0f1690003 is BUILDING for Developmentdfceb1392; observe it, no duplicate deploy. Hosted queries pending.

## Latest verified milestone

- Q01f: native final entity-stage Select remains verified on all3 (14 cases/store).
- New combined entity-selected contract preserves both immutable sequences, re-admits actual capabilities and validates all selected input cells before DTO construction. Flat execution rejects combined plans.
- SQLite, MongoDB and Neo4j now execute selected Where/order/ThenBy/Skip/Take/Reverse after entity stages under their existing native read owners. Standard sync/async LINQ works, closure values rebind per read, original offsets are not replayed, source application constructors are not invoked.
- Both Core targets build;2173 CQL tests and221 full SQLite provider tests pass. SQLite proof covers exact Int64, invalid cells hidden by Take(0), protected traversal and entity work limits. Mongo220/Neo151 CQL controls and23 native cases per database (14 prior +9 combined) now pass; no Q01f cell promoted.
- Combined graph sources, reselection and element/cardinality/numeric terminals after entity selection remain unsupported. Earlier green scopes stay frozen.
- Neo/SQLite evidence in TestResults/Neo4jCurrent: neo-entity-selected-unit.trx:ADEF3FD50AF8394691D7D085322ED2F4CADF89E94EC97405ACC64824BD83777F; neo-entity-projection-native.trx:AD4BB05BC55FC7106D35537BE8F32BF2A7CE1D311A7BA4D8292E55B2E03D398E; Neo JSON:2AC91B0F9409A877CC084103D5D318D9B5858E7F2100C57F8961B35BC702FC7A; SQLite JSON:898EEFACE177E922DB613431DE9F1506C24520B30BC37ADF6ABD4B02786641A2. Exact fixture cleanup/journal removal, stopped container and absent17687 verified.
- Mongo evidence in TestResults/Neo4jCurrent: mongo-entity-selected-unit.trx:E3B631277F86D2D9AFF00BEBDF55C73A1F8622D2C9524A5CCA8247B8B3EB435B; mongo-entity-projection-native.trx:69C16B3B9F615F5EE782CA0ED7411D58FCF75A146AFC07F539160EDE329C1F86; JSON:AA04C90275F44D9E914AA01007D378F8BA2684F10EDEA2C46965859F30D67924. Exact fixture cleanup, journal removal and27029 listener teardown verified.
- Evidence in TestResults/CoreCurrent: entity-selected-owner-core.trx:8B2D359FCEB9C1E74B780E345F3698EC7693F2C1E6AAA854F53723F3DD3694E9; entity-selected-sqlite-full.trx:629CCDA3A1E72922E36752B2CCB5125DC334C7C348D2BF61B6A6ADDE0ED38AC9.
- See [manual](CQL_MANUAL.md#explicit-capture-of-selected-stages-after-entity-projection), [shared owner tests](../../../OASIS%20Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicEntitySelectedExecutionTests.cs), [SQLite proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests/CqlEntitySelectedCompositionTests.cs).

## Exact next work

1. Three-database hub is restored: pinned fixture reconciliation passed; 14 persistent archive/journal files total207492bytes, original/local SQLite preserved. Fresh native demo passes681SDK queries,138walkthrough HTTP requests,30save checks,526settled operations/608provider attempts; exact fixture cleanup and service teardown pass. See CQL_DATABASE_DEMO_HUB.md.
2. CosmosDB teaching page/hub native, interactive HTTP and actual browser rendering/clicks are verified:rows/projection/LongCount each200; code23.4px line height at13px font, no page horizontal overflow; clean teardown. Eight launcher profiles/16discovery plans pass. Same arbitrary query across all eight providers is not verified; IPFS/SOLID/ActivityPub remain point profiles.
3. Exact next: implement reselection after entity-selected stages, preserving retained columns, source identity/order and native proof-before-DTO; then integrate element/cardinality/numeric terminals and combined graph sources. Q01f stays red. All3 combined stage implementations now have the same23-case native proof; Neo keeps ProviderDefault restrictions and rejects unsupported snapshot scope.
4. Indexed other aggregates/selected stages, hosted production auth/durable settlement, scaling and full ORM/EF/NHibernate parity remain open. Create genuinely missing environments as authorized, reusing suitable resources first.

## Other verified scope and remaining limits

- Three databases:12original investor-demo rows green; Q01a-e/Q23/Q23e/Q49a scoped green. Q01f and full EF/NHibernate parity remain red.
- Cosmos DB:46controls/47native cases and5manager operations; controlled-identity HTTP rows/scalar/projection passed. Hosted cloud, numeric metadata, conditional lifecycle and production auth/billing remain open.
- IPFS/SOLID:52native storage checks each;191/384CQL controls. Native manager/LINQ/sample/HTTP point profiles passed; richer general queries remain open.
- ActivityPub:311provider controls/1964affected regressions; native public-status point manager/LINQ/sample/HTTP passed. No posting/federation or broad query claim.
- Indexed ingestion session serializes compare-and-publish, advances only acknowledged cursors and stops on failure/uncertainty. Host uses5second bounded complete-range re-observation; incremental transport/production worker accounting/scaling remain open.
- Production JWT, durable external outbox/authority settlement, hosted-chain acceptance and broad cross-provider snapshots are unverified.
- Original Solana recovery remains red: no known backup and missing original root/chunk. Never reset/replay the original ledger or call a new declared-range fixture recovery.

## Resources and recovery

- [Single cleanup ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) owns resource paths/hashes/retention. Reuse SDK10.0.401/NuGet/cache/Agave4.3.0; no new installations in this milestone.
- Persistent schema2 state:LocalState/COSMIC/SolanaCqlCurrent, coverage3073. Schema3:LocalState/COSMIC/SolanaMetadataCurrent, coverage3325, acknowledged recorda03306d4-649f-4871-8f27-8e13869b59a1/index86d676b6-c2c5-4c8f-98b2-a88e0fb8bb11.
- Preserve keys, acknowledgements, uncertain/pending journals and retained WSL ledger. Schema3 index32768bytes/ack511bytes; no pending seed. Completed prior TRX/JSON evidence is hash-archived by the runner.
- All logs/TRX/generated JS stay in worktree TestResults; owned disposable SQLite fixture is torn down. Build outputs are regenerable; source/keys are not cleanup targets.
- Shipping dependency graph plans:TestResults/ProtocolCqlCurrent/index-http-web4-*.json (69projects/61in-worktree). Reuse8external sibling outputs read-only; never recursively rebuild outside this worktree.
- [Earlier detailed checkpoint](Archive/CURRENT_STATUS_before_selected_metadata_live_2026-10-27.md).

## Key commands
>>>>>>> origin/work/cosmic-cql-sandbox-integration

```powershell
Scripts/TestHosts/Update-CosmicCqlProviderInventory.ps1
```

[Manual](CQL_MANUAL.md) / [architecture](CQL_ARCHITECTURE.md) / [launcher](CQL_DEMO_LAUNCHER.md)
/ [previous detailed status](Archive/CURRENT_STATUS_PRE_AZURE_BLOB_NATIVE_2026-10-29.md).
