# CQL federation implementation contract

25 October 2026. This specifies remaining implementation and acceptance work.
Bounded complete-source SDK row federation is implemented with unit verification below. Authenticated cursors, distributed joins, HTTP enrollment and native federation acceptance remain **unimplemented or unverified**.
Current multi-provider execution returns separate provider results. That behavior
remains the default; federation requires an explicit new intent and capability.

## Generic execution ownership

Core/HolonManager must own immutable federation intent, independent planning,
actual-adapter readmission, budgets, identity reconciliation and publication.
Providers continue to execute native admitted queries. A bounded, explicitly
planned merge is part of federation; it cannot substitute for an unsupported
provider filter, sort, projection, join or policy predicate.

Planning resolves the eligible provider set once. The immutable plan records its
ordered identities, exact scalar/operator contracts, authenticated access binding,
consistency requirement and resource limits. An unavailable or ineligible requested
provider is an error before dispatch; it cannot silently disappear from scope.
Every physical provider attempt uses the existing V2 attempt/receipt owner.
Reconciliation and serialization cannot create a second physical execution.

## Identity, conflict and provenance

Logical holon identity is the exact nonempty holon ID. Provider-local revision
tokens are opaque and cannot be compared across databases. A version number or
modified timestamp alone cannot establish that two providers have the same causal
revision: they may contain divergent writes or clocks.

The plan must explicitly choose an identity mode:

- Preserve copies: retain each provider result and its provenance.
- Reconcile identities: collapse only copies whose authoritative revision and
  content proof agree. A disagreement is a conflict unless an explicit resolution
  policy was admitted before execution.

An explicit provider-priority resolution can select a deterministic winner, but
must report conflicting copies and the selection reason. It must not label the
winner as freshest. A freshness policy requires an authoritative replication or
causal revision contract; elapsed wall time and provider latency are insufficient.
Access is checked before any copy can contribute to grouping, ordering, counts,
conflict diagnostics or output. Provenance must not reveal an inaccessible copy.

Detached projection results need an internal source identity and revision proof
even when those columns are not public selections. Never infer identity from
matching DTO values. All provider buffers and reconciliation proofs validate
before any user DTO constructor or public response runs.

## Global ordering and pages

Global order must declare common exact domains and null semantics admitted by
every target. Logical ID supplies a deterministic final tie-break; preserved
copies also include provider identity. Identity resolution occurs before global
Skip/Take, otherwise duplicate replicas can consume page positions.

Source windows retain their existing per-provider meaning. A separate federated
result window applies after reconciliation and global ordering. The API must
distinguish these windows instead of rewriting one into the other. A page limit
does not grant permission to truncate native inputs or declare a scan complete.

A native prefix can prove a global page only when the planner has a sound bound
on all unconsumed candidates, including duplicates/conflicts. Otherwise execution
must consume the explicitly bounded source or return a limit error. Unsupported
global plans reject; an unbounded client scan is never an implementation option.

## Cursor and consistency contract

Cursor tokens require a versioned, authenticated encoding. Bind the exact actor,
policy version, normalized intent, resolved provider scope, ordering, identity and
conflict policy, consistency mode, remaining budgets, expiry and per-provider
continuation state. Never accept executable expressions, provider substitutions
or client-supplied authorization state from a cursor. Key ownership and rotation
belong to the host's existing secret infrastructure, not a repository constant.

A provider-local snapshot does not establish a common distributed snapshot.
SQLite/MongoDB snapshot capability and Neo4j read-committed capability must stay
distinct. Repeatable pagination requires durable snapshot/continuation ownership
or another admitted stable-revision contract on every target. Without it, offer
an explicitly moving read with documented intervening-write behavior; never
promise stable pages from a sort key alone. Expired or invalidated cursors fail
without silently restarting the query.

## Resource and failure ownership

Declare global and per-target input rows/bytes, candidate rows, reconciliation
identities, output rows/bytes, work, physical attempts and deadline limits. Charge
all consumed buffers, including duplicate copies and discarded conflicts. Reuse
one cumulative deadline/cancellation owner. No target can spend the full global
budget independently. Zero remaining budget proves emptiness or rejects work;
it is not permission to skip a requested target.

V1 federation should publish atomically: a required provider failure, policy
failure, malformed proof, conflict without resolution, exhausted budget or
unknown read outcome returns no results. Record actual attempts and outcomes;
do not replay an uncertain physical attempt to manufacture success. Any future
partial-result mode needs its own explicit completeness and billing contract.

## Distributed joins

Cross-provider joins require a separate bounded plan over two resolved provider
sets, explicit locality and identity semantics, native source-policy enforcement,
transfer/candidate/pair limits and physical-attempt accounting. A same-provider
join capability cannot grant this operation. Left joins must distinguish an
authoritatively absent right row from a failed, inaccessible or truncated source.
No partial source can be interpreted as an empty source.

## Required acceptance

- Positive and denied plans on MongoDB, SQLite and Neo4j, including actual-adapter
  mismatch and all-target preflight before the first physical attempt.
- Replicas with equal IDs and equal/divergent causal revisions, opaque provider
  tokens, clock skew, priority policies and inaccessible conflicting copies.
- Equal sort keys, exact large integers/Decimals/UTC ticks, nullable keys, duplicate
  replicas spanning page boundaries and source-window/result-window separation.
- Cursor tampering, actor/policy/scope changes, expiry, key rotation, intervening
  insert/delete/reparent/write and unsupported repeatable consistency.
- Per-target/global budget exhaustion, cancellation, deadline, failure after
  earlier successful targets and no DTO publication or receipt replay on error.
- Distributed inner/left joins with duplicate identities, missing and inaccessible
  right rows, failed right sources and pair/transfer budget exhaustion.
- Annotated SDK/HTTP examples, native evidence, cleanup ledger, dependency pin
  validation and existing regression suites before capability enrollment.

See the [completion roadmap](CQL_COMPLETION_ROADMAP.md),
[execution ownership](CQL_EXECUTION_PIPELINE.md) and
[same-provider join contract](CQL_JOIN_CONTRACT.md).

## 26 October: implemented planning foundation

`CosmicFederationOptions.Create` captures explicit identity/consistency choices,
independent global Skip/Take, and bounded input rows, per-provider rows, bytes,
work, provider attempts and duration. `CosmicFederationPlanner.Plan` resolves and
snapshots the eligible scope once, pins each target into an ordinary native query
plan, and rejects the entire plan when any target is unavailable, incapable or
outside the authoritative read policy. Source windows remain per-provider;
global windows remain separate. Explicit provider priority must name the exact
resolved provider set and cannot claim freshness.

This is admission only: no federation executor or merged result is exposed yet.
RequireAgreement, repeatable snapshots and graph federation explicitly reject
until their execution/proof contracts exist. Ordinary multi-provider window
rejection remains intact outside the explicit federation planner.

Verification: dual-target Core build succeeded and all 3,556 Core unit tests
passed, including ten new federation planning cases. Evidence is retained in
`Verification/federation-planning-core.trx.gz` and
`Verification/federation-planning-identities.json`. Native execution, global
ordering/reconciliation, cumulative budget enforcement, cursor security,
distributed joins and SDK/HTTP demonstrations remain required acceptance work.

Internal cumulative accounting is now being implemented in CosmicFederationExecutionBudget: one locked counter owner spans all provider input and merge/output work, checks cancellation and a monotonic elapsed deadline, rejects unknown targets/negative or oversized proofs, and retains the first terminal failure. Exact bounds are allowed; a failed budget cannot later authorize another attempt or publish output. This is executor groundwork only. Native input byte/proof capture, dispatch integration and merged-result publication remain unimplemented; the completed unit gate is recorded below.

### Executor integration requirements

The existing `CosmicQueryExecutor` owns adapter snapshot/readmission and delegates
physical reads to `CosmicQueryAttemptOwner`. Federation must retain that owner
rather than call provider adapters through a separate unmetered loop. Preflight
all actual adapters before the first attempt. A linked cancellation deadline must
reach native I/O; post-read budget checks alone do not bound a hung provider.

Mongo `Repositories/HolonRepository.Cql.cs` streams a bounded native cursor;
SQLite `Persistence/Repositories/HolonRepository.Cql.cs` fetches a limit-plus-one
sentinel and rejects excess rows; Neo4j `Neo4jOASIS.Cql.cs` owns a bounded native
read transaction. Preserve these complete-source-or-error contracts. Do not
translate a full native budget into a successful truncated source. Per-source
Skip/Take applies before federation and remains part of the query intent.

The internal budget counts canonical UTF-8 input/output bytes, independently of
provider wire-format budgets. Charge actual detached canonical snapshots, not
an estimated row size or a DTO's projected fields. Native wire limits must still
be enforced during streaming. A subsequent target failure discards earlier
buffered output without erasing its already-settled physical attempt receipts.

Cumulative budget verification: Core dual-target build passed; full suite 3,569 passed, zero skips, including 13 budget cases for shared row/byte/work/output/attempt limits, concurrent charging, oversized arithmetic claims, cancellation, monotonic deadline and terminal rejection. Evidence: Verification/federation-budget-core.trx.gz and federation-budget-identities.json. Native integration remains pending; these unit results do not establish a working federated query path.

### 26 October: native input capture stage

The internal `CosmicFederationInputCapture` now snapshots actual adapters,
readmits all selected targets and inherited physical-attempt ownership before
any dispatch, then calls the ordinary `CosmicQueryExecutor` once per target.
It preserves each source window, detached row validation and read-access checks.
The cumulative deadline is linked into native cancellation; canonical UTF-8 input
bytes and rows share the federation budget. A later error discards the complete
input result without retrying earlier reads. The budget instance survives capture
for subsequent merge/output accounting.

The audit also found ordinary row execution discarded a native warning and
published its rows as complete. The common executor now rejects that envelope
with CQL_PROVIDER_RESULT_INVALID, so federation cannot promote a warned source
into a complete input set. New focused tests cover this boundary alongside
preflight, detachment, later failure, cumulative input limits and cancellation.
Verification passed: Core dual-target build and all 3,579 unit tests, zero skips, including ten new input cases. Retained evidence: Verification/federation-input-core.trx.gz and federation-input-identities.json. Native billing/provider certification has not been run for federation. This stage is internal;
merged global results and a public federation endpoint remain unimplemented.

## 26 October: global SDK row execution

`CosmicFederationExecutor.ExecuteAsync` now joins the admitted complete input
stage to a bounded global merge. It validates every ordering cell, including
priority-losing copies, before identity selection and global paging. Common
ordering uses explicit missing/null/value states, exact scalar domains, Unicode
scalar ordering and ID/provider tie-breaks. Numeric metadata conversions that
lose precision reject. UTC dates and canonical GUIDs retain their declared types.
No source filter, source order or source window is replaced by client evaluation.

PreserveCopies keeps physical copies separate. ProviderPriority selects from the
exact admitted priority list before applying global ordering and Skip/Take. Each
result retains its observed copies, selected provider and selection reason;
CausalAgreementEstablished is false. Neither provider revision strings, clocks
nor Version numbers establish cross-provider freshness. RequireAgreement and
repeatable snapshots remain rejected at planning.

One cumulative budget spans native input, key decoding, identity resolution,
merge sorting and complete canonical output (including observed copy provenance).
A limit, cancellation or bad late cell returns no result. Explicit bounded merge
sort preserves the original budget error instead of wrapping comparer exceptions.
The result reports observed input count and resolved count before its global page.

Verification: dual-target Core build passed; full Core suite 3,591 passed, zero
skips, including twelve new merge cases. The initial two fake-capability failures
were corrected by declaring long? explicitly, as native providers already do.
Retained evidence: Verification/federation-merge-{core,initial}.trx.gz and
federation-merge-identities.json. This is SDK/unit verification only. Actual
MongoDB/SQLite/Neo4j federation, billing receipts, HTTP, cursors, causal agreement,
repeatable consistency and distributed joins remain required acceptance work.

## 26 October native SDK federation acceptance

Fourteen read-only SDK federation scenarios now pass on the retained real
MongoDB, SQLite and Neo4j fixture: complete sources, visibility before global
paging, provider priority/copy provenance, independent source/global windows,
cumulative input rejection and exact decimal/UInt64/Single/Double/Int64/UTC/GUID/
nullable metadata ordering. The ordinary signed HTTP/recovery cohort remains
492 settled operations / 541 attempts; sources stayed unchanged and all seven
native producer/consumer binary pairs matched. The SDK federation scenarios run
before hosted usage enrollment. They do not prove signed federation billing or a
federation HTTP endpoint; both remain open, along with cursors/distributed joins.

The gate exposed a scalar-contract mismatch before any native fixture was
started: federation rejected JSON decimal/integer normalization admitted by
native Single/Double ordering. Two red regressions reproduced it. Core now uses
its existing exact rational ties-to-even IEEE rounding owner, once, and verifies
normalized ties by logical ID. Exact integer/decimal conversions still reject
precision loss; floating normalization follows the explicit declared IEEE domain.
Core dual-target build and all 3,595 unit tests pass, including four new cases.

Evidence: Verification/federation-normalization-{red,core}.trx.gz,
federation-normalization-identities.json, federation-native.trx.gz,
federation-native-source.json.gz, federation-native-binaries.json and
federation-native-result.json. The reusable runner requires all fourteen scenarios
and all three providers before it accepts success. No new environment/download
was used; fixture teardown and cleanup are recorded in the resource ledger.
