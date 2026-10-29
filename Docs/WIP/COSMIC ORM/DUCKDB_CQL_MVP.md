# DuckDB CQL MVP admission

Updated 29 October 2026. **Native/restart and paired demo remain red.**

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

## Exact next step

Create one fixed owned embedded source at LocalState/COSMIC/DuckDbCql only after
persisting initial intent. Prove three acknowledged saves/load round trips, native
three/two/missing queries, paging/budget/tombstone behavior and unchanged records
across process reopen. Preserve missing/uncertain original outcomes instead of
recreating them. Then add Mongo-paired C#/HTTP/browser preset and owned fixture
cleanup. Canonical JSON is plaintext; no MotherDuck/hosted/alias certification is
implied. Keep inventory red until native and paired proof are complete.

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).
