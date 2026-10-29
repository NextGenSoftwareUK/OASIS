# Litestream CQL MVP admission

Updated29 October2026. **Native SQLite, sidecar replication/restore and paired demo
acceptance remain red.** Local SQLite queries alone cannot certify Litestream.

## Implemented scope

An explicit authoritative catalogue of at most100 UUIDs becomes parameterized native
SQLite IN selection, tombstone-column filtering, binary UUID order and LIMIT/OFFSET.
The adapter uses the existing save/load tables, connection and JSON decoder.
Unsupported content predicates, arbitrary ordering and children reject; no managed
filter or whole-source scan substitutes. Empty admitted selections return without
native I/O. Payload budget4MiB is checked after decoded string allocation; no
pre-allocation bound or prompt in-flight SQLite interruption is claimed.

This provider accesses a local SQLite file. Litestream's separate replication
service is not implemented or simulated by the CQL adapter. No remote replica,
restore, failover, encrypted storage or complete ORM parity claim is made.

## Verified controls

Fourteen pass, zero failed/skipped,476ms: nine CQL controls and five provider
metadata/initial-state checks. The old metadata test referenced a removed version
method; it now verifies the actual storage-category contract without a shim.

The original Microsoft.Data.Sqlite10.0.12 metapackage was absent from the cache.
The project now explicitly uses existing workspace Microsoft.Data.Sqlite.Core10.0.9
and SQLitePCLRaw.bundle_e_sqlite3 2.1.11, avoiding duplicate runtime downloads.
Native execution/version compatibility must still be verified.

- Tests: LitestreamCqlTests.cs and LitestreamOASISProviderTests.cs in LitestreamOASIS.UnitTests.
- Evidence: `TestResults/LitestreamCqlCurrent/litestream-cql-controls.trx`.
- SHA256: `F182AE037E1A7BE97786D3A8AADBB0C6E9F2616D4619DB1252D4BC03568540CE`.

## Exact next step

Admit one owned fixed SQLite source under LocalState/COSMIC/LitestreamCql, persist
setup intent before writes, then verify save/load, three/two/missing query results,
paging/budget/tombstones and unchanged source across reopen. Preserve uncertainty.
Inspect existing Litestream sidecar installations/cache before any new allocation;
prove actual replication and restore separately before claiming a branded complete
Litestream demo. Add Mongo-paired C#/HTTP/browser coverage over the same admitted
source. Keep remaining gates red rather than substituting SQLite proof for sidecar
acceptance.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
