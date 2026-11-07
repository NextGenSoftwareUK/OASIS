# DuckDB CQL MVP admission

## Source recovery — 2 November 2026

The launcher and implementation were absent from the newer task branch. Named
files are restored from retained pin 237403a75b6983e013141cf7ba44bf66858abafd.
Nine focused CQL controls plus five provider identity tests pass; the native/Mongo
consumer compiles with the current Core. Launcher syntax passes. Original native
and browser acceptance below is historical evidence, not a newly replayed run.
The retained source, seed and four save ACKs were not opened or rewritten. No
new environment or native fixture was created. Hosted DuckDB remains unverified.


## Original branch integration — 30 October 2026

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
# SDK and HTTP checks, then serve the demo on loopback port 5116.
Scripts/TestHosts/Run-CosmicDuckDbCqlNative.ps1 -Federated -Serve

# Automatic checks and teardown.
Scripts/TestHosts/Run-CosmicDuckDbCqlNative.ps1 -Federated
```

Stop using the page button to complete verified temporary Mongo cleanup. This scope
proves bounded UUID selection/paging, not arbitrary content predicates, MotherDuck,
production central registration or complete DuckDB ORM parity.

## Exact next step

Continue missing provider MVPs from the all-provider inventory; leave this green
scope alone absent a reproduced bug. Full ORM/parity acceptance remains open.

[Breadth reconciliation](PROVIDER_BREADTH_RECONCILIATION_2026-10-30.md) / [status](CURRENT_STATUS.md).

The recovery laptop passed native and Mongo-paired DuckDB replay. Read-only
activation passed against the retained source and WEB4 opt-in registration
builds. Local Core candidate 6fcc2fead73f542480d30f981e24764683917b9c adds
DuckDB query tariff admission on top of SQL Server's pin. The WEB4 admission
fixture is ready for verification against that candidate before publication.
Development still requires placement of a qualified read-only database.

The retained demo file, LocalState/COSMIC/DuckDbCql/source.duckdb, is
1,323,008 bytes, with no adjacent WAL at qualification. SHA256:
DB649A3DD1A2FCE1694CBA1F9E303505596A3D3289B01F8166A6D232D3FFCA19.
It contains three teaching records and a tombstone. Core tariff pin and four
native/paired preset plans passed. For Development, first confirm persistent
placement and verify the file hash after transfer. Set
CQL_DUCKDB_QUERY_CONNECTION_STRING to the absolute mounted file with
access_mode=READ_ONLY. Enroll only the three live GUIDs from demo-seed.json in
the read catalogue, add query-only zero-cost admission and the four canonical
presets. Use Verify-HostedQuerySource.py DuckDBOASIS --readiness before
preparing a cohort. No file was copied or deployed during this check.

For the Development enrollment review, use a dedicated path such as
`/data/cql/duckdb/source.duckdb` on the verified WEB4 volume. Require the
qualified SHA256 after placement and check that no adjacent WAL exists before
setting `Data Source=/data/cql/duckdb/source.duckdb;access_mode=READ_ONLY`.
The retained seed and source snapshot agree on these three live IDs, in preset
order:

1. `0d3cbcef-c8cf-4083-af88-5a71de0486ef`
2. `3dc27e40-909f-4323-a6fe-15d835e332f1`
3. `37f7f99a-a41e-41a6-9259-07778fd410f2`

Use the first two for the subset and all three for the full preset. Keep the
tombstone `6888e38f-7d88-40bd-b599-d124b0f7e2be` outside the public catalogue.
The recovery-laptop file still matches the qualified digest. Remote placement
and activation remain open.

Generate the four preset values for the reviewed settings using the qualified
seed. The command prints only the public presets. Review its output as
`CQL_HOSTED_QUERY_EXAMPLES__DuckDBOASIS` alongside the scoped grant and tariff.

```powershell
$seed = Get-Content -Raw LocalState/COSMIC/DuckDbCql/demo-seed.json | ConvertFrom-Json
$provider = 'DuckDBOASIS'
$presets = foreach ($count in 3, 2) {
    $suffix = if ($count -eq 3) { 'records' } else { 'subset' }
    $selection = @{
        orderBy = 'None'
        take = 25
        includeMetadata = $true
        filters = @(@{
            field = 'Id'
            scalar = 'Guid'
            operator = 'In'
            value = @($seed.Ids | Select-Object -First $count)
        })
    }
    @{
        id = "public-$suffix"
        label = "DuckDB teaching records ($count)"
        request = $selection + @{ provider = $provider }
    }
    @{
        id = "mongo-public-$suffix"
        label = "DuckDB teaching records ($count) + my Mongo snapshots"
        operation = 'Federation'
        request = @{
            scope = 'Named'
            providers = @('MongoDBOASIS', $provider)
            source = $selection
            identityMode = 'PreserveCopies'
            maximumProviderAttempts = 2
            take = 25
        }
    }
}
ConvertTo-Json -InputObject @($presets) -Depth 8 -Compress
```

Offline verification against Web4Site `2170b67` passed for both generated
paired presets. The frontend preparation recipe selected the same first two
native IDs from the retained seed in both cases and preserved the original
three-ID and two-ID federation filters. Live browser acceptance remains open.

The WEB4 build dependency manifest declares DuckDB.NET Bindings.Full 1.1.3
and copies its Linux x64 and arm64 native assets. ELF headers matched their
declared architectures. This checks build packaging. Opening the exact
retained file under the deployed Linux runtime remains an acceptance gate.

The recovery laptop hub now runs the verified native and Mongo-paired demo:

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider DuckDB -Federated -RecoveryMongo -VerifyOnly
```

This invocation passed both phases and confirmed the temporary Mongo fixture
was removed. No test listeners remained. Reused the existing source and build
outputs in the recovery ledger. Hosted acceptance remains open.

## Linux retained-source qualification — 7 November 2026

The original database still hashesDB649A3DD1A2FCE1694CBA1F9E303505596A3D3289B01F8166A6D232D3FFCA19
and has no adjacent WAL. Its in-memory gzip transport is5381bytes, SHA256
4b0d0e62c3331148c174c6ed51636300dc33d3296b06811cdee7b39e79d5b01d.
The current retained224-byte seed hashesdc837edd1d778e8dc402d2b1b8712fe783faf4fef1387c4dc7f7e3fb518929de;
its three live IDs and tombstone match the placement plan above. The older
seed digest in historical evidence is not substituted for this exact input.

A dedicated Linux CI gate accepts only those exact compressed/source/seed
hashes, restores a separate read-only copy, and runs the real provider SDK.
It checks native CQL3/2/0 across two provider activations, unchanged file hash
and no WAL. It never calls the native seeding fixture, SaveHolon or Mongo.
The original Windows source and its save acknowledgements remain unopened
by the SDK. This is a new Linux portability gate, not replay of Windows
acceptance. Qualification remains pending until its dispatched run passes.

Linux run42385940278/job128443199243 passed the retained-source native case,
zero skips, on exactdd0ee7a2740770f962be8732a0f0d0d4a760c9c5. The real
DuckDB.NET SDK returned3/2/0 through CQL across two provider activations,
with unchanged database SHA256 and no WAL. No native seeding or Mongo
mutation occurred. Artifact13579442161 is7184bytes, SHA256
35620a1dd3a1408ae6cac7c95b79d25df286b7500697fe7817142f5b24da71d0,
retained90days. It contains the exact synthetic source, retained seed and
TRX. This closes the standalone Linux portability gate; shipping WEB4 native
asset packaging, protected volume placement, registration/admission, live
HTTP federation/privacy and browser acceptance remain separate gates.
[Retained-source Linux qualification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42385940278).

Owned hosted placement is now implemented behind explicit sandbox flags.
The bootstrap accepts a bounded gzip bundle bound to its native byte count
and SHA256, writes an exclusive intent/source/ACK generation, and requires
identical source bytes/receipt plus no WAL on restart. Partial state, symlinks,
changed data and writable/foreign SDK connection strings are refused without
repair or overwrite. The exact source path is/data/COSMIC/SandboxDuckDB/source.duckdb
with access_mode=READ_ONLY. DuckDB runs inside.NET, so the shared supervisor
also supports this explicit source without launching a native daemon.

All16 new runtime controls and all82 focused shared runtime controls pass;
no native IPFS bootstrap was replayed. Entrypoint shell syntax passes. These
checks use owned temporary synthetic byte fixtures, not the original database.
No DuckDB settings, hosted source or live enrollment has been applied. The
scoped image/publisher and live HTTP/browser gates remain open.

Guarded DuckDB publisher now prepares exactly six settings: explicit runtime
flag, compressed original bundle, fixed read-only connection, query-only
admission/tariff, public catalogue and four preset partition. It requires
matching retained database/seed Linux qualification, distinct reviewed IDs,
no WAL and shipping runtime bundle validation. No DNA or existing provider
settings are changed. Apply retains the shared per-key intent/ACK/no-replay
protocol and exact deployment fence. Acceptance verifies all six ACKs, full
configuration equality and mounted source hash/size/receipt/no-WAL before
and after queries. Explicit acknowledged discovery-visitor reuse is supported.

Six new publisher controls,28 response/journal controls and39 preset controls
pass. The shared response fixture was adjusted for a second retained-source
provider; its two earlier fixture failures are retained, not shipping/native
failures. Six protected settings are prepared under prefixduckdb-public;
none were applied. The original source receipt was materialized from verified
Linux CI/artifact evidence with exact database/seed hashes. New image checks
will require the shipped Linux native library and all of its dynamic links.
The existing DuckDB four-preset admission test is included with the five
catalogue controls in the next exact candidate CI gate (six cases total).
