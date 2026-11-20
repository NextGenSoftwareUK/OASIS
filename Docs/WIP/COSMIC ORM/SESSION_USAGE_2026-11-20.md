20 November — FUNCTIONAL SESSION/LINQ/DELETE BREADTH NATIVE ACCEPTED. Run44736376145/job136288694151 SUCCESS on exactaa4378189d2748617d68d43c545839ab89a1342f/Core7b04e2dc707ba02f12848799cdfa7aee02f12714. Strict RAM-only artifact14629032038/3913bytes/SHA256bbcfe50e580a7115583bbeded06c75f354840d6906f40ae3d6c6d5b5ce873eef verified EXACT1TRX+3native JSON:4passed/0failed/0skipped (3real MongoDB/SQLite/Neo4j journeys plus1getter-safety control),8logical successful holon writes/4owned cleanup IDs/18timed operations per store, every20required functional/permissions/cleanup flag true.

Usable session breadth now includes root identity resolution and pending-edit preservation; standard Where/order/Take/Single/Count/scalar Select; tracking/no-tracking/re-enable tracking; mixed create/update flush; no-op/detach/clear; fresh database values and explicit reload; deferred soft/hard homogeneous deletion, cancellation/reload cancellation and Added-removal without a write. Actual native records verify soft retention and hard absence. One foreign root refuses the WHOLE deletion batch; changed parent rejected before write. Computed getters not invoked; real Holon.Id auto-property capture admitted. Mixed SAVE/DELETE transactions remain refused before writes and are NOT implemented; graph-wide tracking/fix-up and remaining full EF/NHibernate rows stay open. These are functional native claims, not exhaustive parity or hosted acceptance.

Tiny isolated CI timings (ms, not production benchmarks): native LINQ query Mongo9/SQLite9/Neo273; entity terminal27/8/251; initial create418/415/873. Performance remains a tracked requirement; broad optimization and edge matrices follow functional breadth. Prior accepted date/read-mode cohorts retained without rerun. Scoped9runtime-path five-reader image candidate is being prepared against unchanged Development8d636b7d, preserving unrelated workflows/solution/DNA/peer work. Exact image/package gates must pass before a normal Development push; no new deployment yet.

No local build/test/runtime/database/archive/report download, no shared Atlas/PIPER data or DNA/replication changes, no staging/production action. Native fixture cleanup verified; artifact read and disposed in RAM/0local files. Source-only private indexes self-remove. Delivery order remains functional ORM breadth, SQL editor coverage, real WEB6 Ask flow, virtual/full/hybrid holons; deeper matrices/performance/hardening afterward, while essential permission/atomicity checks remain mandatory.

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

## Explicit read-only roots (source; native verification pending)

Call session.SetReadOnly(entity) after an existing root is tracked. Scalar edits remain local and are excluded from SaveChangesAsync; State reports Unchanged. Explicit Remove still queues an authorized conditional deletion. Reload refreshes the root while retaining its read-only mode. Detach, Clear and Dispose remove session flags.

SetReadOnly(entity,false) adopts current root values as the new baseline, matching the explicit mutable transition contract. It does not retroactively flush edits made while read-only. Call ReloadAsync first to discard those edits and restore database values. Later mutable changes are eligible for the next explicit SaveChangesAsync. Added roots must first commit their create. Per-query default read-only mode, graph-wide transitions and full framework parity are not claimed from this source.

## Canonical root Find (source; native verification pending)

Use await session.FindAsync<Holon>(id) for a key lookup. A first lookup reads and tracks the authorized native row. A persisted cache hit still queries only the native ID under the same caller policy before returning the tracked instance; pending scalar edits remain intact. An unauthorized or absent persisted identity returns no loaded entity even if a caller previously attached that key. Missing keys are not cached, and Detach permits a fresh native load. Local uncommitted Adds return caller-provided state; tracked key mutation is refused. Find does not implicitly flush. This is neither a cached authorization decision nor a provider/query-wide result cache; graph/composite-key model tooling retains its own roadmap.

## Synchronous entry points (source; native verification pending)

Managed SaveChanges, Read, GetDatabaseValues, Reload, Find, LoadParent and LoadChildren have synchronous forms. Stateless Insert, Save, Delete and Read likewise share the async owned implementations, explicit revisions and acknowledged identity/result publication. The synchronous call waits for that operation to finish and does not create a second persistence path or implicit flush. Existing async entry points remain appropriate for the WEB4 request flow. Source08510d56; its native3 sync/async agreement and write-effect cohort is separate from earlier accepted async cases. Broader graph and mixed lifecycle contracts remain open.

## Stateless logical lifetime (source; native verification pending)

CosmicStatelessSession implements IDisposable. Use using for its logical lifetime; manager/provider connection ownership stays with the caller. An idle session closes idempotently. Dispose during an active operation throws without changing the outstanding operation state, allowing its definite native result to finish. After closure, queries and read/write methods return COSMIC_STATELESS_DISPOSED before evaluating caller roots. A fresh session can use the same borrowed manager/provider. This source has one bounded three-provider lifecycle cohort; no connection pooling or full framework lifetime claim is inferred.

## Stateless standard LINQ (source; native verification pending)

stateless.Linq<Holon>() returns an actor/authority-bound native IQueryable. Rows and entity terminals are freshly materialized; scalar/DTO projections stay native. AsCosmicNoTracking is available; AsCosmicTracking is explicitly refused for stateless queries. The same logical operation gate and closed-session refusal apply to async sequences and synchronous terminals. Managed queries use the shared wrapper with their original identity/publication owner. The new native3 cohort verifies that affected managed sequence/scalar and no-tracking behavior alongside stateless functionality; older accepted suites are retained.

## Native lifetime acceptance

Stateless logical closure passed actual MongoDB/SQLite/Neo4j run44783441788 on Core7e107c1a. Busy closure refuses without abandoning the active write; closed methods refuse before root enumeration; disposal is idempotent; a fresh session uses the same borrowed provider for read/update/delete and physical absence checks. Artifact14653352215,2980bytes,SHA256d86c8e2e3b015f72817a76af140948d970cc0c6f19c6c21a879d98a6cba85404. Shared stateless LINQ has a separate new cohort; these results do not certify its changed shared wrappers. Navigation Core035 alone is deployed/metadata-ready at74bc; full populated browser/model and wider framework acceptance remain open.
