# Redis CQL MVP - controls pass, native demo pending

29 October 2026. RedisOASIS implements ICosmicQueryStorageProvider over its existing
encrypted key-value backend/StackExchange.Redis transport and shared ID compiler.

## Scope

Exact Redis authority/catalogue; ID equality/membership, Boolean algebra and paging,
at most100 selected keys. Unsupported metadata/content/order/children fail before
I/O. Reads use canonical RecordKey and original encrypted backend, never scan the
keyspace to evaluate a CQL predicate. Any failure/cancellation discards all rows.
Result/post-decode4MiB budgets apply; no database-wide/distributed snapshot claim.

Derived aliases retain their actual QueryProviderType and receive no query
capabilities until separately admitted. KeyDB/Valkey/etc do not automatically gain
Redis CQL support or certification from inheritance. No central BootLoader active
Redis case was found; direct demo registration and production registration are
separate gates. No full ORM/history/HyperDrive parity claim.

## Verified controls

RedisCqlTests:8/8,zero skips,342ms. Frozen authority catalogue and caller selection,
unsupported shapes, inactive/pre-cancelled requests never resolve storage keys,
and unadmitted alias identity/capability rejection. No native Redis was started.
Report `TestResults/RedisCqlCurrent/redis-cql-controls.trx`, SHA256
`4C0DD5727D57B40550F5E8D46B2946D6D27585C6965C7B889E983D05839AC400`.

## Reusable native resources and exact next step

Existing stopped `oasis-redis-retained` container:
`48458f978518783740065970cccbf74f6f8626a97660c625467fa561bf4d8005`;
image`5d79a9ce29f8b3fe50fd5c07ee1bf654b5cec557b895835825bf6825d525e4f2`,
Redis8.2.2, volume`oasis-redis-data`, loopback18679. Existing retained authentication
and storage-key DPAPI files at LocalAppData/OASIS/ReusableProviderTests/Redis are
reused, not recreated or printed. Use the reviewed retained-container helper;
never reset authentication after a rejected credential or disable protected mode.

Admit one dedicated COSMIC database/prefix only after native empty-store inspection;
never flush/adopt an occupied database. Persist fixed database choice, source IDs,
key reference and pending intent under LocalState/COSMIC/RedisCql before3 saves.
Reuse that same dataset for native3/2/missing, full restart and exact ciphertext
integrity proof, then Mongo-paired C#/HTTP/browser5/4. Native source has not been
created yet. Logs/TRX only TestResults/RedisCqlCurrent. No new runtime/environment
or dependency download; cached restore rebuilt cleaned assets only.

[Status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
