# Litestream CQL MVP admission

Updated29 October2026. **Native SQLite/reopen pass; sidecar replication/restore and paired demo
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
Native execution with these cached components now passes.

- Tests: LitestreamCqlTests.cs and LitestreamOASISProviderTests.cs in LitestreamOASIS.UnitTests.
- Evidence: `TestResults/LitestreamCqlCurrent/litestream-cql-controls.trx`.
- SHA256: `F182AE037E1A7BE97786D3A8AADBB0C6E9F2616D4619DB1252D4BC03568540CE`.

## Native SQLite/reopen proof

One fixed owned source contains3live records and1tombstone. Four saves report
IsSaved/SavedCount after the native upsert; live loads verify names/creator.
Native three/two/missing queries, tombstone exclusion, paging and result-budget
rejection pass. Original JSON hashes/tombstone columns remain unchanged after
queries and a fresh process reopening the same source. No replica proof follows.

- Fixture: `LitestreamCqlNativeTests.cs` in LitestreamOASIS.IntegrationTests.
- Runner: `Scripts/TestHosts/Run-CosmicLitestreamCqlNative.ps1`.
- Initial1passed744ms/no skips; `TestResults/LitestreamCqlCurrent/litestream-cql-initial.trx`.
- Initial SHA256: `F402CBB685CF16EA737ED85F07087468676710E240B8C46BA4430EA81B687810`.
- Reopen1passed197ms/no skips; `TestResults/LitestreamCqlCurrent/litestream-cql-native.trx`.
- Reopen SHA256: `7C17DAD8B27F1881C746D5F574D3CD44389FE0853061E7F2AE1F3AD91FAC9565`.
- Seed SHA256: `7B5A1E213D6D40E923B622359B582A6DECA22EFDCC310FCF2CFB71F390C46F3E`.
- LocalState/COSMIC/LitestreamCql measured62349bytes; retain original source.

SaveHolonAsync now requires exactly one affected SQLite row before reporting
IsSaved=true/SavedCount=1; native tests verify those acknowledgements.

## Sidecar resource admission

No installed binary was available, so one official v0.5.17 Windowsx86_64 release
was acquired and hash-checked. Reusable runtime:
`C:/Users/david/AppData/Local/OASIS/tools/litestream/0.5.17` (39984512bytes).
Executable SHA256: `271ECB4EC49A4895A98A8FDD884113034DACC9D75B4A8D7A1B02322BAE7FC7B7`.
The verified13,870,760byte ZIP was removed after extraction/version check.
[Official release](https://github.com/benbjohnson/litestream/releases/tag/v0.5.17)
provides Windows convenience binaries but does not officially support Windows;
any acceptance here must retain that platform limit. No daemon/replica started yet.

## Exact next step

Run the pinned actual sidecar against the original source and a dedicated local
file replica, then restore to a distinct owned path and prove matching records/CQL.
Use upstream [file-replica workflow](https://litestream.io/guides/file/) and
[restore contract](https://litestream.io/reference/restore/), preserve original
source/replica/unknown outcomes, stop exact owned daemon and retain compact proof.
Then add Mongo-paired C#/HTTP/browser. Remote storage/PITR/crash recovery remain
separate gates; do not mark complete from local SQLite alone.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
