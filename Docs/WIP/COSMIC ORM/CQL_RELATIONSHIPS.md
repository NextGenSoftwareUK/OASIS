# CQL relationship predicates

23 October 2026. This is an implementation contract; the full relationship roadmap is not complete.

## Direct-child existence

The generic SDK exposes `WhereAnyChild(Expression<Func<IHolon,bool>>)` on the immutable query and its OASISResult fluent wrapper. It appends an ANDed existence condition before the root page. Captured scalar values are frozen by the existing expression compiler; no supplied delegate runs on fetched records. Multiple clauses are independent EXISTS predicates and may match different children.

```csharp
var policy = CosmicQueryReadAccess.OwnerOrPublic(authenticatedActorId);
// Check policy.IsError before using Result.
var query = CosmicQuery<Holon>.Create(
        CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result)
    .WithReadAccess(policy.Result)
    .WhereAnyChild(child => child.Name == "match")
    .OrderBy(parent => parent.Name)
    .Take(10);

// Check query, plan against the actual adapter, then execute through HolonManager.
```

`ChildExists` capability524288 is required at planning and actual-adapter re-admission. An access-bound query also requires native GraphReadAccess. SQLite implements correlated SELECT EXISTS. Neo4j implements a parameterized native EXISTS subquery over HAS_CHILD and the canonical membership projection. Mongo currently rejects this operator before I/O pending its native correlation owner. The operator is not yet part of the bounded WEB4 HTTP DTO. Existing HTTP child graph loading is independent and remains supported on all three stores.

## Membership and visibility

A matching child must have its current ParentHolonId equal the parent Id and cannot be the parent itself. When the parent has a stored role manifest, that schema1 manifest must belong to the parent and list the child in one of its roles. Without a stored manifest, the documented parent-link representation supplies membership. This is a representation contract, never an error-based retry or alternate-provider fallback.

A dangling declaration does not make EXISTS true. A physical parent link outside a declared full role manifest does not count. Reparenting removes the old parent's match immediately; the new parent's declared membership must also include the child, while parent-link records use current ownership. Duplicate matching rows/role entries cannot inflate a boolean EXISTS result. Self-links never count. This direct operator does not traverse deeper cycles; bounded descendant traversal semantics remain separate unfinished work. Stored metadata/manifest corruption is not repaired by the query.

The mandatory owner/public policy is applied to the child inside the native subquery before it can affect parent matching, ordering or paging. Sharing labels in metadata grant nothing. The child predicate uses the same exact canonical/typed metadata domains as normal CQL predicates. There is no implicit active/deleted filtering beyond the existing CQL data contract; specify the intended child filter explicitly.

## Query order and work ownership

Relationship filtering after an existing Skip/Take rejects because a native subquery is required to preserve that order. Root-only projection remains the default: this operator matches parents without loading child graphs into application memory. WithChildren may be composed independently when the selected root graph is also required.

SQLite uses one parameterized native statement with correlated aliases, role JSON membership, child visibility and exact scalar predicates. Parent and child metadata paths cannot accidentally resolve against the wrong row. Parameters share the existing900-binding budget and the existing native instruction/deadline/cancellation owner. No client scan or post-page filtering occurs.

## Neo4j membership projection

Schema3 derives f_HasChildManifest and f_ChildIds from the canonical stored manifest, including named roles. A foreign-owner manifest fails projection construction. Activation uses the existing locked projection upgrade owner to rebuild older projections from canonical snapshots, preserving logical snapshots and revisions. Schema3 projections missing membership fields reject outside the root page.

Native EXISTS requires HAS_CHILD, matching current parentage, non-self identity, current projection/revision and canonical membership (or the explicit absent-manifest representation). Child access and scalar conditions run inside the correlated subquery. Separate clauses use independent aliases and share the900-parameter budget and existing native deadline/cancellation owner. Neo4j retains its declared read-committed consistency; this does not add snapshot support.

## Evidence and remaining work

Neo certification28074 terminal0: Neo unit314 and joint native/WEB4 96 pass, zero skips. New native controls cover private/public/actor-owned exact Int64 matching, independent sibling EXISTS aliases, unlisted physical edges, schema2 rejection before upgrade and schema3 activation migration preserving the exact canonical snapshot. Hash-bound completed-fixture reconciliation1 passed before the fresh run; owned fixture files are absent. Source/PDB711Core/54Shared matched CoreBFB99852BE111503F20F12702BD622192BA1D528D0AD693BDF70E3942B20F57D and Shared55B512686B8993A89BE39E2DFC8DABC47836E469FD019161AB28ABFFAD452034.


Published certification10773 completed terminal0: Core1,785, Shared968, joint91 and SQLite29 pass, zero skips. Native controls cover hidden/public/actor-owned matching before root paging, unlisted physical orphans, declared vs absent new-owner membership after reparenting, independent sibling EXISTS aliases, nested child metadata and exact Int64 beyond2^53. Core tests cover frozen literals, post-window rejection and a planned capability that the actual adapter cannot borrow (zero dispatches).

Published source/PDB checks match711Core/54Shared documents: CoreBFB99852BE111503F20F12702BD622192BA1D528D0AD693BDF70E3942B20F57D, SharedD7FA2D058B6DEB9323392D4FD652AB37FE4184FC3CDD0354F5D061C4458A46FC. Mongo native support, expression-syntax nested Any, descendant existence, relation Count, Boolean OR/NOT composition, public HTTP relation descriptions and later aggregates/joins/federation remain roadmap work.
