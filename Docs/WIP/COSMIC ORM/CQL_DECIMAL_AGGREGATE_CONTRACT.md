# CQL Decimal aggregate contract

Native Decimal/nullable Sum/Average and live signed HTTP are certified on all
three priority stores. The latest demo passes120SDK/120HTTP queries, including
examples38-40; see [demo acceptance](CQL_DEMO_RUNBOOK.md). Dated implementation
checkpoints below retain their historical scope and restrictions at that time.

## Neo4j Decimal query integration - 25 October 2026

Scalar and grouped Decimal/nullable Sum/Average pass **23 focused native tests**,
with no failures or skips. Exact native group keys support filters, ordering and
paging; raw seven-word packets are validated before typed publication, including
off-page errors. Tests cover large cancellation, overflow versus valid means,
nullable/empty inputs, mixed packets, source windows, privacy, cancellation/reuse,
stored JSON precision, double-round boundaries and 137 inputs independent of graph
publication limits. Neo4j retains its declared provider-default read-committed
consistency; this does not introduce snapshot support.

Full acceptance passes **416 native /529 unit tests**, without failures or
skips. Decimal HTTP/demo enrollment remains open.

## MongoDB Decimal native acceptance - 25 October 2026

Native scalar and grouped Decimal/nullable Sum/Average are implemented. The
focused query gate passes **23/23**, and full MongoDB units pass **550/550**,
with no skips. The full **416/416 native CQL regression passes**, with no failures or skips.
This certifies native SDK arithmetic; HTTP Decimal inputs remain unenrolled.

Seven signed base1e9 sums preserve the exact scaled28 numerator. Grouped
comparison/order values use eight-word carry normalization, bounded long division
and discarded-digit/sticky tracking to round once to a legal Decimal coefficient.
Only the final at-most29-significant-digit coefficient is recombined into
Decimal128. Raw packet validation still rejects malformed participation and
out-of-range sums, including groups omitted by result paging. No entity scan,
binary floating conversion or server JavaScript is introduced.

The 23 native controls cover nullable/empty inputs, signs, large cancellation,
overflow versus valid means, mixed integer/Decimal groups, selected filters,
ordering/paging, source windows, privacy, cancellation/reuse,137 inputs independent
of graph publication limits, original stored precision and double-round boundaries.
Separate native component acceptance covers2 tests:2,114 scaled input vectors
and138 final-normalization packets. HTTP Decimal enrollment, live Decimal demos
and Neo4j Decimal arithmetic remain pending.

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
permissions and exact packet normalization are implemented. Native SQLite, MongoDB and Neo4j Decimal arithmetic are certified at the
checkpoints above. Metadata read/Min/Max permissions do not grant arithmetic.

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
SQLite/MongoDB profiles declare Int32/Int64/UInt64/Decimal and nullable forms;
Neo4j now declares the same explicit Decimal input domains. Borrowed
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
divide the exact multiple by1e9. Scaling/division by these powers of ten is exact
for the admitted coefficient/exponent range; the remainder-first form makes the
integer digit proof explicit. The earlier general warning about rounded division
does not apply to these power-of-ten steps. General average division still needs
exact rational normalization. Seven bounded limb sums remain exact. Native query
enrollment and result-key normalization require separate acceptance.

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

## MongoDB packet component acceptance and remaining integration

`DecimalScaledParts` now emits seven exact signed Decimal128 words. A native
component test covers2,114 CLR values and real server reduction; full539 unit
tests pass. No native Decimal query capability is advertised. The focused runner
mode is `-CosmicCqlDecimalKernel` (one test); a later full CQL run expects392 after
this added case. Earlier391-native evidence retains its original scope.

Implemented MongoDB grouped normalization algorithm:

1. Normalize seven signed limb sums right-to-left with nonnegative remainders
   and a floor carry. Prefix the final carry, forming eight words. The carry sign
   determines the total sign. For negative totals, negate the original seven
   sums and normalize again to obtain a nonnegative magnitude.
2. Divide the magnitude by count for Average (one for Sum), using base1e9 long
   division. Remainder*base+word fits Int64 under count<=2^31-1. Quotient words
   and the exact remainder must stay separate.
3. Start at scale28. If the rounded coefficient does not fit96bits, divide the
   unrounded quotient by10, tracking the most-significant discarded digit and
   whether any lower digit or original count remainder is nonzero. Try each
   lower scale without feeding a rounded candidate into the next attempt.
4. At the original scale, round by comparing twice the count remainder with the
   denominator. At lower scales use the discarded digit/sticky flag and even
   coefficient parity. Check the rounded coefficient against2^96-1.
5. Once the coefficient fits, recombine only that at-most29-significant-digit
   coefficient into Decimal128 and multiply by the exact power10^-scale. This
   final native Decimal128 is representable exactly and matches existing grouped
   numeric comparison/ordering domains. Never recombine the full66-digit scaled
   numerator into Decimal128.
6. Mathematical out-of-range Sum and malformed/work proofs must remain errors
   in the shared raw-packet decoder, including groups outside the result page.
   Empty nullable averages produce null. Source access, snapshot, traversal and
   deadlines remain under the existing read owner.

Scalar and grouped profiles share explicit numeric input permissions. Both native
paths and final-value/proof agreement now pass full416 native and550 unit gates.
The earlier component-only checkpoints above retain their historical scope.

## Neo4j native implementation in progress

The staged compiler decodes the existing sign/complement ordinal Decimal key
into seven signed base1e9 words. Eight-word carry normalization retains the full
scaled numerator. Long division and discarded-digit/sticky tracking use Int64
only; no source value or intermediate is converted to Float. Final values are
encoded back to the existing Decimal ordinal key for native comparisons.

Two parameter-only native tests pass without failures or skips: 2,112 signed
key inputs and 138 reduction packets. They cover all 29 scales, signed/zero keys, seeded
coefficients, cancellation of large sums, ties-to-even, scale reduction,
double-round boundaries, overflow and maximum participation. These tests create
no graph data. The first build caught and corrected two C# syntax errors before
this passing native gate. Full Neo4j units also pass 520/520 without skips.
Scalar/grouped source compilation, packet decoding,
query-level certification and explicit capability enrollment remain required.
No Neo4j Decimal arithmetic availability is claimed by this staged component.
