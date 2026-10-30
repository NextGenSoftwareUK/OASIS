# QuestDB CQL MVP — save/schema implemented, native pending

30October2026. Inventory remains red. No native storage/runtime allocated.

## Verified save-state ownership

Holon IDs/provider keys are staged in canonical camel-case runtime JSON, then
published only after exactly one native INSERT row is acknowledged. Zero/unknown/
multiple row counts and open/transport errors return errors without publishing
caller state. Existing IDs/key dictionaries remain untouched on failure; no retry.
HolonManager retains tracking acceptance ownership. Batches count acknowledged
rows, preserve errors and honour stop/continue. History and documents over the
1MiB adapter budget reject before opening a connection.

One connection factory now owns all native connections; original connection-string
constructor is preserved. Factory returns fresh provider-owned DbConnections.
Open uses a15second cancellation deadline; commands retain native15second timeout
and typed Npgsql VARCHAR parameters. No dynamic SQL values are interpolated into
the INSERT. Existing native epoch timestamp/deduplication strategy is retained;
its real schema/consistency behavior still needs acceptance, not assumed from tests.

**16passed/0failed/0skipped**, including5identity/baseline cases and11save tests.
[Tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.UnitTests/QuestDbSaveOwnershipTests.cs).
They check bound SQL/typed parameters, derived fields/decimal JSON, acknowledgement
counts, failure-state preservation, connection disposal and batch behavior.
Injected ADO controls are not native database proof.
Evidence `C:/Source/OASIS-cosmic/TestResults/QuestDbCqlCurrent/save-controls.trx`,
SHA25621D61BABE7B42250954D870B3F1604566AD176833E891F93BDC701457EAC18E2.
Bounded leased builds/cache-only restores pass. Existing Npgsql9.0.3 (also pinned
by PostgreSQL provider) replaces uncached8.0.3; no package/runtime download.

## Next

1. Implement bounded native multi-record CQL with capability admission, response
   identity checks, stable paging and explicit read-consistency limits.
2. Create/reuse one free retained native environment; verify original record/update
   behavior, JSON precision/schema and restart without reseeding.
3. Add Mongo-paired C#/HTTP/browser preset and original-source postpair checks.

## Native audit and remaining gaps

- Activation is now read-only and verifies metadata/schema as described below.
  This does not certify pre-existing row uniqueness or correctness of native writes;
  enabling native DEDUP on old data does not repair earlier duplicates.
- Schema uses `(id SYMBOL,data VARCHAR,ts TIMESTAMP)` with designated ts, DAY WAL
  DEDUP(ts,id), always epoch timestamp. This can co-locate all current holons in one
  old partition; retention/scale/history placement needs design, not a time-series
  performance claim. DEDUP alone is not conditional-write or cross-client CAS.
- Generic reads still select1000 without overflow/identity/JSON bounds; child/depth,
  metadata query/version semantics, history and wider ORM behavior are unverified.
- [QuestDB SQL compatibility](https://questdb.com/docs/concepts/deep-dive/sql-extensions/)
  explicitly lacks row-level DELETE. Existing hard-delete SQL is invalid; refuse it
  explicitly rather than emulating destructive partition/table operations.
- Selected soft-delete paths ignore failed load/save results, producing false success;
  these need proper result forwarding before any delete claim.
- Native WAL acknowledgement/read visibility/durability are distinct; the
  [transaction-applied guide](https://questdb.com/docs/cookbook/operations/check-transaction-applied/)
  describes writerTxn/sequencerTxn checks. Do not infer immediate visibility or a
  distributed snapshot from the current PG INSERT acknowledgement.
- Other avatar write ownership, schema migration, replication, history, conditional
  writes and full EF/NHibernate parity remain red.

RootPodman had no QuestDB image, retained `/var/tmp/oasis-questdb-cql` absent;
other-session matrix records native not run. No unrelated environment adopted,
no native account/service or dataset created. Continue from the next schema/CQL
steps and retain62/226 scoped-demo coverage until independent native/pair proof.

[Current status](CURRENT_STATUS.md) / [ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Explicit table/WAL/dedup admission

**36passed/0failed/0skipped**, including20 schema tests and prior16 controls.
Activation clears its active flag before fresh checks, opens one owned connection,
and reads only `tables()` and `table_columns()` metadata for the three explicit
provider tables. It no longer creates storage or retries PostgresException.
All three tables require positive native IDs, exact names, designated ts, DAY
partitioning, WAL+DEDUP enabled, disabled TTL and no materialized view. Require
exact id:SYMBOL,data:VARCHAR,ts:TIMESTAMP order/types, ts designated, and only
id/ts as upsert keys. Missing/duplicate/extra metadata or transport failure rejects.
No schema-altering remediation or replacement source is attempted.

Metadata lookup binds table name as a VARCHAR parameter; table_columns receives
only the fixed private provider table constants. Up to2table rows/4column rows
are read to detect overflows, with15second metadata deadline plus command timeout.
Opening has its own15second deadline. Readers/connections are disposed on failure.

[Official metadata contract](https://questdb.com/docs/query/functions/meta)
and [dedup configuration rules](https://questdb.com/docs/concepts/deduplication/)
define these checks. Explicitly provision matching tables before activation; e.g.
`CREATE TABLE oasis_holons (id SYMBOL,data VARCHAR,ts TIMESTAMP) TIMESTAMP(ts) PARTITION BY DAY WAL DEDUP UPSERT KEYS(ts,id);`
and corresponding avatar/avatar-detail tables. Audit existing data before enabling
DEDUP; activation does not repair historical duplicates or alter their timestamps.
Future query paths must independently reject duplicates/wrong timestamp lineage.

[Schema tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.UnitTests/QuestDbSchemaTests.cs)
prove all3metadata/schema pairs execute, invalid configuration rejects, transport
errors do not poll, and failed reactivation clears prior state. These remain injected
ADO controls, not a real QuestDB certification.
Evidence `C:/Source/OASIS-cosmic/TestResults/QuestDbCqlCurrent/schema-controls.trx`,
SHA2569C310665B52B18D3AC2C0E2E71149D20386D0DF42D8C3A33670FD8256D8136DD.
Bounded leased build/tests pass, no dependency restore/download needed. No native
service/source/dataset was created; inventory stays red and62/226coverage unchanged.
