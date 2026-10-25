# CQL Decimal aggregate contract

25 October 2026. Core Decimal/nullable Sum and Average capture, explicit input
permissions and exact packet normalization are implemented. Native SQLite,
MongoDB and Neo4j Decimal arithmetic remains unenrolled pending translation and
certification. Metadata read/Min/Max permissions do not grant arithmetic.

## Exact reduction packet

Each input is represented in integer units of10^-28. Its absolute magnitude is
at most `(2^96-1)*10^28`. Split this exact integer into seven most-significant-first
base10^9 digits, applying the input sign to each digit. Native storage reduces
those seven columns independently and returns their signed Int64 sums plus the
non-null participating count. It never returns an input-row buffer.

At count<=2^31-1, each limb sum has magnitude<2^31*10^9 and fits Int64. Core
reconstructs the bounded packet using BigInteger. The full numerator can exceed
Decimal and Int64; cancellation and averages must not fail because an
intermediate exceeds those domains. The complete mathematical result is retained
until final normalization. Invalid count/shape/limb/range proofs fail explicitly.
The legacy integer two-limb decoder rejects Decimal intents.

## Results and rounding

- Sum outside the mathematical Decimal range fails with CQL_SCALAR_OVERFLOW.
- Within range, round once to the greatest legal scale0..28 whose rounded96-bit
  coefficient fits. Rounding is nearest, ties to even, using the original exact
  rational at every attempted scale. Never round an already rounded candidate.
- Average uses the exact scaled numerator divided by the participating count.
- Nullable Sum of no values is zero; nullable Average is null. Required Average
  of no values fails with CQL_EMPTY_SEQUENCE.
- Trailing zero coefficient digits are removed without changing the value.

This final-reduction contract deliberately avoids provider-specific intermediate
rounding/overflow and does not promise CLR's sequential Decimal.Sum evaluation
order. It must be identical across all enrolled native providers.

## Admission and rollout

Core scalar/grouped capture recognizes Decimal input. Native enrollment requires
an explicit Decimal input list and matching grouped result permissions. Existing
two-argument numeric permissions retain signed integer inputs; actual provider
profiles currently declare only Int32/Int64/UInt64 and nullable forms. Borrowed
caller claims cannot enroll Decimal on the actual adapter. HTTP Average binding
and live demos are separate gates after native acceptance.

Backend work must lower the exact persisted Decimal domain into these digits,
retain existing source/access/snapshot/traversal/deadline ownership, and normalize
grouped result filter/order keys with the same final rounding. Error/work proofs
must survive result paging, including Take(0). No float conversion, input scan in
Core or optional lossy path is permitted.

## Acceptance

Core controls cover required/nullable capture, grouped intent, smallest scale,
large cancellation, overflow versus a valid mean, tie-even boundaries, malformed
packets, empty semantics, maximum work and legacy capability isolation. Separate
native controls must cover all three stores, concurrency, privacy, interruption,
result budgets and later reuse before native profiles are enrolled.

Final Core acceptance:3,046 tests,0fail/0skip; both frameworks build. Thirty new
controls include2,048 deterministic independent CLR comparisons where sequential
addition is exact. Provider units72/539/520 pass, proving native Decimal arithmetic
is still unenrolled. Shared scalar/grouped Average range validation now has one
owner; six new scalar range contradictions reproduced the missing guard. Two
empty Decimal Sum facade cases reproduced integer-packet misdispatch and now use
the declared Decimal packet owner. These are Core guarantees, not native acceptance.
