# Using the shared COSMIC session

This feature is implemented in the shipping Core, on the existing actor-bound native CQL reader and authoritative owned batch writer. MongoDB, SQLite and Neo4j passed the baseline session journey in run 44705892869. No-tracking, database values and explicit reload extend the same journey; their current acceptance is recorded in CURRENT_STATUS.md.

## Open and query

Create one session per unit of work with `CosmicSession.Create(manager, nativeProvider, readAccess)`. Use the real provider's `ICosmicQueryStorageProvider`, an explicitly authorized actor and the matching named authority. Check `IsError` before using every result.

`session.Query<Holon>()` returns the actor-bound CQL query. Compose filters, ordering and limits using the existing CQL APIs, then call `session.ReadAsync(query)`. The standard session LINQ bridge is also implemented: use `session.Linq<Holon>()`, compose normal `Where`, `OrderBy`, `Take` or projections, and execute `ToCosmicListAsync` or entity terminals. Its native acceptance is tracked separately below.

Every read executes the native authorization-bound query. Tracking is identity resolution, not an authorization cache. A second authorized query returns the same tracked root instance and preserves pending local edits.

## Change and save

Use `Add(entity)` for a new entity or `Attach(entity)` for an existing detached entity whose current values are the initial baseline. Modify tracked properties and inspect `State(entity)`. `SaveChangesAsync(new CosmicWriteOptions(operationId))` flushes pending creates and updates through `HolonManager.SaveOwnedHolonsAsync`, using the session's actor and authority.

Keep the same explicit operation identity when reconciling an uncertain write outcome. The session does not invent retries. A successful acknowledged write becomes clean; a no-op returns `IsSaved=false` without writing. Attach does not prove database existence or grant write permission.

## Inspect, reload and detach

- `ReadAsync(query, trackResults:false)` returns fresh authorized instances. Editing them does not enqueue a session write.
- `GetDatabaseValuesAsync(entity)` reads current authorized stored values into a separate instance, preserving pending edits in the tracked entity.
- `ReloadAsync(entity)` explicitly discards the tracked root's pending values and replaces its baseline with current authorized stored values, retaining that object's identity. An entity no longer visible is detached; this does not reveal whether it was deleted or became unauthorized.
- `Detach(entity)` removes one tracked instance; `Clear()` removes all. These do not change persisted data.

One session supports one active operation at a time. Finish operations before disposal.

## Scope still open

The implementation covers root entity identity, pending create/update batches, snapshot state, no-tracking reads, database values and explicit root reload. Graph-wide canonical identity and relationship fix-up, Deleted state and mixed atomic delete/create/update, general disconnected graph merge, graph-wide query materialization and transactions spanning calls remain open. A session save is not a cross-call transaction. Do not claim full EF/NHibernate parity from the baseline journey.

## Evidence and resources

The representative native journey creates two owned rows, checks repeated identity reads and pending edits, flushes one update plus one create, verifies a no-op, clears and reloads, and confirms a foreign actor cannot read the private rows. The extension adds database-value, no-tracking and explicit reload checks. CI uses isolated native services and exact owned-row cleanup; no PIPER/shared Atlas data is used. Reports and strict artifact hashes are recorded in the current status and cleanup ledger.

## Standard LINQ tracking modes (source, native acceptance pending)

Core4565e3ec adds `session.Linq<Holon>(trackResults:false)` and `AsCosmicNoTracking()` / `AsCosmicTracking()` on session queries. The tracking mode follows standard query composition, asynchronous enumeration and entity terminals. Both explicit CQL and standard LINQ use one identity publication owner. Scalar projections read stored values, rather than injecting unsaved tracked values into query execution. The extended three-provider journey verifies entity identity, pending edits, scalar projection, Count, tracking modes and foreign-actor exclusion. Full graph relationship fix-up remains open.

## Read-mode acceptance

Run44712697888 on frozen02f0d4cb/Core99d2 passed the three native read-mode/reload journeys. Strict artifact14617648973 (3065bytes; SHA256766e496df49d1b92d68d474ca1c57bad464d59d0e3ff73ef03fc1db75918a995) confirms no-tracking, pending-value preservation, explicit reload, actor isolation and exact cleanup. This acceptance does not replace pending verification of the materially changed standard LINQ bridge in44716587086.

## Deferred root deletion (source, native qualification pending)

Call Remove(entity, softDelete:true/false) on an existing tracked root, then SaveChangesAsync with an explicit operation identity. Delete permission is checked against every stored record before the single native transaction; original provider revisions are required. Success publishes the native acknowledgements and detaches deleted roots; an error retains pending deletion and operation identity for reconciliation. CancelRemoval cancels only local intent; explicit ReloadAsync also clears pending removal after an authorized refresh. Removing a new, uncommitted Add only detaches it.

An explicitly supplied parent must match its authoritative record before commit. Children are not selected for deletion. The current native atomic request supports a single operation kind, so pending save/delete or soft/hard mixtures are refused before any write. Full mixed atomic lifecycle remains open; never interpret separately sequenced operations as one atomic flush.

## Captured query values

Instance auto-properties may be captured as query parameters without calling application getters: the compiler reads validated compiler-generated backing fields. For computed/custom getters, evaluate the value explicitly into a local variable before composing the query. Arbitrary getters, methods and static initialization are not executed by query binding. The native lifecycle journeys exercise captured entity Id; the security control requires a rejected computed getter to remain uncalled.

## Captured query values

Instance auto-properties may be captured as query parameters without calling application getters: the compiler reads validated compiler-generated backing fields. For computed/custom getters, evaluate the value explicitly into a local variable before composing the query. Arbitrary getters, methods and static initialization are not executed by query binding. The native lifecycle journeys exercise captured entity Id; the security control requires a rejected computed getter to remain uncalled.
