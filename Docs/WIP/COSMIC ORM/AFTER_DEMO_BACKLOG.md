# Return after the MVP demo

Updated 30 October 2026. These remain part of the full goal; they are deferred, not complete.

Current priority: fix reproducible demo blockers and deliver provider breadth/presets. Do not expand generic recovery infrastructure or parity depth ahead of the demo.

| Later work | Current evidence / remaining acceptance |
|---|---|
| Durable initialization application coordinator | Native baseline, durable one-obligation application, interrupted-call receipt recovery and Mongo-to-SQLite shell host now implemented/tested. Hosted Mongo-to-SQLite execution now verified for two barriers and35-step three-database demo; broader provider/operator/API composition remains deferred. |
| Fenced source reconciliation and queue completion | Mongo source completion is implemented/tested against exact original source and target evidence, with atomic comparison and retained failure history. Hosted two SQLite conflicts repaired and ordinary delivery verified; broader source-provider repair tooling remains deferred. Never skip/reset a held entry or replay acknowledged writes. |
| Atomic late enrollment / future-write cutover | Baseline primitives do not certify gap-free enrollment while new source writes race capture. |
| Large-store baseline/migration operations | Neo receipt audit currently bounded at 10000; Mongo migration batches1–128. Scaling, operator tooling and full operational acceptance remain. |
| Deep LINQ / EF / NHibernate parity | Resume red checklist rows, especially All/element/numeric terminals after entity-selected stages, reselection and graph composition. Keep green scopes/cursors frozen absent a reproduced bug. |
| General POCO mapping and migrations tooling | Unrestricted entities without IHolon, model mapping/conventions, schema diff/up/down/scaffolding remain deferred. |
| Full ORM acceptance audit | Reconcile tracking/save ownership, conditional concurrency, child loading/saving/depth parameters, rollback/history/deltas, placement and migration against original requirements/provider tests. Existing focused proofs do not establish 100% completion. |
| Broad provider production depth | MVP queries/presets do not certify complete provider semantics, auth, billing/settlement, scaling or cross-provider snapshot consistency. |

Hosted Azure remains pending at the user's direction (free-only; no existing free subscription). Broader provider order and demo gates remain in CURRENT_STATUS and the strict checklist. Legacy HyperDrive issues are documentation-only; implementation changes remain V2 only.

Recovery entry: [CURRENT_STATUS](CURRENT_STATUS.md). Feature acceptance: [CQL_PARITY_CHECKLIST](CQL_PARITY_CHECKLIST.md). Native recovery limits: [REPLICA_BASELINE_AND_RECOVERY](REPLICA_BASELINE_AND_RECOVERY.md).

Azure Blob MVP (30 October): local Azurite + Mongo identity query is now current-branch SDK/HTTP/browser verified (all5/subset4). Return later to hosted free-only Azure acceptance, application encryption, content/metadata queries, business conditional writes and wider HyperDrive acceptance; see AZURE_BLOB_CQL_MVP.md. Do not deepen its verified bounded identity controls without a reproducible bug.

LocalFile bounded identity MVP integrated30 October:7controls/native1/paired1 + independentbrowser5/4, original source unchanged. Return later to plaintext-record encryption, content/metadata queries, child semantics, conditional writes and wider HyperDrive/parity; current profile deliberately does not certify those. See LOCALFILE_CQL_MVP.md.

Aptos demo integrated30 October:18controls/native1/paired1 plus browser5/4. Later: plaintext payload encryption, read-only configuration, SDK in-flight cancellation/transport limits, content/children/conditional writes/public network/HyperDrive/parity. Broad hub enrollment remains a separate unverified gate; the current sandbox direct preset is verified. See APTOS_CQL_MVP.md.

Sui demo integrated30 October:7controls/native1/paired1 + independentbrowser5/4; original/dedicated Storage unchanged. Later: general content/metadata/order/children, conditional writes, public network/finality, snapshots and full HyperDrive/parity. Encryption uses the documented public synthetic fixture key, not production secret management.4MiB bound is post-decode, not native SDK allocation. See SUI_CQL_MVP.md.

Redis demo integrated30 October:8controls/native1/paired1 + browser5/4, original native ciphertext snapshot unchanged. Later: content/metadata/order/children, conditional writes, cluster/snapshot/full HyperDrive/parity, production BootLoader registration and independent Redis-compatible alias admission. Direct demo registration is not production registration. See REDIS_CQL_MVP.md.

PostgreSQL content demo integrated30 October:16controls/native17querycases/paired1 plus browser2/4/4/2, original/content schemas unchanged. Later: hosted tenant authorization/admission/registration, general ordering/numeric/null/NOT/graph queries, conditional writes/encryption/HyperDrive/full parity and independent compatible-alias admission. Local catalogue entry does not implement hosted authorization. See POSTGRESQL_CQL_MVP.md; historical report preserved in Archive.

SQL Server content demo integrated30 October:17controls/native23-queryscenario/paired1 plus browser2/4/4/2, original/content/long-extension rows unchanged. Later: hosted tenant authorization/admission/registration, broader query/conditional-write/encryption/HyperDrive/full parity and alias admission. LocalDB proof does not certify Azure SQL. See SQLSERVER_CQL_MVP.md; historical report preserved in Archive.

DuckDB demo integrated30 October:14controls/native1/paired1 + browser5/4, original3live+1tombstone snapshot unchanged. Later: metadata/content/children/generalordering, conditional writes, native cancellation/transport-allocation proof, encryption/registration/MotherDuck/HyperDrive/full parity. Embedded local acceptance does not certify a hosted service. See DUCKDB_CQL_MVP.md.

Upstash current demo integrated30 October:17controls/hostednative1/paired1 + browser5/4, original hosted values/index unchanged. Temporary free admission expires2026-10-31T21:15:49.3541451Z; permanent free hosting requires separately admitted infrastructure, never paid allocation/reseed by default. Plaintext/encryption, atomic multi-command saves, general predicates/children/concurrency/HyperDrive/parity remain red. See UPSTASH_CQL_MVP.md.

Litestream Linux demo integrated30 October:14controls/source1/restored1/Mongoidentity1/paired1 + browser5/4;22retainedfile hashes unchanged and no liveprobe replay. Windows daemon/restore directory-sync failure and pending probe remain red; original failure logs3311B and Linuxoriginal liveprobe evidence4292B now preserved outside cleanup in LocalState. Later: remote/PITR/crash recovery, Windows durability, broader queries/children/history/conditional writes/encryption/registration/HyperDrive/full parity. See LITESTREAM_CQL_MVP.md/historyarchive.

Memcached demo integrated30 October:15controls/native1/Mongoidentity1/paired1 + browser5/4; actual cache values/CAS unchanged throughout reads. New explicit empty generation warmed/clean-retired, not persistence. Later: shared-index RMW atomicity, conditional writes, native in-flight cancellation, broader queries/history/children/encryption/productionrouting/HyperDrive/full parity. Canonical seed and retired-generation lineage retained; unknown active outcomes must not replay. See MEMCACHED_CQL_MVP.md/historyarchive.

Valkey independent demo integrated30 October:8controls/native1/restart1/Mongoidentity1/paired1 + browser5/4; actualValkey9.0.4/sourceciphertext unchanged. Later: metadata/content/children/generalordering/conditionalwrites/history, clusterreplication/failover, publichosting/centralfactory/HyperDrive/full parity. No inherited Redis/otherfacade certification. See VALKEY_CQL_MVP.md/historyarchive.

KeyDB independent demo integrated30 October:8controls/native1/restart1/Mongoidentity1/paired1 + browser5/4; actual6.3.4/pinnedbinary/process/sourceciphertext verified unchanged. Later: metadata/content/children/order/conditionalwrites/history, active replication/cluster/failover, publichosting/centralfactory/HyperDrive/full parity. No inherited Redis/Valkey certification. See KEYDB_CQL_MVP.md/historyarchive.


### Dragonfly current demo integration (30 October 2026)

Native identity/checkpoint/restart/MongoSDK/HTTP/browser ID-filter flow is verified (14 checks), not full parity. Return to general name/description/metadata predicates, graph/conditional/history semantics, central registration/hosted admission, cluster/active replication/failover and distributed snapshots. Native completed RDB/clean restart does not prove uncheckpointed-write recovery, AOF/per-save crash durability or PITR. Preserve original source and stop receipts. See DRAGONFLY_CQL_MVP.md; no new Redis/shared-executor hardening required.


### Garnet generation2 current demo integration (30 October 2026)

Verified admittedgeneration2 nativecheckpoint/restart/SDK/HTTP/browser ID-filter scope only; originaluncheckpointedsource remains lost/unresolved with originalpendingreceiptsunchanged. No recoveryclaim. Return to generalname/description/metadataqueries, graph/conditional/history, centralregistration/externalatomicityadmission, AOF/crashdurability and HyperDrive/cluster/failover/distributedsnapshotacceptance. NativeLua+LuaTransactionMode profile does not certify arbitraryexternalconfigurations. See GARNET_CQL_MVP.md.


### PocketBase current demo integration (30 October 2026)

NativeREST IDfilter/restart/MongoSDK/HTTP/browser verified,45cases; no new businesswrites in retained-source run. Saveownership/acknowledgement faultcontrols are syntheticHTTP proof, not full nativeconditionalwritecertification. Return to name/description/metadataqueries, graph/history/conditionalwrites, centralregistration/hostedadmission, HyperDrivereplication/failover/loadbalancing and distributedsnapshots. PlaintextcanonicalJSON remains explicit. Preserveoriginaldatabase/password/identity/ACKs; serviceauthentication legitimatelychanges SQLitefiles, logicalrecordbaseline unchanged. See POCKETBASE_CQL_MVP.md.
