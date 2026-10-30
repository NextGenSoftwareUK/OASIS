# QuestDB CQL MVP — save ownership implemented, native pending

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

1. Validate explicitly provisioned QuestDB schema, WAL/dedup keys and actual table
   readiness. Remove implicit creation/broad PostgresException polling from activation.
2. Implement bounded native multi-record CQL with capability admission, response
   identity checks, stable paging and explicit read-consistency limits.
3. Create/reuse one free retained native environment; verify original record/update
   behavior, JSON precision/schema and restart without reseeding.
4. Add Mongo-paired C#/HTTP/browser preset and original-source postpair checks.

## Native audit and remaining gaps

- Existing activation creates tables and retries arbitrary PostgresException; it
  lacks schema/dedup admission and stale-active clearing. Still an open root gap.
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
