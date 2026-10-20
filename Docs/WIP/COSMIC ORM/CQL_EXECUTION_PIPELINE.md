# CQL native execution pipeline

The shared adapter contract and executor are implemented. Core verification passes1,320/1,320; SQLite native full833/833 regression also passes, zero skips. This is a provider orchestration seam, not an authenticated WEB4 endpoint or complete federated query engine.

## Contract and ownership

`ICosmicQueryStorageProvider` exposes a concrete `QueryProviderType`, immutable `QueryCapabilities` and cancellable native `ExecuteCosmicQueryAsync`. SQLite implements this interface using its existing parameterized native path. Other providers do not acquire query support from their ordinary Load/Search methods; they require a native adapter implementation and equivalent-result evidence.

`CosmicQueryExecutor.ExecuteAsync` takes a plan already admitted against an authorized eligible snapshot and a bounded adapter registry. It verifies concrete unique registry identities, resolves only the plan targets, and checks every selected adapter's actual capability profile before any native read. A caller-created or older overbroad plan cannot bypass actual native capability admission. The registry is never used to expand scope or fail over to unselected providers.

The executor rebinds one selected target per native call. Cancellation is checked before and after calls. Native error codes and exceptions are preserved with provider context; thrown failures return CQL_QUERY_FAILED. Any failure returns no result object, including failures after earlier successful reads. Read side effects already incurred are not described as rolled back.

## Results and budgets

The total row budget is1–10,000, default100, across all selected providers. Adapters receive the remaining budget (at leastone to detect overflow after an exact fill). Adapter overdelivery, null envelopes/collections, null or empty row identities, and duplicate identities within one provider reject explicitly. Combined overflow returns CQL_QUERY_LIMIT with no partial result.

Successful rows are projected into detached canonical holon values and grouped with their provider identities in `CosmicQueryExecution`. Duplicate/conflicting logical identities across different providers remain separate, visible records. The result is not globally deduplicated, ordered, paged or snapshot-consistent. Derived runtime entity hydration, relationship selection and clean tracking still require manager integration.

## Local native example

Use only inside a trusted local application or behind a separately enforced authorization boundary:

```csharp
ICosmicQueryStorageProvider adapter = sqliteProvider;
var query = CosmicQuery<Holon>.Create()
    .Where(h => h.Name == "coffee" && CosmicQueryMetadata.Exists(h, "receipt"));
if (query.IsError) throw new InvalidOperationException(query.Message, query.Exception);
var plan = CosmicQueryPlanner.Plan(query.Result, adapter.QueryProviderType,
    new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
if (plan.IsError) throw new InvalidOperationException(plan.Message, plan.Exception);
var result = await CosmicQueryExecutor.ExecuteAsync(plan.Result, new[] { adapter },
    maximumResults:100, cancellationToken:cancellationToken);
if (result.IsError) throw new InvalidOperationException(result.Message, result.Exception);
// Successful records are grouped under result.Result.Providers.
```

An actual WEB4 boundary must obtain authenticated actor/tenant/visibility/residency/encryption/cost permission and derive eligible targets without trusting caller-supplied capabilities or plans. Required follow-on work includes enforced policy predicates, budgets/deadlines, typed manager hydration, additional adapters, metadata scalar semantics, version authority/freshness, conflict handling, global paging and explicit partial-result policies.

## Verification

Seven new controlled cases cover preflight-before-I/O, missing/duplicate bindings, provenance and detachment, total budgets/overdelivery, error/exception preservation, cancellation before/during reads, and malformed provider output. Full Core1,320/1,320 passes13s with zero skips; both target frameworks build. A native SQLite interface/executor case passes in its full833/833 gate (2minutes), zero skips. Reports: cql-executor-core-full.trx and sqlite-cql-executor-full.trx. Exact assembly epochs and reports are recorded in the integration boundary.
