# CQL completion roadmap — MongoDB, SQLite and Neo4j

23 October 2026. The user requested completion of remaining CQL work. Bounded v1 and its demos remain the verified baseline. Unchecked gates below are not implemented/certified simply because an API has been proposed.

Scope: generic Core/HolonManager contracts, native execution on these three providers, hosted authorization/accounting, tests and documentation. Preserve v1 compatibility and Hyperdrive V2 ownership. Other providers, legacy Hyperdrive changes and unrestricted arbitrary C# execution are outside this focused programme.

## 1. Child loading and relationships — first priority

- [ ] Define immutable graph options: loadChildren, recursive behavior, maxChildDepth, child type selection and explicit node/payload/work limits. Reconcile existing depth semantics rather than inventing conflicting meanings for zero/negative depth.
- [ ] Expand graphs beneath admitted provider execution, preserving root paging. No unmetered manager reload loop after execution or silent provider substitution.
- [ ] Preserve selected/omitted child ownership through detached snapshots, typed hydration, tracking and conditional save. Partial queries must not delete unselected children on write-back.
- [ ] Enforce authoritative access on traversed nodes/edges before inclusion or relationship matching. Inaccessible children must not leak through Any/Count or nested output.
- [ ] Implement native direct-child/descendant existence predicates, with defined cycle, duplicate, missing-parent and reparent behavior.
- [ ] Verify all three stores: root-only/direct/recursive/bounded depth, cancellation, excessive graphs, custom child roles, cycles, visibility and later Save behavior.

Source checkpoint: `HolonManager-Cql.V2.cs` explicitly materializes with loadChildren:false; adapters return leaf rows. `CosmicQueryExecutor` owns detached publication and hosted attempt admission. Expansion belongs in that contract. Audit existing provider graph readers and role manifests before introducing another reader.

## 2. Projections, aggregates and grouping

- [ ] Add immutable result shapes and capability admission before native I/O.
- [ ] Implement supported Select, Count/LongCount, Any, Min/Max, Sum/Average and GroupBy shapes natively.
- [ ] Specify missing/null/empty-set semantics, overflow, precision, collation and grouping-key equality; reject unsupported shapes explicitly.
- [ ] Apply access predicates before aggregation; publish detached DTO/scalar results without accidental tracked-entity behavior.

## 3. Joins and composition

- [ ] Add bounded same-provider inner/left joins with defined key domains, null semantics and cardinality limits.
- [ ] Implement selected post-projection/post-window composition through native subqueries, preserving operation order.
- [ ] Cross-provider joins require an explicit bounded distributed plan/accounting contract. No implicit client scan or fallback evaluator.

## 4. Federated results and continuity

- [ ] Define opt-in logical identity deduplication, conflict/freshness selection and provenance; preserve existing grouped results by default.
- [ ] Implement bounded global ordering/pages with deterministic ties, explicit cursor/consistency semantics, intervening-write behavior and cursor expiry.
- [ ] Enforce eligibility, authorization and budgets across all targets. Specify provider-failure behavior before dispatch; no accidental partial success.

## 5. Scalar coverage and authoritative policy

- [ ] Complete the provider scalar/operator matrix, including decimal/double/UInt64 where exact persisted domains support the promised operations. Fix storage projections at their owner; no lossy casts or false capabilities.
- [ ] Expose certified new types/shapes through an explicit transport contract; preserve the published v1 surface.
- [ ] Integrate authoritative shared-recipient and tenant policy. Metadata indexes, client tenant IDs and elevated-role labels cannot alone grant access.
- [ ] Apply policy equally to children, joins, aggregates, federation and output; test cross-actor/tenant denial on each store.

## 6. Completion certification

- [ ] Each feature above has implementations and positive/negative native evidence on all three providers; no skipped acceptance cases.
- [ ] Verify concurrent mutation, cancellation/deadlines, node/row/byte budgets, unknown outcomes, durable attempts/receipts and no-replay for new shapes.
- [ ] Existing Core/provider/WEB4 regressions pass; consumer artifacts match producers and dependency pins match gitlinks.
- [ ] Update annotated SDK/HTTP/browser demos, manual, architecture, operator/reconciliation guide and capability matrix.
- [ ] Reconcile the resource ledger and remove owned disposable fixtures after teardown; preserve shared environments.
- [ ] Commit/push verified task batches; integrate into Development at the agreed cadence or explicit user request.

## Continuation checkpoint

Reviewed the saved goal, v1 evidence, generic query composition/execution/materialization and SQLite/Neo query entry points. No new implementation or acceptance test result is claimed at this checkpoint. Next: inspect existing graph readers/selection manifests and add failing graph-option/publication contract tests before changing native readers.

The existing long-running app goal is paused. The user must use Resume goal for automatic continued turns; no new goal/environment is needed. Active-turn work remains possible. [Status](IMPLEMENTATION_STATUS.md), [manual](CQL_MANUAL.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
