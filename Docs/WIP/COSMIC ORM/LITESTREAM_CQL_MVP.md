# Litestream CQL MVP admission

Updated29 October2026. **Scoped Linux native/live replication/restore and Mongo-paired SDK/HTTP/browser MVP verified.**
Windows sidecar failure remains a separate red platform outcome.

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

## Supported Linux execution setup

Existing Ubuntu26.04 WSL is reused. No Linux runtime/portable test runner was
available. Installed one minimal .NET/ASP.NET10.0.12 runtime (no SDK), pinned native
Litestream0.5.17, and missing official Ubuntu libicu78 78.2-2ubuntu1. Default
globalization is preserved. Verified download archives were removed.

- Runtime: `/var/tmp/oasis-runtimes/dotnet-10.0.12`,109,735,717bytes including admission.
- Sidecar: `/var/tmp/oasis-runtimes/litestream-0.5.17`,38,989,116bytes including admission.
- ICU dpkg Installed-Size38,705KiB; reusable shared OS dependency.
- Reproducible admission: `Scripts/TestHosts/prepare-cosmic-litestream-linux.py`.

The Windows SDK runner cannot load on Linux (BadImageFormat); no SDK clone or
compatibility override was used. The official
[portable test platform](https://www.nuget.org/packages/Microsoft.TestPlatform.Portable/18.11.0)
is restored once in the existing NuGet cache and read directly from its WSL mount.
It supports major runtime roll-forward itself. Test discovery succeeds for all
seven existing test methods; no Linux source or native execution proof yet.

- Portable runner18.11.0 cache: `C:/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0`,47341656bytes.
- Runner DLL SHA256: `78509A95B634BEF64D081D5F13F3733D81424BE0667BB8AE614325E155D11273`.
- Discovery evidence: `TestResults/LitestreamCqlCurrent/linux-test-discovery.log`.
- Discovery SHA256: `5DA733C1748603AFBB4A8006A77398D0BE06B5C177368FA5A14DB054A6C09636`.

## Actual supported-Linux verification

SQLite online backup imported the original complete schema/rows to native WSL
storage without reseeding. Original actor/holon identities,4JSON/tombstone hashes,
4save receipts and inherited Windows probe are preserved. Full source/backup SQL
dumps matched. Import backup SHA256:
`3b0b7bdf02a6caa3f82fb4c7f833e33a73a7a61bfe911754257391af58995320`.

The actual compiled provider passes native selection/paging/budget/tombstone tests
under Linux. The pinned actual sidecar creates an initial native LTX replica; the
.NET child test validates the inherited marker and commits a distinct probe while
replication is running, then observes a subsequent native LTX object. Daemon exit
and native restore both succeed without directory-sync/errors. Restored4holon
hashes, both probe markers and shipping-adapter CQL3/2 pass. No global snapshot,
remote storage, abrupt-crash, PITR or Windows durability claim follows.

- Admission: `Scripts/TestHosts/import-cosmic-litestream-linux.py` (one-time backup;
  refuses existing admission), preserving failed Windows state.
- Runner: `Scripts/TestHosts/run-cosmic-litestream-linux.py` under existing Ubuntu.
- Native source/replica/restore/journals: `/var/tmp/oasis-litestream-cql`,172,398bytes.
- Native1passed/no skips: `TestResults/LitestreamCqlCurrent/litestream-linux-native.trx`.
- Native SHA256: `F64D8E0777CF24002D9D4130C19D3A48553BA747347D56EC57580358B0FC5D2B`.
- Live probe1passed1s/no skips: `TestResults/LitestreamCqlCurrent/litestream-linux-probe.trx`.
- Probe SHA256: `B9D6A3C3C18EDF6CA65DA429510FEB29BD428D532EC08EFE5C653362043E4775`.
- Restored CQL1passed/no skips: `TestResults/LitestreamCqlCurrent/litestream-linux-restore.trx`.
- Restore SHA256: `06C94A3D8DDCF37A5E67C7796A3CFFCB4334C019BA7B975DE0C986DFC7642DF8`.
- Native logs: `linux-replication.log`, `linux-restore.log`, `linux-restore-test.log`
  under the same TestResults folder. No daemon remains running.

A successful receipt allows subsequent native/source/restored checks without
replaying the live probe, recreating the replica or restoring over the original
recovered file. Final-source runner rerun passes with freshness/count checks for
one passed test and zero skips per scope. Original Linux/Windows evidence retained.

## Verified Linux Mongo-paired teaching demo

The actual Linux provider source and Linux Mongo return five physical rows, then
four for the two-ID subset, with explicit provider copies in C#, HTTP and browser.
Invalid actor/preset reject; an inactive Litestream target returns unavailable
without partial rows. The exact temporary Mongo database is verified/dropped.

- Fixture: `LitestreamMongoCqlNativeTests.cs`, with
  `LinuxMongoDemoEnvironmentTests.cs` for owned replica admission/reuse.
- Browser run1passed25s; launcher automated repeat1passed1s; zero failures/skips.
- Current evidence: `TestResults/LitestreamCqlCurrent/litestream-mongo-federation.trx`.
- Paired SHA256: `D997ED81B558245746BCD04A34CA518106331674E0A31AF10863D07BDA1810D8`.
- Replica init/identity proof: `TestResults/LitestreamCqlCurrent/linux-mongo-init.trx`.
- Init SHA256: `EDACD3EA29E362DE3E792F1263D6EBF8654D8F131B4B86B225052DB2BAA1E649`.
- Browser: `TestResults/LitestreamCqlCurrent/litestream-mongo-browser.jpg`.
- Hub:8single/16plans,25paired/50plans,36paired rejections,3archive rejections pass;
  listeners unchanged by planning checks.
- Runner40826 and launcher92578 exited0, owned daemon SIGTERM/wait exit0;
  Linux27039/5116 listeners absent, fixture journal removed and browser tab closed.

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Litestream -Federated -ShowPlan
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Litestream -Federated
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Litestream -Federated -VerifyOnly
```

The page's stop button completes cleanup. The original Linux source, replicas,
probe receipts and reusable Mongo store are retained; no probe replay/reseed.

## Linux Mongo resource scope

WSL NAT cannot share Windows Mongo's loopback service. No firewall/network binding
was widened. One reusable Linux Community8.3.13 daemon was acquired from the vendor
catalogue and digest-verified; only daemon/licenses extracted, installation archive
removed. Native execution on existing Ubuntu26 passes, independently of Windows8.3.11.

- Runtime `/var/tmp/oasis-runtimes/mongodb-8.3.13`:169,204,667bytes including admission.
- Executable SHA256: `6e449a554aca8ba29c91c6d640d47a6ed32c1856384925b707ca684c3743570c`.
- Fixed store `/var/tmp/oasis-cosmic-mongo`:211,031,587bytes; loopback27039,
  replica set cosmicLinuxDemoRs, ordinary journaling,0.25GBcache/32MBoplog.
- Init journal precedes native configuration. Unowned configuration/pending outcomes
  refuse adoption/replay. Later runs verify the original member/config identity.
- Setup `prepare-cosmic-linux-mongo.py`; runner
  `run-cosmic-litestream-mongo-linux.py`, both under Scripts/TestHosts.

This certifies the linked local Linux demo only. Windows sidecar fsync failure,
remote/PITR/crash recovery, production central registration, broader queries,
concurrency/children/history and full ORM parity remain open.

## Exact next step

Continue the next red provider MVP from the inventory; inspect Memcached's existing
implementation and reusable native resources. Preserve this green Linux scope,
Windows failure and all owned source/identity/replica receipts. No further hardening
without a reproduced bug; deeper parity remains deferred for breadth.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
