# CQL finite floating aggregate contract

25 October 2026. Shared normalization passes the full3,090-test Core suite and both Core targets
(netstandard2.1/net10.0); query capture/grouping also passes full3,098 tests. Native provider
translation/enrollment and HTTP/demos are not implemented by this component.
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
Next: actual SQLite/MongoDB/Neo4j native digits/reductions and final group
keys; provider source/privacy/window/consistency/interruption/error certification;
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

Remaining: integrate this component into both query compilers/read owners,
retain original source/access/traversal and complete raw proofs, certify native
scalar/grouped cases, then explicitly enroll provider capabilities. MongoDB,
Neo4j and HTTP floating arithmetic remain pending independently.
