# CQL selected-result composition contract

25 October 2026. Immutable capture, independent admission, complete proof
validation and generic V2 execution are implemented. SQLite native execution
is verified; MongoDB/Neo4j native composition remains pending. This preserves the original full roadmap scope.

## Operation ownership

A projection keeps its original `Source` object and selected columns. Its
`ResultStages` is a frozen, bounded sequence of explicit native operations.
`Where`, `OrderBy`/`OrderByDescending`, `ThenBy`/`ThenByDescending`, `Skip` and
`Take` operate on selected aliases or an exact scalar result. Capture invokes no
result constructor/getter/setter, arbitrary method or runtime evaluator.
Grouped and projection expressions share `CosmicSelectedColumns` resolution.

Stage order is observable. Source `Take(2).Select(...).Where(...)` filters only
the two participating rows. Result `Take(2).Where(...)` cannot become
`Where(...).Take(2)`. A later OrderBy remains a new stage; ThenBy extends the
immediately preceding sort. Skip and Take remain distinct ordered stages,
including zero windows. There are at most32 stages/32 keys per ordering, and
each selected window has a count between0 and10000. Failed intent propagates
without evaluating later operations.

```csharp
// Capture the pipeline, then use the composition planner/manager on SQLite.
var selected = CosmicQuery<Holon>.Create()
    .OrderBy(holon => holon.Name)
    .Take(2)
    .Select(holon => new { Label = holon.Name, Revision = holon.Version })
    .Where(row => row.Revision > 1)
    .OrderBy(row => row.Label)
    .Take(1);
```

The post-selection predicate binds `Revision`, while its source path remains
`Version`. No selected predicate is pushed ahead of the captured source window.
Runtime `CosmicProjectionRecord` aliases require exact typed indexer access;
wrong domains, unknown aliases, nested computed properties and lossy casts reject.

## Native execution acceptance still required

- Independent actual-adapter composition admission: ordinary source/projection
  permissions cannot grant a subquery or selected-result operator.
- One bounded native read owner on each provider. SQLite/Mongo retain snapshot
  behavior; Neo4j retains its declared read-committed guarantee.
- Native subqueries preserve every stage and deterministic source-identity ties,
  even when identities are not selected into application output.
- Explicit input/work/byte and intermediate/output limits. Every source value
  participating in projection must be validated before a later filter/page can
  hide malformed data or excessive work. Take(0) cannot bypass validation.
- A complete detached proof contract that validates identity, stage membership,
  exact domains, ordering and windows before any DTO construction. Ordinary
  `CosmicProjectionRow` buffers do not prove composed execution.
- Global preflight and actual readmission, durable physical-attempt/receipt
  ownership, cancellation/deadline coverage and no partial publication/replay.
- Positive/negative native evidence on all three providers, signed HTTP shape,
  spaced SDK/HTTP/browser/Swagger examples and coherent regression evidence.

Ordinary projection planning and raw projection materialization reject nonempty stages with
`CQL_CAPABILITY_UNSUPPORTED`. Use the separate composition planner/executor: SQLite now has a native owner;
MongoDB/Neo4j remain pending. No adapter may silently ignore selected stages or
replace native execution with client-side evaluation.
Post-join composition, federation and authoritative policy remain separate open
roadmap requirements.

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

## MongoDB composition snapshot owner — implementation in verification

25 October 2026: provider execution now captures source input and every native
stage proof in one snapshot session under one30-second deadline. The complete
source is decoded before result stages; malformed required values cannot be
hidden by Take(0). Input rows/bytes, cumulative stage input work, identity proof
bytes and final result rows/bytes are bounded independently. No entities are
hydrated and no client predicate supplies native results.

Initial native controls cover stage order, stable sorting, all six remaining
budgets and cancellation/reuse. The first native run has exposed ordered
composition failures; this capability is **not certified** until diagnosed and
its expanded native matrix passes. Added controls also cover exact scalar
round trips, required-value errors and concurrent mutation between source/stage
commands. Public HTTP composition and Neo4j composition remain open.

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

## MongoDB relationship and interruption certification — in progress

The additional six controls exercise direct-child and descendant public/private
matching, visibility before source paging, outstanding-I/O cancellation and
shipping30-second deadline expiry. The deadline test observes actual aggregate
maxTimeMS on the wire and requires decreasing allowances across commands; a
fresh per-command timeout cannot satisfy it. Every failpoint is disabled in
finally, the pending task is joined, and a fresh query must succeed afterward.
The native cohort is359; final outcome will be recorded after terminal teardown.
No production source changed for these controls.

### Neo4j implementation constraint

Existing `Neo4jOASIS.Cql.JoinCompiler.cs` captures source cells inside one native
statement and explicitly retains read-committed semantics. Composition should
likewise carry bounded captured cells through native result stages; repeated
source queries cannot establish a repeatable snapshot. Keep GraphSnapshot
unsupported unless a separately implemented and verified guarantee changes it.
This is a design constraint, not completed Neo4j composition.


Final MongoDB relationship/interruption gate: **359/359 native CQL tests**, zero
skips,5m4s, including31 composition controls overall. Direct/descendant private
matches are excluded, public matches remain visible, and visibility precedes
source paging. Outstanding cancellation and the shipping30-second deadline
return no packet, preserve decreasing native wire budgets and allow fresh reuse.
Evidence: `TestResults/CosmicCqlDemo/mongo-composition-privacy-interruption-final.log`
and `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`.
Runner42875 completed successfully and acknowledged owned teardown. Provider
unit evidence remains519/519 from the unchanged-production checkpoint. Next:
Neo4j native composition, then public composition transport/demos and remaining
broader CQL certification/policy/federation requirements.
