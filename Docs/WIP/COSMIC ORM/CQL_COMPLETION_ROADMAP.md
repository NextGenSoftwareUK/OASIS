# CQL completion roadmap — MongoDB, SQLite and Neo4j

24 October 2026. Current verified checkpoint: Core 2,364 unit tests; grouped native implementations on MongoDB, SQLite and Neo4j; integrated signed demo 51 SDK queries/51 HTTP examples with 231 settled operations and 226 attempts. These are distinct evidence scopes, not a combined full-suite certification. See [grouped HTTP evidence](CQL_GROUPING_HTTP_CONTRACT.md).

Next implementation: native grouped filtering on SQLite/MongoDB/Neo4j (shared capture/capabilities/proof verified), HTTP filters/demos, then exact Average ordering; native/HTTP grouped paging and the51/51 live demo are verified on all three providers. Enrolled key/count/extrema/integer Sum ordering is verified on all three providers and through the live demo. Original requirements remain below; historical sections preserve their original cohorts and must not override this checklist. Full CQL/COSMIC ORM completion is not established.

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

Current verified contracts: [projection/aggregate ownership](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md), [exact numeric aggregates](CQL_NUMERIC_AGGREGATES.md), [grouping contract](CQL_GROUPING_CONTRACT.md). Historical milestones below retain their original test cohorts; current status is authoritative here.

- [x] Immutable detached Select and scalar shapes, actual-adapter admission and the existing generic manager/physical-attempt owner.
- [x] Native Count/LongCount/Any, selected Min/Max and exact Int32/Int64/nullable Sum/Average on MongoDB, SQLite and Neo4j, preserving source windows, visibility and protected traversal checks.
- [x] Bounded SDK/HTTP transport and spaced/commented demos for supported scalar/grouped operators: current Core 2,280 controls and integrated 45 SDK/45 HTTP demo, zero skips; 225 settled/220 attempts. Historical full provider regressions retain their own scopes and binary cohorts.
- [x] Empty/null/precision/overflow semantics for the enrolled scalar operations and detached selected publication. Broader scalar coverage and authoritative sharing/tenant policy remain separate unchecked requirements below.
- [x] Immutable scalar/composite GroupBy key capture through the existing projection compiler, independent grouped-output bounds and source/access/window preservation (14 new controls).
- [x] Capture typed grouped result selection and key/Count/LongCount/Min/Max/Sum/Average slots through a common detached DTO-shape owner (13 new controls; full Core2,182 passes).
- [x] Independent grouped capability planning and actual-adapter readmission, including every key/aggregate and output budgets (18 new controls; full Core2,200 passes).
- [x] Validate complete grouped identity/count/result buffers before shared DTO construction; enforce exact scalar domains, key order, proof agreement, source/window/work and payload bounds (24 new controls; full Core2,224 passes).
- [x] Integrate grouped execution with the existing physical-attempt owner, all-provider preflight and all-buffer validation before any DTO construction (15 new controlled executor/hosted cases; full Core2,239 passes). Native provider enrollment remains below.
- [x] Implement grouped Count/LongCount, Min/Max and exact Sum/Average natively on all three providers, with independent grouped capability admission and native source windows.
- [x] Verify initial grouping boundaries: SQLite 19 controls/full 1,364; MongoDB 16 selected grouped controls (prior full 147 CQL/260 units); Neo4j 10 selected grouped controls (prior full 137 CQL/345 units). These counts are scoped checkpoints, not a single new full regression cohort.
- [ ] Complete the expanded all-provider grouped identity/type/access/snapshot/interruption matrix and final coherent regression certification.
- [x] Multiple aggregates, bounded grouped HTTP, named runtime bindings, spaced C#/HTTP/UI/Swagger examples and live three-provider acceptance. Route registration/admission/tariff classification shares an exact Core leaf registry; failed fixture outcomes were reconciled before rerun.
- [ ] Implement and certify post-group filters/order/pages while preserving source-window versus grouped-window operation order and complete work/error proofs.

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

## Historical continuation checkpoints — 23 October 2026

Task branch `fix/hyperdrive-v2-providers`, current Core `cf4a5ab`; scalar milestone parent `f5f65cd75`/Core `af6aed5` is the historical baseline. Scalar HTTP and annotated demos are complete for all three priority stores: Core1982, prior jointnative129 and final dedicateddemo1 pass without skips. Fresh27SDK/27HTTP,182settled/181attempts; signed137-input counts and scalar no-replay each pass on all3. Manifest/gitlinks and192changed-document local links validate. Build slot released; no owned live CQL runtime or retained failed fixture. Existing peer Oracle failure evidence remains outside this batch.

Immutable bounded `Select` intent now passes Core1,989/dual-target builds (Coreaa35a43), including7controls, and the dedicated27SDK/27HTTP row/scalar regression remains green. Projection planning/readmission and the generic detached V2 execution owner now pass Core2,011/dual-target builds (Core27f75b7), including22new controls, with native row/scalar demo regression unchanged. SQLite native Select/typed decoding now passes full1,282 plus final16projection controls on the unchanged provider binary (3newtest-only canonical cases after the fullrun). MongoDB native Select now passes unit237/native91 (8newunit/15newnative controls), preserving exact domains, snapshot/error ownership and no entity hydration. Neo4j native Select now passes final unit328/joint native-WEB4 147 unskipped, including18new native projection controls and exact cursor-buffer/UTF8 limits. Authenticated projection transport now passes fullCore2,059 and signed three-store native acceptance; the expanded annotated30/30demo and broad147regression gates now pass unskipped (195settled/190physicalattempts). Historical next gate at that checkpoint was non-count aggregates/grouping; current remaining gates are listed above. Preserve source operation order, visibility, exact domains, detached results and the existing prepared-attempt owner. Non-count aggregates/grouping, joins, federation, authoritative sharing/tenant policy, extended scalar coverage and strict blocked Neo4j driver-I/O interruption remain unchecked. No100% completion claim. The broad COSMIC ORM goal remains active; no manual resume is required.

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


## Neo4j selected Min/Max — 23 October 2026

Native Min/Max now executes through Neo4j's existing scalar owner, sharing projection guards and exact decoding. Final acceptance: **111 native CQL tests plus 328 provider unit tests, zero skips**, including all 28 new cases. The 18 supported reference vectors pass; UTC DateTime reads remain unsupported and borrowed date permissions are rejected before fetch. Read-committed semantics, blocked-I/O interruption and native character-count wire-bound limitations remain explicit. [Implementation and exact evidence](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#neo4j-native-minmax-slice).

Next: selected-scalar HTTP and demonstrations, followed by the remaining aggregate/read-domain and CQL/ORM gates. This does not complete the full roadmap.


## Selected Min/Max HTTP checkpoint — 23 October 2026

Implemented bounded Selection for Min/Max on the existing scalar route. Count/LongCount/Any reject Selection; Min/Max require it. Projection and scalar HTTP share one typed bounded selector compiler through a generic consumer, without reflection/dynamic invocation or executable delegates. Actual adapter selection/read permissions remain authoritative. Selected values reuse the existing exact projection wire encoder (Int64 decimal text, Guid D, UTC O, nullable null), five-second response checks, one-attempt receipt owner and atomic1MiB output limit.

Full Core/HTTP tests pass **2,117, zero skips**, including11new selected endpoint controls. Browser JS syntax passes. SDK/HTTP walkthrough source now has12 spaced/commented examples/provider, with Min/Max over signed exact Int64 data; the runner requires fresh36SDK/36HTTP. Browser presets and Swagger include selected extrema.

Native verification is **not complete**: combined175case run passed172/failed3. Signed all-store test failed at a late stale Swagger assertion (expected LongCount; sample now Min), after the new signed selected-query checks. Two later tests encountered retained fixture data. Assertion now checks Min plus typed metadata selection. Walkthroughs were not reached, so36/36 is NOT certified. Retained marker SHA256 AC9E6BCCD1AF2A90133F85E32DB7DFD1A7CEA31E28C79EAA90E2908F3ADB2565, stage before-http-request,9graphIDs+137scalarIDs,396journalentries. Do not reset/replay. Existing six-entry no-execution reconciliation does not fit: add an exact reviewed reconciliation that verifies terminal usage outcomes, all146 owned records and scalar-write receipts before cleanup, then rerun broad175 and fresh36/36.


## Selected HTTP and demo gate verified — 23 October 2026

The retained fixture was reconciled by the exact AC9E6B... journal and76BF29... native evidence:180settled operations/175matching receipts,411acknowledged scalar writes and all146owned holons in each of3stores verified before cleanup. Original journal and438snapshot hashes are retained. No queries were replayed. The stale Swagger assertion now checks Min and its bounded Int64 metadata selection.

Fresh combined verification **passes175native regression tests, zero skips (3m7s)**, plus **36SDK queries and36HTTP examples** (12perprovider) on MongoDB, SQLite and Neo4j. The demo reports **216settled operations and211physical provider attempts**. Signed selected queries verify exact negative Int64 values beyond2^53, visibility before reduction (hidden rows contain stronger extrema), source windows, nullable empty inputs and no replay. Full Core/HTTP unit evidence remains2,117passing/0skip, including11new endpoint controls. Browser JavaScript syntaxcheck passes.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx; TestResults/CoreCurrent/cql-selected-scalar-http-current.trx; TestResults/CosmicCqlDemo/v1-demo-result.json, v1-walkthrough.txt, selected-http-native-final.log and selected-http-native-artifacts.json. Five producer/consumer DLL pairs agree (Core109677, shared151E8DC5); the existing runner verified fixture ownership, stopped owned services and removed the exact SQLite/wal/shm/marker after process exit. All four paths and27029/17687listeners were explicitly absent. No runtime/download/new environment was introduced.

This supersedes the pending native/demo checkpoint above. The selected-extrema HTTP/demo gate is complete; the full CQL/ORM roadmap remains open, including Neo4j UTC-date enrollment and strict blocked-I/O interruption, Sum/Average/GroupBy and later federation/query/ORM acceptance.


## Integer Sum/Average Core checkpoint — 23 October 2026

Immutable typed intents, operation-specific exact read admission and constant-size native accumulator normalization now pass both Core targets and all **2,141 tests, zero skips**, including24new cases. Integer Sum checks its final mathematical domain; Average returns Decimal without a binary64 intermediate. Empty/null, precision/rounding and explicit work-limit semantics are specified. **No native provider advertises Sum/Average yet**; native execution/HTTP/demos and the rest of the roadmap remain open. [Contract, examples and evidence](CQL_NUMERIC_AGGREGATES.md).


## SQLite numeric gate — 23 October 2026

Native Sum/Average now share the existing scalar read owner and exact projection operand guard. Native two-limb reduction preserves exact mathematical sums and Decimal averages without a binary64 accumulation or input-row buffer. Final **1,345 SQLite tests pass, zero skips**, including 27 new numeric cases; producer/consumer identities agree and fixed fixture files are absent after teardown. MongoDB/Neo4j numeric execution and HTTP/demos remain open. [Semantics and exact evidence](CQL_NUMERIC_AGGREGATES.md#sqlite-native-sumaverage-verified--23-october-2026).


## MongoDB numeric checkpoint — 23 October 2026

Native Sum/Average now use exact signed limb aggregation through the existing scalar read/snapshot/cancellation owner. The final Mongo suite passes **144 native tests and 245 unit tests, zero skips**;20 native and 8 decoder controls are new. Core and SQLite numerical support remain verified from their earlier checkpoints. See [numeric contract and evidence](CQL_NUMERIC_AGGREGATES.md). Neo4j numeric support, numeric HTTP/Decimal wire and demos, GroupBy, composition/federation, strict blocked-I/O interruption and the remaining roadmap gates are still open. This checkpoint is not full CQL/ORM completion.


## Neo4j numeric checkpoint — 23 October 2026

Neo4j Sum/Average now use exact integer limb reductions through the existing scalar transaction/read-budget owner. The final suite passes **131 native tests and 331 provider unit tests, zero skips**, including 20 new native cases and three packet unit controls. SQLite, MongoDB and Neo4j numeric SDK execution are now verified. See [numeric contract and evidence](CQL_NUMERIC_AGGREGATES.md). Numeric HTTP/Decimal wire and demonstrations are next; grouping, composition/federation, strict blocked-I/O interruption, Neo UTC DateTime reads and the remaining CQL/ORM gates remain open. This is not full roadmap completion.


## Numeric scalar HTTP checkpoint — 23 October 2026

The existing scalar route now accepts Sum/Average with a required typed Int32/Int64 selection (nullable allowed). Average returns valueType Decimal and an invariant string; Sum retains the existing exact integer wire contract. The existing admission, manager, receipt and atomic response owner are reused. All 14 new endpoint controls pass in the full **2,155-test suite, zero skips**. [Contract/evidence](CQL_NUMERIC_AGGREGATES.md). Live three-provider HTTP acceptance and numeric demo examples remain pending; this does not complete the roadmap.

```json
{
  "provider": "MongoDBOASIS",
  "operation": "Average",
  "selection": {
    "metadataPath": ["stock"],
    "valueType": "Int64",
    "nullable": true
  }
}
```

Use the same request shape with SQLLiteDBOASIS or Neo4jOASIS when admitted. Int64 and Decimal response values are strings; parse them with an exact numeric library when arithmetic is required, rather than JavaScript Number.


## Numeric SDK/HTTP demos verified — 23 October 2026

The fresh joint acceptance run passes **195 native regression tests, zero skips**, plus **42 SDK queries and 42 signed HTTP examples** across MongoDB, SQLite and Neo4j. Examples13–14 demonstrate an exact signed Int64 Sum and an ordered two-row Average returning Decimal text `-9007199254740990.5`. Browser presets and Swagger match the spaced/commented C# examples. The actual run recorded **222 settled operations and 217 physical attempts**; no count was inferred from earlier reports. Full endpoint unit acceptance remains **2,155 tests, zero skips**.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx; TestResults/CosmicCqlDemo/numeric-demo-native.log, v1-demo-result.json, v1-walkthrough.txt and numeric-demo-artifacts.json. Core, Shared and all three provider producer/native-consumer hashes agree. The retained Neo container is exited, Mongo/Bolt/demo ports have no listeners, and the readiness marker and fixed SQLite fixture are absent after acknowledged cleanup. No runtime/environment/download/restore was added.

Run `Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1 -IncludeRegression` for the same bounded acceptance or `-Serve` for the browser demo. Numeric transport and these integer-domain demos are now verified. Decimal/double/UInt64 selector expansion, GroupBy, composition/federation, strict blocked-I/O interruption, Neo UTC DateTime reads and all other unchecked roadmap gates remain open.


## SQLite native GroupBy checkpoint - 23 October 2026

SQLite now advertises the independent grouped adapter contract for Count/LongCount, Min/Max and exact Int32/Int64 Sum/Average. Reduction runs in SQL over the admitted source predicate and ordered source window; it does not hydrate holons or evaluate groups in memory. Every key and selected aggregate domain is checked, including unselected identity keys. Composite keys, NULL-first ordering, case-insensitive logical GUID equality and normalized UTC date equality follow the Core contract.

The current native profile permits up to 1,000 output groups, 32 selected columns and 32 MiB of buffered proof/result data. Input participation has an explicit Int32.MaxValue work bound, independently of the output-group limit: the native sentinel detects excess and returns an error. Excess groups, payload, invalid domains and true final numeric overflow return no partial results. Exact grouped numeric normalization uses the shared Core signed-limb owner; nullable all-empty Sum is zero and Average is null.

Read execution reuses SQLite's scope predicate, deadline/traversal guards and concurrency detector. Relationship scope checks run before source windows. An owned read snapshot is rolled back and its connection closed; a caller-owned serializable transaction/connection remains owned by the caller. Cancellation and subsequent reuse are covered. Inactive providers return CQL_PROVIDER_NOT_READY explicitly.

Verification: **1,364 native SQLite tests passed, zero skipped**, including 19 new grouping controls. Evidence: TestResults/SQLiteCurrent/cql-grouped-full-current.trx; TestResults/CosmicCqlDemo/sqlite-group-full.log and sqlite-group-artifacts.json. Core SHA256 starts 6D193362, Shared 996EE8E6, SQLite 98D67EC2; producer and test-output copies match. The fixed fixture database, WAL and SHM are absent after teardown. This certifies this SQLite slice only: MongoDB/Neo4j native grouping, grouped HTTP/demos, post-group filters/order/pages and remaining roadmap gates are still open.


## MongoDB grouped native read checkpoint - 23 October 2026

MongoDB now implements ICosmicQueryGroupedStorageProvider with independent grouped planning/readmission. It shares projection's bounded selected-row read owner for deadline/cancellation, snapshots, protected membership checks, traversal outcomes and buffering. Empty selected windows retain mandatory protected validation. Native cursor batch size is one. Counts and extrema reduce natively; integer Sum/Average use the existing exact Decimal128 signed-limb/shared Core normalization path. Source windows precede reduction; work and group sentinels reject excess without partial publication.

Verification: producer/consumer build, **260 unit tests and 147 native CQL regression tests passed, zero skips**. The native total includes three new grouped controls: all six aggregate families without hydration, ordered source windows and excess-group rejection. Evidence: TestResults/MongoCurrent/cql-grouped-read-owner-final-current.trx, cql-grouped-native-final-current.trx, cql-grouped-native-regression-current.trx; TestResults/CosmicCqlDemo/mongo-group-{read-rebuild,native-final,native-regression}.log. Producer/consumer hashes in TestResults/MongoCurrent/grouped-native-artifacts.json: Core6D193362, Shared996EE8E6, MongoB9500284. Owned fixture absent and retained Mongo process stopped after verified identity and admin shutdown.

This is initial native integration evidence. Expanded grouped composite/null/date/GUID/visibility/cancellation/overflow acceptance is still open, as are Neo4j native grouping, grouped HTTP/demos and post-group composition. The overall grouping gate remains unchecked.


## MongoDB expanded grouped acceptance - 23 October 2026

All **16 selected native grouping tests pass, zero skips** (13 new boundary cases plus the three initial native controls). This test-only batch requires no production change. Coverage now includes composite identities; explicit NULL/missing equivalence; empty nullable Min/Max/Sum/Average; exact cancelling Int64 totals and Decimal averages; true final overflow; 137 participating rows under one output-group budget; access filtering before malformed hidden values can participate; invalid participating keys/values; Unicode scalar ordering; GUID identity across string case; distinct 100 ns timestamp keys; protected traversal limits despite Take(0); pre-cancellation and subsequent reuse; payload exhaustion and zero remaining group/output budgets.

Evidence: TestResults/MongoCurrent/cql-grouped-boundaries-final-current.trx and TestResults/CosmicCqlDemo/mongo-group-boundaries-final.log (earlier11/15 controls also pass). The prior full147 native CQL regression/260 unit evidence remains the production-code checkpoint; no new full160-suite run is claimed. Strict mid-I/O interruption and grouped snapshot race controls remain separate acceptance work; this pre-cancel test does not prove them. Neo4j native grouping, grouped HTTP/demos and post-group composition remain open.


## Neo4j native grouped read checkpoint - 23 October 2026

Native grouped execution is integrated through ICosmicQueryGroupedStorageProvider and independent actual-adapter readmission. It uses the existing read budget/fetch-one transaction, native source/ownership/traversal compilation, detached cell decoder and shared exact numeric normalization. Owned read transactions roll back after consumption. Global input proofs, group counts and detached UTF-8 bytes are checked before success; native character lower bounds suppress oversized result payloads before transfer. Error codes survive normalization. Empty source windows still execute mandatory protected checks.

Verification: **345 unit tests and 137 native CQL tests passed, zero skips**, including six new native grouping controls. Tests cover all six aggregate families without hydration, source windows, output/payload/zero-group budgets, nullable empty values/keys and traversal errors outside empty windows. The first native draft incorrectly assumed auto-version1 in a fixture with versioning disabled; explicit version2 now proves nonzero arithmetic. Core6D193362/SharedA6AD60B7/Neo45C6403E producer and native copies match. Evidence: TestResults/Neo4jCurrent/cql-grouped-read-final-current.trx, cql-grouped-native-final-current.trx, cql-grouped-native-regression-current.trx and grouped-native-artifacts.json; TestResults/CosmicCqlDemo/neo-group-{final-build,native-final,native-regression}.log.

The reusable runner's four modes that include Neo4jNativeCqlTests now expect six additional cases (137/200/202/201 respectively); only the137 CQL subset was rerun in this checkpoint, not all broader modes. Inspection confirms no owned CQL nodes and no graph fixture manifest after teardown. Expanded Neo4j grouped equality/access/race acceptance, UTC date coverage, grouped HTTP/demos and post-group composition remain open. Initial native grouped execution now exists on all three providers; this does not close the full grouping or overall CQL roadmap.


## Neo4j grouped boundary checkpoint - 24 October 2026

All10 selected native grouping cases pass0skip, including four new boundary controls: composite identities with NULL-first/Unicode scalar ordering, cancelling Int64 intermediates with exact Decimal average, explicit final overflow without partial values, and access filtering before malformed hidden inputs participate. Production source is unchanged in this batch. Prior full137 native/345unit evidence remains the production checkpoint; a full141 run is not claimed. The reusable runner's four inclusive mode counts advance by four (141/204/206/205); broader modes were not rerun here.

Evidence: TestResults/Neo4jCurrent/cql-grouped-boundaries-current.trx, grouped-boundary-artifacts.json and grouped-boundary-final-inspection.log; TestResults/CosmicCqlDemo/neo-group-boundaries.log. The native consumer retained Neo45C6403E/Core6D193362 and used rebuilt Shared13EC9571; the Neo producer directory retains SharedA6AD60B7 from the prior checkpoint. Shared source is unchanged between those informational rebuilds; these DLL copies are not byte-identical and no matched-copy claim is made for this selective test batch. Owned CQL nodes0, fixture manifest absent and retained container exited after teardown. Grouped HTTP/demos, post-group composition, date coverage and concurrency/race gates remain open.


Grouped HTTP request compilation and the protected `/api/data/query-cql/grouped` route are implemented. See [grouped HTTP contract](CQL_GROUPING_HTTP_CONTRACT.md) for JSON, limits, wire domains and current evidence. Full2,271 controlled tests pass0skip; live grouped HTTP/demo acceptance and post-group composition remain open. Scalar and grouped selections share one typed visitor and alias/path validator.


## Grouped CQL live demo - 24 October 2026

The retained signed HTTP demo now passes on MongoDB, SQLite and Neo4j: **45 SDK queries,45 HTTP examples,225 settled operations and220 provider attempts**. Each provider runs15 spaced/commented examples; example15 groups an ordered two-row source window by stock and verifies LongCount, exact Int64 Sum and Decimal Average. Browser UI and Swagger include grouped presets. This is the single integrated live-demo acceptance test, not a rerun of every provider regression suite.

The first grouped HTTP attempt exposed missing admission-path registration. The pinned failed fixture was reconciled against194 settled operations/189 attempts and all146 owned rows per store before scoped cleanup; no reset or blind replay occurred. Core's exact CQL registry now drives hosted admission/Swagger and production tariff checks. The corrected live run completed, receipts settled, owned fixture files were removed and runner services stopped.

Evidence: TestResults/CosmicCqlDemo/v1-demo-result.json, v1-walkthrough.txt and grouped-demo-native-verified.log; TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx. Executed producer copies match Core594AAEDB and Shared8E034BFB. Reconciliation evidence: grouped-fixture-reconcile.log, neo4j-cql-v1-web4-reconcile-current.trx and cql-v1-web4-reviewed-grouped-admission-reconciliation.json. The old42/42 numeric demo result remains historical. Post-group filters/order/pages, joins, federation and other unchecked roadmap gates remain open.


## MongoDB native grouped ordering - 24 October 2026

MongoDB now enrolls exact grouped ordering for supported key domains, Count/LongCount, Min/Max and integer Sum result domains. OrderBy/ThenBy operate on reduced selected columns; complete normalized group identity is the final ascending tie-breaker. Exact integer sum order recombines signed limbs in Decimal128: bounded totals occupy at most29 digits, within its34-digit exact domain. No Double conversion or client sorting is used. Private sort fields are removed before strict packet decoding. Source windows and global input-work proofs remain before sorting/output limits.

Native Average ordering remains explicitly unsupported until normalized Decimal output-order semantics are implemented; its unrounded native quotient cannot silently substitute for Core's rounded Decimal result. SQLite/Neo4j grouped ordering, HTTP result-order fields and post-group filters/pages remain open.

Verification: **262 Mongo unit tests and162 full native CQL tests passed, zero skips**. The18 selected grouped controls include new count/composite-identity ordering and adjacent Int64 sum values beyond2^53. Evidence: TestResults/MongoCurrent/cql-grouped-order-{unit,native,regression}-current.trx, group-order-artifacts.json; TestResults/CosmicCqlDemo/mongo-group-order-{build,native,regression}.log. The explanatory total-digit comment was corrected from28 to29 after verification; arithmetic is unchanged. Actual fixture database absent and owned Mongo process stopped after identity-verified admin teardown. This is Mongo ordering evidence, not all-provider post-group completion.


## Neo4j native grouped ordering - 24 October 2026

Neo4j now enrolls grouped key, Count/LongCount, Min/Max and integer Sum ordering. Selected sort columns precede the complete original composite identity as an ascending final tie-breaker. Explicit null ranks give ascending NULL-first and descending NULL-last semantics. Integer sums compare normalized floor quotient/remainder pairs; no overflowing total recombination or Double conversion occurs. This covers negative remainder borrowing and adjacent integers beyond2^53. Protected failures and payload failures remain ahead of successful rows; input-work proofs and source windows remain intact.

Verification: **347 unit tests and144 full native CQL tests passed, zero skips**. Thirteen selected grouped controls include three new native ordering cases. Evidence: TestResults/Neo4jCurrent/cql-grouped-order-{unit,native,regression}-current.trx, group-order-artifacts.json and group-order-final-inspection.log; TestResults/CosmicCqlDemo/neo-group-order-{build-final,native,regression}.log. The first build caught a local variable name collision, corrected before final build/native tests. The reusable runner's inclusive mode counts advance to144/207/209/208; only144 full CQL mode was verified here, not the broader modes.

Average ordering stays explicitly unsupported pending exact normalized Decimal semantics. SQLite grouped ordering, HTTP result-order fields and grouped filters/pages remain open. The full CQL/ORM goal is not complete.


## SQLite grouped result ordering — 24 October 2026

SQLite now supports selected grouped key, Count/LongCount, Min/Max and exact integer Sum ordering. OrderBy/ThenBy run in SQL after reduction and before the output bound. Complete original group keys provide ascending deterministic final ties. Existing key collations are retained; extrema use NOCASE for GUID and BINARY comparison otherwise. SQLite native ascending/descending null placement matches the Core contract.

Sum ordering compares floor quotient and nonnegative remainder in base 4294967296, preserving negative borrowing and adjacent Int64 values beyond 2^53 without recombining an overflowing total or converting to floating point. Aggregate operation/type permissions are explicit; Average ordering remains rejected pending exact normalized Decimal comparison.

Verification: **1,378 full SQLite integration tests passed, zero skipped**, including four new ordering controls (ascending/descending Int64 boundaries, negative borrowing, count ties and Average admission). Evidence: TestResults/SQLiteCurrent/cql-group-order-current.trx and cql-group-order-regression-current.trx; TestResults/CosmicCqlDemo/sqlite-group-order-{build,tests,regression}.log. Producer and consumer SQLite DLL SHA256 E7000E84E5023C86FB1550A850ACF6B57301C2EC8762FBAEECC40D784FF7910C match; Core SHA256 19F1CA8BC1EF425876D3CB5C6F756BA32C22DA18B0B804EAF06FCD7366D34B81. Exact paths/bytes are retained in TestResults/SQLiteCurrent/group-order-artifacts.json. Owned fixture DB/WAL/SHM verified absent after teardown.

Grouped ordering is now implemented for MongoDB, SQLite and Neo4j for the enrolled operations above. HTTP result-order fields, exact Average ordering, post-group filtering/paging and the remaining roadmap gates are still open; overall CQL/ORM completion is not claimed.


HTTP grouped result ordering, 24 October 2026: added bounded resultOrder aliases/directions, bound with exact selected CLR expressions through the shared Core owner. Source windows remain independent; actual provider admission rejects unenrolled operations before dispatch. Full Core/linked-production suite passes 2,314/0 skips (http-group-order-full-current.trx). Controlled endpoints cover all three profiles and unsupported Average. Post-group filtering/paging and exact Average ordering remain open; live ordering evidence is recorded below. See CQL_GROUPING_HTTP_CONTRACT.md for JSON usage and evidence.


## Ordered groups live demonstration — 24 October 2026

Example16 now demonstrates native grouped ordering in both readable C# and HTTP walkthroughs: group visible rows by Name, select exact Int64 Sum, order Total descending then Name ascending. Three adjacent negative totals beyond2^53 verify actual values and ordering. Browser preset **GroupBy / ordered exact totals** and grouped Swagger resultOrder fields expose the same feature. Source order/windows remain separate.

The retained runner completed **48 SDK queries and48 HTTP examples** across MongoDB, SQLite and Neo4j; **228 operations settled and223 provider attempts**. Actual session96782 terminal0, integrated native test1pass/0skip. This is live demo acceptance, not a rerun of every provider suite. Evidence: TestResults/CosmicCqlDemo/ordered-group-demo-native.log, v1-demo-result.json and v1-walkthrough.txt; TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx. Producer/copy checks verified Core19F1CA8B and SharedDF5015E4. Node24.19.0 syntax check passed for the browser script. Fixture manifest/SQLite/WAL/SHM verified absent after owned cleanup; Mongo27029 and Neo17687 listeners absent.

HTTP result ordering live acceptance is complete for the enrolled operations. Exact Average ordering, post-group filters/pages and the broader unchecked CQL/ORM roadmap remain open.


## Post-group paging contract — 24 October 2026

Core now captures immutable `Skip`/`Take` on a grouped selection as ResultOffset/ResultLimit, independently of Source.Offset/Limit. Repeated Take narrows; Skip consumes a prior Take; skipping beyond it produces an explicit zero limit. Ordering changes preserve the result window. Negative, overflowing and group-budget-exceeding windows reject without mutating earlier intent. This first contract bounds offsets/pages by the independently declared MaximumGroups proof budget; it does not increase that budget.

Paging requires its own SupportsResultPaging capability. Existing constructor signatures remain available and default false. Actual-adapter readmission checks the permission; source paging and grouped ordering permissions cannot grant it. Result validation rejects buffers exceeding the composed result limit, including nonempty Take(0) buffers.

**Native paging is not yet enrolled on any provider.** Before enrollment, native execution must prove complete source participation and preserve protected traversal failures, malformed off-page groups, true final numeric overflow, group/work/payload bounds and owned read teardown even for Take(0). A LIMIT that hides these errors is insufficient. Native provider implementations and hosted paging fields/demos remain required; this is not end-to-end paging completion.

Both Core net10.0/netstandard2.1 targets build; **2,320 Core/linked-production unit tests pass, zero skips**, including six new paging cases. Evidence: TestResults/CosmicCurrent/group-page-core-current.trx; TestResults/CosmicCqlDemo/group-page-core-{build,}.log (test log is group-page-core.log). Private Core cbd77d42c0fb05a879ccf7d75707c4a0a0a3907c; net10 SHA256 5F9A28609728B2B77BAB95F60E533C253B359768FDC2597EDC6936187E188E66 and netstandard SHA256 E3E38DD7DB2F8B4DA1D65441E8514098F749BB7255349DC5E9EC69E19129743C. Native provider binaries from prior gates remain historical cohorts until refreshed.


SQLite native paging verification24October2026: **1,384 full integration tests passed, zero skips**, including six new native paging cases; selected9 grouped controls also pass. Both Core targets build and **2,322 unit tests pass, zero skips**, including complete-proof selection/domain/work controls. Evidence: TestResults/SQLiteCurrent/group-page-{native,full}-current.trx; TestResults/CosmicCurrent/group-page-proof-current.trx; TestResults/CosmicCqlDemo/sqlite-group-page-{build,core,selected,full}.log. Native cases cover selected ordered rank plus retained off-page counts, malformed off-page aggregate for Take0/Take1, skipped final Int64 overflow, excess group budget on an empty page and independent source/result windows. Existing full-suite ownership/cancellation/traversal cases remain green.

Core17000b1eabb778526ba2044bce6cfa0db8f25f6c is paired with the parent gitlink/manifest. Executed producer/copy hashes match Core051A066408A38315DD1D60353D2F44A4C6A237AB414AA894A540C874B53A7FA4 and SQLite34AF25D87599C02CE119E60C614FB1C48C2CB008E0921AFBBAFA6B65F196CF55; netstandard Core167D456AD3B379E93B70FD747252A0D05DC4DFF5766A37D0EC64F7D0814045A6. Exact paths/bytes are retained in TestResults/SQLiteCurrent/group-page-artifacts.json. Fixture DB/WAL/SHM verified absent. Mongo/Neo paging, HTTP paging, filters and exact Average ordering remain open.


## MongoDB native grouped paging — 24 October 2026

MongoDB now enrolls grouped Skip/Take. An ordered $setWindowFields computes one-based group positions with complete normalized identity ties; a Boolean resultMember marks the composed result window. The complete-group limit sentinel stays independent of the requested page. Temporary position/sort fields are removed before strict decoding. Every bounded proof packet is decoded, including off-page domains and exact arithmetic; Core verifies native marker agreement and all-buffer invariants before constructing only selected results. Off-page rows consume global group/payload budgets, preserving the shared proof contract already used by SQLite.

Verification: **295 Mongo unit tests and168 full native CQL tests passed, zero skips**. Six new native cases cover selected ranks/complete proof, malformed off-page aggregate for Take0/Take1, overflow in a skipped group, excess complete-group budget on an empty page, and independent source/result windows. Two new unit controls cover rank/sentinel compilation and strict Boolean marker decoding/off-page validation. Existing native privacy, traversal, deadlines, snapshots and read-ownership gates remain green in this full cohort.

Evidence: TestResults/MongoCurrent/group-page-unit-current.trx and group-page-artifacts.json; TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx; TestResults/CosmicCqlDemo/mongo-group-page-{unit,native}.log. Actual23680terminal0. Producer/native copies match Core051A066408A38315DD1D60353D2F44A4C6A237AB414AA894A540C874B53A7FA4 and Mongo472F71B36C5AE638E0780BBFE08C5507154EF19AE3CACBA737076600A6F3690E. Tests acknowledged fixture database cleanup; runner-owned Mongo67060 exited and27029 listener verified absent afterward. Retained data/runtime are reusable. The runner's stale CosmicCql count144 is corrected to the verified168.

Neo4j paging, HTTP paging fields/demos, post-group filters and exact Average ordering remain open. This is Mongo paging certification, not full CQL/ORM completion.


## Neo4j native grouped paging — 24 October 2026

Neo4j now enrolls grouped Skip/Take. Native reduction and selected ordering precede the complete-group sentinel. For paged intent, Cypher collects only that bounded ordered proof, assigns native zero-based positions and publishes an exact Boolean resultMember. Unpaged intent streams the same strict packet shape with true membership. No source holons hydrate; every off-page proof still goes through exact domain/arithmetic decoding and shared Core validation before selected DTO construction. Protected failure branches remain before source/result windows. Full group/payload proof budgets remain independent of requested page size.

Verification: **349 Neo4j unit tests and150 full native CQL tests passed, zero skips**. Six new native cases cover native selected ranks/complete proof, malformed off-page aggregate for Take0/Take1, true overflow in a skipped group, excess complete-group budget despite an empty page and independent source/result windows. Two new unit cases cover bounded native rank compilation and strict Boolean packet decoding. Existing full native access/traversal/deadline/ordering controls remain green.

Evidence: TestResults/Neo4jCurrent/group-page-unit-current.trx, neo4j-cql-native-current.trx and group-page-artifacts.json; TestResults/CosmicCqlDemo/neo-group-page-{unit,native}.log. Initial globallease refusal made no build mutation; actual10167terminal0. Native producer/copies match Core051A066408A38315DD1D60353D2F44A4C6A237AB414AA894A540C874B53A7FA4 and NeoD52B4404C3A9EE9558837331ADC382CA353AF7624946D4FB64A597CE47990240; runner also verified Shared53D9BCAF75F1697788F0B08FEAFD636F8EE0BA38DBE3748721619E1F0B458F4C. Fixture teardown acknowledged; cql-graph-fixture.json verified absent and exact retained container/image exited. Runner inclusive counts advance to150/213/215/214; only150 fullCQL mode was run here, not broader modes.

MongoDB, SQLite and Neo4j native paging are now verified. HTTP result-window fields/demos, post-group filtering and exact Average ordering remain open. Overall CQL/ORM completion is not claimed.


HTTP paging contract/controlled endpoint checkpoint24October2026:2334Core/linkedunit0skip, including12new compiler/endpoint controls. Native paging is verified onMongo/SQLite/Neo. Demo17/51SDK+51HTTP sourceprepared, liveacceptance awaiting otherowner reviewedMongo fixture cleanup/source gate. See CQL_GROUPING_HTTP_CONTRACT.md; overall roadmap remains incomplete.


## HTTP grouped paging live acceptance — 24 October 2026

`resultSkip`/`resultTake` now run through native paging on MongoDB, SQLite and Neo4j. Example17 shows the same query in spaced/commented C# and HTTP, with browser and Swagger fields: group by Name, sum exact Int64 values, order Total descending/Name ascending, then Skip1/Take1. The selected C-public total remains -9007199254740990; source windows remain independent. Complete bounded proof includes skipped groups.

Verification: **2,334 Core/linked-production unit tests pass, zero skips**; the fresh integrated native demo passes **51 SDK queries and51 HTTP examples**, **231 settled operations and226 physical attempts** across all three providers (actual32210terminal0, native1PASS0skip). Evidence: TestResults/CosmicCurrent/http-group-page-current.trx; TestResults/CosmicCqlDemo/http-group-page-unit.log, group-page-demo-native.log, group-page-demo-artifacts.json, v1-demo-result.json and v1-walkthrough.txt; TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx. Producer/native copies match for Core and all three providers; runner verifies Core051A0664/SharedD5273C49. Browser script passes Node24.19.0 syntax check. This is the integrated demo acceptance test, not an all-suite provider rerun.

The other session completed reviewed fixture cleanup and its Mongo historical-type regressions before granting the live window. No blind reset/replay occurred. Final owned fixture marker/SQLite/WAL/SHM are absent, Mongo27029 and Neo17687 listeners absent. Post-group filtering, exact Average ordering and broader unchecked roadmap gates remain open.


## Shared grouped filtering capture — 24 October2026

Immutable ResultPredicates now capture Where over exact selected columns using the existing source expression grammar and literal snapshots. Source intent stays unchanged; no arbitrary getters/delegates execute. Provider admission and manual materialization explicitly reject filters until independent native capability/proof enrollment exists. See [filtering contract](CQL_GROUP_FILTERING_CONTRACT.md) for semantics and remaining gates.

Root-cause correction: grouped ordering after result Skip/Take is now rejected with CQL_COMPOSITION_UNSUPPORTED, matching row queries. The earlier paging checkpoint's statement that ordering changes preserve windows did not preserve operator semantics; use Where/OrderBy before paging until native subquery stages exist. The shared selected-column resolver also distinguishes a one-column DTO from its scalar cell (genuine1case red, then corrected).

Both Core targets build and2,343Core/linked-production tests pass0skip. Nine new filter controls cover capture/source/literal isolation, scalar/DTO/record types, field comparisons/membership, unsafe execution rejection, operator order, clause bounds, default-denied admission and manual proof publication. Evidence/SHAs in the filtering contract. Native filter profiles/validation/translations, HTTP filters/demos and exact Average ordering remain open; native filtering is not claimed complete.


Grouped filter capabilities/proof24October2026: shared admission now declares exact key/aggregate-result domains and operators independently of source/ordering permissions. Complete proof validation checks native filter/page membership over matching ranks while retaining validation/budgets for all nonmatches. Exact rational numeric comparison avoids Int64 rounding and binary64 subnormal underflow. BothCoreTFMs build;2,364unit/linked-production PASS0skip (21newcontrols). Existing provider profiles still default-deny filters; native SQLite/Mongo/Neo translation and HTTP/demos remain next. See CQL_GROUP_FILTERING_CONTRACT.md for detailed evidence and remaining gates.
