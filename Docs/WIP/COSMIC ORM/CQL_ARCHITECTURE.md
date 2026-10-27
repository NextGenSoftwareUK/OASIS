# CQL architecture

Current implementation state: [CURRENT_STATUS](CURRENT_STATUS.md); feature acceptance: [parity checklist](CQL_PARITY_CHECKLIST.md).

1. **Capture:** bounded immutable query/selected-stage intent and closed literals; no user callback execution.
2. **Plan/admit:** explicit scope, eligible targets, exact scalar/relationship capabilities, visibility and work bounds.
3. **Own execution:** V2 HolonManager and physical-attempt owner; preflight all targets and re-admit actual contracts before dispatch.
4. **Native adapters:** Mongo aggregation/snapshot owner, SQLite serializable read owner, Neo4j captured scalar pipeline/read owner. Do not invent stronger provider consistency.
5. **Validate/return:** acknowledged complete detached evidence, exact values and shared remaining work; materialize only where the requested result contract permits it.

For projected All, native counterexample reduction follows every selected stage. Complete source/stage evidence validates before the Boolean is published; final row evidence and the terminal scan consume their budgets.
Source relationship composition has separate admission from entity All predicates. All three native source compilers now support the separate source-composition capability. Complete protected scopes are validated before business truth and paging; source predicates stay native.
Keep source translation, protected errors, membership/visibility and remaining-work accounting in their owning layers. No parallel fallback or silent retry.

[Active acceptance contract](CQL_PROJECTED_ALL_EXECUTION_CONTRACT.md) defines the current invariants.
[Full architecture/history](Archive/CQL_ARCHITECTURE_history_2026-10-27.md) retains earlier detailed designs, subsystem contracts and checkpoint evidence.
[Full ORM history](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_history_2026-10-27.md) retains replication/history/migration/placement and other programme requirements.

The native-only acceptance assembly links the shipping demo and provider adapters;
it does not implement replacement query execution. Its host supplies a V2
HolonManager with an explicit provider registry. The source/composition demo checks
both successful results and protected errors through that manager. Fixture creation,
ownership journals and exact cleanup stay in the test host, outside the read-only
public example. HTTP host contracts remain in their separate integration project.
The partial IQueryable bridge is shared Core code, independent of database type.
It registers owned expression chains with weak keys, binds standard LINQ closure
parameters per execution, captures an immutable CosmicQuery, then re-admits live
capabilities and invokes the existing V2 typed query owner. It neither compiles
application predicates to delegates nor fetches rows for client filtering. The
initial increment flattens exactly one explicitly named physical sequence;
remaining terminal/repeated-selection dispatch remains Q01 work, with explicit unsupported errors.
Standard cardinality and All terminals dispatch through CosmicScalarPlanner and
the existing V2 scalar owner, independently admitting scalar capabilities. All's
predicate remains separate from its source window. Entity cardinality after a
window now carries its own CardinalityPredicate descriptor and independent
SupportsPostWindowCardinality admission. The shared scalar predicate lowering
counts matches for cardinality and counterexamples for All, retaining each
operation's own meaning and the original source window.
Queryable Select transitions to a typed selected-sequence provider. The shared
query envelope owns standard/async enumeration for entity and selected results.
The selected provider rebinds source intent and selected stages per execution,
then uses the existing projection or selected-composition owner; selected All
uses the independent projected-All owner. Constructors and setters run only in
the established validated projection materializer, never during capture and
never as an alternative to missing native execution.
Entity element terminals delegate to the existing element planner/executor and
V2 typed hydration owner. First/Single cardinality, tail windows, Index bounds,
tracking and error semantics stay in that owner. The bridge handles a caller's
OrDefault argument only after an acknowledged empty result; it cannot replace a
native failure or ambiguous cardinality with a default object.
Queryable value aggregates bind typed paths and exact result domains before
planning against independent scalar-selection capabilities. Plain scalar Select
can lower into the same native selector aggregate; selected stages are never
moved across the reduction. Int32 Average now has an independently admitted double-output contract; Int64
uses independent checked-prefix enrollment, now verified on MongoDB, SQLite and Neo4j. The existing decimal-output
CQL contract is preserved rather than approximated with a client conversion.
The Int32 double-output mode shares integer-packet validation with decimal means
and grouped slots, and adds no provider row buffer. The scalar descriptor records
the exact result mode; scalar selection admission checks the independent flag
again at execution. The selected-scalar validator also verifies the new result's
finite Double type and Int32 input bounds. Existing capability constructors leave
the flag false, so no other provider is silently enrolled.
MongoDB Int64 double Average orders the selected source window by its canonical
ordering keys plus identity tie-breaker, then computes exact Decimal128 running
sums. The bounded input/count domain fits Decimal128 precision. Its six-field
packet includes a strict Boolean `prefixOverflow`; missing, numeric or additional
fields are invalid. The shared numeric owner checks exact limbs/count, rejects
contradictory no-overflow sums, and returns overflow for any visited prefix.
Only an acknowledged checked Int64 total is converted once to double before
division. Neo4j also independently advertises this mode. No client row accumulator
or decimal-mean conversion grants this capability.

SQLite attaches a connection-owned checked Int64 prefix aggregate under the
existing exclusive read owner. The selected source carries an explicit ordering
ordinal; SQL aggregate ORDER BY consumes that ordinal after source paging.
The callback retains a bounded running total/count and overflow latch, checks
cancellation/deadline/work budgets, and never buffers source rows. SQL computes
the independent exact limb/count packet in the same statement. The owner checks
hook failures before interpreting its strict 0/1 acknowledgement and unregisters
the hook on disposal. Kernel and V2 manager native gates prove reverse ordering,
post-page prefixes, nulls, overflow, cancellation and teardown.

Neo4j checked Int64 means reassert the admitted source ordering plus identity
immediately before native collect, after SKIP/LIMIT. Cypher reduce retains a
checked total and overflow latch. Positive and negative branches use their own
safe boundary subtraction; only an admitted branch performs addition. The
ordered participating-value list remains on the server and uses proportional
memory within the input-work bound and native transaction memory/timeout limits.
The exact eight-field integer packet includes Boolean prefixOverflow in every
normal/error union branch. Empty windows and genuinely empty sources retain the
same required/nullable shared-owner semantics. Unit packet tests and83 native
checks/store prove both overflow directions, reversal, paging and empty results.

Selected element terminals lower through CosmicProjectionElementPlanner into
existing native composition plus bounded Skip/Take and an independently enrolled
Reverse stage for tails. SQLite reranks descending prior ordinals; MongoDB assigns
new window ranks in descending prior-rank order; Neo4j orders the previous stage
positions descending before collecting the next stage. Detached stage proofs are
validated by the existing composition owner. Its shared ExecuteOwnedAsync checks
empty/multiple cardinality before constructing any DTO. V2 HolonManager gates the
new public async entry; the element result carries HasValue separately from Value.
The IQueryable bridge binds source/selected predicates per execution and handles
caller-supplied defaults only after acknowledged empty proof. Budgets include
full bounded source participation and each terminal-lowering stage.

Selected cardinality adds explicit RequestsCardinality intent to composition
plans and independently enrolled SupportsCardinality. Legacy constructors keep
that permission false. Empty stage lists are admitted only for cardinality intent.
The shared composition attempt owner now accepts a typed finalizer after detached
proof validation: row/element finalizers construct their selected output, while
cardinality finalizers normalize native counts directly. Cardinality debits final
proof payload and terminal scan work across providers before publishing values.

CosmicProjectionCompositionPacket.NativeSelectedCount is mandatory for this
intent and must equal complete validated output participation. SQLite executes a
native count over the same materialized stage CTEs in its serializable owner.
MongoDB executes a count facet in its existing snapshot session, explicitly
acknowledging zero; its positive rank decoder permits zero only for this terminal.
Neo4j preserves the acknowledged source/final-stage native header count. Missing
or contradictory counts fail before result publication. No DTO constructor runs
for Count/LongCount/Any. Standard selected predicates append after captured
windows, with closure binding repeated per execution.

Entity cardinality predicate capture uses the bounded scalar terminal grammar,
including child/descendant markers and expansion admission. The terminal
admission descriptor inherits source scope/read access and has no source window.
Planning and execution independently admit its read paths and explicit traversal
permissions. Existing capability constructors keep post-window cardinality false.
SQLite applies matching predicates to its ordered source subquery, with protected
CASE reductions and a serializable owner where relationship scopes require it.
MongoDB and Neo4j share their scalar terminal traversal/error lowering, changing
only the match contribution for cardinality. An empty terminal page visits no
terminal scopes; source-scope failures still precede paging. IQueryable binds
parameters per execution, while fluent cardinality capture freezes them.

Selected numeric reduction uses immutable CosmicProjectedNumericIntent and
CosmicProjectedNumericPlan descriptors plus a bounded exact-word acknowledgement
in the composition packet. Each provider independently enrolls scalar numeric
permissions through NumericReductions; legacy capability constructors keep it
absent. Selected scalar expressions and flat-record numeric selectors bind an
exact column. The V2 manager and shared composition attempt owner preserve source
and stage scope, cross-check native participating count against validated selected
cells, charge acknowledgement/proof/terminal work, and normalize through existing
integer/decimal/floating numeric owners. The numeric finalizer constructs no DTOs.

SQLite currently enrolls signed Int32/Int64 Sum/Average and nullable forms. Its
native reduction runs over the final materialized stage CTE in the same
serializable owner. Two integer sum words retain exact totals; Int64 double means
reuse the checked aggregate hook ordered by the final stage rank. The hook checks
budgets and is removed on disposal. Selected ordering/offset occurs before prefix
accumulation. SQLite now also enrolls decimal/Single/Double/UInt64 and nullable selected reductions; all3 wider domains are independently enrolled;
selected Min/Max uses its independently enrolled extrema contract described below. Q01 remains red for Q01d/e/f.

MongoDB Q01a now independently enrolls signed Int32/Int64 Sum/Average and nullable
forms. Source and selected reductions share AppendNumericKernel, preserving the
published exact-word arithmetic and checked-prefix logic. The selected path
projects its admitted column and final rank, then executes that kernel in a
facet under the existing selected snapshot session/deadline. Empty input receives
an explicit native zero packet. The repository reuses strict numeric decoders,
checks terminal work/proof bounds, requires one packet and returns it through the
shared selected owner. Scalar/record selectors and all27 native integer cases
are verified on all3 providers.

Neo4j Q01a now independently enrolls the same signed integer domains. The numeric
terminal consumes the final native selected-position list and captured scalar
cells in the existing query. It reuses integral-word expressions and the guarded
checked-prefix reducer, emitting one exact count/word/Boolean acknowledgement.
The decoder requires complete source and stage participation before accepting
that terminal, rejects duplicate/missing/malformed packets and charges final
proof payload plus terminal scan work. Empty input has an explicit zero packet.
The shared owner validates participating count and normalizes the result without
constructing DTOs. All27 integer cases/store and129 Neo focused CQL tests pass;
Q01a is green while wider domains and selected extrema remain red.

SQLite Q01b reuses existing decimal limb, UInt64 split and floating packet kernels against the final selected CTE. Floating hooks belong to the same serializable connection owner, enforce its deadline and release on disposal. The decoder validates domain-specific packet width and participation before the shared typed finalizer. No client-side aggregation or record construction is introduced. Native proof:37 wider-domain cases in `CqlLinqStagedWideAcceptanceTests.cs`.

Mongo Q01b sends final selected cells to existing exact kernels within the same snapshot session. Empty packets preserve native domain types (Decimal128 decimal words; Int64 floating words). Strict scalar decoding owns packet validation; terminal proof budgets charge actual domain width. No client aggregation or DTO construction.

Neo Q01b derives exact decimal/floating words from captured final selected values and reduces each word natively. UInt64 uses the existing canonical20-digit splitter. The same single-query owner validates complete stages before normalization; packet widths and proof charges are domain-specific. No extra MATCH, client aggregation or DTO construction. This does not strengthen Neo repeatable-read guarantees.

Q01c shared extrema uses independently captured ExtremaDomains, rechecked against live adapter capabilities before execution. It appends null exclusion (nullable/reference domains), exact column ordering and Take(1) after all selected stages, respecting the32-stage bound. The common attempt owner validates complete native source/stage/order/output proof before its finalizer reads one detached cell. Required empty values produce CQL_EMPTY_SEQUENCE; nullable/reference empties return null. Neither record construction nor client aggregation participates. Old capability constructors leave extrema unenrolled. SQLite, MongoDB and Neo4j now have native proof and explicit enrollment.

SQLite Q01c explicitly enrolls selected ordering domains for extrema. Native final filter/order/Take execute in its existing serializable composition owner. Expanded all-null proof reproduced UInt64 key normalization padding the JSON null token; the shared UInt64 Column normalizer now returns SQL NULL before numeric padding, preserving null semantics for every consumer. Required values retain exact20-digit numeric keys. Regression tests cover null/zero/UInt64.MaxValue. Reviewed native continuation passed all218 extrema cases without replaying creates.

Mongo Q01c enrolls exact selected ordering domains. Final null filtering, ordering and one-row selection run under the existing snapshot session/deadline; each stage retains native identity/rank proof. The shared owner reads the winning detached field only after validating complete participation and every stage. All218 extrema cases/33 domains and202 Mongo CQL tests pass.

Neo Q01c enrolls exact selected ordering domains; null filter/order/Take operate on the captured selected positions in the same native query. Complete stage/ordering proof is validated before the shared finalizer reads the winning field. All218 cases/33 domains and137 Neo CQL tests pass. No stronger repeatable-read guarantee is implied. All3 Q01c cells are now verified; repeated Select Q01d is independently verified below.

Q01d separates physical native Columns from immutable OutputColumnIndices/OutputAliases. Reselection changes only the final shape/bindings; earlier predicates/order/windows retain their physical dependencies and complete native proof. Later selected predicates/terminals resolve outward aliases back to physical indices. Final construction maps validated cells and never creates intermediate DTOs. Retained physical cells continue to consume normal work/payload budgets. The standard provider captures a bounded deferred parent binding (maximum32 repeated selections), re-evaluating earlier closures at execution; fluent selections remain immutable. Composition-only adapters are admitted through their composition contract, without an unrelated ordinary-projection requirement.4 shared tests/2,536 CQL regressions pass; all3 native acceptance now passes26 cases/store.
