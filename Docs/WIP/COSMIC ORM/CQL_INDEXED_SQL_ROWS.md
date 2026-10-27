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
