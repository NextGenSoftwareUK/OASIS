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
