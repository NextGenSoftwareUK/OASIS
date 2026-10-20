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

## Typed manager integration gate

Audit found the existing metadata mapper can overwrite canonical Id/Name/audit/visibility/runtime fields and silently swallow custom conversion errors. V2 canonical ownership is now protected at that existing hydration boundary before typed CQL reuses it. The fix protects all public Holon properties case-insensitively while retaining metadata as data; legacy behavior is unchanged and documented only. The conversion-error boundary is implemented in the latest checkpoint below; complete typed CQL materialization, schema/security admission and callback/resource coverage remain open, so no public typed manager query API is claimed.

Canonical ownership verification: eight new controlled cases pass in fullCore1,328/1,32815s0skips; bothTFMs build zero errors. The real SQLite typed-load case passes in full834/834 regression (115s), zero skips; it preserves canonical identity/audit/visibility with clean tracking despite conflicting metadata. Reports: metadata-ownership-core-full.trx and sqlite-metadata-ownership-full.trx. At this historical ownership checkpoint conversion failures were still swallowed; the latest V2 conversion checkpoint below supersedes that gap.

## V2 conversion failure boundary — verified

V2 hydration now uses the existing canonical stored-record serializer for declared property types instead of the legacy per-type conversion chain and silent catches. StoreAsJsonString is honored explicitly. Matching names must be unambiguous and have public non-indexed setters; conversions finish before application setters run. Unknown metadata stays as data and canonical properties remain protected. The direct MapMetaData helper throws on invalid values; public typed V2 point loads by ID/key (sync/async) and collection/search wrappers return COSMIC_METADATA_HYDRATION_FAILED, IsLoaded=false, the cause and no result.

Core1,334/1,334 passes14s; bothTFMs build0errors. Focused SQLite8/8 passes3s across ID/key/all/search ×sync/async. Initial searchcases failed earlier because SearchManager did not resolve the instance provider binding for Default; both V2 request builders now use the existing ResolveProviderType boundary. Original failure/diagnostic reports are retained, and tests continue to use Default. Full842/842 SQLite regression passes112s, zero skips (sqlite-metadata-conversion-full-verified.trx), with the retained fixture absent after normal teardown. Legacy mapper behavior is unchanged and documented only.
