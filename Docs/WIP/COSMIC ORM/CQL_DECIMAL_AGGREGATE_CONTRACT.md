# CQL Decimal aggregate contract

## SQLite native implementation - 25 October 2026

SQLite now enrolls native Decimal/nullable Sum/Average. The existing exact
Decimal key supplies57 scaled digits plus sign. Sign-dependent6-digit padding
forms seven9-digit words; negative complements are decoded before signed INTEGER
SUM. No source Decimal passes through SQLite REAL. Scalar and grouped reductions
share the same word expressions and Core packet normalizer.

Grouped filtering/order uses a nine-argument constant-packet SQLite function
(seven sums, count, operation/nullability mode). It normalizes the same published
Decimal value, including final rounding, and retains typed errors even when
result paging hides a group. Existing query budget/error/teardown ownership
covers the function. No entity hydration or manager-side input scan is involved.

Full1,702 native/72unit tests pass without failures/skips, including23new Decimal
controls. The final five cover privacy/cancellation/input-budget/rounding/original
JSON precision. Full results are recorded in implementation status. MongoDB/Neo4j native Decimal lowering
and live HTTP/demo enrollment remain pending. Published v1 numeric input domains
must not expand implicitly just because an SDK adapter gains a new capability.


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

## Remaining MongoDB/Neo4j lowering constraints

MongoDB must preserve the existing original-value Decimal metadata admission.
Multiplication of an admitted Decimal coefficient by10^28 can retain its at-most
29 significant digits in Decimal128, despite a large positive exponent. Extract
each base1e9 digit with exact remainder first, then subtract that remainder and
divide the exact multiple by1e9. Do not truncate a rounded Decimal128 quotient:
a near-integer division can cross its truncation boundary at large magnitudes.
Seven bounded limb sums remain exact. This is a design constraint, not a native
MongoDB implementation or acceptance claim.

Grouped filtering/order on MongoDB/Neo4j must normalize the full scaled packet;
recombining a66-digit numerator into Decimal128/Int64 is invalid. An eight-word
base1e9 carry-normalized magnitude can be divided by the bounded count with
Int64-safe remainder*base operations. Final coefficient scaling and tie-even
rounding must use original discarded digits/remainder, matching Core. Existing
integer average key kernels cannot simply receive the full scaled numerator.

Do not enroll Decimal in the shared scalar profile while grouped capabilities
reuse that profile unless both native paths are implemented and certified.
Explicit transport binding remains separately enrolled after native gates.

SQLite final gate:1,702 native/72 unit pass0fail/0skip,23new native controls,
including mixed integer/Decimal packet widths. Core3,048 and shipping WEB4 build
pass. The generic scalar/grouped HTTP binder now checks its own v1 numeric input
list before SDK capture. Decimal Sum cannot become public HTTP merely because
SQLite's SDK profile gains that domain. MongoDB/Neo4j and HTTP live gates remain
open. Evidence is retained in the resource ledger and implementation status.
