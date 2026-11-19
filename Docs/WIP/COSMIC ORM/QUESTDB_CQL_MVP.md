# QuestDB CQL MVP — bounded implementation, native pending

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

1. Create/reuse one free retained native environment; verify original record/update
   behavior, JSON precision/schema and restart without reseeding.
2. Add Mongo-paired C#/HTTP/browser preset and original-source postpair checks.

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

Publication checkpoint: implementation commit4fe8db7e3 is local; GitHub rejected
two push attempts with internal server error30Oct14:22UTC. Remote task ref verified
still c4b4a0bc0 after first failure. Preserve local commit, retry normal publication
with a later verified batch; no force push or source rollback.

## Bounded CQL implementation

**65passed/0failed/0skipped**: prior36save/schema controls,9compiler/adapter cases,
20query/empty-catalogue cases. Single native parameterized SELECT over the trusted
complete catalogue<=100GUIDs, with LIMIT selected+1 and no managed full-table scan.
Capability admission only permits captured identity equality/membership/Boolean
selection and stable admitted-key paging. Unsupported names/metadata/order/graphs
reject. Empty catalogue/pre-cancellation open no connection; inactive/history
reject explicitly.

Projection is `id,cast(ts AS LONG) AS cosmic_ts,data`. The raw Int64 timestamp must
be0(epoch), avoiding driver timezone conversion and refusing mixed timestamp
lineages. Native IDs must be canonical, unique and selected, JSON payload IDs must
match, and read authority is rechecked before publication. Missing/deleted rows
are omitted; duplicate/foreign/overflow/null/projection/transport failures return
no partial rows. Metadata admission alone does not guarantee old row uniqueness.

Data is consumed through sequential TextReader with strict incremental UTF8
encoding:1MiB per document/4MiB aggregate, preserving surrogate pairs split across
4096character chunks. Final page/result budget is checked separately. A linked
15second query deadline covers async opening, command/reader and text processing;
connections/readers are disposed on failure. These row/document limits do not
promise a fixed database scan cost or a distributed snapshot.

[Official temporal conversion semantics](https://questdb.com/docs/query/sql/alter-table-change-column-type/)
confirm TIMESTAMP numeric values are microseconds since epoch; real SQL/PG driver
acceptance remains a native gate. [Compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.UnitTests/QuestDbCqlTests.cs)
and [query tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.UnitTests/QuestDbNativeQueryTests.cs)
check bound parameters, native row/timestamp ownership, exact decimal JSON,
UTF8-versus-character budgets and split-surrogate handling, aggregate/row/result
limits, cancellation and connection disposal.
Evidence `C:/Source/OASIS-cosmic/TestResults/QuestDbCqlCurrent/cql-controls.trx`,
SHA25615AB13B1EADA1DBC0047B1235A7C7D6B01F57097E666D455F5F79577E1FD3FA5.
Cache-only restores/bounded leased builds pass; no new dependency download or
native environment. Generic lookup/deletion/history gaps above remain separate.
Next is the retained native source and original-data restart/pair/browser proof;
inventory remains red,62/226demo coverage unchanged.

## Retained native QuestDB10.1.0 checkpoint

Official free image pinned to
`sha256:6f733a142ee7ca8623d65135b46ed6cf78aac1e9ab15d1493ad7df837721e2e4`,
image IDe29d6aca1d1e92cd43f311088a9c2e9f6fd4a40858996e3c80f0b0b4f27e18de,
312,060,883B expanded. Source `/var/tmp/oasis-questdb-cql`, container
`oasis-questdb-cql`, authenticated loopback HTTP19000/PG18812,2CPU/2GiB,
JVM128..512MiB and fixed workers. Local plaintext scope; no hosted/TLS claim.
Initial native HTTP unauthenticated SQL access rejects. Credentials/receipts
retained privately outside TestResults; source/table IDs6/7/8 survived restart.

Native first1+restart1 tests pass/0failed/0skipped. Actual PG driver validates
schema and65-control implementation:3original rows+1replacement, original JSON/
exact decimal metadata/native epoch timestamps/table IDs preserved, obsolete
field removed, all3/subset2/missing0, stable paging and explicit budget rejection.
A test-only documented WAL transaction-applied observer waits for visibility;
no writes are replayed and no production immediate-read guarantee is implied.
[Native test](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.IntegrationTests/QuestDbCqlNativeTests.cs).
Evidence under `C:/Source/OASIS-cosmic/TestResults/QuestDbCqlCurrent/`:
- native-first.trx SHA256855e79c8b1a4e44c928443c58fd2e063840bddb198080f82435106de42f4ed65
- native-restart.trx SHA256f0c60b8e3d98d0bd6796a231d900bf74cda0598b00f6d7328ec02b231bff36bf

First stop verifier expected0; native Java SIGTERM returned143 with fresh HTTP/PG
closed, engineSTOPPED and QuestDB-is-shutdown markers. Corrected helper accepts
0/143 only when those current-start completion markers exist and no deferred/
failed lifecycle shutdown is reported. Original receipt/observation retained;
no source recreation, reseed or DDL replay. Native process code143 is distinct
from successful test runner exit0.

Source14,306,019B retained; no pending intents. Native service stopped/exit143,
no OOM; ports19000/18812 closed. Native runner reuses Linux .NET10.0.12 and cached
portable testplatform18.11.0, Windows cached integration restore/bounded build.
Mongo-paired/browser acceptance remains open, now deferred behind the approved
[10-provider content-query set](CONTENT_QUERY_DEMO_ROADMAP.md). Inventory stays red,
62/226scoped demos unchanged. GitHub publication recovered through538bf51d6 before
this checkpoint; earlier server-error incident is historical.
