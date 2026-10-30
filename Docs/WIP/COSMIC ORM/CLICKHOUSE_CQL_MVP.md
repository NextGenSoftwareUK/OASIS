# ClickHouse CQL MVP — controls verified; native and paired demo pending

Updated 30 October 2026. Inventory remains red. No native-server proof yet.

## Implemented scope

ClickHouseOASIS now implements ICosmicQueryStorageProvider using the shared bounded
identity compiler, bound to ClickHouse authority. Trusted complete catalogue at most
100 GUIDs; identity equality/membership/Boolean composition and stable paging.
Arbitrary predicates, metadata ordering and child graphs reject during admission.

The native reader uses one parameterized UUID IN query against oasis.holons FINAL,
with asynchronous ADO open/read/cancellation, execution timeout and native result
row/byte limits. FINAL applies ReplacingMergeTree version resolution. It does not
establish atomic conditional writes or a distributed snapshot. The stream checks
native identity/payload identity/read authority/duplicates before publishing rows,
enforces a cumulative4MiB decoded payload budget and never publishes partial rows
after native terminal failure. SDK wire-byte limiting remains unverified separately.

SaveHolon stages identity/copied provider keys/JSON until the native INSERT completes;
only then publishes caller ID/keys and IsSaved/SavedCount. Typed UUID/String/UInt8
parameters replace interpolated SQL for this save boundary. Explicit async_insert=0
requires synchronous insertion acknowledgement. No SDK-specific affected-row count
is assumed. HolonManager retains ownership of change-tracking acceptance.
Batch saves honor continueOnError and return acknowledged counts and real failures.
Failed activation clears stale active state; deactivation clears it too.

The existing history flag had no implementation. Holon saves and CQL now explicitly
reject true before native IO. Other avatar/general load/delete/history paths remain
an independent audit gap; they are not silently used by the new CQL adapter.

## Proving controls

28passed/0failed/0skipped: metadata4, compiler/admission9, saveownership6,
streaming query9. Provider and unit builds pass; final restores cache-only.
Tests cover authority mismatch, unsupported predicates/graphs, missing/invalid
plans/budgets, pre/active cancellation, identity/payload/duplicate/foreign corruption,
stream terminal failure/no partial output, stable paging, decoded budget stopping
enumeration, empty/no IO and missing records. Save tests exercise failed new/existing
identity, incomplete acknowledgement, published payload equality, unsupported history
and batch stop/continue/counts. IO is injected; these are not native backend tests.

[Compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.UnitTests/ClickHouseCqlTests.cs),
[query tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.UnitTests/ClickHouseNativeQueryBoundaryTests.cs),
[save tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.UnitTests/ClickHouseSaveOwnershipTests.cs).
Raw TestResults/ClickHouseCqlCurrent/controls.trx SHA256
`76d15bd6e1265641f29897760bdb3662a6916ccc05e22cde0721e88460d73954`.
Stale GetProviderVersion test removed rather than inventing a shipping API.

## Exact next step

No cached native ClickHouse image found. Select one official free local release,
journal runtime/source/authentication and retain original server/database/table
identities before business writes. Verify actual schema/parameter transport/native
3/filter2/missing0/page1/budget and original JSON/UInt64 version across real restart.
Never replay unknown writes or replace a baseline. Then Mongo-paired C#/HTTP/browser
5/4 and launcher tests; keep the inventory red until complete scoped proof.

Current ReplacingMergeTree versions default to server milliseconds, so concurrent
writes can tie; broader source CAS/history/transaction/replication/child/migration
semantics remain open. This bounded demo is not full EF/NHibernate parity.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
