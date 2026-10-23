# CQL integer Sum/Average contract

23 October 2026. Core foundation verified; no native adapter advertises these operations yet. Native SQLite, MongoDB and Neo4j execution, transport and demonstrations remain required. This is part of the full aggregate roadmap, not a replacement for decimal/double/UInt64 or GroupBy coverage.

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

Evidence: TestResults/CoreCurrent/cql-numeric-foundation-current.trx and TestResults/CosmicCqlDemo/numeric-core-final.log. The JSON reference vectors under Core.UnitTests/Fixtures/CqlNumericAggregateVectors.json are machine-readable expectations for subsequent native enrollment, not native certification. Native reduction, native fault/work-limit controls, public Decimal wire encoding, SDK/HTTP demos, grouping and later roadmap gates remain open.
