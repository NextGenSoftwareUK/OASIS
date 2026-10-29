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

## Actual Windows replication/restore attempt — remains red

The pinned actual daemon ran with the original source and a dedicated file replica.
A child test waited for initial native LTX, committed one new probe table/marker,
and observed a subsequent native LTX object. That test passed1s. Original holons
were untouched. The daemon exited after the child; no Litestream process remains.

However, upstream Windows directory sync calls report **Access is denied** for
local LTX/replica directories and database close. Native restore produced a file
but exited1 at output-directory sync. A successful child or usable recovered file
cannot override these explicit native failures. Replication/restore acceptance
stays red; do not discard durability errors or retry/reseed the probe.

Read-only data inspection1passed194ms verifies all four original holon JSON hashes,
the committed probe, and restored-file CQL3/2. This proves recovered contents only,
not successful durable restore. The test's adapter activation touches only the
separate restored file. Original source/replica/probe/restore are preserved.

- Runner: `Scripts/TestHosts/Run-CosmicLitestreamReplication.ps1`.
- Probe/restore tests: `LitestreamReplicationTests.cs` in integration project.
- Data inspection: `TestResults/LitestreamCqlCurrent/litestream-restore-inspection.trx`.
- Inspection SHA256: `C555E5CF85263A13FE8C0E50F38747EE3AF545F77BB5A1B906F5B543087C4442`.
- Native log: `TestResults/LitestreamCqlCurrent/replication.log`.
- Native log SHA256: `4A0A8D207198E1C7D8A01D4B2D08414F86B6C76AD6010BA907FFE753DA037A1C`.
- Restore log: `TestResults/LitestreamCqlCurrent/restore.log`.
- Restore log SHA256: `594740FD32B7BAF38858D594B29C821A0012C61C27BB7654EABFC60A4B2A1613`.
- Persistent source/replica/shadow/restore/probe/journals total234462bytes.
- `replication.pending.json` is deliberately retained; runner refuses replay.

The first attempt's file URL was interpreted as invalid C:\C:\ and the child
command consumed backslashes. The documented YAML replica.path form and absolute
forward-slash child paths fixed parameter encoding; native source hashes were
reverified before that corrected attempt. Its original admission is preserved as
`replication-first-path-failure.json`. No probe existed at that earlier failure.

## Exact next step

Use the supported Linux Litestream binary in the existing WSL environment to
investigate/verify replication with native Linux filesystem semantics. Windows
convenience runtime remains a preserved failed platform outcome. No Linux dotnet
runtime is currently on PATH/in the inspected standard install directories; avoid
installing a duplicate SDK. Plan a verified SQLite backup/restore portability test
with existing tools while preserving original source/probe identities, then CQL
verification on the recovered file. Do not bypass fsync errors, reset original
replica, replay writes or claim Linux proof from Windows results. Mongo-paired
teaching coverage and remote/PITR/crash recovery remain separate open gates.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
