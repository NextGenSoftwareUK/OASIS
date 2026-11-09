# SQL-family breadth checkpoint

28October2026. PostgreSqlFamilyProviderBase delegates to shipping PostgreSQLOASIS/Npgsql. Product-native acceptance must run on each actual product; inherited PostgreSQL evidence is not hosted-provider certification.

## Neon

- Replaced roughly400lines of duplicated storage code with a thin family adapter. Identity remains NeonOASIS; shared writes derive provider keys from ProviderType.Value. Constructor and usePooler option retained; version-control property/storage operations inherited.
- Project now references canonical PostgreSQL provider and its existing Npgsql9.0.3 dependency instead of separately pinning8.0.5. No new package downloaded.
- Neon single-host pooling uses the documented -pooler hostname, preserving explicit ports. Known Neon hosts use VerifyFull TLS; non-Neon/proxy/multi-host settings are preserved. usePooler=false preserves an explicitly configured hostname; it does not silently unpool an already pooled host.
- Four constructor/family controls reproduced red before consolidation; final10/10 unit cases pass (5new+5existing). Metadata fixture now supplies valid synthetic Npgsql syntax instead of the literal test string. No network operation in these controls.
- Cached producer/consumer builds passed with2warnings each. Shared parent files/Core/Shared public API unchanged. Canonical shared-suite binding, hosted Neon TLS/pooler/auth acceptance, existing schema/data compatibility and complete relevant interface certification remain open. No schema migration/reset or hosted DB was attempted.

Connection behavior source: [official Neon pooling guide](https://neon.com/docs/connect/connection-pooling). Reproduce using existing provider build lease, bounded cached build of NeonOASIS then NeonOASIS.UnitTests, --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false; dotnet test the unit project --no-build --no-restore.

Continue PostgreSQL-wire wrappers through this base where their native semantics fit. Preserve product-specific tenant/context behavior; do not replace a different transport or erase such behavior just to produce a thin class. Other SQL engines retain their own real native clients.

## DuckDB

[Native baseline](DUCKDB_STORAGE_BASELINE.md):52/52 plus6native controls on pinned real DuckDB.NET1.1.3. Thin SQL transport reuses KeyValueStorageProviderBase; default soft deletion preserved, native revision predicates verified, legacy files refused without changes. MotherDuck/cloud, version/concurrency stress, full interfaces and global routing remain separate gates. FlyIO/Nile were inspected and already share PostgreSQL storage, so no cosmetic rewrites were made.

## MotherDuck

28October2026: replaced duplicated storage with the shipping DuckDB SQL/record adapter, retaining MotherDuck identity and constructor. The token is URI-escaped inside the md: Data Source rather than a separate ADO parameter; local-file configuration and conflicting token sources are rejected. No process-wide credential mutation. See [official connection syntax](https://duckdb.net/docs/connection-string.html).

Cached producer/consumer builds passed (2warnings each); 10/10 metadata/configuration tests passed without opening any connection. Unit runtime copies are disabled because this host does not execute native operations; real native acceptance belongs in the shared integration host. An obsolete test for a nonexistent GetProviderVersion method was removed.

No new package/runtime/download. Producer bin/obj:7,906,072/858,177B; unit bin/obj:40,448,538/1,021,661B, no native runtimes directory. Hosted canonical52, authentication, pinned SDK1.1.3 extension compatibility and selected-catalog handling remain open. The shared legacy-table guard currently scopes main schema without a catalog predicate; attached MotherDuck catalogs require acceptance/review before certification. Local DuckDB52 does not certify MotherDuck cloud.
