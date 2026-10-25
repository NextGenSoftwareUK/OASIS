# CQL selected-result composition contract

25 October 2026. Immutable capture, independent admission, complete proof
validation and generic V2 execution are implemented; native provider execution
is not yet enrolled. This preserves the original full roadmap scope.

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
// Capture only at this checkpoint: native adapters still reject this shape.
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

Current planning and raw projection materialization reject nonempty stages with
`CQL_CAPABILITY_UNSUPPORTED`. This avoids silently ignoring the new intent or
performing client-side evaluation while native owners are being implemented.
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
