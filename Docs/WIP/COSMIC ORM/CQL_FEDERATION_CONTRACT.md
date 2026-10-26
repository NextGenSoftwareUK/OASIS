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

## Target-specific authorization — 26 October 2026

The generic planner now accepts a complete bounded list of
`CosmicFederationReadPolicy(provider, access)` bindings. Each concrete resolved
target must appear exactly once, with a matching authoritative provider (or an
explicit owner/public policy) and the same actor across the entire plan. Missing,
extra, duplicate, null, foreign-authority or mixed-actor bindings reject the whole
plan. The input list is snapshotted; changing it cannot change an admitted plan.

The planner binds each mandatory policy before that target's native source
window, preserving the source Skip/Take values. Existing source access cannot be
replaced or weakened. Ordinary WithReadAccess behavior is unchanged. Input
capture checks every target actor against inherited usage admission before any
physical admission/read; output is still checked against that target's catalogue.
A private row allowed by one provider's catalogue cannot borrow that grant on
another provider. This is application-supplied trusted authorization, not client
metadata/tenant labels or automatic grant discovery.

Dual-target Core build and all3,607 Core tests pass, including12new snapshot,
policy coverage, binding/window, rebinding, actor-admission and private-row controls.
Evidence: Verification/federation-access-core.trx.gz and
federation-access-identities.json. Earlier14native SDK federation scenarios
predate this extension. Signed hosted multi-provider authorization, federation
HTTP/billing, cursors and distributed joins remain required acceptance work.

## HolonManager federation materialization — 26 October 2026

`HolonManager.ExecuteFederationQueryAsync<T>` now routes V2 federation through
the existing collection metadata decryption, typed hydration and tracking owner.
Selected copies and observed alternatives retain provider/identity provenance;
mapping cannot change identity or escape the target read policy. Native rows
remain detached. The existing reserved metadata/encryption policy is reused,
without a second decryption implementation. Wrong keys, plaintext envelopes,
duplicate or conflicting encrypted fields, and typed setter failures return real
errors with no result. Legacy mode rejects before native execution.

The same federation budget continues through mapping and final canonical output.
An output regression exposed the persistence serializer dropping Copy.Row as an
IHolon navigation member, undercounting row payload. Federation now captures each
entity root explicitly in a data-only envelope containing all selected/observed
copies and provenance; it does not change the persistence navigation contract.
The earlier fixture-only diagnosis was incorrect and is superseded by this root
fix. Both raw large metadata and expanded typed output now hit the output bound.

Dual-target Core build and all3,617 tests pass, including ten new manager,
encryption-fault, tracking, error, V2-guard and row-payload cases. Evidence:
Verification/federation-materialization-{core,initial,row-omission}.trx.gz and
federation-materialization-identities.json. Earlier14native SDK scenarios predate
this finalizer and the per-target policy extension. Native manager/hosted
federation authorization, HTTP, billing, cursors and distributed joins remain open.

26 October HTTP implementation checkpoint: POST /api/data/query-cql/federation now uses existing compiler intent, complete per-target authorization, V2 manager materialization, shared attempt/tariff owner and explicit public leaf projection. It checks actual JSON size as well as canonical bytes. Core dual-target/shipping WEB4 build,3,636 Core tests and320 existing WEB4 tests pass (one Stripe sandbox skip). Nineteen new endpoint cases include three-target receipts and no dispatch after late policy denial. Signed real-provider federation HTTP/billing and native policy/materialization/demo acceptance remain open. Full schema/example/evidence: CQL_FEDERATION_HTTP_CONTRACT.md.

## 26 October: signed native federation and demos verified

The actual shipping controller passes13requests (12admitted/settled, one anonymous)
with23durable provider attempts on real MongoDB, SQLite and Neo4j. Cases cover
provider-specific private grants, visibility before global paging, priority/copy
provenance, revocation, late denial and authority mismatch with zero attempts,
input/output exhaustion, empty sources, schema rejection, captured default,
three-store metadata decryption, wrong-key rejection and unchanged stored
ciphertext. Every admitted operation settles with the expected exact tariff cost;
completed read receipts survive later service errors. No billing account is used.

The reusable demo also executes the spaced/commented
Examples/CosmicCqlDemo/V1FederationWalkthrough.cs: one global SDK example and one
signed global HTTP example, in addition to138SDK/138HTTP per-provider examples.
Swagger has a federation request example; its token interceptor derives allowed
paths from the shared CQL catalogue, including composition and federation. Demo
federation admission reserves three attempts; leaf routes retain one.

The complete cohort passes505settled operations/567attempts,14SDK federation
scenarios and process-restart recovery on all3providers. Native source snapshots
are unchanged and seven producer/consumer pairs match. The controller's source
hash matches the earlier HTTP capture; the reusable snapshot list now includes
that file explicitly for future runs. Owned fixtures/services were verified and
removed; no installation, download or new environment was used.

Evidence: Verification/federation-hosted-native{.trx.gz,-source.json.gz,-binaries.json,-result.json}
and federation-hosted-controller-identity.json. Core3,636 tests and the existing
shipping320tests/one Stripe sandbox skip are separate prior regression gates.
Authenticated cursors, causal agreement/repeatable distributed snapshots,
distributed joins and full remaining ORM roadmap acceptance remain open.

## Canonical continuation intent binding — 26 October 2026

`CosmicFederationIntentFingerprint.Create(plan)` now computes a streaming,
culture-independent SHA256 identity for the complete admitted intent. It binds
model/engine artifact identities, original/resolved provider scopes, source and
global windows, ordering, predicate kinds and exact scalar representations,
identity priority, consistency and every budget ceiling. Every target's actual
mandatory predicate is included, so authoritative record IDs and actors are bound
rather than only a provider name. Equivalent sorted grant catalogues bind equally.

The visitor accepts only known immutable CQL nodes and never constructs entities
or executes provider queries. JSON framing separates path segments and node
boundaries. The encoding is streamed directly into the hash with a1MiB hard bound;
excess returns CQL_FEDERATION_BINDING_LIMIT and no digest. Model/engine artifact
changes deliberately invalidate bindings instead of assuming compatible semantics.

Dual-target Core build and all3,654 tests pass, including18culture/snapshot,
policy/actor/catalogue, scope/priority, windows/budgets, traversal/predicate,
no-materialization and size-limit cases. Evidence:
Verification/federation-binding-core.trx.gz and federation-binding-identities.json.

This is an unkeyed intent digest, not authentication or an authorization grant.
It is not accepted as a cursor. Protected host-owned token state, key ownership/
rotation, expiry, continuation idempotency/replay prevention, cumulative remaining
budgets and signed/native paging acceptance remain required. Provider-specific
contract/deployment identities and the original HTTP scope intent also belong in
the host's final protected binding. HTTP currently continues to reject Cursor.

### Continuation workflow constraints (not yet implemented)

The prepared codec carries a fixed next-request key and lineage. The HTTP flow
must require that exact key and verify its binding to the admitted usage handle,
so the existing durable idempotency owner rejects concurrent/replayed redemption.
A caller-chosen new key must not reset remaining budgets. Invalid/expired or
policy-mismatched state must produce affirmative no-provider-execution evidence.

Moving continuations will reread the same bounded source windows under fresh
target authorization; every physical read needs a new admitted receipt. This is
an explicit next read, never a replay to resolve an uncertain previous outcome.
Resolved providers stay pinned. Remaining per-target/global rows, bytes, work,
attempts and active execution time must decrease; client think time is separate
from the fixed absolute cursor expiry. No depleted target may silently disappear.

The host must bind original HTTP scope/intent and deployed provider contracts in
addition to the Core plan digest. Its configured shared Data Protection key owner
must survive intended replicas/restarts and enforce rotation/revocation. The
codec does not configure those keys. Response publication must remain behind the
existing durable usage-settlement buffer. Signed/native replay, expiry, mutation,
revocation, budget and recovery tests remain acceptance requirements.

## Protected continuation-state codec — 26 October 2026

An internal WEB4 codec now uses host-supplied ASP.NET Data Protection with a
versioned purpose and fixed182byte binary payload. It authenticates/encrypts
actor, intent/deployment digests, lineage, exact next-request key, issue/expiry,
global offset, pinned provider scope and remaining global/per-target budgets.
Input arrays are snapshotted; unused slots and layout/version are canonical.
Expiry is at most five minutes; foreign actors/deployments, future/expired state,
wrong key owner/purpose, tampering and malformed/oversized tokens fail closed.

No default key provider, filesystem key ring or HTTP enrollment is created.
Production key persistence/sharing/rotation/revocation belong to the host. Tests
use memory-only ephemeral keys. Full Core3,675 tests pass, including21codec cases;
the shipping WEB4 build passes. Evidence:
Verification/federation-cursor-codec-core.trx.gz and
federation-cursor-codec-identities.json.

This is protected state only, not a redemption protocol. Fresh-policy/intent
validation, binding the exact next key to durable usage idempotency, atomic replay
rejection, cumulative remaining-budget execution, publication and signed/native
paging acceptance remain unimplemented. HTTP still rejects Cursor input. The
prior505/567 native federation checkpoint is separate and predates this codec.

### Redemption ownership inspection — 26 October 2026

The durable replay owner is `Web4UsageExecution.AdmitAsync` in Core's
`Services/Subscriptions/Web4UsageExecution.cs`. It canonicalizes the avatar,
derives the operation through `UsageCanonical.OperationId(service, avatar, key)`,
claims the outbox with `TryBeginAsync`, and rejects an existing claim before
returning a handle. Authority start also precedes handle publication. Therefore
cursor redemption must reuse this owner rather than introduce another replay
store or derive operation IDs in the controller.

`UsageExecutionHandle` currently exposes OperationId and UserId but no supported
check that a supplied continuation key belongs to its admitted operation. The
next Core change should expose a narrow binding check implemented with the same
canonical owner, including the captured service and avatar. Tests must prove
correct key/owner acceptance, foreign key/service/avatar rejection, and unchanged
binding on `WithNativePrices` derived handles. Host code must require the cursor's
exact server-issued key in the HTTP header and verify the handle binding before
provider preparation. Header equality alone is insufficient evidence of admission.

This inspection does not implement redemption. The codec checkpoint is published
as parent commit 837abaaa8; origin was verified at that exact commit. The shared
build/output and root Git slot was explicitly returned for the peer S3 regression.
No build, download, environment or service was introduced during this inspection.

26 October durable request-key binding prerequisite verified: UsageExecutionHandle.IsBoundToRequest(service, avatar, key) reuses UsageCanonical.OperationId against the captured durable owner, independent of later options mutation. Admission and binding share key validation. Ten strict memory-only controls reject foreign keys/avatars/services, invalid key lengths/control characters and empty avatars without authority/outbox calls. Core dual-target build/all3,685 tests pass, zero skips. Evidence: Verification/federation-request-binding-core.trx.gz and federation-request-binding-identities.json. HTTP cursor redemption, priced-handle binding coverage, cumulative budgets and signed/native paging acceptance remain open.


26 October cumulative budget groundwork verified: internal immutable remaining accounting preserves global/per-provider rows, input/output bytes, work, attempts and active duration across resume. Original scope/ceilings and global/provider consumption consistency must match. Zero limits remain zero; terminal/cancelled executions cannot publish snapshots. Active time rounds up to prevent fractional reset; client think time is excluded and still needs independent absolute cursor expiry. Core dual-target build/all3,696tests pass, eleven new cases. Evidence: Verification/federation-budget-resume-{core,initial-core}.trx.gz and federation-budget-resume-identities.json. This is not yet connected to executor/manager/HTTP cursor flow; protected redemption, accounting publication and signed/native continuation acceptance remain open.


26 October executor/manager accounting integration verified: successful federation results publish RemainingBudget only after complete reads, merge, typed materialization and canonical output charging. ExecuteWithBudgetAsync / HolonManager.ExecuteFederationQueryWithBudgetAsync propagate validated remaining scope/vector/ceilings, per-target capture limits and active deadline. Depleted complete sets reject before provider dispatch; null state cannot reset limits; errors publish no success snapshot. Public Restore returns OASISResult with structural validation but is not cursor authentication, authorization or replay control. Core dual-target/all3,707tests and shippingWEB4build pass. Evidence: Verification/federation-accounting-integration-{core,initial-core}.trx.gz and identities.json. HTTP cursor redemption/settlement-safe publication and signed/native continuation acceptance remain open; route still rejects Cursor. Manual includes a spaced C# SDK accounting example.

