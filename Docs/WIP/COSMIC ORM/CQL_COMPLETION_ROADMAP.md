# CQL completion roadmap — MongoDB, SQLite and Neo4j

23 October 2026. The user requested completion of remaining CQL work. Bounded v1 and its demos remain the verified baseline. Unchecked gates below are not implemented/certified simply because an API has been proposed.

Scope: generic Core/HolonManager contracts, native execution on these three providers, hosted authorization/accounting, tests and documentation. Preserve v1 compatibility and Hyperdrive V2 ownership. Other providers, legacy Hyperdrive changes and unrestricted arbitrary C# execution are outside this focused programme.

## 1. Child loading and relationships — first priority

- [x] Define immutable leaf/graph shape, recursive behavior, maxChildDepth, child type and node/payload limits; preserve provider-native work limits. [Core contract and pending native work](CQL_GRAPH_LOADING.md). Runtime graph budget enforcement remains in the following gates.
- [x] Expand graphs beneath admitted provider execution, preserving root paging. No unmetered manager reload loop after execution or silent provider substitution.
- [x] Preserve selected/omitted child ownership through authorized graph selection and conditional save end to end. Complete/selected typed role hydration now shares one owner: retained full store observation is mandatory, omitted IDs are not resolved, and a partial view cannot inherit complete replacement permission. Native authorized cases now verify AcceptChanges cannot grant replacement, a root-only conditional save advances exactly one root version, and every child version/provider revision remains unchanged, with/without manifests on all three stores (Mongo25/Neo joint88/SQLite21). Partial queries must not delete unselected children on write-back.
- [x] Add explicit portable graph consistency requests/admission. Neo4j read committed must not satisfy a requested repeatable snapshot; ProviderSnapshot is admitted for SQLite/Mongo, rejected by Neo at planning/readmission before I/O, and native race tests request it explicitly. Cross-provider common snapshots remain outside this certified guarantee.
- [ ] Enforce authoritative access on traversed nodes/edges before inclusion or relationship matching. Core now carries immutable owner/public actor identity, preserves its native root predicate through composition, binds WEB4 JWT/admission and rejects foreign rows before typed publication. Native child predicates and shared partial assembly are now verified on Mongo/SQLite/Neo (23/19/86-case cohorts). Detached node policy validation and partial typed publication now pass Core1,762; native-to-typed SQLite verification passes in the published19-case graph gate. Explicit GraphReadAccess admission and safe SDK/HTTP graph composition are now implemented and pass signed native HTTP visibility/privacy/receipt checks on all three providers. Inaccessible children must not leak through Any/Count or nested output.
- [ ] Complete native direct-child/descendant existence certification, including cycles, duplicates, missing parents and reparenting. Direct children run natively on all three stores and through bounded HTTP groups. Descendant owners now exist on all three: SQLite recursive CTE (full 1248/focused 45), Neo quantified paths (unit 316/native 45/joint 109), Mongo flat snapshot traversal (unit 220/native 61/joint 109, including final compiler-budget refresh). All advertise depth 32/nodes 10000. Real33-edge boundary and moved-subtree controls now pass fullSQLite1249/focused46, Mongo62 and jointNeo110, including declared-empty/absent new-owner membership. Concurrent reparent/post-read cancellation controls now pass SQLite48/Mongo64 and4newNeo cases; The fresh joint 114-case certification passes after exact reviewed HTTP fixture reconciliation; Core 1,834 also passes. Strict in-flight driver interruption/deadline latency remains open. Descendant HTTP now passes Core1830/joint109 with bounded server-policy groups; sixth SDK/HTTP demos pass fresh18/18 counts,152 settled/151 attempts. See [contract](CQL_DESCENDANT_CONTRACT.md) and [examples](CQL_RELATIONSHIPS.md).
- [x] Verify graph loading on all three stores: root-only/direct/recursive/bounded depth, cancellation, excessive graphs, custom child roles, cycles, visibility and later Save behavior. Native Mongo27/Neo joint91/SQLite25 pass; the last controls prove SQLite cycle/cancellation/read-owner reuse and Neo coherent snapshot/projection/self-edge cycle rejection. Relationship predicate semantics remain in the separate unchecked gates.

Source checkpoint: Core now materializes the admitted graph shape. SQLite expands root pages inside one bounded native read owner using the reusable Shared assembler. MongoDB now uses one bounded snapshot session and advertises GraphExpansion; native104/admission31 pass, including16 graph cases. Neo4j now advertises GraphExpansion and expands under one bounded read-committed transaction; native81/unit31 and scoped18 pass. Its native consistency differs from SQLite/Mongo snapshots. The newer public graph gate passes Core1781/Shared960/Mongo23/SQLite19/joint86, including signed HTTP child visibility and privacy checks. Unchecked coverage/relationship requirements above remain explicitly open; this is not full CQL completion.

## 2. Projections, aggregates and grouping

[Implementation contract and native acceptance invariants](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md). Count/LongCount/Any are shipped and verified as recorded below; native SDK/HTTP projections are implemented as recorded below; non-count aggregates and grouping remain planned.

- [ ] Complete immutable result shapes and capability admission before native I/O. Count/LongCount/Any Core intent, actual-adapter readmission, detached per-provider execution and the common row/scalar attempt owner are implemented in d71eebd (full Core 1,956 passes). All3 native count/existence adapters and scalar HTTP are now verified; projection/grouped shapes remain required.
- [ ] Implement supported Select, Count/LongCount, Any, Min/Max, Sum/Average and GroupBy shapes natively. SQLite Count/LongCount/Any pass full1,269 tests, including12new native controls; Mongo scalar count passes unit229/native76, including9new unit/12new native cases. Neo scalar now passes unit322/jointnative129, including15new controls. Count/LongCount/Any native slices are verified on all3; scalar transport now passes signed joint native129 on all3 (exact137 input each); annotated scalar demos pass27SDK/27HTTP,182settled/181attempts, scalar no-replay3stores, generatedSwaggerbothroutes and finalCore1982/demo1. Projections and remaining aggregate operations are still required.
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

## Current continuation checkpoint — 23 October 2026

Task branch `fix/hyperdrive-v2-providers`, current Core `cf4a5ab`; scalar milestone parent `f5f65cd75`/Core `af6aed5` is the historical baseline. Scalar HTTP and annotated demos are complete for all three priority stores: Core1982, prior jointnative129 and final dedicateddemo1 pass without skips. Fresh27SDK/27HTTP,182settled/181attempts; signed137-input counts and scalar no-replay each pass on all3. Manifest/gitlinks and192changed-document local links validate. Build slot released; no owned live CQL runtime or retained failed fixture. Existing peer Oracle failure evidence remains outside this batch.

Immutable bounded `Select` intent now passes Core1,989/dual-target builds (Coreaa35a43), including7controls, and the dedicated27SDK/27HTTP row/scalar regression remains green. Projection planning/readmission and the generic detached V2 execution owner now pass Core2,011/dual-target builds (Core27f75b7), including22new controls, with native row/scalar demo regression unchanged. SQLite native Select/typed decoding now passes full1,282 plus final16projection controls on the unchanged provider binary (3newtest-only canonical cases after the fullrun). MongoDB native Select now passes unit237/native91 (8newunit/15newnative controls), preserving exact domains, snapshot/error ownership and no entity hydration. Neo4j native Select now passes final unit328/joint native-WEB4 147 unskipped, including18new native projection controls and exact cursor-buffer/UTF8 limits. Authenticated projection transport now passes fullCore2,059 and signed three-store native acceptance; the expanded annotated30/30demo and broad147regression gates now pass unskipped (195settled/190physicalattempts). Next implementation gate: non-count aggregates/grouping. Preserve source operation order, visibility, exact domains, detached results and the existing prepared-attempt owner. Non-count aggregates/grouping, joins, federation, authoritative sharing/tenant policy, extended scalar coverage and strict blocked Neo4j driver-I/O interruption remain unchecked. No100% completion claim. The broad COSMIC ORM goal remains active; no manual resume is required.

## Historical graph checkpoint

Core graph publication and SQLite native SDK expansion pass full Core1,722, Shared954 and SQLite1,218 tests, zero skips. SQLite's 15 native graph cases prove snapshot consistency, read ownership, depth/type budgets, partial save ownership and 150-member batching. [Exact boundary and next steps](CQL_GRAPH_LOADING.md). MongoDB snapshot-native expansion is also certified (native104/admission31, including16 graph cases). Neo4j native expansion is now implemented with explicit read-committed semantics (native81/unit31/scoped18 pass); final hosted82/unit31 gate also passes. Explicit per-provider consistency admission is now implemented in Core d548e55 (Core1730/Mongo19/SQLite15/Neo32/hosted82 pass; final refreshed consumer/source audit passed). Owner/public graph policy and safe HTTP graph transport are implemented in Core0ca01b9 with Core1781/Shared960/Mongo23/SQLite19/joint86 passing; graph demos are being verified. Next: remaining graph coverage/relationship predicates and later phases above.

The user resumed the existing goal and automatic continuation is active. The full objective and unchecked gates remain intact. No new environment is needed. [Status](IMPLEMENTATION_STATUS.md), [manual](CQL_MANUAL.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

23 October runtime-record prerequisite: Core945c65e adds immutable typed runtime column bindings and detached records through the existing projection compiler/planner/executor. FullCore2,018 passes unskipped and both framework targets build. Seven controls cover bounded immutable shapes, exact domains, whole-buffer rejection, source preservation and no arbitrary expression invocation. Projection HTTP/compiler/transport and annotated native demos are still pending; no full CQL/ORM completion claim.

23 October public projection checkpoint: the bounded compiler/endpoint and generic runtime records pass fullCore2,059/0skip plus signed native three-store acceptance1/1. Actual privacy before paging, exact Int64/null/GUID/nested values, durable receipts/no-replay and no-execution unsupported/schema rejection are verified. Annotated10-exampleSDK/HTTPwalkthroughs plus browser/Swagger examples are implemented; final30/30demo and broad147native regression pass unskipped (195settled/190physicalattempts). [Public projection contract](CQL_PROJECTION_HTTP_CONTRACT.md).

Min/Max Core prerequisite now passes full2,102/dual-target builds (11new controls) in Corecf4a5ab. It adds typed selection intent, operation/read-domain admission and exact detached results through the existing scalar owner. Nineteen acceptance vectors lock empty/null/wrong-domain, precision, negative, Unicode/GUID/tick semantics. Native Min/Max remains the immediate implementation gate; existing count profiles do not advertise it. Sum/Average/GroupBy and later roadmap phases remain required.

SQLite Min/Max native implementation now passes initial5 controls and expanded32/33 plus the corrected1fixture case on the same production binary. All19reference vectors are consumed natively. Full SQLite regression remains pending; next providers are MongoDB and Neo4j, followed by selected-scalar HTTP/demos. This does not complete the aggregate/grouping phase.


## MongoDB native Min/Max — 2026-10-23

Implemented in the existing scalar owner, sharing projection cells/windows and exact decoding. Native reduction validates all participating values without entity hydration or a second input sort. Final fresh native matrix: **124 passed, zero skipped**; full provider unit suite: **237 passed, zero skipped**. All 33 new cases pass, including the shared 19-vector matrix, snapshot reparenting, actual aggregate cancellation/reuse, traversal errors outside paging and standard/legacy/mixed UUID ordering. [Implementation and exact evidence](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#mongodb-native-minmax-slice).

SQLite and MongoDB selected extrema are verified. Next: Neo4j selected extrema, then selected-scalar HTTP/demos. Sum/Average/GroupBy and subsequent CQL/ORM roadmap gates remain open; this is not 100% completion.
