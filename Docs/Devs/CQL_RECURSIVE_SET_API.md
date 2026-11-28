# Recursive set requests and backend SQL

SQL and Ask Your Data engines are owned by ONODE WebAPI Services/Cql/Text. Browsers call POST /api/data/query-cql/sql and /ask; do not update browser parser copies.

The existing POST /api/data/query-cql/set body gains an optional Set on either operand. An operand is either the existing leaf source, or {"set":{"kind":"Concat","left":{},"right":{},"take":1}}. A nested operand cannot carry leaf filters, columns, windows or graph settings alongside Set. Child nodes carry kind, left, right, ordering, reverse, skip, take and matching entityStages or selectedStages. Provider, caller identity, read permissions and budgets belong to the root and cannot be overridden by children. Maximum depth8 and32 leaves; native admission checks every leaf and intermediate bound before publication, including Take0.

Entity sets compare Holon IDs; explicit selected sets compare the visible value tuple. Both sides must have compatible selected types/width. Each node preserves its own ordering and windows. Selected output stages may filter, order, page, distinct and rename/subset visible aliases. Root output columns reflect the final visible aliases. Mixed entity/selected inputs fail.

Selected set reductions support Count, LongCount, Any and All through the native count contract. Counts include occurrences after semantic windows and predicates; MaximumResults limits publication, not scalar cardinality. All counts counterexamples and requires a predicate. Int64 LongCount is returned as an invariant string under cql-scalar-v1. No DTO constructor or managed set-membership substitute is used for counts. SQL currently exposes COUNT(*), COUNT(1) and ANY(*) over bounded selected WITH/derived set sources; SUM(alias) and AVG(alias) are implemented for known numeric visible aliases. JSON requests use Reduction=Sum/Average and ReductionColumn naming that alias. The canonical result-domain mapping is shared with existing scalar queries: integral Average returns Decimal, encoded as an invariant string. These additions await exact native API/WEB4 qualification.

Example server SQL:

```sql
WITH combined AS (
 (SELECT Name AS Label FROM MongoDBOASIS LIMIT 2)
 UNION ALL
 (SELECT Name AS Label FROM MongoDBOASIS LIMIT 2)
) SELECT COUNT(*) AS Total FROM combined
```

INTERSECT binds more tightly than UNION/EXCEPT; equal-precedence operators associate left. Parenthesize inputs needing independent ordering or paging. Keep each selected leaf explicitly bounded when used by WITH/derived sources. Source authority and caller permissions remain the shipping provider responsibility through the same native manager execution owner.

## Qualification and surface scope

Source implementation is pending isolated native HTTP and actual WEB4 CI qualification; no live acceptance is implied. Existing REST route and JSON clients retain flat-body compatibility; null nested operands are omitted from captured flat request serialization. Postman includes a nested example. Native CI checks owner, foreign and anonymous callers and exactly one settled provider attempt. Dedicated typed gRPC/GraphQL CQL set operations are not present in the inspected server surfaces; this existing transport gap must not be reported as parity or silently bypass authorization. SDK/MCP route coverage is tracked by the repository surface checker separately from native feature acceptance.


Typed selected-set views retain native tuple membership while changing the final DTO/anonymous/scalar shape. Filtering, ordering, windows, value Distinct, further selected set operations, cardinality, numeric/extremum and element terminals use the same native snapshot/attempt/proof owner. Intermediate DTOs are not constructed. CLR constructors do not determine native tuple equality; explicit custom comparers remain unsupported. Mixed identity-set late-projection operands remain a separate open scope. Qualification status is maintained in GPT_COSMIC_CURRENT_STATUS.md and the set-family ledger; source presence does not imply full parity or live acceptance.


### Selected-set extrema

The server SQL compiler accepts `WITH q AS ((SELECT Name AS Label FROM MongoDBOASIS LIMIT 2) UNION ALL (SELECT Name AS Label FROM MongoDBOASIS LIMIT 2)) SELECT MIN(Label) AS First FROM q` and `MAX(Label)`. The existing set JSON request uses `reduction: "Min"` or `"Max"` with `reductionColumn` naming a currently visible output alias. Each provider must enroll that exact extrema domain. Original set membership and every earlier selected window/filter remain before null exclusion, native winner ordering and Take(1). Complete proof and actor/provider admission run through the same one-attempt owner; only the validated winning cell is returned in `cql-scalar-v1`. No enclosing DTO constructor runs. Empty reference/nullable extrema return null; empty non-nullable value extrema fail with `CQL_EMPTY_SEQUENCE`. This does not add entity-set numeric reduction or mixed identity/selected operands. Qualification pending; no full parent-row credit. Browser engines are not changed. Existing generic SDK/MCP JSON forwarding uses the same route; dedicated typed gRPC/GraphQL set operations remain a documented gap.


### Selected-value counts

`WITH q AS ((SELECT Name AS Label FROM MongoDBOASIS LIMIT 2) UNION ALL (SELECT Name AS Label FROM MongoDBOASIS LIMIT 2)) SELECT COUNT(DISTINCT Label) AS Total FROM q` excludes nulls and counts unique visible Label values. `COUNT(Label)` preserves non-null occurrences. Both lower on the server to the existing native selected stages and `LongCount` contract, after every earlier membership/window/filter. In a multi-column tuple set, distinct counting selects only the requested visible alias before its final Distinct; it does not change the earlier set equality. Original budgets and caller/provider proof still apply. Qualification pending.


2026-11-28 qualification update: selected-set extrema and value/distinct counts passed46137078181 on d0d03d2f/Core1b061dca (compiler17/native15/actual WEB4/exact cleanup). This supersedes pending notes above. Full set parent scope and dedicated typed gRPC/GraphQL operations remain open; no new hosted acceptance claimed.


### Entity-stage selected operands (native SDK qualification pending)

CosmicProjectedSetInput.FromEntityProjection captures a native entity-stage query followed by selection, preserving the exact source/actor/model and all entity windows before selected equality. Registered LINQ entity-mode selections can feed further selected-value sets with the same native manager/adapter and detached result shape. Each provider must independently advertise entity-input capabilities and implement the combined entity/projection contract. Shared root guard-payload allowances are divided across leaves; declared source-row maxima reserve worst-case entity-stage work without changing reported selected-work counters. Explicitly lower source limits or increase admitted budgets when this conservative reservation is exhausted. Existing ordinary selected bounds and complete proof remain enforced. This does not add identity-set late-projection operands or their SQL/JSON transport; those are separate open families. No full Q50/Q51/Q53/Q54 credit.


### Entity-stage input JSON (qualification pending)

A selected set leaf may specify EntityStages (Where/OrderBy/Skip/Take/Reverse) after its ordinary source Where/order/page and before explicit Columns; selected Stages then follow selection. This field uses the same bounded entity-stage schema as set output, but at its own leaf position. Leaf EntityStages require Columns and cannot accompany a nested Set envelope. Actor/provider/model and all source bounds remain fixed by the enclosing request. Null EntityStages is omitted from request fingerprints. Runtime columns and the exact selected pipeline lower through the native entity-source set contract; no independent managed evaluator. Owner/foreign/anonymous native HTTP qualification pending. SQL capture for this family remains separate, and dedicated typed gRPC/GraphQL set contracts remain open.
