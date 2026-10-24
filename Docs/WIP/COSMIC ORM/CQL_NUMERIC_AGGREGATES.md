# CQL integer Sum/Average contract

23 October 2026. Core foundation and SQLite native execution are verified. MongoDB/Neo4j execution, numeric transport and demonstrations remain required. This is part of the full aggregate roadmap, not a replacement for decimal/double/UInt64 or GroupBy coverage.

## Public intent and result domains

Sum accepts exact Int32/Int64 selectors, including nullable forms. Its result retains that input CLR domain and is checked against its final mathematical range. Average accepts the same input domains and returns Decimal (nullable Decimal for nullable input). This deliberately differs from Enumerable's binary64 integer averages: CQL does not round large Int64 data through Double. The source query, actor, provider scope, relationships, ordering/window and consistency request are retained unchanged. Unsupported types/conversions fail during construction; row/count/Min/Max permission cannot grant numeric aggregation.

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
