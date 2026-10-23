# Bounded native descendant existence

23 October 2026. Generic contract checkpoint; native implementations and the full roadmap are not complete.

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

No current provider declares DescendantExists at this checkpoint. MongoDB, SQLite and Neo4j therefore explicitly reject descendant requests before physical I/O. Direct-child SDK/HTTP queries remain supported. Native execution is the next implementation step; there is no client evaluator or manager reload loop.

## Required native semantics

- Roots match if at least one qualifying canonical descendant is reached within MaximumDepth. Direct children have depth1; the root never matches itself.
- Each traversed edge requires current ParentHolonId and declared canonical role membership, or the explicit absent-manifest parent-link representation. Reparenting must invalidate an old path. Dangling declarations do not manufacture nodes.
- Mandatory trusted access applies before visiting/inclusion and before traversal through an intermediate node. An inaccessible branch cannot make a match or increase the logical visited-node count.
- MaximumNodes limits distinct visited visible canonical descendants for each root and relationship condition, independently of native whole-command work/deadline/byte budgets. A provider may stop once existence is established. If another node is required after the budget is exhausted, return CQL_QUERY_LIMIT; never truncate into false.
- A depth boundary deliberately limits the question; unvisited descendants beyond it are not an error. Duplicate equivalent physical edges are deduplicated. Conflicting canonical identities or an encountered identity repeated on the same path produce CQL_DATA_INVALID, rather than infinite traversal. No inspection beyond an already conclusive existence result is required.
- Native traversal/filtering precedes root sorting/Skip/Take, under the provider's declared consistency owner. GraphSnapshot must not be inferred for Neo4j. No cross-provider common snapshot is implied.
- Cancellation/deadlines and invalid data publish no partial rows. All root/relationship work belongs to the admitted physical attempt and accounting receipt.

## Verification boundary

Core tests cover bounds, immutable expression/actor binding, independent output shape, post-window rejection, first-error preservation, explicit capability consistency, certified depth/node admission and actual-adapter zero-dispatch rejection. Native membership, policy pruning, cycles/duplicates, budgets, concurrency and transport evidence remain necessary before a provider may advertise this capability.

See [completion roadmap](CQL_COMPLETION_ROADMAP.md) and [direct-child semantics](CQL_RELATIONSHIPS.md).

Core23da2dcb5fc44d2bcbc447aaca5d7cec4a963840 verification39427 terminal0: fullCore1815/Shared968/joint98 pass unskipped. net10.0 and netstandard2.1 build cleanly. Final consumer source/PDB712Core/54Shared match CoreE6E7EE404AA0B2282036291A868E5C7F3CA108C3CE26148AD0F39EF866D44733 / Shared64C24839C7AD46FD710D5DB6DB32610374DC55B1A57B984E8C81A44469E19980. Native descendants remain unsupported on all three adapters until their execution gates pass.
