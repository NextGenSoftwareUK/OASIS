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
