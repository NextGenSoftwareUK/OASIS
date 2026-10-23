# Bounded native descendant existence

23 October 2026. Native descendant checkpoint; the full roadmap is not complete.

## SDK and admission

```csharp
var limits = CosmicQueryTraversalOptions.Create(
    maximumDepth: 4,
    maximumNodes: 250);

// Check limits.IsError before using Result.
var query = CosmicQuery<Holon>.Create()
    .WhereAnyDescendant(child => child.Name == "target", limits.Result)
    .OrderBy(parent => parent.Name)
    .Take(10);
```

The immutable node freezes the child expression with the existing scalar compiler. Traversal does not request graph output. WithChildren remains independent. Bind trusted read access before the root window; it applies to every hop. WhereAnyDescendant after Skip/Take rejects rather than changing query order. Fluent OASISResult composition preserves the first error.

Options require depth1-32 and nodes1-10000; defaults are depth8/nodes1000. Adapters declare DescendantExists1048576 together with ChildExists and an immutable CosmicQueryTraversalCapability. Its explicit certified bounds can be smaller than the generic absolute maxima. Missing capability or excessive requested bounds rejects at planning and actual-adapter readmission, before dispatch. A supplied plan cannot lend its traversal budget to the executing adapter. Existing public capability constructor signatures are preserved; new overloads carry traversal limits.

MongoDB, SQLite and Neo4j declare DescendantExists with depth32/nodes10000. Direct-child SDK/HTTP queries remain supported on all three stores; descendant HTTP transport is pending.

## Required native semantics

- Roots match if at least one qualifying canonical descendant is reached within MaximumDepth. Direct children have depth1; the root never matches itself.
- Each traversed edge requires current ParentHolonId and declared canonical role membership, or the explicit absent-manifest parent-link representation. Reparenting must invalidate an old path. Dangling declarations do not manufacture nodes.
- Mandatory trusted access applies before visiting/inclusion and before traversal through an intermediate node. An inaccessible branch cannot make a match or increase the logical visited-node count.
- MaximumNodes limits distinct visited visible canonical descendants for each root and relationship condition, independently of native whole-command work/deadline/byte budgets. The bounded reachable scope must satisfy the node budget before a result is published; overflow returns CQL_QUERY_LIMIT even when a matching node was encountered earlier. Never publish a positive or negative match from a truncated scope.
- A depth boundary deliberately limits the question; unvisited descendants beyond it are not an error. Duplicate equivalent physical edges are deduplicated. Conflicting canonical identities or an encountered identity repeated on the same path produce CQL_DATA_INVALID, rather than infinite traversal.
- Native traversal/filtering precedes root sorting/Skip/Take, under the provider's declared consistency owner. GraphSnapshot must not be inferred for Neo4j. No cross-provider common snapshot is implied.
- Cancellation/deadlines and invalid data publish no partial rows. All root/relationship work belongs to the admitted physical attempt and accounting receipt.

## Verification boundary

Core tests cover bounds, immutable expression/actor binding, independent output shape, post-window rejection, first-error preservation, explicit capability consistency, certified depth/node admission and actual-adapter zero-dispatch rejection. Native membership, policy pruning, cycles/duplicates, budgets, concurrency and transport evidence remain necessary before a provider may advertise this capability.

See [completion roadmap](CQL_COMPLETION_ROADMAP.md) and [direct-child semantics](CQL_RELATIONSHIPS.md).

Core23da2dcb5fc44d2bcbc447aaca5d7cec4a963840 verification39427 terminal0: fullCore1815/Shared968/joint98 pass unskipped. net10.0 and netstandard2.1 build cleanly. Final consumer source/PDB712Core/54Shared match CoreE6E7EE404AA0B2282036291A868E5C7F3CA108C3CE26148AD0F39EF866D44733 / Shared64C24839C7AD46FD710D5DB6DB32610374DC55B1A57B984E8C81A44469E19980. At that earlier contract-only checkpoint all adapters rejected native descendants; SQLite is implemented in the newer section below.

## SQLite native execution

One correlated recursive CTE traverses canonical membership/current-parent edges and trusted visibility at every hop, tracks depth/path/cycles and caps materialization at nodes+1. It validates the bounded scope before matching the scalar predicate, then roots are sorted/paged. Independent clauses share the existing900-binding/native VM/deadline owner. WithChildren remains independent; descendant queries alone return leaf rows. A deferred Serializable read owner supports read-only connections and reuses caller Serializable transactions without committing them.

Connection-local provider functions record explicit cycle/node/manifest outcomes; the owning reader propagates typed errors before any result publication and unregisters both functions before releasing the connection. Business predicates/traversal stay in SQL. Canonical manifest validation uses the single strict Core parser under the native read budget, with only one cached parent observation. Both direct-child and descendant queries validate a parent's observation before testing child existence, including empty parents. Malformed source cannot silently become a negative match.

FullSQLite1248 and focused45 native relationship/graph controls pass, zero skips. A genuine empty-parent malformed-manifest red2 proved the omission and green controls prove the common-owner fix. Tests cover depth1/2, partial graph-independent output, exact Int64, independent clauses, root paging, hidden intermediate pruning, unlisted branches, node overflow/reuse, cycles/function teardown, read-only storage and caller transaction rollback. Broader concurrency, cancellation and all-provider descendant certification remain roadmap work.

Final SQLite58545/joint96796 terminal0: full1248/focused45/joint98 pass unskipped. Consumers match712Core/54Shared source/PDB: CoreE6E7EE404AA0B2282036291A868E5C7F3CA108C3CE26148AD0F39EF866D44733 / Shared45E098A99A5308A7DC78B9ECD8CCD3EC969A45DCCC97F09CC9F3846DE7A93617. Fixed SQLite fixture and owned HTTP/native resources are absent; reusable services stopped.

## Neo4j native execution

Neo4j uses quantified path patterns with inline EXISTS membership/current-parent and trusted child access predicates, pruning excluded branches during traversal. This feature requires Neo4j5.9+; the retained native certification runtime is5.26.12. The official [Neo4j5 path-pattern guide](https://neo4j.com/docs/cypher-manual/5/patterns/variable-length-paths/) documents inline pruning. No APOC, new procedure or alternate evaluator is installed.

Each scope retains distinct endpoint/cycle/match states capped at nodes+1; duplicate equivalent physical edges cannot inflate the node budget. Simple prefixes retain their first repeated endpoint as a typed cycle outcome. Parent and child projection lookups use separate MATCH clauses so a self-loop can reuse its one canonical projection relationship and reach cycle validation. Independent conditions retain separate scope aliases and exact scalar parameters.

Native error scopes are outside root sorting/Skip/Take, under the same read-committed transaction/deadline owner as result scopes. Cycle or node-budget failures publish no rows; the reader consumes typed native flags. Root business/access predicates restrict the scope before traversal. Read committed remains explicit; no snapshot guarantee is inferred. WithChildren remains independent of filtering.

Final50927 terminal0: Neo316unit/45native/joint109 pass unskipped. Positive/negative controls cover depth, private intermediate pruning, exact Int64 and independent clauses, unlisted branches, duplicate edges, budget/reuse, root paging and cycles. Root cause red1 after introducing inline pruning identified MATCH relationship uniqueness suppressing a self-loop; separate canonical projection lookups fixed it. Earlier4failed assertions were an incorrect fixture-name literal and were corrected separately. Two completed-stage fixture reconciliations used exact hashes and validated native absence/all9SQLite rows before removal; no operation replay/reset.

Consumers match712Core/54Shared source/PDB: CoreE6E7EE404AA0B2282036291A868E5C7F3CA108C3CE26148AD0F39EF866D44733 / SharedF51B52FE83B3F22422BEED382F2EA2B53AB7D0323176FC73BAC432032AD8692B. The newer Mongo owner is described below; descendant transport and broader acceptance gates remain open.

## MongoDB native execution

Mongo uses a flat breadth-first aggregation, with one correlated lookup per depth level. It supports the generic depth32/node10000 bounds without recursively nesting lookup pipelines. Every hop derives current parent identity, canonical declared membership and trusted child visibility before inclusion. Child predicates are compiled once through the existing exact scalar translator; roots are excluded from their own business matches. No client collection scan, temporary collection, JavaScript function or native extension is used.

Each bounded lookup fetches at most MaximumNodes+1 visible canonical candidates. An independent logical node counter and visited-ID set span all levels of one root/condition. First repeated/conflicting identities produce a data error; overflow remains an error even when a match was found earlier. Further lookups get an empty frontier after a failure. Every condition completes before the final business conjunction, so an earlier negative match cannot hide a later scope overflow.

A protected native envelope keeps persisted fields such as row/error/frontier/outcomes from shadowing internal state. A separate facet branch preserves the first error outside root sorting/Skip/Take; the reader checks the provider-owned error field before publishing any detached row. Positive and error outcomes share the same snapshot as canonical membership validation and the existing aggregate/graph deadline owner. Graph output remains independent.

The retained certification engine is Mongo8.3; other engine versions have not been certified by these runs. Native limits are additional to logical traversal limits: the existing command BSON1MiB/depth90 guards, a preallocation/pre-dispatch1000-stage guard, no disk spilling, engine stage memory limits, native result/document limits and the existing30-second/64MiB read budgets. Mongo documents [pipeline limits](https://www.mongodb.com/docs/manual/core/aggregation-pipeline-limits/) and the [facet memory limit](https://www.mongodb.com/docs/manual/reference/operator/aggregation/facet/).

An actual native negative control lowers the owned replica's facet budget to1byte rather than allocating a large dataset. It exposed the mapper handling292 but missing named146/ExceededMemoryLimit. The mapper now recognizes both; the [Mongo8.3 facet executor](https://github.com/mongodb/mongo/blob/r8.3.0/src/mongo/db/exec/agg/facet_stage.cpp) uses the named memory error. The test verifies146, CQL_QUERY_LIMIT, absent partial output, exact parameter restoration in finally and successful reuse.

The first completed native batch passed58 cases; the expanded negative batch passed59/60 and exposed the mapping defect. After that root fix,219 unit tests,60 native cases and109 joint cases passed without skips. Final40754 actual terminal0:220 unit tests,61 native tests (19 descendant controls) and109 joint tests pass with no skips, including both-order negative/overflow scopes and preallocation rejection. Source/PDB712Core/54Shared verified: CoreE6E7EE404AA0B2282036291A868E5C7F3CA108C3CE26148AD0F39EF866D44733 / Shared325CF326FCF0B58D6E2FBAAF84BBE27CE4851112DE500F11E5D2A2707CEB038A. Evidence is retained in TestResults/CosmicCqlDemo/mongo-descendant-source.log; no Core/Shared source change is part of this provider checkpoint.
