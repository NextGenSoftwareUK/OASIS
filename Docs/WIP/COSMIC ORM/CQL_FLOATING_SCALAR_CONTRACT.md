# CQL floating scalar implementation contract

24 October 2026. Core's finite bit-key codec/group capture passes the full
2,507-test suite and both configured targets, netstandard2.1/net10.0. SQLite
Double SDK metadata passes the full1,485-case native suite; Mongo/Neo Double
and all Single query enrollment remain open. Remaining native adapters, transport and
live examples remain open; the Decimal acceptance scope is unchanged.

## One finite value-key owner

`CosmicFloatingScalarResults` encodes finite binary64 values as16 uppercase hex
digits with numeric ordinal ordering. Negative values invert their bits;
nonnegative values flip the sign bit. Both zero encodings have one identity.
NaN/infinity cannot become keys. Decode rejects malformed, noncanonical,
nonfinite and negative-zero packets with `CQL_DATA_INVALID`.

Single uses the exact widened binary32 value in this same key space. Its decoder
requires exact binary32 representability; it cannot round a binary64-only packet,
overflow or underflow it. A stored representation must be normalized to its
declared finite type before producing a key. This codec alone does not admit a
storage type, authorize a query or prescribe client-side execution.

Six new Core facts exercise8,192 finite random bit patterns, extrema/subnormals,
ordinal ordering, zero identity, malformed/nonfinite keys and invalid Single
packets. Evidence: TestResults/CosmicCurrent/floating-codec-full-current.trx;
build/test log: TestResults/CosmicCqlDemo/floating-foundation-full.log.

## Storage prerequisite: Single must remain numeric and acknowledgement-safe

Mongo's metadata serializer handles Double but previously sent boxed Single
through complex JSON text. Six finite root/nested/array controls reproduce that
loss; three nonfinite controls already reject. BSON has no Single primitive.

The replacement must preserve BOTH binary32 round-trip identity and Core's
canonical JSON snapshot value used for save acknowledgements. Blindly widening
Single0.1 to Double changes its serialized numeric spelling from0.1 to
0.10000000149011612, violating the second invariant. The new acknowledgement
control reproduces this separately:9pass/1red. The repaired producer stores the
Single's canonical round-trip numeric spelling as BSON Double, preserving both
binary32 round-trip and snapshot value identity. Full493 Mongo units now pass;
the full231-case native gate including3 real storage round-trips also passes.
Both zero signs have one numeric identity;
the storage test normalizes zero before comparing binary32 bits.

Historical Single values already stored as strings stay strings. Do not infer or
silently migrate their type. Explicit migration intent and native evidence are
required. No legacy Hyperdrive code changes are part of this work.

Actual42737 terminal0:493 full Mongo units and231 full native CQL cases pass,
zero skips; source snapshots remain unchanged. The three new native storage
cases verify fractional/subnormal/maximum values through actual save/load,
numeric BSON and canonical snapshot comparison. No floating query is dispatched.
Evidence: SubscriptionUsageCurrent/floating-storage-full-current.trx and
mongo-cql-native-current.trx; CosmicCurrent/floating-foundation-identities.json;
CosmicCqlDemo/floating-storage-native-final.log. Native fixtures cleaned and
owned service stopped; no new environment/download/restore was needed.

## Remaining enrollment gates

### SQLite Double SDK slice verified

The prepared adapter interprets JSON numeric tokens as finite IEEE binary64,
including the rounding inherent in an explicitly Double-valued query. This does
not change integer or Decimal query semantics. Nonzero underflow cannot become a
matching zero; overflow and foreign JSON types remain outside the value domain.

`cosmic_cql_double_key` runs inside SQLite's predicate/order/reduction pipeline,
using the original token and the shared Core16-character key. The same connection
read-budget owner registers/unregisters it and checks cancellation, deadline and
the4,096-character scalar-token limit before publication, even for excluded rows.
Projection, extrema and grouping decode through Core's finite-key owner. There
is no hydrated-row filtering or REAL-based substitute path.

Core now captures Double/nullable Double group keys without granting a provider
capability. The first gates pass2,507 full Core tests and26 focused native Double
cases. Actual40902 terminal0 also passes fullSQLite1,485 regression,0fail/skip;
Mongo/Neo Double, all Single query paths and floating transport/live demos remain
open. Source snapshots are unchanged; native/producer pairs match Core57A3770B,
Shared6D182110 andSQLite02B743C3. Evidence:
CosmicCurrent/double-sqlite-core-current.trx, SQLiteCurrent/double-focused-current.trx,
SQLiteCurrent/double-full-current.trx and double-identities-current.json;
CosmicCqlDemo/double-sqlite-full.log. Owned fixed DB/WAL/SHM are absent after
verified teardown. No installation or restore was required.

- Define and verify the finite persisted value domain on all three stores,
  including binary32 normalization, native comparisons/order and typed decoding.
- Cover null/membership/field comparisons, extrema/grouping, wrong domains,
  boundaries and native budget/cancellation ownership.
- Expose certified types additively through WEB4 with exact round-trip strings;
  preserve enum identities and the existing v1 surface.
- Run matched provider/native/HTTP tests and update annotated demos/docs. Do not
  infer completion or enable capabilities from this foundation checkpoint.

## Mongo Double SDK enrollment verified — 24 October 2026

The native operand owner normalizes BSON int/long/Decimal128/Double to the
explicitly requested finite IEEE binary64 domain before predicates, ordering,
projection, grouping or extrema. Conversion failure and nonzero underflow use an
invalid object state, preserving the distinction from nullable null/missing.
Non-numeric BSON never becomes numeric by parsing strings. Both signed zeros
publish canonical positive zero. Existing exact integer/Decimal paths are unchanged.

Double comparisons, membership and field comparisons share that normalized
operand. Detached publication requires finite native BSON Double. Sum/Average
input domains remain Int32/Int64; this does not enroll floating arithmetic.
Native tests cover predicate families, binary64 boundaries/subnormals, adjacent
values, nullable groups and malformed/nonfinite/underflow/overflow rejection.
Actual79326 terminal0: full254 native CQL and493 unit tests pass, zero failures/skips. The native cohort includes23 new Double cases. Evidence: TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx and double-full-current.trx; TestResults/CosmicCqlDemo/mongo-double-native.log. The reusable runner stopped its owned Mongo service and bounded the11,095,999-byte raw log to65,103 bytes. No new runtime/download/restore was needed. This gate did not capture producer/consumer identity hashes inside the lease; a matched-artifact claim is deferred to final certification. Neo Double, all Single query paths and floating HTTP/demos remain open.

## Neo4j Double SDK enrollment verified — 24 October 2026

The schema3 query projection keeps native Int64 integers and original JSON
number lexemes. A shared Cypher operand admits only those numeric kinds,
normalizes through native binary64 conversion, rejects non-finite/overflow and
nonzero underflow, and canonicalizes signed zero. Numeric-looking stored strings
remain outside this domain. Predicates, field comparisons, membership, ordering,
selected values, extrema and grouped keys reuse the operand. Detached packets
require finite native Double; they cannot supply strings, integers or Decimal
as proof of an already-normalized selected result. Exact integer/Decimal
queries retain their existing domains; floating Sum/Average is not enrolled.

Prepared native controls cover adjacent values, nullable grouping/extrema,
field comparison, invalid domains, maximum/subnormal values and explicit
Int64-to-Double rounding. Actual78290 terminal0:232 full native CQL and457 units pass, zero failures/skips, including15 new native and2 new packet controls. Evidence: TestResults/Neo4jCurrent/{neo4j-cql-native-current.trx,double-full-current.trx,double-identities-current.json} and TestResults/CosmicCqlDemo/neo-double-native.log. The runner verified matching Core353753B9 and Shared86216993 across producer/consumer outputs before execution. Native consumer identities were captured inside the lease; no source-before/after snapshot was captured in this batch. The graph fixture journal and Bolt17687 listener are absent after teardown. All Single query capabilities and floating HTTP/demo enrollment remain open.

## Double HTTP contract — linked-unit verification

The additive Double scalar enum identity is7; previous identities0–6 remain
unchanged. Source filters, selected/nullable metadata and grouped selected-alias
filters bind Double. Literal values must be canonical invariant finite .NET
binary64 round-trip strings,1–24 characters, verified by parse/re-encode
equality. Examples:0,0.1,5E-324,1.7976931348623157E+308. Noncanonical spelling,
rounding, underflow, overflow, NaN, infinity and JSON numeric tokens reject
before dispatch. Use value.ToString("R", CultureInfo.InvariantCulture) to encode
a finite SDK value; canonicalize signed zero to0.

Selected, extrema and grouped Double outputs use valueType Double and canonical
round-trip strings. Nonfinite provider packets cannot become successful JSON.
This shares the existing manager/admission/receipt and output owners; it grants
no Double Sum/Average input capability.

Actual2067 terminal0:full2,542 linked-unit tests pass0fail/skip, including35 new
wire/endpoint controls. Evidence: TestResults/CosmicCurrent/
double-wire-full-current.trx and double-wire-identities-current.json;
TestResults/CosmicCqlDemo/double-wire-full.log. The native HTTP unknown-type
control now uses NotAScalar so additive Double enrollment cannot invalidate it.
Live all-provider Double HTTP/demo acceptance remains pending; Single remains
unenrolled.

### Live Double demo finding — grouped ordering binder

First live run14358 reached Mongo HTTP example27 and rejected its selected
Double ordering with400 CQL_REQUEST_INVALID. Earlier wire controls verified
capture/filtering but omitted a complete grouped request with resultOrder.
The single typed ordering binder lacked Double/nullable Double cases; both are
now added, with full-request positive/nullable regressions. Verification pending
while the peer's scoped Core/Karma batch owns the next build cohort. Source26
passed; the live81/81 gate is not yet certified. Failed journal and outcomes are
preserved for digest-pinned reconciliation before any rerun.

### Double SDK/HTTP/demo acceptance verified — 24 October 2026

The missing Double/nullable Double grouped-ordering binder is repaired at its
single typed owner. Both complete grouped requests (two filters, count column,
typed order and output page) pass in full2,552 linked units; retained authoritative
report TestResults/OracleCurrent/karma-ack-total-final.trx includes these2 controls
and the peer's8 karma controls, alongside the earlier35 wire/endpoint cases.

Reviewed reconciliation21528 passes before rerun: exact journal SHAE788... and
native proof SHA8A604... match, all438owned records across3stores and207settled
operations/201attempts/201receipts verified; original rejected request has no
native attempt or receipt. No original request was replayed. The shared pinned
verifier preserves historical grouped recovery and dispatches only known digests.

Fresh53891 terminal0 passes81SDK queries/81signedHTTP examples on all3providers,
261settled operations/256attempts,0fail/skip. Examples26–27 select/group adjacent
binary64 readings1.0000000000000004 and1.0000000000000007 with distinct identity.
Swagger FiniteDouble and four offline Decimal/Double browser presets pass.
Standalone demo builds from the existing cache, no restore.

Source snapshots remain unchanged; all5 producer/native-consumer pairs match
inside the global lease. Evidence: CosmicCqlDemo/v1-demo-result.json,
v1-walkthrough.txt, v1-binary-identities.json, v1-source-identities.json,
double-demo-native.log, double-standalone-build.log and v1-decimal-ui-check.json;
Neo4jCurrent/neo4j-cql-v1-demo-current.trx and reviewed Double reconciliation
artifacts. Fixed SQLite DB/journal/readiness absent; owned Mongo/Bolt/demo
listeners absent after teardown. This certifies this Double demo slice, not a
new full combined provider regression or complete CQL. Single and broader
policy/join/federation/interruption/certification requirements remain open.

## SQLite Single SDK enrollment verified — 24 October 2026

SQLite's shared floating intrinsic now has distinct declared binary32/binary64
entry points under the same native read-budget/cancellation owner. Single
parses the original JSON numeric token directly with TryGetSingle, then widens
the resulting binary32 exactly for Core's common16-character order key.
Parsing Double then casting Single would double-round inputs just above a
binary32 halfway boundary; the prepared native test includes such an input.
Both types reject non-finite conversion and nonzero underflow-to-zero, while
signed/exponent zero canonicalizes to one key.

Single predicates, membership, field comparison, ordering, selected values,
extrema and grouped filters/keys use the typed operand; detached Single packets
must decode to an exactly representable finite binary32 value. Numeric token
limits and deferred cancellation/deadline classification remain shared.
No floating Sum/Average inputs or implicit Single/Double key-space mixing are
granted. Provider-specific unsupported controls use genuinely unenrolled enums.

All28 native cases pass: boundaries, nullable behavior, group identity,
wrong domains, token budget and direct binary32 rounding. Core captures nullable
Single keys without granting other providers support. The first gate exposed
a separate root cause: projection capability descriptions incorrectly shared
the32-column limit, while the complete primitive/nullable registry has33types.
The descriptor registry now has an independent128-domain bound; query columns
remain capped32. Two new Core cases verify independence and boundedness.

Actual40087 terminal0:both configured Core builds, full2,555 linked units,
focused28 and full1,513SQLite native tests pass0fail/skip. Sources unchanged
inside lease; Core1191F0FB (netstandard9C1767CC), Shared43C05C54 andSQLite25CB1EFE
match native copies; linked-unit Core copy also matches. Evidence:
CosmicCurrent/single-sqlite-full-current.trx; SQLiteCurrent/
single-focused-current.trx,single-full-current.trx,single-identities-current.json;
CosmicCqlDemo/single-sqlite-full.log. Owned fixed DB/WAL/SHM absent after teardown.
Core efad088d6e1d1fcdc0af2bfea0adf964280bfa78 published with paired parent pin.
Mongo/Neo Single and Single HTTP/demos remain open; this is not full CQL closure.

## Mongo Single binary64 normalization component verified — 24 October 2026

Native SingleFromNativeDouble uses exact power-of-two bin thresholds/spacing,
bounded254-bin selection and integer/fraction ties-to-even rounding. It avoids
decimal-place conversions of native binary64 input. Overflow-midpoint values
and nonzero underflow reject; signed zero canonicalizes. The component requires
actual BSON Double input and returns an invalid object state for foreign types.

A deterministic native vector test is prepared:8192finite binary64 patterns,
1024binary32 values with each exact midpoint and its adjacent binary64 neighbors,
plus overflow/subnormal/zero/nonfinite/foreign-type controls. It compares actual
Mongo aggregation results bit-for-bit against finite CLR binary32 conversion.
Vectors exist only as bounded aggregation literals, not persisted records.

This is one required normalization component, not Single query enrollment.
Int64/Decimal128 source domains need exact handling: a preliminary binary64 cast
can erase the side of a binary32 halfway threshold. Keep those requirements
open; no Single capability is granted from binary64-only kernel evidence.
Official Mongo arithmetic reference: [rounding and IEEE behavior](https://www.mongodb.com/docs/manual/reference/operator/aggregation/round/);
the implementation splits binary units explicitly instead of relying on
decimal-place round conversion. Actual19377 terminal0:full255native CQL and493Mongo units pass0fail/skip.
All12,313native vectors pass, bit-for-bit, including8192finite Double patterns
and1024Single neighborhoods/midpoints. Source snapshots unchanged; Core3E4F8E4F,
Shared9BF60883 andMongo9A0821EB match both native and unit copies inside lease.
Evidence: SubscriptionUsageCurrent/mongo-cql-native-current.trx,
single-kernel-full-current.trx,single-kernel-identities-current.json;
CosmicCqlDemo/mongo-single-kernel-full.log. Owned Mongo service stopped and
acknowledged fixture cleanup passed. No Single query profile is enrolled.
