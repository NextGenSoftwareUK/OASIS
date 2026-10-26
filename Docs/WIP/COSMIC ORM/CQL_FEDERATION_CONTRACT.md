# CQL federation implementation contract

25 October 2026. This specifies remaining implementation and acceptance work.
Federated global results, cursors and distributed joins are **not implemented**.
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
