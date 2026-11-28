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
