# DuckDB CQL MVP admission

Updated 29 October 2026. **Native/reopen verified; paired demo remains red.**

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
- Reopen SHA256: `6514F4513209A019DB36DEE1801DF81A8111E6CB0983561ECCFB17AB3E1C08F0`.
- Persistent seed SHA256: `01B37D20ADF928AF58B1557FF019C37C0C85AB12DA0016215B8D70487CBB2C35`.
- `LocalState/COSMIC/DuckDbCql` measured 1324005 bytes; retain original source.

## Exact next step

Add the Mongo-paired C#/HTTP/browser preset with provider provenance, request and
unavailable-provider controls, then validate/remove only the owned Mongo fixture.
Reuse the original DuckDB file/seed/receipts/snapshot read-only. Keep paired admission
red until actual verification. No MotherDuck/hosted/alias certification is implied.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
