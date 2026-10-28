# Indexed SQL canonical-row execution milestone

27October2026. Execution primitive implemented/tested; not yet generic
HolonManager/LINQ/WEB4 indexed execution enrollment or full chain/demo acceptance.

## Explicit schema and storage policy

SqliteCosmicChainIndexStore now supports two declared bindings:

- schema1, default constructor: encrypted history only; query execution rejects.
- schema2, explicit canonicalRowProjection:true: canonical row projection, with
  Id/version/versionID, Name, creator/public/deleted visibility fields and receipt.

The projection fields are deliberately queryable plaintext in this protected
index. Full canonical payload/metadata and checkpoint remain encrypted. Metadata
indexing is not enrolled; existing reserved-key policy is not expanded or bypassed.
An existing history-only source cannot silently become schema2; use an explicit
new binding/migration. A committed missing query table is an error, not silently
reconstructed. No existing native SQLite query profile is redefined.

History insertion, latest canonical row projection and authenticated checkpoint/
manifest commit atomically. Projection insert failure rolls all effects back.
Encrypted checkpoint binds projection profile and history metadata digest. Query
integrity validates bounded history identity metadata and projection proofs/latest
revision, including exact SQLite storage types/Boolean domains. Corruption cannot
silently omit a row by changing a query field. Integrity validation scans at most
10,000 history metadata rows, not full payloads; this cost is explicit and is not
a managed business-predicate fallback. Only SQL-selected payloads are decrypted.

## Row query contract

Core AdmitIndexedRows requires the actual bound single source/target and keeps
native admission closed. SQL execution reuses the existing canonical SQLite AST
translator through a new scoped helper. Whitelisted canonical fields only:
Id/Name equality, Version integer comparisons, public/creator visibility,
Boolean composition, supported membership, canonical ordering and bounded paging.
Metadata, graph, tail/selected-stage, scalar and projection queries remain red.

The SQL WHERE applies tombstone exclusion and mandatory actor/catalogue policy
before ORDER/LIMIT/OFFSET. No post-download business filtering. One SQL transaction
captures index checkpoint/integrity/page, then closes before remote source proofs.
Each selected original source record is independently reread and compared; fresh
finalized head is captured afterwards. Source/checkpoint/freshness/receipt failures
return no partial rows. Empty results still require a valid checkpoint/fresh head.
Typed row construction occurs only after all source proofs pass; children are empty.
This primitive does not yet enroll general manager tracking/save-state APIs.

The result exposes checkpoint SourceProvider and ExecutionProvider separately,
plus fresh head, rows and source receipts. It never labels SQLite execution as
native Solana querying. Existing full-history native coverage remains blocked at
FirstAvailableBlock1402 for declared start0.

```csharp
// Server-resolved source/schema2, observer and mandatory policy; never client grants.
using var index = new SqliteCosmicChainIndexStore(path, source, key,
    canonicalRowProjection: true);

var query = CosmicQuery<Holon>.Create(
        CosmicQueryScope.Named(source.SourceProvider).Result)
    .WithIndexedSource(requirement).Result
    .WithReadAccess(serverReadPolicy).Result
    .Where(holon => holon.Version >= 2)
    .OrderBy(holon => holon.Name)
    .Take(5).Result;

var plan = CosmicQueryPlanner.Plan(query, source.SourceProvider, new[] {
    new CosmicQueryTarget(source.SourceProvider,
        SqliteCosmicChainIndexStore.CanonicalRowCapabilities, source)
});

// Check every OASISResult before accessing Result in application code.
var page = await CosmicIndexedQueryExecutor.ExecuteAsync(plan.Result, index, observer);
```

## Verification and next work

30 actual SQLite tests pass:13 history/publisher controls and17 indexed-row cases,
including real SQL visibility/authoritative catalogue, ordering/paging, empty and
injection-shaped literals, cancellation, stale/missing/changed source evidence,
metadata rejection, projection/history/type corruption and atomic projection
rollback.56 Core chain checks pass, including correct/wrong binding and proof that
native execution remains rejected. Two genuine before-failing tombstone/page
cases are preserved; fix adds the deletion predicate at the indexed SQL owner.

No native source writes, new runtime/environment/download or index deployment.
Owned test DBs are removed after nonpooled connections dispose.

Next: shared indexed row-result/receipt execution through HolonManager/LINQ with
selection/tracking ownership; scalar/projection execution; explicit metadata index
policy; WEB4 authority/tariff bindings; native declared-range acceptance and demos.
Full COSMIC/EF parity and original source recovery remain open.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/ProtocolCqlCurrent/indexed-rows-before-tombstone.trx | 11B7DDC91C3FB8C0C67CE104EDBA447C23FFA1BD902083D0CD2E19680172E285 |
| TestResults/ProtocolCqlCurrent/indexed-rows.trx | 1950EC2F3841352DF566B8FC7D22BEDDEB3FBBD735813F034EFBAEB4E2C1257C |
| TestResults/ProtocolCqlCurrent/indexed-core.trx | A6DA5F89888014B4B2483804E2A905A0CBC71406A934229F7E0E579FB0D8E895 |

## Shared reader contract follow-up

Core now owns ICosmicChainQueryIndexReader and CosmicChainIndexRows. SQLite
implements that explicit indexed contract; the shared result captures bounded
collections (10,000 maximum) with one non-null receipt per non-null row. It keeps
checkpoint and fresh head even for empty pages. Row objects remain provider-owned
until the future shared materialization owner detaches them. Construction alone
is not evidence admission. The actual SQLite owner still validates source proofs.

Six new Core tests cover collection capture, empty evidence and invalid/unbounded
pages. All 62 focused Core checks and 30 real SQLite checks pass; SQLite tests now
invoke the reader interface and verify actual binding/capabilities. Both Core
targets and SQLite build. No native route or inherited billing enrollment added.

The existing attempt owner deliberately rejects indexed plans. Next implement
explicit indexed physical-attempt/receipt ownership before manager/LINQ mapping;
do not route indexed plans through native execution or bypass prepared billing.

- `TestResults/ProtocolCqlCurrent/index-contract-core.trx` SHA256 `C63174EE2E9E7E10DCC84D291AB041A0DCE553B2EF970D10543062C1B88085B7`.
- `TestResults/ProtocolCqlCurrent/index-contract-sqlite.trx` SHA256 `EE40EABB849A4FF571BB01D1CC16C648C44365742FFCEDE86DE1837A663A6249`.

## Shared SDK execution checkpoint

CosmicIndexedQueryExecutor now executes through the shared reader contract. It
preflights actual indexed source/capabilities and mandatory server actor policy,
requires complete non-warning acknowledgement, enforces total/query-window bounds,
revalidates checkpoint freshness and row/receipt/source position identities, rejects
tombstones/hidden rows/duplicates, then publishes detached canonical Holons with
selection observation. Empty pages retain and validate head/checkpoint evidence.
No managed business filtering or second source transport path is introduced.

The trusted reader continues to own complete SQL selection and independent source
decoding; the shared owner does not pretend a returned receipt is a second independent
observation. This is an explicit SDK operation. Native adapters remain closed.
Inherited or expired prepared admission cannot be bypassed: active prepared contexts
return CQL_INDEXED_BILLING_UNSUPPORTED before reader I/O. Pricing/recording both SQL
execution and source-proof transport is still required before WEB4 enrollment.

14 new shared execution cases cover detached success/empty completion, receipt
mismatch, visibility/deletion, incomplete/warning pages, duplicates/result bounds,
stale head, missing actor policy/wrong source/cancellation and prepared billing
rejection with zero reader/outbox calls. Total76Core checks and30actualSQLite
checks pass, zero skips;15SQLite scenarios now use the shared execution owner.
Both Core targets/SQLite build. Test fake initially omitted mandatory visibility
capabilities; corrected its advertised profile, with planner admission asserted.
Typed manager/LINQ mapping, dual-resource billing, scalars/projections andWEB4 remain
open. No production chain/CQL acceptance claim is added.

- `TestResults/ProtocolCqlCurrent/index-executor-core.trx` SHA256 `F8F29D8890A88191278FD84BEDA39BBAB5B472941579DBDA4217AD5961BB5DBE`.
- `TestResults/ProtocolCqlCurrent/index-executor-sqlite.trx` SHA256 `0F78B95DE8F2779D9D8A2AFFACEAA567552C0B0DAFFF49599389AA3610D5EEB4`.

## Typed HolonManager checkpoint

ExecuteIndexedQueryAsync<T> now provides explicit HyperDrive V2 typed indexed
execution. CosmicIndexedQueryExecution<T> keeps typed Rows, Checkpoint, Head and
per-row Receipts; SourceProvider and ExecutionProvider remain under the checkpoint.
It reuses MapCqlQueryExecution and the existing metadata decryption/hydration/clean
tracking owner, without a second mapping or tracking implementation. Hydrated
Id/version/versionID/deletion must still match the receipt. Typed setter failure
or identity mutation returns an error with no partial typed result.

```csharp
// Check plan/result envelopes before using Result. Keep the evidence-bearing page.
var result = await manager.ExecuteIndexedQueryAsync(plan.Result, index, observer);
if (result.IsError)
    throw new InvalidOperationException(result.ErrorCode + ": " + result.Message);

var page = result.Result;
foreach (var row in page.Rows)
{
    // CustomOASISProperty fields are hydrated; initial change tracking is clean.
    Console.WriteLine(row.Name);
}

Console.WriteLine(page.Checkpoint.Source.SourceProvider);    // Logical source.
Console.WriteLine(page.Checkpoint.Source.ExecutionProvider); // Physical index.
```

Five new manager cases prove typed custom metadata, clean-to-dirty tracking, empty
evidence retention, hydration failure, receipt-bound identity mutation rejection
and V1 rejection before reader I/O. Combined chain/typed regression cohort130pass;
SQLite30pass with15real index scenarios now executing through HolonManager, with
clean baseline assertions on successful rows. Both Core targets/SQLite build.
Prepared contexts remain rejected before I/O by the shared executor. LINQ indexed
result adaptation, explicit dual-resource accounting, scalars/projections, WEB4
and native full-range source acceptance remain open. Indexed read success does
not claim Solana conditional-save support or repair original missing history.

- `TestResults/ProtocolCqlCurrent/index-typed-core.trx` SHA256 `900BEBB74EB0F17FB64EB34AEC477E7D36ECAD7E0403CE5DA97CD90B9A8A1F8F`.
- `TestResults/ProtocolCqlCurrent/index-typed-sqlite.trx` SHA256 `763CFD628106EC3E2A54053074853688F16B6EA9DD219D6937729C77929AC50A`.

## Indexed LINQ SDK rows

Use an explicitly authorized schema2 source, registered index reader and trusted
source observer. The same CQL expression binder handles standard Where, ordering,
Skip and Take; execution stays native SQL with independent selected source proofs.

```csharp
// source includes a named logical provider, indexed read requirement and server actor policy.
var created = CosmicLinq.CreateIndexed(manager, index, observer, source);
if (created.IsError)
    throw new InvalidOperationException(created.Message);

var name = "A";
var query = created.Result
    .Where(row => row.Name == name)
    .OrderBy(row => row.Name)
    .Take(5);

// Nothing has executed yet. Closure values bind separately for each read.
var result = await query.ToCosmicIndexedPageAsync();
if (result.IsError)
    throw new InvalidOperationException(result.ErrorCode + ": " + result.Message);

var page = result.Result;
Console.WriteLine(page.Checkpoint.Source.SourceProvider);
Console.WriteLine(page.Checkpoint.Source.ExecutionProvider);
foreach (var row in page.Rows)
{
    // Typed metadata is hydrated and change tracking starts clean.
    Console.WriteLine(row.Name);
}

// page.Receipts aligns with page.Rows; page.Head records the fresh source head.
```

ToCosmicListAsync and sequence enumeration are also available when only rows are
needed; retain the page result when provenance/checkpoint/receipts are required.
Each terminal is a separate execution. Unsupported Select/scalar/element/reverse/
post-window filter shapes reject before reader I/O; there is no client reduction.
Prepared billing contexts remain rejected before I/O until SQL and source-proof
transport receive explicit dual-resource attempt/receipt accounting. This SDK
profile does not enroll WEB4 or certify original native chain coverage.

Seven new Core cases prove deferred typed/empty pages, row-only reads, explicit
projection/stage/scalar rejection, pre-cancel and foreign local-query rejection.
One actual SQLite case proves reusing a LINQ chain with a changed captured Name
executes fresh SQL selection/proofs, keeps source receipts and clean-to-dirty
tracking. Combined chain/native-LINQ/typed regressions179pass, SQLite31pass,
zero skips. Both Core targets and SQLite build. Native LINQ remains on its
existing owner; indexed intent never becomes an ersatz native adapter.

- `TestResults/ProtocolCqlCurrent/index-linq-core.trx` SHA256 `651F0228D8555207BECEFAEE4625AFB81C072CA6188E8F0F7D432798FCAF974B`.
- `TestResults/ProtocolCqlCurrent/index-linq-sqlite.trx` SHA256 `2798D672E523CC8AFF1B2D09454729D946BF1AB5833F2D346132A4671C4C6AC9`.

## Physical read attempt boundary checkpoint

CosmicIndexedReadAttempts now carries one exact source/index binding and one
server-created attempt sink in an async lifetime scope. Durable sinks must begin
before invoking I/O. The boundary checks lifetime/provider/index identity and
cancellation both before dispatch and inside the callback after asynchronous
admission. Nested/replaced/expired scopes cannot execute. Transport exceptions
propagate; the boundary does not invent a completion or retry uncertain reads.

The actual SQLite snapshot/decryption owner is wrapped once, before any source
proof transport. Each Solana genesis/slot/block/retention/history RPC in the chain
observer has its own boundary. The repository's sole transaction-reader delegate
is instrumented below the canonical root/chunk decoder, so those reads are not
collapsed into one aggregate observer attempt. No scope means unchanged SDK
execution; this telemetry mechanism is not authorization or prepared enrollment.
Existing non-indexed provider behavior is preserved; no legacy defect is fixed.

Six new Core cases verify separate SQL/root/chunk-shaped attempts, wrong provider/
index rejection, cancellation during asynchronous begin, expired/nested scope and
unknown transport failure without fabricated completion. Core chain cohort94pass.
14Solana range controls now compare exact RPC fixture invocation count against
attempt count before separate proof rereads; full782Solana unit cases pass. These
are controlled transports, not a native ledger acceptance claim. SQLite31pass;
real repeated LINQ reads verify exactly one SQL attempt each. Both Core targets,
SQLite and Solana provider builds pass.

Explicit friend access is limited to the two provider test assemblies so fixture
tests can install the otherwise internal attempt sink. Production sink creation
remains Core-owned. Next: durable begin/receipt pricing and inherited actor/lifetime
admission in PreparedStorageExecution, with bounded actual physical-read tariffs.
Prepared indexed execution still rejects before I/O; no WEB4 billing claim yet.
Uncancellable already-dispatched SDK reads may finish after observation cancellation;
retain unknown outcomes rather than asserting no execution or resubmitting.

- `TestResults/ProtocolCqlCurrent/index-attempt-core.trx` SHA256 `9541398A6A261D8E9D4DC59CADF92876EBCDC25A027E02996373AB82D386CE4A`.
- `TestResults/ProtocolCqlCurrent/index-attempt-sqlite.trx` SHA256 `6A79B254F8237F56B9A198DE210364C3DF5A30C7F58A09163371D11ACC1FBCDB`.
- `TestResults/ProtocolCqlCurrent/index-attempt-solana.trx` SHA256 `301D18B6E31FA15937F3891BDD8D7D53DA3F4C866470AB443F0961698930EFBC`.

## Prepared accounting now implemented

[Indexed physical-read accounting](CQL_INDEXED_BILLING.md) supersedes prior
checkpoint statements that all prepared contexts reject. Exact server policies
now enable durable SQL/RPC service-tariff capture with inherited actor/lifetime
checks. Missing policies still reject before I/O. WEB4/reservation/native
acceptance and other indexed shapes remain open; no demo/parity promotion.


## Indexed native selected-cell SDK milestone (27 October)

ICosmicChainQueryIndexProjectionReader explicitly enrolls native selected columns
separately from row/filter capabilities. SQLite schema2 selects requested canonical
cells alongside encrypted payload/source receipt in the same transaction. It does
not load a row query and run a compiled client selector afterward. Visibility,
deleted-row exclusion, ordering and source windows remain in the SQL statement.
Canonical fields: Id, Name, Version, VersionId, CreatedByAvatarId, IsPublic,
IsDeleted, with exact Guid/string/int/Boolean CLR domains. Metadata is not enrolled.
The optional plaintext canonical projection policy and encrypted payload/metadata
remain unchanged; schema1 stores do not gain projection support.

CosmicIndexedProjectionExecutor uses the existing CosmicIndexedQueryExecutor as
its single source-evidence owner. One selected dispatch supplies native cells and
canonical witnesses; existing prepared actor/tariff/lifetime rules, complete
checkpoint/older-anchor validation and source receipt/visibility checks run before
publishing results. Every selected cell must equal its validated canonical source
field. Only then does CosmicProjectionResults validate payload/types and invoke
any application DTO constructor/setter. No partial DTO list survives failure.

HolonManager.ExecuteIndexedProjectionQueryAsync is the shared V2 SDK entry point;
CosmicProjectionPlanner already binds explicit indexed targets. Detached DTOs,
anonymous shapes and scalar column selections retain checkpoint/head/receipts.
A scalar column selection is NOT a Count/Sum aggregate. Indexed IQueryable.Select,
HTTP selected endpoint, selected-result composition, metadata and aggregate shapes
remain open. No parity row or complete provider-demo claim is promoted here.

Tests exercise actual SQLite source visibility/windows, empty rows, every selected
CLR domain, scalar columns, tampering, missing/changed source, stale evidence,
cancellation and payload limits. Core tests additionally reject forged cells,
warning/incomplete evidence and missing prepared tariff policy before dispatch.
Actual source transports are controlled in this projection milestone; existing
live-chain row acceptance does not certify live-chain projection execution.
Evidence: TestResults/ProtocolCqlCurrent/index-projection-*-full.trx.


## Standard indexed LINQ Select (27 October)

The existing shared CosmicLinqProjectionProvider now has an explicitly bound
indexed execution owner. Source Where/order/Skip/Take and closed values use the
same execution-time binders. Standard Select supplies native selected columns;
flat repeated Select reuses column bindings without constructing intermediate
DTOs. Lists and synchronous enumeration execute that same provider pipeline.
ToCosmicIndexedProjectionPageAsync retains checkpoint/head/source receipts.

The source reader must explicitly implement ICosmicChainQueryIndexProjectionReader.
A row-only index rejects Select with CQL_CAPABILITY_UNSUPPORTED before I/O.
Selected-result Where/order/windows and aggregates/elements remain unsupported
for indexed execution, with rejection before SQL/source dispatch. Apply supported
source filters/windows before Select. No native scalar aggregate is inferred from
selected column support. Positional constructor selection without expression member
bindings remains unsupported (M17); anonymous shapes and public parameterless DTOs
with explicit member assignments are the supported materialization forms.

Seven new actual SQLite checks prove deferred filters, evidence, empty rows,
changed source failure, cancellation/stage rejection, flat reselection with zero
intermediate constructors, lists/enumeration and explicit terminal rejection.
Full Core4154/SQLite49/Solana799 gates pass. The updated shared C# walkthrough
runs verbatim in the retained live Solana/SQLite native gate (one pass, no new
seed/source write), including standard Select and exact original source commit.
HTTP projection enrollment and production admission remain open.
Evidence: TestResults/ProtocolCqlCurrent/index-select-{core-full,sqlite,solana}.trx
and TestResults/SolanaCurrent/solana-indexed-native.trx/json.


## Indexed native cardinality SDK (27 October)

ICosmicChainQueryIndexScalarReader separately enrolls Count/LongCount/Any.
SQLite schema2 validates its authenticated history/current-row projection manifest
and runs COUNT(*) or EXISTS over the admitted SQL source window in the SAME read
transaction. Actor visibility/deleted exclusion and predicates precede ordering,
Skip and Take. No application rows or business payloads are downloaded/decrypted
for counting. Take(0), empty input and skipped-out input yield exact zero/false.
The source observer then supplies a fresh finalized head and required older anchors.

The shared CosmicScalarPlanner and HolonManager.ExecuteIndexedScalarQueryAsync
use CosmicIndexedScalarExecutor. It re-admits actual capabilities/actor/source,
complete operation-bound acknowledgement, checkpoint freshness and source window,
then decodes exact count domains through CosmicScalarResults. Count checks Int32
overflow; LongCount retains Int64. The generic evidence domain does not inherit
SQLite's current10,000-history-entry integrity-scan limit; that remains a provider
profile/scaling constraint, not a promise of arbitrary index size support.

Assurance is authenticated-index-snapshot: the scalar is exact for the authenticated
index projection at its checkpoint, with live head/anchor freshness. It does NOT
claim every counted source transaction was independently re-read or every cached
business payload is currently readable. A damaged business payload alone does
not invalidate a still-authentic index count; row materialization has a different
proof/readability obligation. Global manifest count/hash stay internal and are not
included in the actor-filtered SDK result. Checkpoint/head remain public evidence.

PreparedStorageExecution's existing read owner is generalized, not duplicated:
exact source tariffs, actor/lifetime, aggregate reservation and durable begin/receipt
rules apply. Cardinality requires one SQL snapshot and acknowledged source
head/genesis/block reads; it never fabricates per-record transaction receipts.
Unknown begin/acknowledgement fails and retains the original outcome for reconciliation.

13newCore/18actualSQLite cases prove operation/ack/window binding, missing policy,
incomplete capture, unknown begin/receipt, fresh evidence, wide integer domains,
visibility/windows/empty results, tampering, head failures and cancellation. Full
Core4180/SQLite67/Solana799pass. Existing row/projection prepared paths regress green.
LINQ Count/LongCount/Any, HTTP enrollment, post-window predicates, other aggregates,
metadata and live-source cardinality acceptance remain open. No broad parity cell
is promoted. Evidence: TestResults/ProtocolCqlCurrent/index-cardinality-*-full.trx.


## Standard indexed cardinality terminals and native demo (27 October)

Standard IQueryable Count/LongCount/Any now bind through the existing scalar
planner and indexed scalar reader, never through entity/page enumeration. Deferred
source closure values are rebound for each execution. Unwindowed terminal predicates
are admitted as source Where; predicates AFTER an existing source window remain
explicitly unsupported and reject before SQL/head reads. All, numeric/element
terminals and selected-result cardinality remain outside this indexed enrollment.
A row-only reader returns CQL_CAPABILITY_UNSUPPORTED before dispatch.

Nine new actual SQLite cases verify terminals, source windows/Take0, deferred
values, predicate overloads and unsupported post-window/All rejection. FullCore4180,
SQLite76 andSolana799pass. The live Solana/SQLite SDK gate executes the shared
ChainIndexedWalkthrough.RunCardinalityAsync verbatim and returns1/1/true for the
retained native record; no new seed/submission. Each terminal is a separate
snapshot/read, not a cross-query atomic snapshot. Cardinality retains the
previous authenticated-index-snapshot assurance, not per-record source rereads.
The async evidence-aware SDK remains available for cancellation/checkpoint access;
these standard LINQ terminals are synchronous primitive-returning operations.
HTTP cardinality enrollment remains next. Larger-index scaling/metadata and full
parity remain open. Evidence: TestResults/ProtocolCqlCurrent/index-count-linq-*.trx
and TestResults/SolanaCurrent/solana-indexed-native.trx/json.

## Metadata policy foundation

The shared explicit queryable-root policy and source admission are implemented. Explicit schema3 now authenticates selected queryable metadata roots and executes native rows/cardinality;22newcases/98indexed-store/205fullprovider tests pass. Existing schema2 remains canonical-only and is not migrated. SDK/WEB4/live-dataset acceptance and selected metadata columns remain open. See [schema3 contract and verified scope](CQL_INDEXED_METADATA.md).
