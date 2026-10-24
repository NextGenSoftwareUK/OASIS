# CQL UTC DateTime contract

24 October 2026. Shared CQL already defines UTC DateTime literals and detached values. SQLite and MongoDB retain their recorded native date domains; Neo4j native enrollment now passes192 CQL cases and354 provider unit tests.

## Representation and meaning

- Declare `DateTime` or `DateTime?` metadata paths explicitly. Literals must have `DateTimeKind.Utc`; Local/Unspecified literals fail capture rather than acquiring an assumed timezone.
- The Neo4j stored projection retains ISO strings with uppercase `T`/`Z` and zero to seven fractional digits. The query validates Gregorian calendar fields, including leap days, and normalizes to `yyyy-MM-ddTHH:mm:ss.fffffffZ` for comparison.
- No epoch-millisecond, binary64 or server-local-time conversion is used. Adjacent100ns values remain distinct. Equivalent fractional widths compare/group as one UTC value.
- Offset strings, lowercase `z`, out-of-range dates/times and fractions wider than seven digits do not enter this DateTime domain. DateTimeOffset and date arithmetic require separate declarations; they are not implicit conversions.

## SDK operations

Admitted metadata date paths support comparisons, membership, ordering, detached selection, Min/Max and grouped identity/extrema/result predicates. DateTime does not become a Sum/Average input. Bind literals before composing the expression:

```csharp
var start = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
var end = start.AddTicks(2);

var selected = CosmicQuery<Holon>.Create()
    .Where(holon => CosmicQueryMetadata.Value<DateTime>(holon, "stamp") >= start
        && CosmicQueryMetadata.Value<DateTime>(holon, "stamp") < end)
    .OrderBy(holon => CosmicQueryMetadata.Value<DateTime>(holon, "stamp"))
    .Select(holon => CosmicQueryMetadata.Value<DateTime>(holon, "stamp"));
```

Check the OASISResult, plan against actual provider capabilities, and execute through the shared manager owner as in the manual. Arbitrary date methods inside query expressions are not evaluated by the ORM.

## Invalid and optional data

Invalid persisted values fail selected date materialization with `CQL_DATA_INVALID`; source predicates exclude values outside their declared domain. Malformed strings must not cause a native substring/temporal exception or accidentally match after truncation. Required null/missing selected dates error. Nullable projected leaves map null/missing to null, while explicit metadata Exists/IsNull tests retain their existing missing-versus-null distinction. Result filtering/paging cannot hide an invalid participating grouped date.

## Storage and transport boundaries

Neo4j schema3 already stores the raw strings used by this translator; this change does not require a data migration or reindex solely for dates. Decoded dates use invariant exact parsing and retain UTC kind. Canonical holon date fields not already admitted are not implicitly enabled by metadata enrollment.

The current HTTP source-filter enum still exposes String/Integer/Boolean/Guid. DateTime selection already has its own explicit shape grammar, but source and selected-result DateTime predicate transport need an additive validated wire extension and live examples. That work remains open; SDK enrollment is not a claim of complete HTTP date parity.

## Verification

Verified Neo4j gate:192 native CQL cases, including20 new date cases for range/membership/order/projection, adjacent ticks, normalized grouped keys, full calendar extrema, field comparisons, optional leaves,14 malformed-value cases and excluded-group validation. Final354 provider unit tests pass, including UTC-only literal and driver-parameter controls. See the checkpoint below for exact cohort evidence.

## Neo4j UTC DateTime checkpoint — 24 October 2026

Actual session41795 terminated successfully: **192 full native CQL tests and354 provider unit tests passed, zero failures/skips**. Twenty new native cases cover range/membership/order/projection with adjacent100ns ticks, equivalent fractional-width grouping, nullable extrema, the complete DateTime calendar range and leap days, field comparisons, optional leaves, fourteen invalid-type/calendar/text cases, and grouped Take0 that cannot conceal invalid data. Two older DateTime-denial controls were retargeted to still-unenrolled Decimal, retaining the actual-provider admission invariant; their original red report is preserved.

Core/Shared/provider/native-test C# source hashes remained unchanged during the final gate. Actual producer/native/unit copies match Core`ABD06B39...`, Shared`944F93E9...` and Neo4j`2E1D6787...`. Exact paths, sizes and hashes: `TestResults/Neo4jCurrent/date-artifacts.json` and `date-final-source-hashes.json`. Reports: `neo4j-cql-native-current.trx`, `date-final-unit-current.trx`; logs: `TestResults/CosmicCqlDemo/neo-date-final-{native,unit}.log`. Initial86776 red190/2 is retained in `date-native-red-current.trx` and `neo-date-native.log`.

The owned graph marker and17687 listener are absent; the exact retained Neo4j container is stopped. This is native SDK DateTime support, not a new HTTP date-predicate/demo certification. The existing HTTP DateTime selection shape can use the new provider capability; source/selected-result date predicate parsing and examples remain next. Broader scalar, policy and roadmap requirements remain open. See [the UTC contract](CQL_UTC_DATETIME_CONTRACT.md).
