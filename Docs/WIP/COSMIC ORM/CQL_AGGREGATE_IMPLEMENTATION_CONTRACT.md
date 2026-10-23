# CQL projection and aggregate implementation contract

23 October 2026. Implementation design for the unchecked second phase of the completion roadmap. These are acceptance requirements, not shipped aggregate APIs. Count/LongCount/Any are the first implementation slice; Select, Min/Max, Sum/Average and GroupBy remain required in the same programme.

## Query and result ownership

An immutable scalar/projection intent must retain its source query, provider scope, trusted access, relationship predicates, root window and explicit consistency requirements. Terminal execution must not materialize every holon and evaluate LINQ in the manager. A native adapter executes the admitted shape and returns detached scalar/DTO results through OASISResult; scalar values must not accidentally become tracked holons.

Count counts eligible roots in the composed window, not expanded children. Skip/Take apply before the terminal aggregate. Without a window, a root-row publication limit must not silently truncate an aggregate's input. Native work, memory, deadline and cancellation budgets still apply. Graph expansion and consistency intent must be handled explicitly: removing output expansion must never remove a requested snapshot guarantee.

Access is applied before aggregate participation. Child/descendant scope validation remains ahead of root publication/windowing as specified by the relationship contract. An earlier positive Any or a negative business predicate must not hide a required traversal error. No partial scalar or grouped result is published after cancellation, invalid data, overflow or native failure.

## Capability and provider scope

Planning and actual-adapter readmission must validate the terminal operation, input path/type, result type and bounds before native I/O. A borrowed plan or a capability bit alone cannot grant an implementation. Preserve existing provider interface/constructor signatures; add explicit supported contracts instead of breaking existing adapters.

Named/current-default requests select their eligible native provider. All-provider execution must report its per-provider outcomes. Do not add provider counts together and call the sum a global logical count: replicas may contain the same holon. Global deduplication, ordering/windowing, cursors and partial-failure policy remain the explicit federation phase.

WEB4 result shapes require a documented version/discriminator and existing authoritative JWT/admission/accounting. Preserve v1 rows behavior. Every admitted physical provider attempt has its normal receipt; rejected shapes have zero native dispatch and no fabricated usage.

## Scalar semantics to lock with tests

- Count and LongCount have explicit checked Int32/Int64 result domains; empty input yields zero. Any returns Boolean and must retain required scope validation.
- Min/Max define missing/null/empty-set behavior for nullable and non-nullable inputs before advertising support.
- Sum/Average define input domain, empty input, exact accumulation, output precision/rounding and overflow. Never silently coerce numeric strings or round an Int64 through binary64.
- GroupBy defines key identity, missing versus null, numeric type equality, key ordering and bounded result cardinality. Apply access before grouping.
- Audit supplementary/BMP Unicode ordering boundaries and invalid strings before reusing a native string sort as a cross-provider aggregate contract. Do not infer general collation equivalence from ASCII examples.
- Select admits explicit scalar/member expressions and supported DTO construction. Validate detached data and access before executing constructors/factories; reject arbitrary executable methods and unsupported shapes.

Native controls must exercise the operation itself, including datasets larger than the root publication limit, windows, exact values beyond2^53, overflow, empty/null/mixed-type data, hidden actors/tenants, cancellation/deadlines and unsupported-adapter zero-dispatch. Cross-provider controls must distinguish physical replication from logical identity.

## Implementation sequence

1. Add immutable scalar intent and typed capability/readmission contracts plus manager execution ownership; preserve all source-query restrictions.
2. Implement native Count/LongCount/Any on MongoDB, SQLite and Neo4j, with exact outcomes and shared attempt ownership. Add native positive/negative controls before declaring capabilities.
3. Implement explicit detached Select and the remaining aggregates/grouping with the scalar semantics above.
4. Expose certified shapes through documented WEB4 transport and spaced/commented SDK/HTTP demos. Certify authorization, receipts and actual completed-query evidence.
5. Complete the separate join/federation and scalar/policy phases; do not mark the full roadmap complete after this phase alone.

[Roadmap](CQL_COMPLETION_ROADMAP.md), [architecture](CQL_ARCHITECTURE.md), [descendant contract](CQL_DESCENDANT_CONTRACT.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
