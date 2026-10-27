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
var page = await index.QueryRowsAsync(plan.Result, observer);
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
