# CQL completion roadmap - MongoDB, SQLite and Neo4j

Current checkpoint: **25 October 2026**. Native flat inner/left joins,
signed HTTP and bounded selected-result composition are implemented on MongoDB,
SQLite and Neo4j. The annotated demo now has40 examples per provider in each SDK/HTTP walkthrough.

- Latest separately verified cohorts: Core **3,098** (including UInt64 numeric capture/normalization and input-domain admission),
  Shared **1,189**, SQLite **1,763 production integration /74 unit**, MongoDB **490 native /571 matched unit**,
  Neo4j **490 native /552 matched unit**; zero skips at their recorded checkpoints.
- The expanded live demo passes **120 SDK /120 HTTP**, **327 settled operations
  /319 provider attempts**. Twenty composition/join browser presets pass offline, generated Swagger
  checks and standalone demo build pass. These are distinct cohorts.
- Selected-result composition captures immutable stages and independently admits
  complete proof/work contracts, with **SQLite, MongoDB and Neo4j native SDK execution verified**. Ordinary
  projection execution rejects composed stages. Bounded public composition transport
  is verified; broader certification and graph-source enrollment remain open.
- Full CQL/ORM completion remains unproven. Historical checkpoints below retain
  their original scope; remaining certification/policy/federation gates stay open.

SQLite, MongoDB and Neo4j UInt64 native scalar/grouped Sum/Average are now verified.
Unsigned HTTP compiler/wire tests, shipping build and live signed numeric
HTTP/demos pass (examples35-37). Additional domains and the full policy/federation scope remain open.

Historical verified batch: a shared Core participation validator now replaces
MongoDB/Neo4j duplicated checks and fixes missing SQLite numeric group coverage
validation. Ten original SQLite contradictions reproduced the defect. Full
Core 2,970/SQLite 71 units and three actual native SQLite corruption/reuse cases
pass. Full SQLite acceptance now passes **1,651 tests, zero skips**; MongoDB/Neo4j
native regressions pass **361/362 tests**, zero skips; fresh provider units pass
**538/519**. Task-branch publication follows this complete verification batch.

MongoDB grouped ownership follow-up: full native **364/364**, zero skips,
5m35s. Three new tests certify snapshot membership/keys/values through concurrent
reparenting/writes, actual-I/O cancellation, one cumulative deadline and clean
provider reuse. Broader grouped policy/scalar certification remains open.

Protected empty-window correction in progress: `Take(0)` bypassed native
relationship validation in scalar, row, graph and projection shortcuts. SQLite
and Neo4j each reproduced seven scalar plus three shape failures; MongoDB
reproduced nine of ten cases (its projection path was correct). Protected-aware
shortcuts, one shared SQLite scope owner and shared Mongo source-window shaping
now pass **10 SQLite /3 Neo4j /10 MongoDB focused native controls**, zero skips.
The scalar-only checkpoint separately passed SQLite1,658/71 and Neo369/519.
Full refreshed acceptance now passes **SQLite1,661/71, Mongo374/538 and
Neo372/519 native/unit tests**, zero failures/skips. Fresh102SDK/102HTTP demo,
309settled/301attempts, joint native1/1, source freeze and six binary identities
also pass. Owned fixtures/services are cleaned; task-branch publication follows. Full
CQL/ORM completion is still unproven; every other open requirement below remains.

Scope: generic Core/HolonManager contracts, native execution on these three providers, hosted authorization/accounting, tests and documentation. Preserve v1 compatibility and Hyperdrive V2 ownership. Other providers, legacy Hyperdrive changes and unrestricted arbitrary C# execution are outside this focused programme.

## 1. Child loading and relationships — first priority

- [x] Define immutable leaf/graph shape, recursive behavior, maxChildDepth, child type and node/payload limits; preserve provider-native work limits. [Core contract and pending native work](CQL_GRAPH_LOADING.md). Runtime graph budget enforcement remains in the following gates.
- [x] Expand graphs beneath admitted provider execution, preserving root paging. No unmetered manager reload loop after execution or silent provider substitution.
- [x] Preserve selected/omitted child ownership through authorized graph selection and conditional save end to end. Complete/selected typed role hydration now shares one owner: retained full store observation is mandatory, omitted IDs are not resolved, and a partial view cannot inherit complete replacement permission. Native authorized cases now verify AcceptChanges cannot grant replacement, a root-only conditional save advances exactly one root version, and every child version/provider revision remains unchanged, with/without manifests on all three stores (Mongo25/Neo joint88/SQLite21). Partial queries must not delete unselected children on write-back.
- [x] Add explicit portable graph consistency requests/admission. Neo4j read committed must not satisfy a requested repeatable snapshot; ProviderSnapshot is admitted for SQLite/Mongo, rejected by Neo at planning/readmission before I/O, and native race tests request it explicitly. Cross-provider common snapshots remain outside this certified guarantee.
- [ ] Enforce authoritative access on traversed nodes/edges before inclusion or relationship matching. Core now carries immutable owner/public actor identity, preserves its native root predicate through composition, binds WEB4 JWT/admission and rejects foreign rows before typed publication. Native child predicates and shared partial assembly are now verified on Mongo/SQLite/Neo (23/19/86-case cohorts). Detached node policy validation and partial typed publication now pass Core1,762; native-to-typed SQLite verification passes in the published19-case graph gate. Explicit GraphReadAccess admission and safe SDK/HTTP graph composition are now implemented and pass signed native HTTP visibility/privacy/receipt checks on all three providers. Inaccessible children must not leak through Any/Count or nested output.
- [ ] Complete native direct-child/descendant existence certification, including cycles, duplicates, missing parents and reparenting. Direct children run natively on all three stores and through bounded HTTP groups. Descendant owners now exist on all three: SQLite recursive CTE (full 1248/focused 45), Neo quantified paths (unit 316/native 45/joint 109), Mongo flat snapshot traversal (unit 220/native 61/joint 109, including final compiler-budget refresh). All advertise depth 32/nodes 10000. Real33-edge boundary and moved-subtree controls now pass fullSQLite1249/focused46, Mongo62 and jointNeo110, including declared-empty/absent new-owner membership. Concurrent reparent/post-read cancellation controls now pass SQLite48/Mongo64 and4newNeo cases; The fresh joint 114-case certification passes after exact reviewed HTTP fixture reconciliation; Core 1,834 also passes. Neo4j now passes18 cold/warm caller-cancellation and shipping 30-second deadline controls across all query shapes, plus the full 267-case native CQL regression on committed867182ad8. Other unchecked relationship/access requirements remain open. Descendant HTTP now passes Core1830/joint109 with bounded server-policy groups; sixth SDK/HTTP demos pass fresh18/18 counts,152 settled/151 attempts. See [contract](CQL_DESCENDANT_CONTRACT.md) and [examples](CQL_RELATIONSHIPS.md).
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
- [x] Implement and certify post-group filters/order/pages for enrolled domains while preserving source-window versus grouped-window operation order and complete work/error proofs. SDK/native/HTTP Average comparisons and the57/57 live demo now pass on all three providers; broader scalar/policy certification remains below.

## 3. Joins and composition

Required semantics and acceptance are defined in the
[native join implementation contract](CQL_JOIN_CONTRACT.md). MongoDB, SQLite and Neo4j SDK flat joins are available; the broader join
acceptance below remains open.

- [x] Add bounded same-provider flat inner/left joins with defined key domains, null semantics and cardinality limits; native SDK, signed HTTP and93/93 annotated demos now verified on all three providers.
- [ ] Complete remaining expanded join source/consistency/policy certification and any declared graph-source enrollment.
- [x] Implement bounded flat selected post-projection/post-window composition through native subqueries, preserving operation order. Generic SDK and signed HTTP/native demos now pass on all three providers; graph-source enrollment remains explicitly unsupported.
- [ ] Cross-provider joins require an explicit bounded distributed plan/accounting contract. No implicit client scan or fallback evaluator.

## 4. Federated results and continuity

Required behavior and acceptance are specified in the
[federation implementation contract](CQL_FEDERATION_CONTRACT.md). This is pending
implementation; current results remain separate per provider.

- [ ] Define opt-in logical identity deduplication, conflict/freshness selection and provenance; preserve existing grouped results by default.
- [ ] Implement bounded global ordering/pages with deterministic ties, explicit cursor/consistency semantics, intervening-write behavior and cursor expiry.
- [ ] Enforce eligibility, authorization and budgets across all targets. Specify provider-failure behavior before dispatch; no accidental partial success.

## 5. Scalar coverage and authoritative policy

SQLite floating native SDK integration passes48focused queries and the clean
1,763production integration/74unit gate. MongoDB/Neo4j and HTTP enrollment remain open.


Floating exact packet normalization is verified in fullCore3,090 and both TFMs.
Shared capture/result publication also passes fullCore3,098. Native/HTTP enrollment remains open; see the
[floating aggregate contract](CQL_FLOATING_AGGREGATE_CONTRACT.md).


Decimal Core capture/scaled-limb normalization and SQLite native arithmetic are
verified (1,702native/72unit). MongoDB native Decimal integration passes23 focused native controls and550 units; full416 native acceptance passes without failures or skips. Neo4j Decimal query integration passes23 focused native cases/full529 units; full416 native acceptance passes without failures or skips. Decimal HTTP/compiler/wire and live120SDK/120HTTP demo acceptance now pass.
See [contract](CQL_DECIMAL_AGGREGATE_CONTRACT.md). UInt64 native/HTTP/demo gates
are complete, while this broader scalar requirement stays unchecked.


- [ ] Complete the provider scalar/operator matrix, including decimal/double/UInt64 where exact persisted domains support the promised operations. Fix storage projections at their owner; no lossy casts or false capabilities.
- [ ] Expose certified new types/shapes through an explicit transport contract; preserve the published v1 surface.
- [ ] Integrate authoritative shared-recipient and tenant policy. Metadata indexes, client tenant IDs and elevated-role labels cannot alone grant access.
- [ ] Apply policy equally to children, joins, aggregates, federation and output; test cross-actor/tenant denial on each store.

Policy audit, 25 October: the current query owner/public rule is authoritative
only for that declared rule. Core `Managers/SocialManager.ShareHolonAsync` shares
to social providers and records statistics; it is not a shared-recipient ACL.
No tenant/recipient authority was found in the inspected Core Interfaces/Managers
`TenantId`/`SharedWith`/`SharedTo`/`SharedBy`/`SharedHolon`/`ShareHolon` search beyond
that social entry point. This bounded search does not prove absence elsewhere.
A future shared/tenant contract must define an authenticated authority, grant
revocation/version consistency and native source/traversal predicates before
capability enrollment. Metadata or client labels must not supply those grants.


### Existing WEB4 sharing authority audit — 25 October

The broader ONODE search found `Helpers/HolonAccess.cs`, `Controllers/ShareController.cs`
and `WebAPI.UnitTests/HolonAccessTests.cs`. This extends the earlier bounded Core
search; it does not establish native CQL sharing support.

- `HolonAccess.CanRead` currently admits creator/public access and recipients in
  `SHARED_AVATAR_IDS`. `SHARED_WITH_<avatar>` flags support lookup; the shared-with-me
  endpoint rechecks the recipient list, so an index flag alone is not a grant.
- `ShareAsync` checks creator or Wizard, unions recipients, then loads/saves through
  HolonManager. Before reuse, verify the actual conditional-save path and binding
  of issuer authority to the committed grant; no explicit expected revision or
  revocation contract appears in this helper's public signature.
- The single-recipient controller action can return test-data success after a
  failed/unavailable real share when `UseTestDataWhenLiveDataNotAvailable` is enabled.
  That response cannot prove a persisted authorization grant. CQL enrollment must
  use real mutation outcomes and reject unknown/failed grant persistence.
- The stored-list parser accepts older text representations and falls from malformed
  JSON into comma parsing. Strict native policy enrollment needs a canonical,
  bounded grant representation and explicit migration, rather than granting from
  permissively parsed metadata or an index flag.
- `RecordShares` writes current recipient flags but does not remove old flags.
  Existing list rechecks remain necessary. No revocation action was found in the
  inspected ShareController; implement and verify revocation/version consistency
  before declaring the full sharing requirement complete.

Next policy work must reconcile this existing API with one generic Core authority
and native source/traversal checks on all three providers. Audit metadata mutation
permissions before treating stored lists as authoritative; this inspection does
not establish which other writes are owner-restricted. Preserve current CQL
owner/public behavior until the stronger contract and migration are verified.
Tenant membership authority remains a separate unimplemented requirement. No
sharing production code was changed during the running empty-window regression.

### UInt64 arithmetic foundation — 25 October

Core capture/normalization and immutable per-operation input restrictions pass
both frameworks and **3,006 tests** (36new). Existing provider numeric declarations
remain signed-only; public profile/readmission controls pass SQLite72/Mongo539/
Neo520 units. No native unsigned arithmetic or HTTP enrollment is claimed.
Next implement scalar/grouped exact unsigned limbs and average kernels on each
store, certify boundary/null/overflow/access/window/interruption behavior, then
expose the verified domains through annotated transport/demo examples. The scalar
coverage checkbox remains open. [Contract](CQL_NUMERIC_AGGREGATES.md#uint64-core-foundation--25-october-2026).

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


## SQLite native grouped filters — 24 October2026

SQLite now enrolls grouped Where for exact selected keys, Count/LongCount, Min/Max and Int32/Int64 Sum (including nullable domains). Supported clauses include comparisons, selected-field comparisons, AND/OR/NOT, null/Boolean tests, bounded membership and ordinal Contains/StartsWith/EndsWith. Integral mixed Decimal/finite floating-point literals use exact floor/ceiling boundaries and parameters; Decimal/double values never become approximate SQLite numeric parameters. Exact normalized Average filtering remains unsupported and fails at planning.

Native predicates compute filter membership after reduction, then a cumulative matching count applies result Skip/Take. The complete ordered group buffer remains proof, so nonmatches consume group/payload budgets and cannot hide malformed values or final overflow. Core independently checks marker agreement. Sum filtering, sum ordering and integral field comparisons share normalized floor-limb arithmetic, preserving adjacent Int64 totals and negative borrowing without recombining an overflowing total. Source Int32 mixed comparisons share the same boundary owner; their prior behavior remains covered by the full suite.

Verification: **1,401 full SQLite integration tests passed, zero skips**, including17new controls; the first15focused cases also pass. New controls cover multi-aggregate selection without holon hydration, filter-before-page matching ranks, Sum/Count field comparisons, nullable negation and fractional inequality, adjacent values beyond2^53, both Int64 limits, negative borrowing, subnormal/fractional numeric boundaries, numeric/string membership, literal wildcard/quote text, empty suffix versus null, Boolean/GUID/enum/UTC-tick keys, excluded malformed/overflow groups, full group budgets and explicit Average denial.

Evidence: TestResults/SQLiteCurrent/group-filter-{selected,full}-current.trx and group-filter-artifacts.json; TestResults/CosmicCqlDemo/sqlite-group-filter-{build,selected,full}.log. Actual93674terminal0 focused15 and59762terminal0 full1401. Executed producer/consumer pairs match Core318DD259BD399963568FCE25A9CC0633D96F51EA19BA11E2C218417BA4802D73 and SQLite2A41618E5AB15C5AAC11D533336082959E72BF5798359D107A7223332FCE6B26 (exact paths/measured bytes in artifact report). Fixed fixture DB/WAL/SHM verified absent after teardown. No service/download/install/restore/environment was added.

MongoDB/Neo4j native filters, HTTP filters/demos and exact Average semantics remain open. This certifies SQLite's enrolled grouped filters, not full CQL/ORM completion.


## MongoDB native grouped filters — 24 October2026

MongoDB now enrolls grouped Where over selected keys, Count/LongCount, Min/Max and exact integer Sum, including nullable domains. Clauses support comparisons, selected-field comparisons, AND/OR/NOT, null/Boolean tests, bounded membership and ordinal Contains/StartsWith/EndsWith. Integer sums use the same exact Decimal128 reduction expression as grouped ordering; bounded totals fit its precision. Decimal literals retain exact Decimal128 values. Native matching ranks count only accepted groups for paging, while complete ordered proof remains subject to domain/work/group/payload checks. No $match discards reduced proof. Average filtering remains explicitly unsupported pending normalized Decimal semantics.

The first full native run found one GUID mismatch: normalized grouping keys use32hex characters while ordinary GUID literals contain hyphens. Grouped filter literals now use the existing ComparableGuid owner, covering equality and membership consistently. No parallel encoding rule or fallback was added.

Verification: **385 Mongo unit tests and183 full native CQL tests passed, zero skips**, including15new native cases and3new unit controls. Coverage includes complete proof, aggregate combinations, matching-rank pages, nullable negation/fractional inequality, exact adjacent Int64 values and both limits, negative borrowing, binary64 subnormals, numeric/string membership, literal wildcard/quote text, null/empty suffix semantics, Boolean/GUID/enum/UTC-tick key equality+membership, excluded malformed/overflow groups, full group limits and explicit Average denial.

Evidence: TestResults/MongoCurrent/group-filter-unit-final-current.trx and group-filter-artifacts.json; TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx; TestResults/CosmicCqlDemo/mongo-group-filter-{unit-final,native-final}.log. Initial82274terminal1:182pass/1Guidred; failure evidence retained in MongoCurrent/group-filter-native-red-current.trx and mongo-group-filter-native.log. Final20476terminal0 after normalization fix and expanded key-membership assertions. Producer/native copies match Core318DD259BD399963568FCE25A9CC0633D96F51EA19BA11E2C218417BA4802D73 and MongoDD4FB0C9AA25AF7481659D33D8A1F295750D52BD31E7147291686555236AD5E0; exact paths/measured bytes in artifact report. Fixtures acknowledged cleanup; runner-owned Mongo stopped and27029 listener verified absent. Runtime/data reused, no new environment/download/restore.

SQLite and MongoDB native grouped filters are verified. Neo4j filters, HTTP filters/demos and exact Average semantics remain open; full CQL/ORM completion is not claimed.


## Neo4j native grouped filters — 24 October2026

Neo4j now enrolls grouped Where for supported selected keys, Count/LongCount, Min/Max and exact integer Sum. Clauses support comparisons, selected-field comparisons, AND/OR/NOT, null/Boolean tests, bounded membership and ordinal Contains/StartsWith/EndsWith. Sum filtering and ordering share normalized floor-limb comparisons without recombining overflowing totals or converting to Float. Decimal/finite floating literals become exact integral boundaries before driver binding. Average filtering remains unsupported; existing Neo4j DateTime domain gaps remain explicit rather than newly advertised.

Native predicates mark groups after reduction. Unpaged filters stream the complete bounded proof. Paged filters collect only the ordered group-budget sentinel and derive each packet's rank from preceding matches; nonmatching groups remain mandatory proof. This bounded prefix-rank implementation uses at most the declared native group buffer (default/max enrollment1,000); native read deadlines/cancellation and shared all-buffer validation remain in force. Core independently checks filter/page membership and validates nonmatches for domains, numeric overflow, source work, group/payload budgets and ordering before DTO construction. Protected failures remain ahead of windows.

Verification: **352 Neo4j unit tests and164 full native CQL tests passed, zero skips**, including14new native filter cases and3unit controls. Cases cover aggregate combinations, full proof and matching pages, nullable negation/fractional inequality, exact adjacent Int64 and both limits, negative borrowing, binary64 subnormal boundaries, numeric/string membership, literal wildcard/quote text, null/empty suffix, Boolean/GUID/enum key equality+membership, excluded malformed/overflow groups, group budgets and explicit Average/DateTime admission denial. Existing access/traversal/deadline/order cases remain green.

Evidence: TestResults/Neo4jCurrent/group-filter-unit-current.trx, neo4j-cql-native-current.trx and group-filter-artifacts.json; TestResults/CosmicCqlDemo/neo-group-filter-{unit,native}.log. Actual84093terminal0. Fresh producer/native copies match Core4C58BC9D8A6CD0D90F2526E0549B60671E34B68C04909D11B3755D7BB67B36BD and NeoD93C89D04396A6AD22937F4B52CC01935A33A7D3259B836475513976D20C18F7; SharedA142C8A4 verified by runner. This Core60285b30 cohort includes the coordinated account-version fix and differs from earlier native evidence. Owned fixture teardown acknowledged, cql-graph-fixture.json absent and exact retained container/image exited;17687 listener absent. Inclusive runner counts advance to164/227/229/228; only164fullCQL mode ran here, not broader modes.

Grouped filtering is verified on SQLite, MongoDB and Neo4j for their explicitly enrolled domains. HTTP result filters/demos, normalized Average semantics and broader unchecked CQL/ORM gates remain open.

HTTP grouped-filter implementation checkpoint, 24 October 2026: bounded exact selected-alias predicates now bind through the shared Core Where owner before result ordering/paging. Int64/Decimal wire literals preserve precision through strings; unrepresentable Decimal literals are rejected. Initial full Core regression: 2,386 passed, zero skipped. Added all-provider controlled endpoint cases await the shared build window; native demo example18 and fresh live acceptance remain next. Original completion checklist remains open.

HTTP result-filter controlled checkpoint verified: 2,389 full Core units, zero failures/skips (session92762). Next: spaced/commented SDK/HTTP/browser demo example18 and fresh all-provider live acceptance; then remaining Average/scalar/roadmap requirements.


Grouped-filter live checkpoint —24 October2026: actual74024terminal0, native demo1PASS0skip plus replica-init1PASS;54 SDK queries/54 HTTP examples across MongoDB/SQLite/Neo4j,234 settled operations/229 provider attempts. Example18 verifies exact Int64 selected-Sum filtering before matching-group paging; browser syntax and generated Swagger filter example pass. Producer/native Core4C58BC9D, Shared8F33C322, Mongo9DC9681C, SQLiteEA7573D5, Neo609661BC match; exact paths/bytes/SHA256 retained TestResults/CosmicCqlDemo/group-filter-demo-artifacts.json. Fresh report/transcript/log: v1-demo-result.json, v1-walkthrough.txt, group-filter-demo-native.log. This is scoped demo acceptance, not a new full native regression or full roadmap certification. Remaining Average normalization/order/filter, scalar and broader roadmap gates remain open.


SQLite normalized Average checkpoint —24October2026: actual90537terminal0,1,409 full native integration tests PASS0skip. Eight new cases cover all4integer input declarations, nullable membership and Average-field comparisons, negative/full-range values, overflow-sized numerators, exact repeating fractions, matching pages and2,048-input even/odd halfway rounding. Static negative controls retain separate capability permission checks; mixed underlying CLR fields remain rejected by shared capture. TestResults/SQLiteCurrent/average-final-current.trx and average-artifacts.json, plus CosmicCqlDemo/sqlite-average-final-{build,consumer-build,native-tests}.log retain final evidence. Exact Core4C58BC9D and SQLiteB4163A9E producer/consumer pairs match. Earlier16532test-intentred is retained separately. No new full remote-provider or HTTP Average gate is claimed; MongoDB/Neo4j remain unenrolled.

## MongoDB exact Average checkpoint — 24 October 2026

Actual test session62749 terminated successfully: **191 native CQL tests and427 provider unit tests passed, zero failures/skips**. Eight new native cases include524 packet-level comparisons against Core, count and Int64 limits, both signs, repeating fractions, halfway-even/odd rounding, coefficient-overflow rounding, all four integer input declarations, nullable membership/field comparisons, exact ordering and matching-group pages. Native normalization does not use server JavaScript or a binary64 intermediate.

Evidence: `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`, `TestResults/MongoCurrent/average-final-unit-current.trx` and `average-artifacts.json`; logs under `TestResults/CosmicCqlDemo/mongo-average-{native,final-unit}.log`. Actual producer/native/unit copies match Core SHA256 `4C58BC9D...`, Shared `D6AFCA32...` and Mongo `CB31EA51...`; the artifact report contains exact paths, sizes and full hashes. The runner stopped owned27029 and acknowledged fixture teardown; retained runtime/data remain reusable. This certifies native Average comparisons for MongoDB. New live HTTP Average examples and Neo4j support remain outstanding; it is not full CQL/ORM completion.

## Neo4j exact Average checkpoint — 24 October 2026

Actual session68272 terminated successfully: **172 full native CQL tests and352 provider unit tests passed, zero failures/skips**. Eight added native cases include524 packet-key comparisons with Core, positive/negative repeating fractions, Int64/count limits, halfway-even/odd rounding, coefficient-overflow rounding, all four integer inputs, nullable membership/field comparisons and matching-group pages. Production Cypher uses only bounded Int64 limb arithmetic; it requires no extension or floating-point conversion.

Core, Shared, MongoDB, SQLite and Neo4j C# source hashes remained unchanged throughout the gate. Actual producer/native/unit copies match Core `ABD06B39...`, Shared `7A2DDFA5...` and Neo4j `C0C15771...`. Exact paths, sizes and hashes are retained in `TestResults/Neo4jCurrent/average-artifacts.json` and `average-source-hashes.json`. Native evidence: `neo4j-cql-native-current.trx`, `average-final-unit-current.trx` and `TestResults/CosmicCqlDemo/neo-average-{native,final-unit-tests}.log`.

SQLite's shared-key migration also passed eight focused Average cases (session12470); its tested consumer hashes are recorded separately in `TestResults/SQLiteCurrent/decimal-key-average-artifacts.json`. That focused gate and the earlier1,409-case SQLite regression are distinct cohorts. Exact Average result filtering/ordering is now native-verified on all three providers. A new live HTTP/SDK Average demo and the broader roadmap remain outstanding. No full CQL/ORM completion is claimed.

## Live grouped Average demo — 24 October 2026

Actual session70730 terminated successfully: **236 Neo4j/WEB4 native regression tests,57 SDK queries and57 HTTP examples passed, zero failures/skips**. The signed three-provider run recorded237 settled operations/232 provider attempts. Example19 compares the exact published Decimal mean with a two-bound range, orders it and takes a result page. Expected stock7/items2/mean`-9007199254740990.5` and Decimal schema tagging pass on MongoDB, SQLite and Neo4j. The browser preset has the same request grammar; script syntax and generated Swagger examples pass. This gate executes the annotated SDK/HTTP code and validates Swagger; it does not claim browser interaction was automated.

Core/Shared/provider/CQL wire/compiler/demo C# hashes remained unchanged during the run. Actual producer/native host DLL pairs match Core`ABD06B39...`, Shared`E78803F0...`, MongoDB`C022AAB5...`, SQLite`BDBC9D35...` and Neo4j`E1E4AE41...`. Exact paths, sizes and hashes: `TestResults/CosmicCqlDemo/group-average-demo-artifacts.json` and `group-average-demo-source-hashes.json`. Fresh result/transcript/log: `v1-demo-result.json`, `v1-walkthrough.txt`, `group-average-demo-native.log`; native report: `TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx`.

Readiness/graph markers are absent; owned27029/17687/5088 listeners are absent and the retained Neo4j image is stopped. The236-case gate is Neo4j/WEB4 plus joint live acceptance, not a rerun of the entire MongoDB191-case or SQLite1409-case native suites. Those earlier cohorts retain their recorded scope. Wider scalar domains, Neo4j DateTime, joins/composition/federation, authoritative policy and remaining roadmap certification remain open.

Development batch publication —24October2026: verified code/demo batch merged and pushed to parentDevelopment0a758f368 and privateCoreDevelopment574af92. Parent merge-tree3a53875037a0534ef1b4bc030b53cd20a151559c is identical to the tested d738aba36 source tree; this reconciled14diverged ancestry commits without source changes or branch switches. Remote task/Development refs verified for both repos; manifest/gitlink validation passes. Other session's unfinished Dynamo working files remain excluded. Evidence: TestResults/CosmicCurrent/development-batch-current.json. This records publication toDevelopment, not completion of the remaining roadmap or verification of Railway deployment. Subsequent documentation-only records stay on the taskbranch until the next batched integration.

## Neo4j UTC DateTime checkpoint — 24 October 2026

Actual session41795 terminated successfully: **192 full native CQL tests and354 provider unit tests passed, zero failures/skips**. Twenty new native cases cover range/membership/order/projection with adjacent100ns ticks, equivalent fractional-width grouping, nullable extrema, the complete DateTime calendar range and leap days, field comparisons, optional leaves, fourteen invalid-type/calendar/text cases, and grouped Take0 that cannot conceal invalid data. Two older DateTime-denial controls were retargeted to still-unenrolled Decimal, retaining the actual-provider admission invariant; their original red report is preserved.

Core/Shared/provider/native-test C# source hashes remained unchanged during the final gate. Actual producer/native/unit copies match Core`ABD06B39...`, Shared`944F93E9...` and Neo4j`2E1D6787...`. Exact paths, sizes and hashes: `TestResults/Neo4jCurrent/date-artifacts.json` and `date-final-source-hashes.json`. Reports: `neo4j-cql-native-current.trx`, `date-final-unit-current.trx`; logs: `TestResults/CosmicCqlDemo/neo-date-final-{native,unit}.log`. Initial86776 red190/2 is retained in `date-native-red-current.trx` and `neo-date-native.log`.

The owned graph marker and17687 listener are absent; the exact retained Neo4j container is stopped. This is native SDK DateTime support, not a new HTTP date-predicate/demo certification. The existing HTTP DateTime selection shape can use the new provider capability; source/selected-result date predicate parsing and examples remain next. Broader scalar, policy and roadmap requirements remain open. See [the UTC contract](CQL_UTC_DATETIME_CONTRACT.md).


## UTC wire/demo acceptance — 24 October 2026

Actual45734 terminated successfully: **256 Neo4j/WEB4 native regression cases,
63 SDK queries and63 signed HTTP examples passed, zero failures/skips**. The
joint MongoDB/SQLite/Neo4j walkthrough recorded243 settled operations and238
provider attempts. The separate full Core/linked-production cohort passes2,424
cases, including32 new UTC wire controls. These are distinct scopes; this does
not rerun the entire MongoDB191-case or SQLite1409-case suite.

Examples20–21 cover exact UTC source ranges/projection and selected grouped-date
filters/order/page, preserving neighboring100ns values and UTC result tags.
Malformed/timezone/precision wire values fail compilation. The older hosted
DateTime-denial assertion now checks still-unenrolled Decimal. Its original
253/256 failure is preserved; exact ownership and122 settled operations were
verified before the retained mixed-size fixture was removed and this gate rerun.

Evidence: TestResults/CosmicCurrent/utc-wire-final-current.trx;
TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx;
TestResults/CosmicCqlDemo/utc-demo-final-native.log, v1-demo-result.json,
v1-walkthrough.txt and v1-binary-identities.json. Producer/native-host SHA256 pairs
were captured inside the global lease: Core BC24B11D, Shared83968E9A,
MongoB7A03FAA, SQLite267F7C88, Neo51EBF916. All112 checked local documentation
links resolve. No installs/downloads/restores or new environments were needed.

Fixture/readiness markers and owned27029/17687/5088 listeners are absent; retained
Neo4j is stopped. Free C:69,475,381,248 bytes after teardown. Broader scalar types,
joins/composition/federation, authoritative sharing/tenant policy, strict blocked
I/O interruption and final coherent certification remain open. Full CQL/ORM
completion is not claimed; the active goal continues.


## Exact UInt64 Core/SQLite acceptance — 24 October 2026

Core9d5efa9 adds UInt64/nullable capture and grouped keys, with exact Decimal
literal packets and lossless conversion bounds. SQLite reads original JSON
integer tokens rather than rounded REAL values and uses20-digit ordinal keys.
No storage migration or reindex is required. Row predicates/membership/field
comparison/order, detached projections, Min/Max and grouped keys/result filters
share the existing generic planners, read owner and detached proof validation.
Mixed fractional/out-of-range literals reject explicitly; Sum/Average retain their
Int32/Int64 enrollment. Other providers/HTTP do not inherit UInt64 permission.

Actual52891 terminated0: **all1,435 SQLite integration tests pass, zero skips**,
including22 new UInt64 cases and four independently repaired legacy-search cases.
The separate full Core cohort passes**2,428 cases**, with four new capture controls.
Initial11 native cases passed; eight broader red cases were four test-expression
mistakes and four peer search baselines, all resolved before the final unfiltered
run. This is not a rerun of MongoDB/Neo4j suites or the63/63 HTTP demo.

Core/SQL/test source hashes stayed unchanged during the final gate. Producer/native
copies match Core35AE105E, Shared50CABB2E and SQLite014AAE08; the full Core unit
consumer matches the same Core producer. Exact identities/source hashes:
TestResults/SQLiteCurrent/uint64-artifacts.json and uint64-final-source-hashes.json.
Reports: TestResults/CosmicCurrent/uint64-full-current.trx and
TestResults/SQLiteCurrent/uint64-final-current.trx. Log:
TestResults/CosmicCqlDemo/uint64-final.log. The exact owned SQLite DB/WAL/SHM are
absent after teardown; no downloads/restores/new environments were needed.

Next: MongoDB and Neo4j UInt64 persistence/query domains, transport/examples,
then remaining Decimal/double coverage and the broader unchanged roadmap.
See [the UInt64 contract](CQL_UINT64_CONTRACT.md). The full goal remains active.


## Three-provider UInt64/native and wire checkpoint — 24 October2026

Actual99510 terminated0: fullCore/linked-production**2,474 tests**, Neo4j**205
native CQL tests and453 provider units**, zero failures/skips. Twenty wire controls,
13 native Neo cases and five Neo unit cases are new. The native gate covers exact
UInt64 range/membership/order/projection, nullable grouped keys/Min/Max, field
comparison and10 invalid-domain cases. Both existing schema3 integer values and
exact raw wider-number text normalize to20-digit ordinal keys. No storage rewrite,
reindex, toFloat or signed-integer conversion is introduced for these keys.

Source/wire hashes stayed unchanged. Inside-lease producer/native/unit identities
match Core9EA092FC, Shared2FD86EA9 and Neo86F7CE9C. Exact evidence:
TestResults/Neo4jCurrent/uint64-artifacts.json, uint64-source-hashes.json,
neo4j-cql-native-current.trx, uint64-unit-current.trx;
TestResults/CosmicCurrent/uint64-wire-full-current.trx;
TestResults/CosmicCqlDemo/uint64-wire-neo-final.log.

Mongo acceptance11416 also passed205 native/478 units; SQLite's prior full1,435
cohort remains scoped to its earlier binary. These are separate verified cohorts,
not a simultaneous full regression of all three stores. The latest live demo remains
63SDK/63HTTP UTC examples; new UInt64 live transport/examples still require their
own gate. The additive UInt64 enum, selected/nullable binding, grouped alias
predicates and central exact-string wire formatting pass the20 new Core controls.

Core/Shared receipt repair was necessary for the current native seed path and is
owned by the parallel provider session. Its committed dependency pin/publication
must be reconciled before a Development deployment batch. This checkpoint remains
on the task branch. Original joins, federation, broader Decimal/floating metadata,
authoritative policy, interruption and final certification requirements stay open.
See [the matrix](CQL_SCALAR_CAPABILITY_MATRIX.md) and [UInt64 contract](CQL_UINT64_CONTRACT.md).


UInt64 live checkpoint24October2026: actual69794 terminal0;69SDK/69HTTP examples
across all three providers,249 settled operations/244 attempts, zero failures/skips.
Examples 22–23, browser presets and generated Swagger named examples now match the
published unsigned wire contract. Dedicated joint acceptance only; broader native
cohorts retain their recorded scope. Owned fixture/services cleaned up; dependency
and Development publication remain coordinated with the provider session.
[Exact evidence](CQL_UINT64_CONTRACT.md#live-uint64-examples2223--24-october2026).
Next: remaining scalar domains and unchanged joins/federation/policy/certification.

## Double wire checkpoint — 24 October 2026

Source, selected/nullable and grouped alias Double contracts now pass35 new
compiler/endpoint controls inside full2,542 linked-unit tests,0fail/skip.
Canonical finite round-trip request/response strings preserve binary64 values;
old enum identities are unchanged. Native unknown-type controls now use
NotAScalar. Live three-provider Double demos, Single and all unchecked broader
acceptance gates remain open. See floating contract for evidence.

## Double live demo gate verified — 24 October 2026

Complete Double grouped-order capture now has2 regressions inside full2,552
linked units (including peer8karma controls). Exact reviewed failed fixture
reconciliation passed207settled/201attempts without replay. Fresh81SDK/81HTTP
acceptance passes on MongoDB/SQLite/Neo4j with261settled/256attempts,0fail/skip.
All5producer/consumer pairs match and sources unchanged inside lease. Four
offline browser presets/Swagger examples and standalone cached demo build pass;
fixtures/readiness/owned listeners absent after teardown. Single and all broader
unchecked acceptance remain open. This is not a full combined provider rerun.

## SQLite Single gate verified — 24 October 2026

Core primitive/nullable type registry now has an independent128-domain bound,
preserving32selected columns; two tests lock the distinction. Nullable Single
group keys are captured generically without enrolling other providers.
Shared native floating normalization parses raw tokens directly to binary32.
BothCoreTFMs/full2,555linkedunits and focused28/full1,513SQLite pass0skip;
source snapshots unchanged and producer/native/Core-unit copies match.
Fixed SQLite DB/WAL/SHM absent after teardown; no installs/restores needed.
Core efad088 is published with paired parent gitlink/manifest.
Mongo/Neo Single, Single HTTP/demos and all unchecked broader gates remain open.

## Mongo Single binary64 component — 24 October 2026

The native power-of-two/ties-even component now passes12,313vectors against CLR
binary32 conversion inside full255Mongo native CQL/493units,0skip.
Source snapshots unchanged; Core/Shared/Mongo producer copies match both native
and unit consumers. This does not enable Single queries: exactInt64/Decimal128
source normalization and complete native query acceptance remain required.
Neo Single, Single HTTP/demos and all broader unchecked gates remain open.

## Mongo Single integer component — 24 October 2026

Exact integer normalization passes 9,700 native vectors against an independent
BigInteger reference, including signed halfway neighborhoods. The shared
rounding refactor retains all 12,313 binary64 vectors. Full Mongo native 256 and
unit 493 suites pass with no failures/skips, unchanged sources and matching
producer/native/unit binaries. Original Decimal128 normalization and full Single
query acceptance remain open; no Single capability has been enabled.

## Single decimal conversion and promotion gates — 24 October 2026

Mongo original Decimal128-to-Single normalization passes 1,873 native vectors
without intermediate Double conversion. Gate 15900: full 257 Mongo native and
493 unit tests pass, no failures/skips, unchanged sources and matched binaries.
Single query enrollment still requires complete query-owner integration.

SQLite promoted Double thresholds now retain their original precision rather
than narrowing back to Single. Core captures supported lossless same-wire
membership promotions while preserving stored-domain and nullability guards.
The three new native regressions were red before the fix (41532). Gate 26125
finished successfully: both Core TFMs build, 2,558 Core tests, 31 focused Single
and 1,516 full SQLite tests pass with no failures/skips. Source snapshots are
unchanged and producer/consumer identities match. Neo Single, Single transport
and demos, and every broader unchecked acceptance requirement remain open.

## Mongo Single query gate — 24 October 2026

Single/nullable Single native query support now passes gate33648:281native
CQL tests and494provider units, zero failures/skips. Twenty-four new native
cases cover predicates/membership/field comparison, persisted Single boundaries,
invalid numeric/foreign domains, nullable grouping, order/paging/extrema and
original Int32/Int64/Decimal128/Double projection with promoted literals.
A detached packet test rejects nonfinite or nonexact widened Single results.

The translator dispatches on original BSON type, normalizes once in a native
lexical binding and reuses that value in predicates, ordering and projection.
Missing/null raw state remains separate from invalid numeric sentinels.
Selected decoding verifies exact widened binary32 rather than rounding packets.
The stored JSON numeric round trip may differ from exact widened Single; the
query owner normalizes it. Red59084 had five copied storage assertions that
incorrectly required Double identity; all were corrected to the established
Single round-trip invariant without weakening exact query/output assertions.

Evidence: TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx,
single-query-full-current.trx, single-query-identities-current.json and
TestResults/CosmicCqlDemo/mongo-single-query-corrected.log. Red evidence retained
as single-query-red-current.trx / mongo-single-query-full.log. CoreE5145A09,
Shared7BC991A8 andMongo7759FD3B match native and unit consumers; source snapshots
unchanged. Owned fixture cleanup passed and27029listener absent after teardown.
Neo Single, Single HTTP/demo integration and every unchecked broader roadmap
requirement remain open. This cohort does not certify joins or federation.

## Neo Single exact lexeme component — 25 October 2026

Gate48304 passes233native/457unit cases, no failures/skips. New native209vector
component compares original schema3 JSON numeric text against exact binary32
midpoints with31bounded native search iterations. Base10^8limb arithmetic keeps
25bitcoefficient products exact inInt64, avoiding decimal-to-Double double
rounding. Original significant digits/scientific order decide midpoint sides
and ties-to-even; only the final exact binary32 value widens toDouble.
Grammar/4096token bounds, nonzero underflow, overflow and finite checks reject
invalid values. Native vectors cover signed even/odd midpoint neighbors across
8exponentbands, finite extremes and malformed/oversized inputs.

Sourcesunchanged; CoreE5145A09/SharedB97E6093/Neo74C00065 producer copies match
native+unit consumers. Evidence: TestResults/Neo4jCurrent/
neo4j-cql-native-current.trx, single-kernel-full-current.trx,
single-kernel-identities-current.json and TestResults/CosmicCqlDemo/
neo-single-kernel-full.log. This verifies the conversion component only.
NeoSingle query admission, complete native query integration and Single
transport/demos remain open, as do all broader unchecked roadmap requirements.

## Neo Single query gate — 25 October 2026

Gate76459 passes249native/459unit cases, zero failures/skips. Sixteen new native
query cases exercise range/membership/order, nullable group keys/filtering,
Min/Max, field comparison, invalid data domains, original JSON number/integer
boundaries and promoted Double literal/membership precision. Two new detached
packet controls require exact widened finite binary32 and canonical zero.
Original209vector native midpoint component remains in the full suite.
Single/nullable Single metadata query capabilities are now enrolled in Neo4j.
Floating Sum/Average are not enrolled by this checkpoint.

Earlier46503 failed one obsolete native unsupported-Single control;7204 then
passed all249native but failed one obsolete unit unsupported-Single control.
Both negative controls now use unsupported ConsoleColor, retaining provider
admission checks. Production Single code needed no change between these gates.
Red reports remain separate from the final verified cohort.

Source snapshots unchanged; CoreE5145A09/Shared95DAA150/Neo6C2CD2FF producer
identities match native and unit copies. Fixed cql-graph-fixture.json absent,
container exited after teardown. Evidence: TestResults/Neo4jCurrent/
neo4j-cql-native-current.trx, single-query-full-current.trx,
single-query-identities-current.json and TestResults/CosmicCqlDemo/
neo-single-query-final.log. Single HTTP/compiler/demo changes are separate
unverified preparation; all broader unchecked roadmap acceptance remains open.

## Single wire compiler/endpoint gate — 25 October 2026

Gate1286 passes both CoreTFM builds and full2,595linked tests, zero failures/skips.
Thirty-seven new Single controls cover canonical string literals, overprecision/
nonfinite/underflow refusal, selected outputs, grouped/nullable aliases and
complete grouped result ordering/paging. Single is additive enum8; Double stays7.
Source snapshots unchanged and Core63D88559 producer matches rootunit consumer.
This cohort includes the parallel provider session's then-uncommitted Core/Shared
search privacy fix; its publication is owned separately by that session.

Request Single values must be canonical invariant finite binary32 round-trip
strings, for example "1.0000002". Selected output also uses strings with explicit
Single type. Canonicalzero is "0"; numeric JSON values, signedzero spelling,
noncanonical exponent spellings and overprecise inputs are refused. Existing
scalar enum identities remain stable. Evidence: TestResults/CosmicCurrent/
single-wire-full-current.trx andsingle-wire-identities-current.json;
TestResults/CosmicCqlDemo/single-wire-core-full.log. Live native Single HTTP/SDK
demos remain pending; compiler/endpoint units alone do not establish that gate.

## Single three-provider live demos - 25 October 2026

Gate 37983 passed **87 SDK and 87 signed HTTP examples** across MongoDB, SQLite
and Neo4j, with 267 settled operations and 262 provider attempts. Examples 28-29
query binary32 readings and group, filter and order selected Single aliases.
HTTP returns canonical strings such as `"1.0000002"` and `"1.0000004"`. All six
browser presets passed the offline check; Swagger FiniteSingle examples were
verified in the live host. The standalone demo project also builds without restore.

All five producer/consumer binary pairs matched and source snapshots stayed
unchanged inside the build lease. The fixture journal, SQLite/WAL/SHM and
readiness files were removed; owned listeners on 27029 and 17687 were absent.
Shared runtimes and data directories remain reusable.

Evidence in `TestResults/CosmicCqlDemo/`: `v1-demo-result.json`,
`v1-source-identities.json`, `v1-binary-identities.json`, `v1-walkthrough.txt`,
`single-live-corrected.log` and `single-standalone-build.log`.

The initial run failed because a client checker applied an older one-row Int64
assertion to a two-row Single projection. Both projection and grouped checker
branches were corrected. Reconciliation gate 81194 verified all 208 settled
operations, 203 attempts/receipts and 438 provider records before exact cleanup,
without replaying requests. Failure and reconciliation evidence are retained;
the prior 81/81 Double proof is preserved in `single-prior-double-*` files.

Single transport and live demos are verified. Joins, federation, expanded policy
and interruption certification, and every other unchecked roadmap requirement
remain open. This dedicated demo gate does not replace full provider regression
suites.

## Neo4j interruption regression checkpoint — 25 October 2026

The pinned source-driver/read-transport lifecycle batch passes full465provider
unit tests and259nativeCQLtests, zero skips. Native duration4m3s. All10cold/warm
paused-Bolt cases pass for rows, projections, scalar and grouped reads. Failed
creation returns pool capacity; failed close reports its error and retires the
transport. Deactivation retains resource ownership until both close tasks
acknowledge success. No result publishes on cancellation.

Exact evidence: native TRX SHA256
6CAAA45E0B7C362CD0BBC53BC2811ADAB56B2D01ABB98FB5FAFEA066FC1A46D6;
unit TRX SHA256
A02FB67264B525C91C8CC71ACC75D4664B6CC1E2104BF799E11201DF80556D06.
Both are under TestResults/Neo4jCurrent; compact build/run log is
TestResults/CosmicCqlDemo/neo-read-transport-full-regression.log.
Producer/native driver, Core and Shared hashes verified before execution.
Owned runtime stopped; graph fixture journal and listeners17687/17788 absent.
Free disk after gate64605769728bytes; the reusable Neo volume and generated
outputs remain retained/shared, size attribution unmeasured. No new download,
installation or environment during this gate. No shared data was deleted.

This is a verified scoped batch, not full CQL completion. Strict deadline latency,
DNS/TCP/TLS connection phases, broader non-CQL driver regressions, joins,
federation and the unchecked authorization/scalar/composition roadmap remain.
The full original COSMIC ORM goal remains active.

## Connection creation and native deadlines — 25 October 2026

Gate61225 completed successfully on the corrected shipping assembly identity:
6driverunits,18nativeinterruption/deadlinecases and465providerunits pass, zero
skips. Native18duration4m5s. Real30second deadlines cover cold/warm rows,
projections, scalar and grouped queries; caller cancellation remains covered.
DNS resolution/cache waits, closed-owner rejection and a held real TLS
ClientHello are covered by the driver tests. Shutdown is joined before reuse;
TCPShutdownBoth is preserved before stream/socket disposal.

NativeTRXSHA2562296A0EF80DA02FBA71C115700E77CE2B769CD98FE7A251A7F059069C9DA5E4B;
driverunitTRXSHA2569B5EDF30184E9B9DC609D7A813629D52385F99F2B142630583EF52A0424B3A3C;
providerunitTRXSHA2565E565A7A7A2CB4C325478DFF95E1C6F22ED790285A6F7D69E16FBCB2CF439087.
Currentfiles are in TestResults/Neo4jCurrent. Log:
TestResults/CosmicCqlDemo/neo-graceful-interruption-current.log.
Driverproducer/provider/nativeconsumer hash agrees:
C9A8D3524484B39671FD3CCAD97B7BB3AFEA566B06B93279C6F4C40B304857F3;
exactassemblyidentity is Neo4j.Driver4.3.2.2/publickeytokenb646bc66d277ac07.
Upstreamoriginalkeypreserved; CompatibilityPublicKey.snk/public-only288bytes
is independently hash-pinned. PATCH.json records9modifiedupstreamfiles.

OwnedNeo runtime stopped; listeners17687/17788/17789 and graphfixturejournal
absent. Freeafter65940660224bytes. ExistingSDK/cache/Neo volume and fixedbuild
outputs retained/reusable; sizesunmeasured. Newtestsource and compactevidence
retained; no download/install/newenvironment or unrelated cleanup. OwnerCOSMICORM.

The priorfull267run passed249normalcases but failed18relayteardowncases on the
pre-ShutdownBoth connector. This new18casegate supersedes those interruption
failures, but is NOT a freshfull267green cohort. Fullcurrent267/nativeWEB4/non-CQL
regressions remain pending, alongside joins/federation and the rest of the
unchecked CQL/ORM roadmap. No fullcompletionclaim; goalremainsactive.


25 October committed transport checkpoint: all 267 native Neo4j CQL tests pass
unskipped on867182ad8. This closes the earlier full-cohort recertification gap
for that transport patch; fresh hosted demos and non-CQL regressions remain
separate. [Exact evidence and remaining boundaries](CQL_DESCENDANT_CONTRACT.md).
The full roadmap, including joins/federation and the original ORM scope, remains active.

## Ordinary save ownership hardening — 25 October 2026

Published parent checkpoint `18a05b148` / Core `f0290ff` fixes prepared logical
child-role ownership and SQLite ordinary persistence/acknowledgement. Verified
Core2,604, Shared1,189, normal SQLite1,578 and fresh87SDK/87HTTP examples; six
producer/consumer pairs match and source remained unchanged. These are separate
cohorts. See [ownership evidence](SQLITE_ORDINARY_ROLE_OWNERSHIP.md).

Next MongoDB ordinary single/batch admission now has eight red/green controls;
full Mongo unit502 passes after switching only ordinary entry points to
`CreateForSave`. Conditional/replay plans retain their existing owners. Native
CQL regression is running with exact stored-BSON preservation checks for direct
sync/async single/batch partial-view saves. This is not yet a native-pass claim.
Mongo single-record role persistence and acknowledgement, Neo4j ordinary guards,
and every unchecked advanced query/roadmap requirement remain open.

### MongoDB ordinary admission verification completed

The native runner terminated successfully: full281 native CQL cases pass, zero
skips, 3m51s. Its four existing partial-view cases now exercise all direct
sync/async single/batch saves (16 probes), both legacy/no-manifest and manifested
records, child-type and access-filtered selections. Each probe records intent
before submission and proves the complete stored Holon BSON remains unchanged.
Existing manager root-only positive controls still pass. Full unit502 passes,
including eight new controls first reproduced red against the original code.
Only ordinary single/batch planning changed to CreateForSave; conditional/replay
plans remain intact. Single-record role persistence/acknowledgement and Neo4j
ordinary guard work remain open; this is admission certification only.

## MongoDB ordinary role persistence and acknowledgement — 25 October 2026

The single-record native writer now resolves the captured role manifest against
its exact observed BSON before serialization. That same observation is protected
by the existing whole-document replacement CAS. Root-only writes preserve stored
roles even when caller metadata is cleared; selected writes persist their captured
logical roles. No transaction requirement was added to standalone single writes.

Single and graph ordinary saves share post-commit caller publication. All identities
and parent edges publish before role acknowledgements; manager-prepared staged
objects remain manager-owned. Rollback, rejected and unknown writes do not enter
this publication path. Conditional and committed-replica plans retain their owners.

Eight new native controls first failed against the original implementation:
289 total /281 pass /8 semantic failures. After the fix, full MongoDB CQL289 and
unit502 pass, zero skips. The eight controls exercise sync/async single/batch saves,
root-only preservation, selected empty membership, stored manifests, returned
manifests and caller observation authority. This does not certify every custom-role
or failure/interruption permutation; extend that matrix alongside Neo4j work.

Original red evidence: TestResults/MongoCurrent/ordinary-role-persistence-red.trx.
Green native evidence: TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx.
Green unit evidence: TestResults/MongoCurrent/ordinary-role-persistence-green.trx.
Joins/federation and other unchecked roadmap requirements remain open.

## Neo4j ordinary child-selection admission — 25 October 2026

Ordinary single/batch saves now use the same CreateForSave ownership contract as
Core/Shared, SQLite and MongoDB. Known partial selections reject before session
creation, even after AcceptChanges. Root-only saves remain admitted. Capture-stage
failures return HYPERDRIVE_PAYLOAD_INVALID; conditional/replay plans retain their
existing owners and native transaction/unknown-outcome handling is unchanged.

Red no-I/O controls:8failed/4root-onlypass. Green full unit477 passes, zero skips.
Fresh full native CQL267 passes, zero skips,8m16s. Four native partial-view cases
now execute16sync/asyncsingle/batch direct-save probes, comparing all owned record
properties and HAS_CHILD edges before/after rejection. Existing conditional
root-only positive controls also pass. Producer/Core/Shared/driver copies match.

The first broad run retained an unwritten fixture after an invalid test seed;
exact hash-bound reconciliation proved zero native records/references/projections
before removing only its marker. Three fixtures now allow ordinary capture to
produce the same logical manifests instead of assigning untrusted JSON. Assertions
were preserved. Original red and reconciliation evidence remain in TestResults.

This closes this admission batch, not ordinary role acknowledgement or all CQL.
Eight separate new acknowledgement cases are prepared but not included in267.

### Final Neo4j acknowledgement verification

The candidate above is now verified by full native CQL275/275 (8m21), focused
roles8/8, strengthened native outcomes16/16 and refreshed units477/477, zero skips.
The final275 and outcomes/unit batches ran under explicit whole-runner global
producer/consumer leases. Core/Shared/pinned-driver copies matched producers.
Session66975 terminated0; fixture marker absent and reusable container exited.
The earlier pending275 statement is superseded by this result. Cohorts remain
separately recorded; this does not certify every custom-role/federation/join gate.

## Shared custom-role ordinary-save contract — 25 October 2026

One source-linked contract now verifies eight ordinary-save combinations on each
priority store: synchronous/asynchronous, single/batch, direct provider/V2 manager.
Two Children have deliberately descending identities, plus one typed Extras
member. Assertions verify exact logical role names and order on caller/returned
acknowledgements, native CQL typed round-trip, persisted values, clean accepted
query baseline and deep tracking after a typed child edit. This covers the generic
prepared-write bridge as well as direct native callers.

All24 new cases pass: MongoDB's full297 native CQL suite (3m58), Neo4j's expanded
16-case ordinary-role gate (8newcustom+8existing,8s) and SQLite's8 new cases (3s).
All have zero skips and execute under explicit global build leases. Neo4j's prior
full275 and SQLite's prior full1578 remain separate unchanged-production cohorts;
no full283Neo/1586SQLite claim is made. No production implementation changed in
this batch. Remaining access/grouping/joins/federation and wider ORM gates remain
open. Fixed reusable fixtures were retired through their known-outcome owners.

## Immutable join intent checkpoint — 25 October 2026

Core now captures same-provider `Join` and `LeftJoin` intent through the existing
projection/path compiler. Scalar and named composite keys have1–8 components;
DTO member initialization is aligned by alias, without invoking constructors,
getters, methods or a client evaluator. Exact scalar domains share one registry
with grouping; implicit numeric conversion does not acquire a join capability.

Both source query objects remain immutable and intact, including filters,
relationships, source ordering/windows, graph requirements and trusted access.
Provider scope sets must agree; mixed actors or an unbound side paired with an
actor-bound side reject. Either source can require the stronger native snapshot.
Independent left/right input rows/bytes, pair-work and output rows/bytes have
validated bounds. Null key components are defined not to match.

Both Core frameworks build; fullCore2,627 passes (23new intent controls), zero
skips. No provider I/O or new environment is involved. This is an intent/compiler
checkpoint only: join result selection, capabilities, physical planning/attempts,
native implementations, signed HTTP and demos remain unimplemented. No native
provider advertises join support, and row/projection execution cannot execute a
join intent. The unchecked join milestone remains open.

## Detached join selection capture — 25 October 2026

Join intent now supports `Select((left,right) => ...)` for scalar, anonymous and
flat DTO descriptions, reusing the shared detached-shape compiler. Columns retain
their source side, raw path/domain and output CLR type. Explicit `right != null`
and `right == null` selections capture row presence independently of nullable data.
User-overloaded comparisons do not become native presence tests.

Left-join right-side value types must represent absence. An identical scalar may
be lifted to Nullable<T>; numeric widening cannot be hidden inside that lift.
Reference/nullable fields remain distinct from missing-row presence. Computation,
entities, nested/collection output, duplicate aliases and over32columns reject.
No selected constructor, setter, getter or application method runs during capture.

FullCore2,645 passes (18new selection controls), zero skips; both frameworks build.
This is result-description capture only. Future join packets must carry validated
right-row presence/identity independently of selected nullable values, and all raw
buffers must validate before shared DTO construction. Join capability/planning,
physical attempt ownership, native matching on three stores, output validation,
HTTP and executable demos remain open. No native join support is advertised.

## Join capability admission and planning — 25 October 2026

Core now has independent join kind, exact key-path equality, output-path and work
limit permissions. Ordinary predicate/projection support cannot enroll a native
join by itself. Graph-source validation and one common snapshot for both sources
require separate explicit promises; independent source snapshots do not suffice.

The planner captures the authorized eligible target list once, then reuses the
existing source planner independently for both original queries. It retains both
windows, filters, access predicates and graph requests. It admits every selected
target before returning a plan. The existing multi-provider source-window
restriction remains until a distributed paging contract is implemented.

Native readmission checks one matching provider against its actual query and join
contracts, including both sources, rather than trusting planned claims. Exact
nullable metadata domains, key/output widths, all seven work/byte budgets and
Take(0) cannot bypass admission. No provider discovery or native I/O occurs.

Both Core frameworks build; fullCore2,672 passes (27new planning controls), zero
skips. Initial netstandard build exposed an Enum name collision; explicitly using
System.Enum resolved it before the verified run. No provider join enrollment,
physical execution/receipts, raw packet validation, matching, HTTP or executable
join demo is claimed. Those requirements and the overall join milestone remain open.

## Detached native join packet validation — 25 October 2026

Core now validates complete bounded left/right source proofs, independently
reported source/pair counts and native output pairs before constructing any DTO.
Source proofs retain logical identity, owner/public visibility, exact keys, raw
selected cells and ordering values. Missing/right-row presence is independent of
nullable data; published CosmicJoinResult<T>.HasRight is always available without
exposing unselected source identities.

Validation checks both input row/byte budgets, source windows, duplicate IDs,
canonical identity/access coherence, repeated paths, exact scalar domains and
source ordering. Native pair keys, counts, completeness and deterministic order
must agree; no omitted/duplicated pair or unmatched row can hide existing matches.
Core verifies key-group cardinality but never generates native join output rows.
Snapshot requests also reject contradictory observations of the same identity.
All input proof buffers validate even when no output pair can be returned.

Output cells must agree with source/presence proofs and retain signed zero and
decimal representation. Key equality remains logical numeric equality. The shared
detached-shape constructor runs only after full packet validation; cancellation
and any validation failure publish no partial result.

Both frameworks build; fullCore2,704 passes (32new result controls), zero skips.
All builds used the normal global lease after the peer's verified release. No
provider I/O or new environment was involved. This is packet validation and
construction only: multi-provider/global-budget execution ownership, durable
attempts/receipts, native implementations, HTTP and executable join demos remain
open. The future executor must validate every provider packet before any DTO.

## Generic join execution checkpoint — 25 October 2026

The generic V2 `HolonManager.ExecuteJoinQueryAsync` entry point now uses
`CosmicJoinExecutor` and the explicit `ICosmicQueryJoinStorageProvider` contract.
Every actual target is checked for source/join capabilities, concrete identity
and inherited provider permission before the first durable attempt. Both source
actors must match inherited admission. Final dispatch rechecks actual capabilities.

One durable physical attempt owns each provider's two-source native join. Seven
query-wide remaining budgets cover left/right rows, pair work, output rows and
left/right/output bytes; later providers receive the remaining allowance. Zero
allowance requires an explicit native probe/error outcome rather than truncation.
All provider packets validate before any selected DTO is constructed. Failures
and cancellation return no partial publication; Core never evaluates a join by
scanning entity results.

Both Core frameworks build and the full Core suite passes **2,731/2,731**, zero
skips. This adds 27 controls beyond the prior 2,704 checkpoint. The first candidate
exposed six existing SDK admission regressions: requiring query tariffs during
preflight incorrectly conflated provider permission with endpoint pricing.
Preflight now reuses the existing centralized prepared provider permission owner;
optional SDK tariffs remain compatible, while paid HTTP retains its tariff gate.
A later-denied-provider regression also proves no earlier read/attempt begins.

This checkpoint is controlled-adapter execution/accounting evidence. **No real
provider implements or advertises native joins yet.** SQLite, MongoDB and Neo4j
native matching/proof production, HTTP transport, executable join demos and
remaining composition/federation/security certification are still open.

## SQLite native join SQL compiler — 25 October 2026

SQLite now compiles each join source's protected predicates, deterministic
ordering and independent window into SQL, sharing the existing 900-parameter
budget. Native INNER/LEFT JOIN emits ordered source-position pairs. Null or
missing key components never match. Source and pair limits retain one detection
row so a future decoder can reject overflow rather than publish truncation.
Selected cells and ordering/key proofs reuse the existing exact projection-domain
guards; input proofs are separate from fanout pairs to avoid repeated large cells.

The provider builds and its complete **46-test unit suite passes**, including
seven new tests running generated joins against real in-memory SQLite with the
production UTF-8/progress/length budget owner. These establish matching, fanout,
null semantics, source-window order and zero/one remaining pair sentinels. This
is a separate compiler cohort, not a new full 1,578-test integration result.
The compiler remains internal and no join capability is advertised. The next
required work is native serializable transaction ownership, source/proof decoding,
all seven byte/work limits and graph/traversal checks before provider enrollment.
MongoDB/Neo4j implementation and join transport/demos remain open.

## Verified native SQLite flat joins — 25 October 2026

Final leased certification passes both Core targets and full **Core 2,734**,
normal **SQLite integration 1,615**, and **SQLite unit 46**, zero failures/skips.
The native integration suite includes **29 new join cases**: inner/left fanout,
all seven remaining budgets with fresh reuse, malformed/missing unmatched input,
both-source visibility, cancellation, twelve scalar key domains, composite keys,
missing/null ordering through independent source pages, and a caller-owned
snapshot retaining a matching pair after an acknowledged concurrent mutation.
The caller keeps transaction ownership; a fresh read observes the changed pair.
Three additional Core cases lock ordering-presence proof ownership/validation.

The original broad integration invocation also selected two historical
`Category=Reconciliation` tools whose exact old fixtures are absent. That run
reported 1,614 pass /2 failures; the corrected normal gate explicitly excludes
that category and passes 1,615/1,615 after the final extra snapshot case. Neither
historical tool is counted as a skipped acceptance case or a passing regression.
Evidence: `TestResults/CosmicCqlDemo/sqlite-join-certification.log`,
`TestResults/CoreCurrent/join-ordering-current.trx`, and
`TestResults/SQLiteCurrent/sqlite-join-{normal-full,unit}.trx`.

Core producer and native consumer DLL SHA256 both equal
`639CB2A585F74AD54282500A0751783F2D09C0AA1E018F009A09E0CBFA904E13`.
The fixed SQLite database/WAL/SHM and pending fixture markers are absent after
terminal teardown. No dependencies or new environment were installed/restored.
This publishes SQLite SDK flat joins only; the remaining join gates above remain
open. Existing MongoDB/Neo4j suites are separate earlier cohorts.

## MongoDB native join compiler — 25 October 2026

MongoDB now compiles bounded independent left/right proof pipelines and a native
`$lookup` matching pipeline. Both source commands require one shared snapshot.
Right-source filtering/order/window run before key matching. Normalized typed
comparison keys reuse existing GUID/UTC/scalar owners; missing/null components
cannot match. Inner and left matching retain their distinct unmatched behavior.
Source and pair count windows report bounded cardinality; detection rows preserve
zero/one remaining allowances without turning overflow into truncated success.
Deterministic native pair ordering follows both source orders and logical IDs.

Proof pipelines preserve the existing protected traversal error branch outside
source paging. Matching can unwrap only successful rows after the future native
owner has validated both complete proof streams in that same snapshot. Pair
pipelines own deep copies of source stages. Metadata names and data remain
literal BSON values; no script or client-side matching is generated.

Provider and unit consumer builds pass; the complete MongoDB unit suite passes
**510/510**, zero skips, including eight new compiler controls. Evidence:
`TestResults/CosmicCqlDemo/mongo-join-compiler-tests.log` and
`TestResults/CosmicMongoCurrent/join-compiler-unit.trx`. This is compiler evidence
only: **MongoDB does not yet implement or advertise native joins**. A single
snapshot/deadline/native-attempt owner, bounded typed proof decoding and positive/
negative native integration gates are next. Existing MongoDB native 297 remains
a separate earlier cohort. No Mongo service, database or new environment was used.

## Verified MongoDB SDK flat joins — 25 October 2026

MongoDB now executes flat inner/left joins through generic V2 HolonManager with
one snapshot/deadline owner and native matching. The final full native suite
passes **328/328**, zero skips, including **31 new join cases**; the full MongoDB
unit gate passes **510/510**. Existing Core **2,771**, SQLite **1,615 native/46 unit**
and Neo4j **275 native/483 unit** are separate earlier cohorts, not a new combined
all-provider gate.

Native join evidence covers fanout/unmatched presence, all seven remaining work/
byte budgets and reuse, required invalid/missing unmatched cells, both-source
visibility before matching/paging, pre-cancellation, twelve exact scalar key
domains, composite keys, missing/null ordering through independent source pages,
a real acknowledged concurrent write between source reads, blocked-read
cancellation and a cumulative deadline with fresh reuse. The snapshot control
retains the original match; a subsequent fresh read observes that it disappeared.

The first expanded run passed327/328: timeout classification was correct, but the
test assumed a third command must start. MongoDB legitimately timed out during
its second command. The final control observes actual wire `maxTimeMS` budgets
**30,000 then18,997**, requires strict decrease and retains timeout/no-publication
assertions. This proves the allowance is not renewed for each command without
assuming a server clock schedule. Original failure evidence is preserved as
`TestResults/SubscriptionUsageCurrent/mongo-join-deadline-original-red.trx`.

Final evidence: `TestResults/CosmicCqlDemo/mongo-join-native-final.log` and
`TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx` (4m29s).
Core producer/consumer SHA256 both639CB2A585F74AD54282500A0751783F2D09C0AA1E018F009A09E0CBFA904E13;
MongoDB producer/consumer both6C54E0FEF2EBAA4CBF149AB60D0BBB9CC5C9CD25265D3453532552AE26E75C99.
Owned MongoDB27029 is stopped after acknowledged fixture teardown; the pinned
runtime/data remain reusable. No download/install/restore or duplicate environment.

MongoDB and SQLite SDK flat joins are available. Neo4j joins, graph-source joins,
joined HTTP/browser demos, post-projection/window composition, federation and
remaining policy/certification gates stay open. Full CQL is not complete.

## Neo4j native join compiler checkpoint — 25 October 2026

Neo4j now compiles one bounded Cypher statement that captures each source's
protected filter/order/window and scalar proofs once, then matches those captured
maps natively. Input capture aggregates inside independent subqueries, so an empty
source cannot erase the other source's proofs. Inner/left matching excludes null
key components and preserves native source positions; pair overflow retains a
detection row. Complete source counts and bounded pair counts are explicit output
proofs. Source ownership/traversal error branches remain outside source pages.

Both translators share the same bound-parameter dictionary and existing900-entry
budget. Metadata names retain encoded bound paths; no parameter renaming/string
replacement or user-supplied Cypher is used. Native lower payload bounds suppress
oversized scalar buffers and gate matching; exact decoded UTF-8/work validation
belongs to the upcoming read owner. Matching never uses a client evaluator.

Provider builds and the full Neo4j unit suite pass **483/483**, zero skips, with
six new compiler controls. The existing retained Neo4j5.26.12 environment passes
**four scoped native compiler cases**: inner/left fanout and either source empty.
This is not a new full287 native regression claim. Earlier full275 and scoped
custom-role cohorts retain their own recorded scope.

The normal runner now offers exclusive `-CosmicCqlJoinCompiler`, using the same
build identity checks, retained environment and fixture teardown. Broader family
expected counts include these four new cases but are not claimed executed here.
Evidence: `TestResults/CosmicCqlDemo/neo-join-compiler-unit.log`,
`TestResults/Neo4jCurrent/join-compiler-unit.trx`,
`TestResults/CosmicCqlDemo/neo-join-compiler-native.log` and
`TestResults/Neo4jCurrent/neo4j-join-compiler-current.trx`.

**Neo4j does not yet implement or advertise the public native join interface.**
The execution/read owner, typed proof decoder, all seven limit/error controls,
policy/interruption/key-domain native matrix and SDK enrollment are next. Native
source capture retains Neo4j's read-committed contract; it does not grant a
repeatable snapshot. Graph joins, HTTP transport and the full roadmap remain open.

## Verified Neo4j SDK flat joins — 25 October 2026

Neo4j now executes flat inner/left joins through generic V2 HolonManager. The
final full native CQL suite passes **320/320**, zero skips (9m23s), including
**33 new SDK join cases** and the four previously verified compiler cases.
The complete unit suite passes **492/492**. Focused join certification passes
37/37. Core2,734, SQLite1,615native/46unit and MongoDB328native/510unit remain
separate earlier cohorts, not a new combined all-provider run.

Join controls verify fanout/unmatched presence, all seven remaining budgets with
fresh reuse, invalid/missing required data in unmatched sources, visibility on
both inputs before matching/pages, twelve exact key domains, composite keys,
missing/null ordering through pages, generic source types whose constructors
throw, and actual cold/warm Bolt interruption. Shipping30-second deadlines and
caller cancellation complete while real relay bytes remain held; no shortened
test timeout or alternate query path is used. A pure capability control verifies
that graph-source/shared-snapshot permissions remain disabled.

The one-statement native capture retains **read-committed** consistency. Both
inputs are captured once; native matching refers to those captured values and
positions. This does not create a repeatable snapshot. MongoDB and SQLite use
their own snapshot owners for flat joins. All three adapters use the same generic
join plan, remaining-limit, durable-attempt and Core preconstruction validation
contracts. No provider hydrates source entities to evaluate the join.

Final evidence: `TestResults/CosmicCqlDemo/neo-join-owner-full.log`,
`TestResults/Neo4jCurrent/neo4j-cql-native-current.trx` and
`TestResults/Neo4jCurrent/join-owner-unit.trx`.
Neo4j producer/native consumer SHA256 both
35E5E456AB0E4AFB40FF311F15499B4882A18D6FE792E3E93B45928B030F3223;
Core639CB2A5/Shared91714E81/pinned driverA6C87EF9 identities also match.
Retained container is stopped, owned fixture manifest absent and native/relay
ports are no longer listening. No install/download/restore or duplicate environment.

**SDK flat joins are now available on MongoDB, SQLite and Neo4j.** Joined HTTP
requests/browser demos, graph-source joins, post-selection/window composition,
distributed joins/federation and remaining policy/certification gates are still
open. This checkpoint does not close the full join milestone or CQL roadmap.

## Runtime join bindings and WEB4 request compilation — 25 October 2026

Core adds bounded `JoinColumns` / `LeftJoinColumns` key capture using existing
`CosmicProjectionSelection<T>` bindings. Expression and runtime keys share one
source/scope/actor/key-domain validation owner. Composite aliases/types must match;
key capture retains both original source objects/windows/access and freezes the
binding lists. No key DTO is constructed and no selector delegate is evaluated.

`CosmicJoinBinding.Left`, `.Right` and `.Presence` bind output aliases to one
captured join. Right value types on a left join require explicit `allowMissing`
lifting; the original source path stays required. A missing right row and nullable
data in a present row remain distinct through `HasRight`. `SelectColumns` produces
immutable `CosmicProjectionRecord` rows only after the existing complete-packet /
all-provider validation barrier. Foreign-owner, duplicate or excessive bindings
reject. The runtime path does not introduce an evaluator or provider fallback.

WEB4 adds a strict proposed join request model and a pure `CompileJoin` binding
owner. It reuses the existing typed selection/source compilers; both sources get
the host actor and provider. Client actor/per-source provider/executable expression
properties reject. Public bounds are1–8 keys,1–32 outputs,1–1000 rows per input,
1–10000 matched/output pairs and1–100 published results; all three payload limits
are fixed at1MiB. Source windows remain independent of these error limits.

Both Core frameworks build and the full Core/linked production compiler suite
passes **2,771/2,771**, zero skips:19 new runtime-record controls and18 new WEB4
compiler controls beyond2,734. Three controlled adapter identities exercise the
existing generic executor; this is not a fresh native three-provider transport
certification. Latest native SDK cohorts remain SQLite1615, Mongo328 and Neo320.

The request compiler is a transport prerequisite only. **No join HTTP action or
route is registered yet.** Endpoint admission, route/tariff classification, wire
serialization, signed native end-to-end verification and annotated HTTP/browser
examples remain required. The broader join/CQL roadmap stays open.

## Join HTTP endpoint implementation — 25 October 2026

`POST /api/data/query-cql/join` is implemented in the authorized production
controller. It reuses prepared admission, tariff/provider eligibility, actual
adapter capability planning and the generic V2 join execution/receipt owner.
The shared exact route registry now includes the join leaf. Results expose
bounded column descriptors, exact scalar wire values and independent `hasRight`
presence. Escaped JSON output is checked against the existing response byte cap.

Full Core/linked WEB4 tests pass **2,785/2,785**, zero skips. The first test run
found four mock packets missing the compiler's automatic ID ordering tie-break;
fixtures were corrected without weakening production validation. Original red
and final green TRX evidence are retained under `TestResults/CoreCurrent`.
The linked native HTTP host now includes the join model/compiler/endpoint and
builds with **zero errors**; existing warnings remain. Its browser authorization
route list includes `/join`. Evidence:
`TestResults/CosmicCqlDemo/join-http-host-build.log`.

**Still required:** signed native HTTP join acceptance on MongoDB, SQLite and
Neo4j, durable receipt/replay checks, annotated SDK/HTTP/browser/Swagger join
examples and final regression certification. This endpoint implementation is
not yet a claim of production transport certification or full CQL completion.

## Signed native join HTTP acceptance — 25 October 2026

The authorized join action passes the real-Kestrel signed-JWT acceptance flow
on MongoDB, SQLite and Neo4j: **1/1, zero skips**. Fifteen joins and fifteen
replay probes cover inner/left selection, unmatched presence/null values, exact
Int64 strings, owner/visitor visibility and hidden right inputs sharing matching
stock keys. Every join has one durable attempt and receipt; replay returns409
without another dispatch. The full flow reconciles **195 settled operations /
190 physical attempts**, including existing row/scalar/projection regressions.
This does not add joins to the interactive87-example demo or close the broader
join certification matrix.

Mongo's compiler now explicitly retains null right-ordering fields after an
unmatched lookup. Decoder shape validation remains strict. The original frozen
failure passes with the producer fix; exact ownership and durable settlement
checks precede teardown. A later test-only wire assertion was corrected to the
existing column-described exact string representation. Original red reports
are retained separately.

Evidence: `TestResults/CosmicCqlDemo/join-http-native-final.log`,
`TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx` and
`TestResults/Neo4jCurrent/cql-v1-web4-native-evidence.json`. Owned fixtures were
removed and ports27029/17687 verified absent after teardown. No new environment.
Annotated interactive join demos, composition, federation, extended scalar/
policy coverage and remaining final certification gates stay open.

Final Mongo regression after the ordering-proof fix passes **510/510 provider
units** and **328/328 native CQL tests**, zero skips. The native run takes4m29s;
no additional producer changes followed the signed three-provider HTTP gate.
Reports: `TestResults/SubscriptionUsageCurrent/join-null-order-unit.trx` and
`TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`.
Interactive join examples and the broader roadmap remain open.

## Annotated join demos verified — 25 October 2026

Examples30/31 now demonstrate left/inner joins in the shared C# walkthrough,
signed HTTP walkthrough, browser presets and generated Swagger. C# uses the
generic Core/V2 planner/executor on every provider; HTTP binds actor policy to
both independent sources. Comments explain absence, nullable right outputs,
exact Int64 values and native matching. Browser presets retain editable,
indented JSON and separate explanations.

Fresh dedicated native demo passes **93 SDK /93 HTTP**, **288 settled operations
/283 physical attempts**, joint test1/1 zero skips. Actual generated Swagger
join route/schema checks pass. Production browser JavaScript and six provider/
kind presets pass offline; that is request-generation evidence, not rendered
browser interaction. Standalone demo build passes with zero errors.

Run `Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1` for verification, or add `-Serve`
and open `http://127.0.0.1:5088/demo` (Swagger at `/swagger`). Use **Finish demo**
for verified settlement/cleanup. The console transcript is
`TestResults/CosmicCqlDemo/v1-walkthrough.txt`; summary `v1-demo-result.json`;
log `join-demo-native.log`. The earlier87/87 cohort remains historical.

Owned fixtures are removed and test ports stopped. Broader composition,
federation, expanded policy/scalar and final certification gates remain open.

## Selected-result composition capture — 25 October 2026

Core captures immutable `Where`, selected ordering/ThenBy and Skip/Take stages
without rewriting the original source window, access or graph. Grouping and
projection share one selected-column alias resolver. Runtime records keep exact
alias domains, scalar selections bind their single selected value, and capture
invokes no DTO or arbitrary application code. Stages/windows remain in order;
ThenBy extends only the immediately preceding sort. Stage count is bounded32.

Dual Core frameworks build; full Core/linked WEB4 passes **2,799/2,799**, zero
skips (14 new controls). Tests cover window/filter order, source retention,
frozen literals, exact runtime aliases, rejected code/conversions, stage/window
limits, zero windows, error propagation and no borrowed native admission.

**Native execution is still pending:** ordinary provider projection permissions
and ordinary raw buffers reject composed stages. No provider silently ignores
them or evaluates selected predicates in memory. Next is the native proof/work
contract and SQLite/MongoDB/Neo4j owners, then signed HTTP/demos. This is capture
progress, not completed composition. [Contract](CQL_COMPOSITION_CONTRACT.md).

The shared alias refactor also passes the fresh native three-provider developer
demo: **93 SDK /93 HTTP**, **288 settled /283 attempts**, joint1/1 zero skips.
This verifies existing operators against the new Core producer; it does not
certify native execution of the new composed stages. Evidence:
`TestResults/CosmicCqlDemo/projection-composition-demo-regression.log` and
`TestResults/CoreCurrent/projection-composition-current.trx`. Owned fixtures are
removed and test services stopped normally.

## Composition admission and complete proof — 25 October 2026

Core adds separate `CosmicProjectionCompositionCapabilities` and target/plan/
actual-readmission owners. Source permissions, projection columns, selected
filter operators/domains, ordering domains and paging stages are independently
required. Every selected provider preflights; actual provider identity and its
current capabilities must match again before native execution. Capabilities and
eligible-target snapshots are frozen. Ordinary projection permissions still
cannot authorize composed execution.

Complete composition packets carry all participating input cells/access/source
ordering, native identity membership for every result stage, final native rows
and explicit source/cumulative-stage counts. Core validates all input before a
later filter/page can hide malformed or private data. It checks every stage's
native membership/order, stable ties from the previous stage, exact final
source payloads and count agreement before constructing any DTO. The verifier
uses shared scalar predicate semantics; it rejects a wrong native result and
never publishes reference-computed replacement rows. Joined payload validation
shares exact cell identity, including signed zero and Decimal representation.

Input rows/bytes, cumulative stage-row participation, stage proof bytes and
published rows/bytes have independent limits. Cancellation is checked through
cell validation, stage evaluation, stable ordering and publication. The generic
multi-provider executor must retain all-buffer validation before construction.

Both Core frameworks build; full Core/linked WEB4 passes **2,841/2,841**, zero
skips:21 admission and21 proof controls beyond2,799. Tests cover independent
stage/operator/type permission, actual downgrade/foreign identity, source/column
permissions, snapshot isolation from caller mutation, full off-page validation,
filter/window order, stable sort ties, changed identities/payload, private data,
nullable/runtime outputs, signed zero/Decimal bits, independent budgets and
cancellation without DTO construction.

**Still open:** generic physical-attempt execution/remaining global budgets,
native SQLite/MongoDB/Neo4j subqueries and proof decoding, HTTP composition and
annotated demos. No native provider advertises this new execution shape yet.
Existing ordinary queries and join demos remain supported.

Final unchanged-operator regression on the new Core producer passes **93 SDK /
93 HTTP**, **288 settled /283 physical attempts**, dedicated joint1/1 zero
skips. This exercises existing grouped filters and join payloads through their
new shared admission/verification helpers. It does not claim native composed
subqueries execute. Evidence: `TestResults/CosmicCqlDemo/composition-proof-final.log`,
`TestResults/CoreCurrent/composition-proof-current.trx` and
`TestResults/CosmicCqlDemo/composition-proof-demo-regression.log`.

## Generic V2 composition execution and measured-query guard — 25 October 2026

`HolonManager.ExecuteProjectionCompositionQueryAsync` now uses one generic
executor and `ICosmicQueryProjectionCompositionStorageProvider` contract.
Actual provider/column/stage permissions preflight across every target before
first I/O and are rechecked immediately before native dispatch. All packets
validate before any DTO construction; native failure/cancellation returns no
partial result and never falls back to entity hydration or an in-memory query.

Six remaining budgets are shared across providers: input rows, input bytes,
cumulative stage-row participation, stage proof bytes, published rows and bytes.
A later provider receives the remainder, including zero. Zero accepts only a
complete empty proof or an explicit limit error; it never authorizes truncation.
Named runtime records reuse this execution/validation owner. Existing ordinary
projection execution cannot bypass the composition contract.

Controlled inherited admission verifies one durable query attempt/receipt per
provider, actual service pricing and actor binding. A new denial control found
that an ordinary SDK query under **measurement-required** admission could begin
without a query tariff. The shared prepared query owner now rejects that case
before attempt/dispatch on row, scalar, projection, grouped, join and composition
paths. Signed-native providers retain their own actual receipt contract.

**Compatibility:** an explicit SDK admission whose server policy does not require
measurement still permits optional query tariffs. No price/receipt is invented.
Unprepared local trusted SDK calls and non-query storage admission retain their
existing behavior. Earlier historical optional-SDK-pricing guarantees remain
valid; measurement permission and provider permission remain distinct.

The new provider contract is exercised with controlled adapters only. **Native
SQLite/MongoDB/Neo4j composition subqueries/proof decoders, public HTTP composition
and executable composition demos remain required.** Existing joined demos are
separate from this new shape. See [composition contract](CQL_COMPOSITION_CONTRACT.md).

Final generic-execution gate passes **2,876/2,876 Core/linked WEB4 tests**, zero
skips (35 new controls beyond2,841), and both Core frameworks build. Fresh
unchanged-operator live acceptance passes **93 SDK /93 HTTP**, **288 settled
operations /283 physical attempts**, dedicated joint1/1 zero skips. Evidence:
`TestResults/CosmicCqlDemo/composition-executor-policy-final.log`,
`TestResults/CoreCurrent/composition-executor-current.trx` and
`TestResults/CosmicCqlDemo/composition-executor-policy-demo.log`.
This is controlled composition execution plus real existing-operator regression;
no provider advertises native composition yet. Next: native SQLite subqueries /
proof decoding, then MongoDB and Neo4j, followed by public transport/examples.

## SQLite native composition owner — 25 October 2026

SQLite implements the generic composition interface. Source capture and all
materialized result stages share one serializable read owner; an existing caller
transaction is borrowed and never committed/disposed by the query. Read-only
connections are supported. Both statements share the production30-second /
instruction budget, relationship guards and900-parameter pool.

Complete selected input cells are decoded/validated before native result stages
run. A later filter or Take(0) cannot hide required-value/type errors. SQLite
produces every stage's ordered identities and independent source/work counts;
the adapter maps those native positions to detached source cells, without
hydrating entities or evaluating predicates in memory. Core checks the complete
packet before construction. Ordering keeps the prior stage's rank on ties.

Grouped and composed filters share one exact native scalar/null/string SQL
owner. Identifiers are generated from column positions; selected aliases are
never pasted into SQL. Graph expansion sources are explicitly rejected during
composition admission until a separate graph proof contract exists. Direct
child predicates retain their existing protected native owner.

The initial corrected native gate passes30/30, zero skips;15 scalar-domain cases
cover exact integers, UInt64, Decimal, Double, Single, GUID, UTC ticks, Unicode,
Boolean, enums and nullable values. Six exhausted budgets, visibility, preserved
windows, stable ordering, caller snapshot reuse, cancellation and child matching
are also covered. Nine compiler controls execute generated SQL; full provider
unit suite55 passes. Full native/Core regression is being finalized below.

MongoDB/Neo4j composition, public HTTP composition and annotated live composition
demos remain open. Existing join demos remain a separate verified feature.

Final SQLite composition acceptance passes **1,648/1,648 normal native integration
tests**, **55/55 provider unit tests** and **2,877/2,877 Core/linked WEB4 tests**,
zero skips. The native suite adds33 composition cases beyond1,615; unit SQL adds
nine beyond46. Historical exact-fixture `Category=Reconciliation` helpers are
excluded from normal acceptance, not skipped acceptance cases. Full native run
is3m4s. Actual SQL-entry cancellation, read-only connections and constructor-
throwing source entities pass alongside the initial30 cases.

Both Core frameworks build. Exact owned SQLite DB/sidecars and pending markers
are absent after teardown. Evidence: `TestResults/CosmicCqlDemo/sqlite-composition-full.log`,
`TestResults/SQLiteCurrent/composition-owner-full.trx`,
`TestResults/SQLiteCurrent/composition-compiler-unit.trx` and
`TestResults/CoreCurrent/composition-graph-admission.trx`.
This certifies the native SQLite SDK slice; MongoDB/Neo4j composition and public
HTTP/examples remain open. Earlier93/93 demo evidence remains a separate cohort.

## MongoDB composition pipeline compiler — 25 October 2026

The Mongo compiler captures one bounded source proof pipeline and independent
native stage-proof pipelines. Each stage retains the original protected source
window. Source proof includes exact cells/access/ordering and native source
count; stage transport projects only identity, rank and count. The required
snapshot owner must validate source errors/cells before executing any stage.
No provider interface enrollment is added at this compiler checkpoint.

Selected grouped/composed predicates share one BSON owner for exact numeric,
GUID, null, membership and Unicode string behavior. Result sorts use stable
least-significant-first native window passes: each pass keeps prior rank as its
tie. This handles repeated sort terms and32 selected terms without duplicate
BSON keys or a32-field compound sort. Take(0) emits an explicit false match,
while the source retains its overflow sentinel and protected traversal envelope.
Generated field paths use positional slots; client aliases are not pasted into
BSON paths. Stage pipelines are independently cloned.

Window numbering reference: [MongoDB8.3 `$documentNumber`](https://www.mongodb.com/docs/v8.3/reference/operator/aggregation/documentnumber/).
This reference does not substitute for native execution verification.

**Still required:** snapshot-session decoding/execution with a shared deadline,
all six budgets, native mutation/privacy/type/interruption controls, provider
capability enrollment and public composition transport/demos. SQLite's verified
native implementation remains available. Mongo compiler controls and existing
native regression evidence are recorded below once their gates complete.

Mongo compiler checkpoint final: **519/519 provider unit tests**, zero skips,
including nine new composition controls. The unchanged-operator native Mongo
CQL suite passes **328/328**, zero skips, after the shared predicate refactor.
Evidence: `TestResults/CosmicCqlDemo/mongo-composition-compiler-final.log`,
`TestResults/CosmicCqlDemo/mongo-composition-compiler-native.log` and
`TestResults/SubscriptionUsageCurrent/composition-compiler-unit.trx`.
This does not certify new composition pipelines against MongoDB yet: the
snapshot execution/decoding owner and native composition controls are next.

## MongoDB composition rank producer correction — 25 October2026

Native testing found that `$documentNumber` rejects compound sortBy. The rank
producer now uses `$sum: 1` with a `documents: [unbounded,current]` window.
This retains the complete native source/selected ordering and prior-rank ties;
there is no client sorting or fallback. The initial338-case native run passed335
and failed the3 ordered composition controls with the exact server error.
The corrected expanded353-case native gate is in progress, including12 exact
selected-domain filter/sort controls,2 required-value/Take0 controls and a
concurrent source/stage mutation control. The execution owner uses one snapshot,
one deadline and all six remaining budgets. Final acceptance is recorded after
that gate; public composition transport and Neo4j composition remain open.

Mongo window contract: [setWindowFields documents windows](https://www.mongodb.com/docs/manual/reference/operator/aggregation/setwindowfields/).

MongoDB snapshot composition acceptance: **353/353 native CQL tests** (25 new
composition controls) and **519/519 provider unit tests**, zero skips. The native
run took4m39s. This verifies stage order, repeated-key stability, all six budgets,
pre-cancellation/reuse,12 exact scalar filter/sort domains, required-value errors
outside Take0 and concurrent mutation between source/stage reads. All source and
stage commands share one snapshot/session/deadline; cumulative native document
counts retain compound tie ordering. No entity hydration or fallback evaluation.

Evidence: `TestResults/CosmicCqlDemo/mongo-composition-owner-final.log`,
`TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`,
`TestResults/CosmicCqlDemo/mongo-composition-owner-unit.log` and
`TestResults/SubscriptionUsageCurrent/composition-owner-unit.trx`.
Initial server-error evidence is retained separately. Further composition
privacy/relationship and outstanding-I/O/deadline controls remain required;
Neo4j composition and public HTTP/composition demos remain open.

25 October MongoDB composition privacy/interruption certification passes **359/359 native**, zero skips (31 composition controls). Unit checkpoint remains519/519; production unchanged. See [final native evidence and remaining scope](CQL_COMPOSITION_CONTRACT.md). Neo4j composition and public composition transport/demos remain open.

## Neo4j composition compiler — implementation in verification

25 October2026: one shared detached-source compiler now serves joins and the
new composition compiler. The source is captured once; selected result stages
operate on captured scalar cells inside the same native statement. Selected
filters share the existing exact grouped predicate translator. Client aliases
resolve to positional cells and literals remain parameters. Result sorts have
explicit CQL null ranks and preserve prior-stage rank on ties.

Source overflow sentinels and invalid-source proofs survive Take0. Source/work/
proof admission guards retain exceeded counts in the header while suppressing
further stage work. Explicit stage headers represent empty results. Complete
source proofs sort before stage identities. A strict packet decoder, existing
read/deadline/attempt owner integration, capability enrollment and expanded
native acceptance remain required; this compiler does not advertise execution.
Neo4j read committed remains explicit; GraphSnapshot is not granted.

Seven new unit controls and eight actual native Cypher controls are being
verified with the full existing query regression. Public composition transport
and annotated live composition demos remain open.


Neo4j compiler checkpoint final: **328/328 native CQL tests** (eight new actual
Cypher composition controls) and **499/499 provider unit tests** (seven new),
zero skips. Full native duration9m14s. This verifies the compiler plus the existing
join/group regression after shared detached-source/predicate extraction. It does
**not** certify a composition packet decoder or provider execution enrollment;
those are next. Existing read-committed/snapshot admission remains unchanged.
Evidence: `TestResults/CosmicCqlDemo/neo-composition-compiler-final.log`,
`TestResults/Neo4jCurrent/neo4j-cql-native-current.trx` and
`TestResults/Neo4jCurrent/composition-compiler-unit.trx`.

## Neo4j composition execution owner — implementation in verification

25 October2026: Neo4j now implements the generic composition provider contract.
Actual capability/readmission precedes native I/O. The existing leased native
read transport,30-second cumulative cancellation/deadline owner and read
transaction execute the single captured-source statement. Joins and composition
share one detached-source decoder and exact scalar/UTF-8 byte validation owner.

The new decoder requires a unique leading count/work header, complete ordered
source proofs, contiguous stage headers/positions, valid unique source identity
references and unchanged native counts/work. Empty stages are explicit. All six
remaining budgets are enforced before publication. Core independently verifies
complete stage membership/order before constructing detached results; structurally
valid but false native membership cannot bypass that barrier. Read committed
remains the guarantee; graph-source composition is still rejected explicitly.

The full unit gate passes519/519, zero skips (20 new decoder controls). The
352-case native gate is in progress, with24 new provider/HolonManager cases.
Further native privacy/relationship, empty-source zero-budget and outstanding-I/O
interruption controls remain required. Public composition transport/demos remain
open; this entry does not claim full CQL completion.


Neo4j SDK composition checkpoint final: **352/352 native CQL tests** and
**519/519 provider unit tests**, zero skips. Native duration9m19s. The24 new
provider/HolonManager cases verify stage order/windows, stable repeated-key
sorting, all six budgets/reuse, pre-cancellation,12 exact selected-domain
filter/sort round trips and required-value errors outside Take0. Existing join
regression passes after the shared detached-source decoder extraction. Twenty
new unit controls reject malformed/incomplete proofs and false membership.
Evidence: `TestResults/CosmicCqlDemo/neo-composition-owner-native.log`,
`TestResults/Neo4jCurrent/neo4j-cql-native-current.trx`,
`TestResults/CosmicCqlDemo/neo-composition-owner-unit-final.log` and
`TestResults/Neo4jCurrent/composition-owner-unit.trx`.

All three providers now execute generic SDK composition through HolonManager.
Neo4j uses one captured-source statement under read committed; it does not grant
a repeatable snapshot. Further Neo4j composition privacy/relationship, empty-source
zero-budget and outstanding-I/O interruption controls remain required. Public HTTP
composition and annotated live composition demos are next after certification;
full CQL/ORM roadmap completion remains unproven.

25 October Neo4j focused composition certification passes **42/42 native**, zero skips, including privacy, zero-budget and cold/warm in-flight cancellation/deadline recovery on the same provider. Prior full352 native/519unit evidence is separate; full362 has not been rerun. See [contract and native evidence](CQL_COMPOSITION_CONTRACT.md). Public HTTP composition and annotated demos are next.

## HTTP composition and annotated demos — 25 October2026

`POST /api/data/query-cql/composition` now uses the generic Core/HolonManager
composition planner/proof barrier on MongoDB, SQLite and Neo4j. The exact route
registry requires authenticated actor-bound admission and query tariffs. Stage
order is explicit; nested provider/graph overrides and ambiguous stage fields
are rejected before dispatch. Grouped/composed filters and ordering share one
selected-value binding owner, and projection/composition share exact wire output.

Final gates pass **2,940/2,940 Core/linked WEB4 tests**, zero skips (63new), both
Core frameworks, **102 SDK /102 signed HTTP demo examples**, and the dedicated
joint native test1/1. The live fixture settles **309 operations /301 provider
attempts**. New native controls verify owner/visitor visibility, stage order,
exact Int64 output, repeated-key denial without replay, and malformed-stage MVC
rejection with zero attempts/receipts. Twenty offline browser presets and replay/
exact JSON checks pass; the standalone demo builds with zero errors. Source
identity snapshots remained unchanged throughout the final native gate.

Examples32–34 in `V1NativeQueries.cs` and `V1Walkthrough.cs` demonstrate source
paging versus result paging, filter/window order and exact selected Int64 values.
The browser and Swagger offer matching composition requests. See the
[HTTP contract and examples](CQL_COMPOSITION_HTTP_CONTRACT.md).
Evidence: `TestResults/CosmicCqlDemo/composition-http-core-final.log`,
`TestResults/CoreCurrent/composition-http-current.trx`,
`TestResults/CosmicCqlDemo/composition-http-native-final.log`,
`TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx`,
`TestResults/CosmicCqlDemo/v1-demo-result.json` and
`TestResults/CosmicCqlDemo/composition-browser-presets.log`.

This closes bounded flat selected-result composition across SDK/HTTP on the
three providers. Graph-source composition is explicitly unadmitted. Broader
CQL relationship/join/group/scalar certification, authoritative tenant/sharing
policy, federation and the remaining ORM roadmap still require work.

25 October Mongo grouped identity consistency repair passes538unit/361native, zero skips. Native comparable grouping/extremum fields now must agree with decoded typed values, including off-page rows. [Root cause and red/green evidence](CQL_GROUPING_CONTRACT.md). Expanded all-provider grouping/policy gates remain open.

25October2026 Mongo scalar floating milestone:36new controls,454full native/560matched units pass0skip; six producer/consumer copies match and owned runtime stops cleanly. Only scalar Single/Double Sum/Average enrolled. Next: Mongo grouped arithmetic/final selected result keys, Neo4j arithmetic, then floating HTTP and annotated live demos. No full-roadmap completion claim.

Mongo grouped floating prerequisite now native-verified:2kernel tests/5,115packets normalize Sum/Average bit-for-bit against Core; no grouped capability enrollment yet. Complete454native/560unit scalar acceptance remains its own prior cohort. Next connect the component to grouped compile/read/filter/order with off-page proof validation.

Mongo grouped floating integration complete at the declared native SDK scope:34new controls, full490native/571matched units pass0skip; sixmatching producer/consumer copies and ownedruntime teardown verified. Raw off-page arithmetic proofs survive filters/pages. Full native includes5,127normalization packets with explicit overflow midpoints. Next: Neo4j scalar/grouped floating arithmetic, then floating HTTP and annotated live demos; policy/federation and remaining ORM gates remain open.

Neo4j floating prerequisite:2native digit cases/5,786vectors pass0skip, four matching producer/native copies, no graphwrites. The owned attached WSL/container lifecycle is verified stopped after the run. This is not a new full418native/529unit cohort or arithmetic enrollment. Next: exact final-value normalization plus scalar/grouped compile/read/capability integration, then floating HTTP/demos.

Neo4j scalar floating milestone:36focused cases,454full native/541matched units pass0skip; eightmatching Core/Shared/Neo/sourceDriver producer/native/unit copies. Grouped capability ownership is now independent, preventing scalar enrollment from admitting unfinished plain groups. ProviderDefault consistency used; unsupported ProviderSnapshot still rejects. Next: Neo4j grouped finalization/compile/read/selection/capabilities, then floating HTTP and annotated live demos. Broader policy/federation and ORM gates remain open.

Neo4j grouped finalizer prerequisite now passes2native cases/5,127packets bit-for-bit against Core (0skips). Four producer/native copies match and retained runtime/attachment stops cleanly. Existing454native/541unit scalar checkpoint remains separate; no full456/541rerun or group enrollment claimed. Next: grouped compile/read/selection/off-page proof integration and explicit grouped numeric admission, then floating HTTP/live demos.

Neo4j grouped floating milestone:34new native cases,490full native/552matched units PASS0skip; eight Core/Shared/Neo/sourceDriver producer/native/unit copies match. Complete raw off-page proofs retain typed arithmetic and independent native membership validation; all three native SDK adapters now enroll floating scalar/grouped Sum/Average. ProviderDefault consistency retained; unsupported snapshots still reject. Next: shared floating HTTP arithmetic binder/wire acceptance, annotated C#/HTTP/browser demos, then remaining declared policy/federation/ORM gates.
