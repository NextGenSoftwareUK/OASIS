# CQL finite floating aggregate contract

25 October 2026. Shared normalization passes the full3,090-test Core suite and both Core targets
(netstandard2.1/net10.0); query capture/grouping also passes full3,098 tests. SQLite native SDK integration is now certified below. MongoDB/Neo4j native
translation/enrollment and floating HTTP/demos remain pending.
Existing floating metadata filters/projection/extrema remain separate features.

## Exact native reduction

Each admitted finite Single contributes signed base2^30 digits in units2^-149;
each Double uses units2^-1074. Packets contain ten or seventy most-significant-first
signed Int64 limb sums and a non-null participating count. At count<=Int32.MaxValue,
every limb sum fits Int64. Leading carries stay implicit in those exact sums.
Never collect source rows or substitute a client scan for native arithmetic.

The original persisted value must satisfy the provider's declared finite Single
or Double input contract before contributing digits. Counts, limbs, complete
numerator and one-input representability/canonical signs are validated in Core.
Native work/access/type proofs remain mandatory; packet bounds alone cannot
prove the validity or authorization of every individual source row.

## Final result

Sum and Average return the declared Single/Double type, including nullable forms.
Reduce the exact numerator and divide by participating count for Average. Round
once to nearest, ties-to-even, in the declared IEEE754 format. This is an exact
reduction contract, not a promise to reproduce sequential CLR accumulation order.

Subnormal results use the fixed minimum quantum. An underflowed mean rounds to
canonical positive zero, matching CQL's existing zero-key identity. A sum slightly
above the largest finite value may still round to that value; reaching the IEEE
rounding overflow threshold returns CQL_SCALAR_OVERFLOW, never infinity/NaN.
Average of bounded finite inputs stays bounded. Nullable empty Average is null;
required empty Average returns CQL_EMPTY_SEQUENCE; empty Sum is typed positive zero.

## Verification and remaining integration

Tests independently decompose IEEE fields and binary-search adjacent finite
representations using exact rational distances. They cover every exponent,
seeded bit patterns/reductions, cancellation, subnormal/normal boundaries,
halfway rounding, finite overflow thresholds, nullable/empty inputs, packet
shape/count/work/type violations and maximum participation. All11 new controls
pass without failures/skips within full3,090 acceptance. Evidence:
TestResults/CosmicCqlDemo/floating-aggregate-core.log and floating-aggregate-core.trx.

Capture/result typing, immutable grouped selection and explicit capability
declarations are now implemented. Legacy read permissions remain unchanged.
SQLite native integration is certified below. Remaining: MongoDB/Neo4j native
digits/reductions and final group keys, with provider source/privacy/window/
consistency/interruption/error certification;
explicit HTTP enrollment and signed three-provider demos. No adapter may advertise
floating Sum/Average merely because the shared normalizer exists.

## SQLite native packet component

The scoped native aggregate/key callbacks pass11 focused tests plus74 full
provider units. They run within SQLite execution under the existing read-budget
callback; no manager source-row scan or holon construction is used. Each active
group owns a distinct state; no reference seed is shared across groups/queries.
This bounds aggregate state per group, not all SQLite sort/working memory.

The ephemeral result BLOB is version1, type1(Single)/2(Double), then a signed
little-endian Int64 participating count and ten/seventy signed little-endian
Int64 sums: exactly90/570bytes. Shape/version/type/count are checked before fixed
allocation. Core must normalize every packet to validate limb and full-numerator
proofs, including groups outside the result page. It is not a persisted schema.

Required/nullable Sum/Average key modes are explicit. Typed errors are retained
across SQL filters; budget exceptions preserve their original cause. Registration
requires an open owned connection, and disposal is idempotent. Connections and
parameter datasets are in-memory in these tests and disposed after each case.

This component checkpoint preceded the completed SQLite query integration
and explicit capability enrollment described below. MongoDB, Neo4j and HTTP
floating arithmetic remain pending independently.

## SQLite native SDK enrollment

Scalar/grouped source compilation and packet publication are integrated. The
original source window, protected scope validation, access predicates, read
snapshot, cumulative budget and transaction owners are retained. SQL participation
and packet counts are checked independently. Scalars admit zero inputs; actual
groups require positive inputs. Every raw packet is normalized before typed DTO
construction, including groups outside a requested page.

All48 query controls pass, followed by the clean production runner:1,763native
integration/74unit tests, zero failures/skips, stable sources/sixmatching copies
and verified fixture teardown. They include both domains' boundaries, malformed
inputs, exact cancellation, overflow/valid means, nullable/empty inputs, source
windows, privacy,137inputs independent of graph publication limits, cancellation,
rounded result filtering, exact sum/key comparison, mixed packets and30aggregate
slots. No native REAL SUM or manager-side input scan is substituted.

The reusable command is Scripts/TestHosts/Run-CosmicSqliteAcceptance.ps1; it owns
the global build lease and excludes only the documented historical reconciliation
class. The earlier unfiltered report's two diagnostic lock refusals are preserved
separately; they are not claimed as a passing raw run. MongoDB/Neo4j native
arithmetic and explicit floating HTTP/demo enrollment remain open.


## MongoDB digit extraction — native component verified

A native expression component now extracts ten/70 signed base2^30 words from
an already admitted Single/Double. Each digit uses remainder/division by exact
powers of two before Int64 conversion; the top Double limb omits an unrepresentable
2^1026 modulus. No Decimal128 source conversion or full-value subnormal scaling
is used. This does not enroll floating aggregates. Two native test cases compare
every exponent, both signs and seeded finite values against independent IEEE
field decomposition. After restoring the removed original dependency cache, both
native cases pass:5,786vectors, zero failures/skips. Producer/native Core, Shared
and Mongo assembly hashes match. Runner-owned27029 is absent after teardown.
This is component proof only; scalar/grouped query integration, final result keys
and provider enrollment remain pending. Evidence: TestResults/CosmicCqlDemo/
mongo-floating-parts-evidence.json and SubscriptionUsageCurrent/
mongo-floating-parts-current.trx. Reusable runner mode:
`Run-ReusableUsageRecoveryTests.ps1 -CosmicCqlFloatingParts` under the build lease.


## MongoDB scalar Sum/Average — focused acceptance

The scalar compiler now consumes the admitted binary value without Decimal128
conversion, reduces ten/seventy native Int64 sums and preserves independent
input/invalid/present proofs. The reader requires the declared exact array width
and Int64 word types. Decimal, integer and floating packets cannot impersonate
one another. The shared Core owner validates the complete numerator/count domain
and rounds once to the declared required/nullable Single/Double result.

All36 focused native scalar controls and560 full provider units pass, zero skips.
The tests cover finite boundaries, oversized cancellation preserving a subnormal,
overflow versus a valid mean, halfway ties, both empty paths, malformed original
inputs, source windows, owner/public privacy,137inputs with a one-node graph
publication budget, cancellation/reuse and detached strict packet shapes.
The complete454native regression also passes, followed by560 matched full units;
six Core/Shared/Mongo producer/native/unit assembly copies match. Owned Mongo
27029 is absent after teardown. Exact reports/hashes are recorded in
TestResults/CosmicCqlDemo/mongo-floating-scalar-evidence.json.
This enrolls only SDK scalar arithmetic. Mongo floating grouped aggregates,
selected group comparisons/order and floating HTTP/demos remain pending.


## MongoDB grouped final-value component — native verification passed

The prepared native normalizer carries signed base2^30 sums into nonnegative
magnitude words, divides by participation for Average, and selects the exact
IEEE quantum from the integer quotient's bit length. It reconstructs only a
bounded prefix (at most82bits for Double), preserving discarded guard/sticky
bits and the exact mean remainder before ties-to-even rounding. Decimal128 is
used only for exact bounded integer carries/division/prefixes, never as a
conversion of source floating values or the complete large numerator.

The selected rounded coefficient and power of two produce an exact representable
native floating value; canonical zero is positive. Finite overflow/empty mean
use the component's null sentinel, to be checked with original raw proof packets
by the eventual query owner. Existing shared Core publication remains authoritative.
This component does not enroll grouped capabilities or replace off-page validation.

Two native kernel cases are prepared, covering every exponent, seeded mixed
reductions, cancellation, rounding ties, overflow and maximum participation.
Initial compile found one missing closing parenthesis, corrected before native
execution. Final gate21862 passes both cases:706Single/4,409Double packets,
5,115total; Sum and Average both match Core bit-for-bit. Three producer/native
Core/Shared/Mongo DLLpairs match. Owned27029 is absent after runner teardown.
Evidence: TestResults/CosmicCqlDemo/mongo-floating-kernel-evidence.json.
This is component acceptance, not a new full456native/560unit run. Grouped query
compilation, raw proof validation and explicit capability enrollment remain next.
