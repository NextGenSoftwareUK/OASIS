# CQL numeric Sum/Average contract

SQLite floating Sum/Average now has native scalar/grouped SDK acceptance:
**1,763 production integration /74 unit tests**, zero skips, stable source and
six matching copies. SQL-local constant-size packets retain source/budget/access
and off-page error ownership. MongoDB/Neo4j and HTTP floating arithmetic remain
pending. See [the contract](CQL_FLOATING_AGGREGATE_CONTRACT.md).

## Floating aggregate capture - 25 October 2026

Full Core **3,098 tests** pass without failures/skips; both Core targets build.
Eight new cases verify required/nullable Single/Double Average overloads, Sum,
explicit input admission versus legacy read-only permissions, typed empty sums,
rejection of integer-packet substitution and scalar/grouped publication without
Decimal conversion. Nonfinite and wrong-type values still reject. Existing
unsupported-domain tests now use DateTime/TimeSpan/Boolean instead of newly
supported floating capture.

The shared layer captures immutable floating intents and preserves their declared
result type. The native/HTTP capability profiles remain unchanged: this does not
enable floating Sum/Average on an adapter. SQLite, MongoDB and Neo4j native
translations, group keys, acceptance and explicit HTTP/demo enrollment remain next.
See [the contract](CQL_FLOATING_AGGREGATE_CONTRACT.md).

## Finite floating aggregate foundation - 25 October 2026

Full Core **3,090 tests** pass without failures/skips; both netstandard2.1/net10.0
builds pass. Eleven new controls include every exponent, seeded finite bit
patterns, 256 seeded reductions checked by an independent adjacent-IEEE binary
search, signed cancellation, underflow, ties and overflow thresholds. One-input
packets cannot hide source rounding or noncanonical signed digits.

The new generic Core owner normalizes ten/seventy signed base2^30 native limb
sums into Single/Double, rounding once to nearest/ties-to-even. It preserves
finite output and canonical zero; it neither scans source rows nor grants an
adapter capability. Floating query capture/grouping, native translations on all
three stores, HTTP enrollment and live arithmetic demos remain open. See the
[floating aggregate contract](CQL_FLOATING_AGGREGATE_CONTRACT.md).

Native Decimal/nullable Sum/Average and live signed HTTP are certified on all
three priority stores. The latest demo passes120SDK/120HTTP queries, including
examples38-40; see [demo acceptance](CQL_DEMO_RUNBOOK.md). Dated implementation
checkpoints below retain their historical scope and restrictions at that time.

## Neo4j Decimal query checkpoint - 25 October 2026

Scalar/grouped Decimal Sum/Average integration passes 23 focused native tests
and the full 529-unit suite, with no skips. Full acceptance passes **416 native /529 units**, without failures or skips. Seven signed Int64 sums preserve exact scaled28 values; native rounded
ordinal keys drive group filtering/order while raw packets retain all off-page
error proofs. Existing read ownership, source windows and interruption behavior
remain in force. Neo4j provider-default consistency remains read committed.
Decimal HTTP inputs and live demos remain unenrolled. See the
[Decimal contract](CQL_DECIMAL_AGGREGATE_CONTRACT.md).

## MongoDB Decimal query integration - 25 October 2026

Scalar/grouped Decimal and nullable Sum/Average now have native seven-word
reductions and exact rounded native group filter/order values. Focused23 native
cases and full550 units pass without skips. Full416 native tests pass without failures or skips; native SDK enrollment is verified. HTTP Decimal inputs
and Neo4j Decimal arithmetic remain pending. See the
[exact reduction and rounding contract](CQL_DECIMAL_AGGREGATE_CONTRACT.md).

## SQLite Decimal native acceptance - 25 October 2026

SQLite now enrolls native Decimal/nullable Sum/Average for scalar and grouped
queries, including selected filters/order/pages. Full1,702 native/72 unit tests
pass without failures/skips, with23 new Decimal controls. They cover boundaries,
large cancellation, final rounding, overflow versus valid means, nullable/empty
inputs, source windows, privacy, cancellation/reuse,137inputs independent of graph
publication limits, exact key comparisons and rejection of unrepresentable JSON.

Seven signed INTEGER sums preserve the existing exact Decimal key domain. Grouped
filter/order keys normalize that constant-size packet with the same Core owner
used for returned values. No float cast, entity hydration or manager-side input
scan is introduced. Existing snapshot/traversal/budget/error ownership remains.

MongoDB/Neo4j native Decimal and Decimal HTTP/demo enrollment remain pending.
The shared v1 binder explicitly limits Sum/Average inputs to Int32/Int64/UInt64
and nullable forms, preventing SDK capability expansion from silently changing
transport enrollment. See [Decimal contract](CQL_DECIMAL_AGGREGATE_CONTRACT.md).


## Decimal numeric Core foundation - 25 October 2026

Full Core3,046 and SQLite72/Mongo539/Neo520 unit tests pass without failures/skips;
Core netstandard2.1/net10.0 builds pass. Thirty new controls include2,048 seeded
reference samples. Separate prior native/demo checkpoints retain their scope.

Core now captures Decimal/nullable Sum/Average and normalizes a constant-size
seven-limb scaled28 native packet. Final rounding is nearest/ties-to-even from
the exact rational; mathematical out-of-range sums fail explicitly. Cancellation
can retain tiny values after oversized intermediates. The legacy integer packet
rejects Decimal intents. Existing native profiles remain unenrolled; provider
unit gates prove that read permissions and caller claims cannot upgrade them.

See [the Decimal aggregate contract](CQL_DECIMAL_AGGREGATE_CONTRACT.md). Native
translation/grouped keys on all three stores, HTTP enrollment and live Decimal
arithmetic demos remain pending. This does not change verified UInt64 execution.


## UInt64 live demo acceptance - 25 October 2026

The walkthrough now has **37 examples per provider**: **111 SDK /111 signed HTTP**
queries pass on MongoDB, SQLite and Neo4j, with318 settled operations/310 attempts.
The joint native demo passes1/1 without skips. Source snapshots remain unchanged;
six producer/consumer identities match under the build lease. Standalone demo
builds and9 offline browser preset checks pass. Owned services stopped normally.

Examples35-37 show exact UInt64 Sum, Decimal Average retaining the fraction
18446744073709551612.5, and grouped mean filters/order/pages. Browser presets:
`unsignedSum`, `unsignedAverage`, `unsignedMeanGroups`. Swagger adds named UInt64
Sum/Average examples. These close the unsigned numeric live-demo gate, while
other numeric input domains, sharing/tenant policy, graph-source composition and
federation remain open.

Evidence in TestResults/CosmicCqlDemo: unsigned-numeric-demo.log,
v1-demo-result.json, v1-walkthrough.txt, v1-source-identities.json,
v1-binary-identities.json, unsigned-numeric-demo-build.log and
v1-decimal-ui-check.json. Native report:
TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx.


## UInt64 HTTP compiler and wire acceptance — 25 October 2026

The shared scalar/grouped selection visitor now binds UInt64 and nullable UInt64
Average through the existing typed Core overloads. Sum already uses generic
capture. No reflection, compiled selector or provider-specific HTTP path is added.
Full Core3,017 tests pass without skips;9new controls cover scalar Average required,
nullable and empty output, exact UInt64 Sum wire values0/UMax and grouped input/
result types. Before the fix,5new Average controls failed and4new Sum controls
passed. The shipping WEB4 API builds successfully with existing assets.

Numeric Sum results are tagged `UInt64` invariant decimal strings; Average results
are tagged `Decimal` strings or nullable null. The authenticated source binding,
actual provider capability admission and existing receipt owner are preserved.
Live signed HTTP/native demonstration is the next gate; mock transport tests and
shipping compilation do not substitute for that acceptance.


## Neo4j unsigned arithmetic — 25 October 2026

Neo4j now enrolls explicit UInt64/nullable scalar/grouped numeric input permissions.
Its canonical20-digit key is split into two10-digit decimal chunks. With
`x=upper*10^10+lower` and `10^10=2*2^32+1410065408`, both base-2^32 limbs are
computed using bounded Int64 arithmetic; the intermediate stays below2^62.
Scalar/grouped reduction and grouped sum comparisons share that lowering.
Invalid or missing cells never enter the unsigned substring conversion.

The existing exact average-key compiler already supports the96-bit numerator;
no floating conversion or alternate client evaluator is introduced. Grouped
comparison cells retain their declared type so UInt64 keys/literals use the same
limbs as their aggregate counterparts. Scalar/grouped result normalization stays
in Core, including range validation and overflow classification.

Full391 native and520 unit cases pass without failures/skips, including19new
native numeric controls and the two unsigned key/fractional mean checks.
Unsigned numeric HTTP/demo enrollment remains a separate next gate.


## MongoDB unsigned native enrollment — 25 October 2026

MongoDB now explicitly declares UInt64/nullable numeric input permissions for
Sum/Average and grouped result sorting/filtering. Its existing Decimal128 limb
reduction and exact Decimal mean normalization are reused. UInt64 has at most20
digits; the bounded complete numerator has at most29 decimal digits and fits
Decimal128's34 significant digits. No binary64 or Int64 conversion of the input
is introduced. Limb decoding still requires exact bounded Int64 packet values.

Full391 native and539 unit tests pass without failures/skips, including17 new
unsigned native controls. Evidence is recorded in implementation status.
Tests include UMax boundaries, overflow vs valid averages, nullable missing inputs,
malformed input rejection, source windows, grouped selected filters/order/pages
and off-page overflow. Raw adapter groups include validation-only rows marked
`IsResult=false`; the generic executor owns final result publication.

Use explicit unsigned literals such as `0UL` in UInt64 selected predicates.
Neo4j unsigned numeric enrollment and unsigned numeric HTTP/demo acceptance are
still pending. Earlier read/projection UInt64 HTTP support remains separate.


## SQLite unsigned numeric implementation — 25 October 2026

SQLite now explicitly enrolls UInt64/nullable Sum and Average for native scalar
and grouped queries. Full Core3,008 / SQLite native1,679 / SQLite unit72 pass without failures/skips. This section supersedes the
earlier foundation-only status for SQLite. MongoDB/Neo4j numeric enrollment and
unsigned numeric HTTP/demo acceptance remain pending.

The existing canonical 20-digit UInt64 value is split into two bounded decimal
chunks, then converted to base-2^32 limbs using INTEGER-only SQL arithmetic.
Scalar reduction, grouped reduction and grouped comparisons share this owner;
values above Int64.MaxValue never pass through SQLite REAL. Grouped average sort
keys explicitly identify unsigned input modes. Shared grouped result validation
uses the declared unsigned domain (0..UInt64.MaxValue), preserving Decimal means.
Sum overflow remains an error even when the result page would hide the group.

SDK capture examples (execute the resulting plan through the existing scalar or
grouped HolonManager owner with the actual provider adapter capabilities):

```csharp
// Declare UInt64 explicitly; read permission alone does not grant aggregation.
var source = CosmicQuery<Holon>.Create();

var total = source.Sum(
    holon => CosmicQueryMetadata.Value<ulong>(holon, "counter"));

// Nullable input ignores missing values. An entirely missing set yields null.
var mean = source.Average(
    holon => CosmicQueryMetadata.Value<ulong?>(holon, "counter"));

var grouped = source
    .GroupBy(holon => holon.Name)
    .Select(group => new
    {
        Total = group.Sum(
            holon => CosmicQueryMetadata.Value<ulong>(holon, "counter")),

        Mean = group.Average(
            holon => CosmicQueryMetadata.Value<ulong>(holon, "counter"))
    });
```

`Total` retains UInt64; `Mean` is Decimal. Arithmetic never wraps on overflow.
Use the manual's authenticated source/access binding for application queries;
these examples illustrate numeric capture, not a visibility-policy bypass.

New native cases cover boundary values, exact means, overflow, null/empty inputs,
source windows, invalid input domains, grouped filters/order/pages and comparison
against an unsigned group key. Full regression results are recorded in the
implementation status and cleanup ledger after terminal verification.


Current status, 25 October 2026: exact Int32/Int64 and nullable Sum/Average
are implemented on SQLite, MongoDB and Neo4j, including grouped reduction,
selected grouped filters/order/pages, signed HTTP transport and annotated demos.
The latest shared walkthrough passes 102 SDK/102 HTTP examples across all three
providers; those examples include the enrolled numeric operations. Later dated
sections retain their original, narrower test cohorts.

The earlier grouped-participation checkpoint passed Core 2,970, SQLite 71 unit
and 1,651 native tests with zero skips; MongoDB/Neo4j native regressions pass
361/362 and their unit suites pass 538/519, also zero skips. This is not full CQL completion. Decimal/Double/UInt64 input
Sum/Average, broader certification, policy and federation remain open. Decimal
Average output does not grant Decimal input aggregation.

## UInt64 Core foundation — 25 October 2026

Core capture and exact packet normalization now also understand UInt64 and
nullable UInt64 Sum/Average. Sum retains UInt64; Average returns Decimal (nullable
for nullable input). This foundation passes both Core frameworks and **3,006
Core tests**, including36 new cases. It does **not** enroll native UInt64
arithmetic on any provider or extend the HTTP numeric input contract yet.

The two-argument `CosmicScalarSelectionCapability(operation, reads)` preserves
its published signed Int32/Int64 numeric input domains, including nullable forms.
Min/Max keep their existing exact read permissions. The additive three-argument
constructor accepts an immutable, bounded list of exact input types. A selected
path must satisfy both read permissions and that input list. Scalar/grouped
planning and actual-adapter readmission use this same owner, so new Core domain
knowledge cannot silently expand an old provider's promises.

For unsigned x, split exactly as `high=x/2^32`, `low=x%2^32`. Both limbs lie in
0..2^32-1. With at most2^31-1 participating values, each limb sum is below2^63
and fits Int64; the complete numerator is below2^95 and fits Decimal's96-bit
coefficient exactly. Core rejects negative/excessive/impossible packets before
publication. Sum checks its final UInt64 range; Average retains standard Decimal
rounding without a signed Int64 or binary64 intermediate. Empty nullable Sum is
zero; empty nullable Average is null; required empty Average is CQL_EMPTY_SEQUENCE.

Fresh provider unit suites pass **SQLite72 /Mongo539 /Neo520**, including one
new public-profile/borrowed-plan control per provider. All three actual profiles
still reject unsigned numeric execution, while existing UInt64 Min/Max stays
available. Evidence: CoreCurrent/uint64-numeric-core-{red,green}.trx and each
provider's `uint64-numeric-admission-unit.trx`; logs are under
TestResults/CosmicCqlDemo/uint64-numeric-*. Native scalar/grouped limb generation,
average kernels, native boundary certification and transport/demos remain next.

## Public intent and result domains

Core Sum accepts exact Int32/Int64/UInt64 selectors, including nullable forms. Current native profiles enroll only Int32/Int64; UInt64 native arithmetic remains pending. Its result retains that input CLR domain and is checked against its final mathematical range. Core Average accepts the same input domains and returns Decimal (nullable Decimal for nullable input). This deliberately differs from Enumerable's binary64 integer averages: CQL does not round large Int64 data through Double. The source query, actor, provider scope, relationships, ordering/window and consistency request are retained unchanged. Unsupported types/conversions fail during construction; row/count/Min/Max permission cannot grant numeric aggregation.

```csharp
var sum = source.Sum(
    holon => CosmicQueryMetadata.Value<long?>(holon, "balance"));

var average = source.Average(
    holon => CosmicQueryMetadata.Value<long?>(holon, "balance"));
```

These intents use the existing scalar planner, actual-adapter readmission, HolonManager executor and V2 attempt/receipt owner. Actual provider support is required before dispatch. HTTP and native support are not implied by successful intent construction.

## Empty inputs, precision and errors

Required missing/null values and wrong participating domains are invalid. Nullable missing/null values are ignored. Empty/all-null Sum returns zero, including nullable Sum; empty required Average reports CQL_EMPTY_SEQUENCE, and empty nullable Average returns null. Sum is the mathematical total: intermediate Int64 overflow cannot reject a final in-range result after cancellation. Final out-of-domain Sum reports CQL_SCALAR_OVERFLOW. Negative sums and averages are valid.

Average reconstructs its exact integer numerator first, then uses standard Decimal division and round-to-even under the Decimal 96-bit coefficient/maximum-scale-28 contract. Native binary64 accumulation is forbidden. The explicit participation work bound is 2,147,483,647 values. Exceeding it reports CQL_QUERY_LIMIT, never a truncated success. This is an accumulator work limit, independent of root-row/graph output limits.

## Native accumulator packet

For every admitted integer x, split by signed truncation toward zero:

```
high = x / 4294967296
low  = x - high * 4294967296
```

Native owners aggregate high, low and participating count, alongside all-input invalid-domain counters and protected traversal outcomes. With the work bound, both limb sums fit signed Int64. Core reconstructs only the constant-size packet with BigInteger; it never receives input rows. The full numerator is below 2^94, so conversion to Decimal's 96-bit integer coefficient is exact before division. Packet count/range contradictions error before publication. Native callers must validate every eligible input and cannot clamp or truncate count to satisfy the bound.

Windows precede selected-domain validation; mandatory ownership/traversal validation remains outside the window. Keep existing native cancellation/deadline/snapshot/no-spill owners. Provider numeric-string coercion, floating aggregation and silent fallback scans are prohibited. Decimal128 intermediates are admissible only with a proved exact bound and checked packet conversion; they are not permission to round arbitrary full totals.

## Evidence and remaining gates

Both Core target builds (netstandard2.1/net10.0) and the full 2,141-test suite pass with zero skips. The 24 new controls cover source/access/result domains, empty/null semantics, Int32/Int64 limits and final overflow, intermediate cancellation, exact averages beyond2^53, averages whose sum exceeds Int64, thirds/negative fractions/ties-to-even, maximum-work-bound numerators, impossible packets and borrowed read/operation permissions.

Evidence: TestResults/CoreCurrent/cql-numeric-foundation-current.trx and TestResults/CosmicCqlDemo/numeric-core-final.log. The JSON reference vectors under Core.UnitTests/Fixtures/CqlNumericAggregateVectors.json are machine-readable expectations for subsequent native enrollment, not native certification. Native SQLite acceptance is recorded below; MongoDB/Neo4j reduction, further fault/stress controls, public Decimal wire encoding, SDK/HTTP demos, grouping and later roadmap gates remain open.


## SQLite native Sum/Average verified — 23 October 2026

SQLite now advertises Sum/Average for Core's exact Int32/Int64 and nullable domains. It reuses the projection operand guard and scalar read owner. One native ordered/windowed CTE reduces signed high/low integer limbs, invalid domains and participating count; Core normalizes only that constant-size packet. Every participating value is validated, so a numeric string cannot coerce into a successful total. Sum checks the final mathematical range; Average remains exact through its integer numerator and Decimal division, including cases where an Int64 sum would overflow.

A separate native input-work sentinel allows at most 2^31 selected rows to enter the limb reduction (the permitted bound plus one detection row). Both limb sums still fit Int64 at that sentinel. Exceeding the bound returns CQL_QUERY_LIMIT, never a truncated success. Missing/null optional rows count toward native work even though they do not participate in the arithmetic. The root-row/graph publication caps do not limit numeric input. Existing protected traversal validation, snapshot/deadline/cancellation and caller connection/transaction ownership remain unchanged.

Evidence: genuine capability-red18/18 failures, then18/18 native green, then expanded1,344/1,345. The single failure was a test using137 membership IDs beyond CQL's existing membership bound; the corrected prefix predicate now reaches the intended aggregate. The final fresh **full SQLite suite passes 1,345 tests, zero skips (2m20s)**, including all **27 new numeric cases** (five primary, 13 shared reference vectors, nine boundary controls). Coverage includes intermediate cancellation, final Int32/Int64 overflow, averages beyond2^53/over an overflowing sum, empty/null/wrong domains, windows, visibility before overflow,137 inputs under graph-output cap 1, canonical type corruption, actual-scope cancellation/reuse, acknowledged concurrent writer/snapshot, borrowed transaction retention and traversal errors outside paging.

Authoritative reports: TestResults/SQLiteCurrent/cql-numeric-red-current.trx, cql-numeric-green-current.trx, cql-numeric-expanded-current.trx and cql-numeric-full-current.trx. Compact logs and matching producer/consumer hashes are in TestResults/CosmicCqlDemo/sqlite-numeric-*.log and sqlite-numeric-artifacts.json. The fixed DB/wal/shm paths are absent after verified teardown. No runtime, environment or download was added.

MongoDB and Neo4j numeric execution, numeric HTTP/Decimal wire encoding and demonstrations remain required. GroupBy, broader scalar domains and the remaining CQL/ORM roadmap remain open. SQLite's native gate does not imply all-provider numeric support or overall completion.


## MongoDB native Sum/Average verified — 23 October 2026

The Mongo compiler reduces exact integer inputs into signed high/low limbs and count within the existing scalar read owner. It shares the ordered/windowed projection guards and protected traversal envelope. Decimal128 is used only for bounded exact arithmetic: each input integer converts exactly; division by 2^32 cannot round across a truncation boundary at 34 significant digits; integral limb products, differences and bounded sums are exact. No binary64 accumulator or entity hydration is involved.

The decoder requires exactly five fields, integral count fields and finite Decimal128 limbs. Each limb must compare equal to the reconstructed Int64 in the original Decimal128 domain, preventing Decimal conversion from silently rounding a tiny fraction into an accepted integer. The same input-work sentinel, final overflow and empty/null Core contracts apply.

Before capability enrolment, the genuine native baseline passed all 124 prior cases and rejected all 13 new reference vectors with CQL_CAPABILITY_UNSUPPORTED (137 total, zero skips). Enrolled execution passed 137 native cases and 245 provider unit cases, zero skips. The final expanded native suite passes144 cases, zero skips (3m26s), including all 20 new numeric cases. Seven additional controls cover non-hydration/window order, nullable missing values, numeric-string rejection,137 inputs under graph-output cap 1, traversal failure outside paging, snapshot consistency across reparenting and cancellation during the actual aggregate followed by owner reuse. Eight new decoder unit cases reject malformed, fractional, nonfinite and binary64 packets.

Evidence: TestResults/SubscriptionUsageCurrent/cql-numeric-{red,green,final}-current.trx; TestResults/MongoCurrent/mongo-numeric-unit-current.trx; TestResults/CosmicCqlDemo/mongo-numeric-{red,green,final}.log and mongo-numeric-artifacts.json. Core/Shared/Mongo producer/native/unit hashes match. Owned port 27029 is stopped after the final runner; no environment/download/restore was added. Neo4j numeric enrollment, numeric HTTP/Decimal wire and demos remain open, together with the other roadmap gates.


## Neo4j native Sum/Average verified — 23 October 2026

Neo4j now reduces signed integer quotient/remainder limbs using the existing scalar transaction, read budget and cursor owner. The ordered source window precedes selected-domain checks; mandatory ownership and traversal failures remain outside both pagination and reduction. There is no entity hydration or floating-point total. A work sentinel rejects excess input instead of publishing a truncated sum; nullable missing values still count toward work. The strict seven-field packet requires Int64 limbs and consistent participation counts before Core normalization. Numeric publication occurs after cursor completion and transaction acknowledgement.

The genuine baseline passed all 111 prior native cases and rejected all 13 new numeric vectors as unsupported. After enrollment, all 124 native cases and 331 provider unit cases passed without skips. Three new packet controls reject floating-point fields, additional fields, impossible participation and excess work. The final expanded suite passes **131 native tests, zero skips (3m14s)**, including all 20 new native cases. Seven boundary controls verify non-hydration/windowing, nullable missing values, numeric-string rejection, 137 inputs under graph-output cap 1, traversal failure outside pagination, cancellation after actual fetch and deadline accounting through actual cursor delivery followed by owner reuse. The retained container is stopped and Bolt port 17687 has no listener. Core and MongoDB/SQLite numerical gates remain independently verified. Numeric HTTP/Decimal wire, demos, grouping and the remaining roadmap gates are open.

The operator contract is described in the [Neo4j mathematical operators manual](https://neo4j.com/docs/cypher-manual/current/expressions/mathematical-operators/). Exact signed extrema, cancellation of an intermediate overflowing total, fractional averages and values beyond binary64 precision are verified by the shared reference vectors rather than inferred from operator availability.

Evidence: TestResults/Neo4jCurrent/cql-numeric-{red,green,final}-current.trx and neo-numeric-unit-current.trx; TestResults/CosmicCqlDemo/neo-numeric-{red,green,final}.log and neo-numeric-artifacts.json. Core/Shared/Neo producer/native/unit assembly hashes match. No new runtime, environment, restore or download. Neo remains read committed; strict blocked-I/O interruption and its UTC DateTime read capability remain separate open gates.


## Numeric HTTP transport — unit acceptance verified, native acceptance pending

The existing scalar request now accepts Sum/Average, retaining one typed selection and the existing authenticated actor, provider admission, manager and receipt owner. Sum retains its Int32/Int64 wire domain; Average carries valueType Decimal and an invariant decimal string (or typed null), so browser clients do not round its value through Number. Selectors remain exact Int32/Int64 or nullable input types. All 14 new endpoint controls pass: signed Int64 extremes, all four Average input bindings, nullable empty averages, non-integer selector rejection and required selection presence. The genuine six-case red baseline rejected numeric operations before implementation; the final full suite passes **2,155 tests, zero skips**. These tests exercise the shipping endpoint with controlled adapters and receipt admission; live three-provider HTTP/demo acceptance is still required. Native three-provider execution remains separately verified above.

Endpoint evidence: TestResults/CoreCurrent/cql-numeric-http-{red,green,final}-current.trx and TestResults/CosmicCqlDemo/numeric-http-{red,green,final}.log. Core production source was unchanged; bounded selection binding uses the four public Average overloads without reflection or executing a selector delegate. Decimal wire support does not grant decimal input/query capability.


## Numeric SDK/HTTP demos verified — 23 October 2026

The fresh joint acceptance run passes **195 native regression tests, zero skips**, plus **42 SDK queries and 42 signed HTTP examples** across MongoDB, SQLite and Neo4j. Examples13–14 demonstrate an exact signed Int64 Sum and an ordered two-row Average returning Decimal text `-9007199254740990.5`. Browser presets and Swagger match the spaced/commented C# examples. The actual run recorded **222 settled operations and 217 physical attempts**; no count was inferred from earlier reports. Full endpoint unit acceptance remains **2,155 tests, zero skips**.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx; TestResults/CosmicCqlDemo/numeric-demo-native.log, v1-demo-result.json, v1-walkthrough.txt and numeric-demo-artifacts.json. Core, Shared and all three provider producer/native-consumer hashes agree. The retained Neo container is exited, Mongo/Bolt/demo ports have no listeners, and the readiness marker and fixed SQLite fixture are absent after acknowledged cleanup. No runtime/environment/download/restore was added.

Run `Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1 -IncludeRegression` for the same bounded acceptance or `-Serve` for the browser demo. Numeric transport and these integer-domain demos are now verified. Decimal/double/UInt64 selector expansion, GroupBy, composition/federation, strict blocked-I/O interruption, Neo UTC DateTime reads and all other unchecked roadmap gates remain open.


## Shared adapter packet normalization

The public operation/input-domain FromParts overload and typed scalar overload now share one exact normalization body, enabling native grouped Sum/Average without a provider-local copy of overflow/rounding rules. Full Core2,252 passes unskipped, including13 shared JSON vectors through the adapter-facing API. Permission and native scope validation still precede arithmetic normalization. This does not advertise native grouping. [Grouping prerequisite/evidence](CQL_GROUPING_CONTRACT.md#native-grouped-numeric-prerequisite--23-october-2026).

## Exact grouped Average filtering and ordering: SQLite implementation

The SQLite grouped translator now compares the **published Decimal**, using the existing Core `CosmicNumericScalarResults.FromParts` owner. A query-scoped SQL function receives only the constant-size signed high/low/count packet and declared input domain. It normalizes that packet with the same Decimal.Divide/round-to-even operation used for returned values. This runs inside SQLite statement execution; it does not load holons or filter/sort materialized result rows in the client.

The normalized Decimal is encoded on a common 10^-28 integer grid. A 57-digit fixed-width magnitude plus sign/complement prefix preserves exact order under BINARY collation, including negative values, signed-zero equality, different Decimal scales, large integer parts and tiny fractions. Literals use the identical encoding. Nullable empty means stay SQL NULL. Average-to-Average selected-field comparisons use the same Decimal keys. The shared expression contract still rejects mixed underlying CLR field domains, such as Decimal Average versus Int64 Sum; that broader capture/admission requirement remains open.

The functions are installed on the exclusively owned query connection and removed before releasing it. They check the existing read budget, preserve typed numeric/cancellation/deadline failures, and the read owner checks those failures before publishing any proof. Nonmatching/off-page groups remain in the complete bounded proof. Source Decimal metadata support is not implicitly granted.

Native SQLite verification passed:1,409 integration tests, zero failures/skips. MongoDB and Neo4j Average filtering/ordering remain capability-denied until their native arithmetic can match normalized Decimal rounding; comparing unrounded ratios or binary64 would violate this contract.

### Remaining remote-native Decimal normalization requirements

MongoDB and Neo4j cannot invoke the embedded SQLite callback. Their implementation must reproduce Decimal normalization before membership/rank/order, while keeping the existing Core normalizer as the reference contract. A bounded arithmetic kernel can use base10^9 limbs: a carry below2^31 multiplied by10^9 remains within signed Int64, allowing exact long division of the existing sub-2^94 numerator by its bounded count. Fractional extension is limited to28 digits and Decimal's96-bit coefficient limit; round-to-even must account for remainder and coefficient overflow (including reducing scale before rounding when necessary). Ordering must compare normalized values, not unrounded ratios. Fixed decimal-grid keys can then retain the existing complete-proof contract.

Acceptance must include: both signs and zero; all four integer input declarations; nullable empty groups; unequal denominators; repeating fractions; exact halfway cases with even/odd retained coefficients; coefficient-overflow rounding; adjacent Int64 values beyond2^53; numerators exceeding Int64; membership/field predicates/order/page agreement; malformed/excluded/off-page proofs; cancellation/work/byte limits. Decimal128 division alone is insufficient proof: an intermediate rounding near a final midpoint can disagree with Decimal.Divide. Do not advertise these remote capabilities until native results match the reference and actual-adapter admission tests pass. This paragraph is a design/acceptance requirement, not implemented or verified remote support.


SQLite normalized Average checkpoint —24October2026: actual90537terminal0,1,409 full native integration tests PASS0skip. Eight new cases cover all4integer input declarations, nullable membership and Average-field comparisons, negative/full-range values, overflow-sized numerators, exact repeating fractions, matching pages and2,048-input even/odd halfway rounding. Static negative controls retain separate capability permission checks; mixed underlying CLR fields remain rejected by shared capture. TestResults/SQLiteCurrent/average-final-current.trx and average-artifacts.json, plus CosmicCqlDemo/sqlite-average-final-{build,consumer-build,native-tests}.log retain final evidence. Exact Core4C58BC9D and SQLiteB4163A9E producer/consumer pairs match. Earlier16532test-intentred is retained separately. No new full remote-provider or HTTP Average gate is claimed; MongoDB/Neo4j remain unenrolled.

## MongoDB normalized grouped Average — native verification passed

The new aggregation kernel uses Decimal128 only for bounded **integer** arithmetic and for the final exactly representable Decimal result. It reconstructs the at-most29-digit numerator from signed limbs, extracts an integral quotient and remainder, then uses a28-iteration reduce to extend the coefficient one digit at a time. It stops at Decimal's96-bit coefficient limit or scale28, rounds remainder ties to even, and returns to the prior unrounded state if a rounding carry requires a lower scale. The final coefficient divided by its power of10 is a finite Decimal with at most29 significant digits, exactly representable in Decimal128.

The quotient extraction bound matters: count is at most2^31-1; the initial integer quotient has at most19 digits and each later quotient is a single digit. A nonintegral quotient is at least1/count away from an integer, much farther than Decimal128 rounding error at those magnitudes. Truncation therefore yields the exact integer quotient. All coefficient products have at most30 digits and all remainder operations are exact. This does not assume an unrestricted Decimal128 fractional quotient is equivalent to Decimal.Divide.

Normalized means are computed once per selected Average slot and reused by native filters, field comparisons, membership and result ordering. Private normalized fields are removed from the returned complete proof; the existing Core decoder independently normalizes the original limb/count packet and validates ordering/membership. Nullable empty means return null. Source Decimal capabilities are not broadened.

Native acceptance passed:524 packet projections compared with the Core reference (including signed repeating fractions, count limits, Int64 endpoints, midpoint-even/odd cases, and coefficient-overflow rounding), plus grouped end-to-end filters/order/pages and all four integer input declarations. Final matched provider units:427 passed, zero skips. The complete native CQL suite passes191 cases, zero skips; see the checkpoint below.

## MongoDB exact Average checkpoint — 24 October 2026

Actual test session62749 terminated successfully: **191 native CQL tests and427 provider unit tests passed, zero failures/skips**. Eight new native cases include524 packet-level comparisons against Core, count and Int64 limits, both signs, repeating fractions, halfway-even/odd rounding, coefficient-overflow rounding, all four integer input declarations, nullable membership/field comparisons, exact ordering and matching-group pages. Native normalization does not use server JavaScript or a binary64 intermediate.

Evidence: `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`, `TestResults/MongoCurrent/average-final-unit-current.trx` and `average-artifacts.json`; logs under `TestResults/CosmicCqlDemo/mongo-average-{native,final-unit}.log`. Actual producer/native/unit copies match Core SHA256 `4C58BC9D...`, Shared `D6AFCA32...` and Mongo `CB31EA51...`; the artifact report contains exact paths, sizes and full hashes. The runner stopped owned27029 and acknowledged fixture teardown; retained runtime/data remain reusable. This certifies native Average comparisons for MongoDB. New live HTTP Average examples and Neo4j support remain outstanding; it is not full CQL/ORM completion.

Shared Decimal order-key owner —24October2026: Core CosmicNumericScalarResults.DecimalOrderKey now owns the57-digit common decimal-grid/sign-complement encoding previously private to SQLite. Both Core targets build; full2392unit tests pass0skip, including3new controls for scale/signedzero identity, full-range/subunit ordering and4096 deterministic mixed-scale samples. PrivateCore574af92d1f85ba13a62c4edb7b82801796b0effd; exact DLLpaths/bytes/hashes in TestResults/CosmicCurrent/decimal-key-artifacts.json. Net10ABD06B39/unitcopy match; netstandard2C0FECD5. Provider migrations and Neo4j native key generation are pending separate verification; older native provider cohorts are not relabeled.

## Neo4j normalized Average — native verification passed

Neo4j's kernel uses Int64 operations throughout. It reuses the signed-limb floor/borrow normalization used for exact Sum, converts the positive96-bit magnitude into four base10^9 limbs, and divides by the bounded participating count. Binary-to-decimal conversion intermediates are below 10^9×2^32; division intermediates are below 2^31×10^9, both within Int64. Fractional extension is bounded to28 digits, compares the coefficient against the exact96-bit maximum, and rounds ties to even with a prior-state retry at lower scale if the rounded coefficient overflows.

The final value is encoded on the same10^-28 grid as Core.DecimalOrderKey, using a fixed-width sign/complement string. Native filters, membership, Average-field comparisons and ordering use those keys; published values still come from the original packet through the independent Core normalizer. Keys remain private pipeline values. The runtime uses no floating point, server extension or additional installation.

Native acceptance passed:524 packet-key comparisons with Core plus eight added end-to-end cases in the172-case native CQL suite. Both Core targets/2392units and the eight SQLite shared-key migration controls pass; these are distinct evidence scopes; the final Neo4j gate is recorded below.

## Neo4j exact Average checkpoint — 24 October 2026

Actual session68272 terminated successfully: **172 full native CQL tests and352 provider unit tests passed, zero failures/skips**. Eight added native cases include524 packet-key comparisons with Core, positive/negative repeating fractions, Int64/count limits, halfway-even/odd rounding, coefficient-overflow rounding, all four integer inputs, nullable membership/field comparisons and matching-group pages. Production Cypher uses only bounded Int64 limb arithmetic; it requires no extension or floating-point conversion.

Core, Shared, MongoDB, SQLite and Neo4j C# source hashes remained unchanged throughout the gate. Actual producer/native/unit copies match Core `ABD06B39...`, Shared `7A2DDFA5...` and Neo4j `C0C15771...`. Exact paths, sizes and hashes are retained in `TestResults/Neo4jCurrent/average-artifacts.json` and `average-source-hashes.json`. Native evidence: `neo4j-cql-native-current.trx`, `average-final-unit-current.trx` and `TestResults/CosmicCqlDemo/neo-average-{native,final-unit-tests}.log`.

SQLite's shared-key migration also passed eight focused Average cases (session12470); its tested consumer hashes are recorded separately in `TestResults/SQLiteCurrent/decimal-key-average-artifacts.json`. That focused gate and the earlier1,409-case SQLite regression are distinct cohorts. Exact Average result filtering/ordering is now native-verified on all three providers. A new live HTTP/SDK Average demo and the broader roadmap remain outstanding. No full CQL/ORM completion is claimed.

## Live grouped Average demo — 24 October 2026

Actual session70730 terminated successfully: **236 Neo4j/WEB4 native regression tests,57 SDK queries and57 HTTP examples passed, zero failures/skips**. The signed three-provider run recorded237 settled operations/232 provider attempts. Example19 compares the exact published Decimal mean with a two-bound range, orders it and takes a result page. Expected stock7/items2/mean`-9007199254740990.5` and Decimal schema tagging pass on MongoDB, SQLite and Neo4j. The browser preset has the same request grammar; script syntax and generated Swagger examples pass. This gate executes the annotated SDK/HTTP code and validates Swagger; it does not claim browser interaction was automated.

Core/Shared/provider/CQL wire/compiler/demo C# hashes remained unchanged during the run. Actual producer/native host DLL pairs match Core`ABD06B39...`, Shared`E78803F0...`, MongoDB`C022AAB5...`, SQLite`BDBC9D35...` and Neo4j`E1E4AE41...`. Exact paths, sizes and hashes: `TestResults/CosmicCqlDemo/group-average-demo-artifacts.json` and `group-average-demo-source-hashes.json`. Fresh result/transcript/log: `v1-demo-result.json`, `v1-walkthrough.txt`, `group-average-demo-native.log`; native report: `TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx`.

Readiness/graph markers are absent; owned27029/17687/5088 listeners are absent and the retained Neo4j image is stopped. The236-case gate is Neo4j/WEB4 plus joint live acceptance, not a rerun of the entire MongoDB191-case or SQLite1409-case native suites. Those earlier cohorts retain their recorded scope. Wider scalar domains, Neo4j DateTime, joins/composition/federation, authoritative policy and remaining roadmap certification remain open.


## MongoDB floating scalar milestone - 25 October 2026

MongoDB SDK scalar Sum/Average over required/nullable Single/Double is verified:36focused controls,454full native and560matched units, zero skips. Exact cancellation preserves a smallest subnormal; finite overflow reports CQL_SCALAR_OVERFLOW while a mean can remain finite. Original source scope/windows/access/consistency ownership remains. Mongo floating grouped/selected result arithmetic and HTTP are still unsupported. See CQL_FLOATING_AGGREGATE_CONTRACT.md.


## MongoDB grouped floating SDK acceptance - 25 October 2026

MongoDB now enrolls SDK scalar and grouped required/nullable Single/Double Sum/Average, including group selected filtering/order/pages.34new grouped controls and full490native/571matched units pass,0skip. Precision, nullable groups, mixed numeric slot alignment, privacy/windows, cancellation and off-page overflow are verified. Earlier scalar-only limitations are superseded at this native SDK checkpoint; HTTP and Neo4j floating arithmetic are still pending.


## Neo4j scalar floating SDK acceptance - 25 October 2026

All three providers now support native SDK required/nullable Single/Double scalar Sum/Average. Neo4j acceptance passes36focused cases,454full native CQL tests and541matched units,0skips. Grouped support exists on SQLite/MongoDB; Neo4j floating groups remain explicitly unadmitted through an independent aggregate profile. Source data domains, empty/nullable behavior, exact cancellation, overflow and work/access proofs remain mandatory. Floating HTTP and new live demos are pending.
