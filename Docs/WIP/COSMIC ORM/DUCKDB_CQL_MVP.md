# DuckDB CQL MVP admission

## Current-branch integration —30 October2026

Selective port from preserved f89cf0085. Fourteen controls, one retained-native
scenario and one Mongo federation scenario passed with zero skips. Independent
browser full5/subset4 verified. Original three live rows/tombstone, native JSON/
timestamp snapshot, seed and four save acknowledgements unchanged.

```powershell
Scripts/TestHosts/Run-CosmicDuckDbCqlNative.ps1 -Federated -Serve
```

Open http://127.0.0.1:5116/, run both buttons, then stop locally and wait for cleanup.
Dedicated5116 avoids PostgreSQL5114. Runner builds producer before consumer,
refuses an existing seed with a missing database file, archives successful reports
by hash and preserves unresolved failures. No replacement/reset/reseed.

Current raw evidence under `TestResults/DuckDbCqlCurrent/`:

| Report | SHA256 |
|---|---|
| duckdb-cql-current-controls.trx (14passed) |EEA9EA57E8F7D5EF039B390231AC9CCF6B82E25368AC9ADE992CDC3FFF734D88 |
| duckdb-cql-native.trx (1passed) |6A25FE316C37EE1D971050141F522385307232423B9739F68D35A1DE78EE9CB5 |
| duckdb-mongo-federation.trx (1passed) |E10057CFB6E957C170F7C94CA0666FB483A36295BAB45CED45EB82D1C1818E82 |

Native browser proof: `duckdb-mongo-subset-current.png`. Exact temporary Mongo
fixture verified/dropped, pending/ready absent, runner exit0,27029/5116 stopped.
Embedded connections closed: source accepted an exclusive read handle afterward.
Retained source10files1324005B, unchanged; no install/download/new environment.

Public section22 verified: Web4Site main98823191bfb8c2f792bafa4bce172fbf21d5792c,
Production7452699632success, expanded command/fixed5116link visible. Screenshot
`TestResults/DuckDbCqlCurrent/duckdb-catalogue-public.png`, SHA256
4C3BCC7C61DDA75A0771549FC10B3356F2F0C0FD78D257A4BB3C4AB0EFF6B977.
Backend remains task-branch only; no Railway deployment. Hosted selector unchanged.
Historical proof below retains old5114, superseded by5116. Plaintext/bounded-query/
native cancellation/registration/HyperDrive/parity limits below remain open.

Updated 29 October 2026. **Scoped native/reopen and Mongo-paired C#/HTTP/browser MVP verified.**

The adapter binds the shared bounded UUID selection compiler to DuckDBOASIS.
An explicit trusted catalogue (at most100 UUIDs) becomes parameterized native IN
selection, native tombstone filtering and LIMIT/OFFSET. The existing save/load
JSON decoder is shared with query materialization. No managed predicate or source
scan substitutes for unsupported shapes. A 4 MiB aggregate decoded payload budget
is checked after allocation. Errors return no partial result. Native driver async
methods receive the caller token; prompt interruption of embedded execution is not
claimed without native cancellation evidence. Federation reads providers separately.

## Verified controls

Fourteen tests pass, zero failed/skipped,548ms: nine CQL controls plus five provider
metadata/initial-state checks. Covers catalogue capture/authority, unsupported query
shapes/bounds, inactive/pre-cancelled calls, alias isolation, empty native-free
selection and deactivation. Existing DuckDB.NET.Data.Full1.1.3 packages are reused
from the NuGet cache; producer and consumer build successfully.

The old test called GetProviderVersion, absent from the current provider interface.
It now verifies the actual storage category contract. Constructor name/description
were missing and now identify the shipping provider. No compatibility shim added.

- Test: `DuckDbCqlTests.cs` and `DuckDBOASISProviderTests.cs` in DuckDBOASIS.UnitTests.
- Evidence: `TestResults/DuckDbCqlCurrent/duckdb-cql-controls.trx`.
- SHA256: `DC8C7E95521BF34463285F0719AEBE50224B52401E55856A85B40BA19E9146C1`.

## Native/reopen proof

The original embedded source has three live records and one tombstone. Four save
acknowledgements were recorded; live records load with expected names and creator.
CQL returns three/two/zero live rows, excludes the tombstone, applies native paging
and rejects an undersized result budget without partial data. Native document
hashes and updated timestamps remain unchanged through queries and a fresh test
process reopening the file. No separate server or listener was created.

SaveHolonAsync now sets IsSaved=true/SavedCount=1 only after the existing native
upsert completes successfully. Native write assertions prove this acknowledgement;
broader save/concurrency/history parity remains open.

- Fixture: `DuckDbCqlNativeTests.cs` in DuckDBOASIS.IntegrationTests.
- Runner: `Scripts/TestHosts/Run-CosmicDuckDbCqlNative.ps1`.
- Initial: one passed936ms, zero failed/skipped.
- Initial evidence: `TestResults/DuckDbCqlCurrent/duckdb-cql-initial.trx`.
- Initial SHA256: `18179E3ABB8FF34838A7BD970CEFFD855CA518CC5D65E14305E6A08FBF0E15BD`.
- Reopen: one passed236ms, zero failed/skipped.
- Reopen evidence: `TestResults/DuckDbCqlCurrent/duckdb-cql-native.trx`.
- Latest native/reopen preflight SHA256: `394035EF4BD78354B0E4F0729BBF75C41355F2C31DDF58BA77EBC78106154372`.
- Persistent seed SHA256: `01B37D20ADF928AF58B1557FF019C37C0C85AB12DA0016215B8D70487CBB2C35`.
- `LocalState/COSMIC/DuckDbCql` measured 1324005 bytes; retain original source.

## Mongo-paired teaching preset

Shared C#, HTTP and browser queries return five physical rows (DuckDB3 + Mongo2)
and four for the two-ID subset. Provider copies remain labelled and distinct.
Forged actor/unknown preset reject; an inactive DuckDB target returns unavailable
without partial rows. The temporary three-record Mongo database was verified and
dropped; original DuckDB source and tombstone remain unchanged.

- Fixture: `DuckDbMongoCqlNativeTests.cs` in DuckDBOASIS.IntegrationTests.
- Result: one passed22s, zero failed/skipped.
- Evidence: `TestResults/DuckDbCqlCurrent/duckdb-mongo-federation.trx`.
- SHA256: `9A2749F4CCB2565B31F63B469E5B15BD548C8F56863408B47DA3444101B3BB83`.
- Browser: `TestResults/DuckDbCqlCurrent/duckdb-mongo-browser.jpg`.
- Runner91188 exited0, owned Mongo/HTTP listeners absent, fixture journal removed,
  browser tab closed. Embedded connections are disposed; source is retained.
- Hub: 8 single/16plans, 23 paired/46plans, 32 paired rejections and3 archive
  rejections pass; listeners unchanged.

```powershell
# Inspect without services.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider DuckDB -Federated -ShowPlan

# SDK/HTTP checks followed by browser service on loopback5114.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider DuckDB -Federated

# Automatic checks and teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider DuckDB -Federated -VerifyOnly
```

Stop using the page button to complete verified temporary Mongo cleanup. This scope
proves bounded UUID selection/paging, not arbitrary content predicates, MotherDuck,
production central registration or complete DuckDB ORM parity.

## Exact next step

Continue missing provider MVPs from the all-provider inventory; leave this green
scope alone absent a reproduced bug. Full ORM/parity acceptance remains open.

[Breadth reconciliation](PROVIDER_BREADTH_RECONCILIATION_2026-10-30.md) / [status](CURRENT_STATUS.md).
