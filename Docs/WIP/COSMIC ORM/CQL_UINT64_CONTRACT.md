# CQL UInt64 metadata

HTTP numeric compiler update: Core3,017 and shipping WEB4 build pass after shared
UInt64/nullable Average binding. Sum wire values remain exact UInt64 strings;
Average is Decimal/string or null. Live signed numeric HTTP/demo acceptance is
still pending; see the current numeric contract and manual.


25October Neo4j numeric update: full391 native/520 unit pass0fail/0skip, including
19new unsigned numeric cases. All three stores now enroll native UInt64 Sum and
Average, nullable forms and grouped arithmetic. Unsigned numeric HTTP/demo
acceptance remains a separate in-progress gate.


25October numeric update: SQLite unsigned Sum/Average now passes1,679 native/72
unit; MongoDB passes391 native/539 unit (17new unsigned native controls), all zero
failures/skips. Both enroll exact scalar/grouped arithmetic through existing
generic owners. Core3,008 passes. Neo4j unsigned arithmetic and unsigned numeric
HTTP/demo enrollment remain pending; prior read/projection HTTP support is distinct.
See [numeric contract](CQL_NUMERIC_AGGREGATES.md).


## SQLite unsigned numeric implementation — 25 October 2026

SQLite now explicitly enrolls UInt64/nullable Sum and Average for native scalar
and grouped queries. Full Core3,008 / SQLite native1,679 / SQLite unit72 pass without failures/skips. This section supersedes the
earlier foundation-only status for SQLite. MongoDB/Neo4j numeric enrollment and
unsigned numeric HTTP/demo acceptance remain pending.

The existing canonical 20-digit UInt64 value is split into two bounded decimal
chunks, then converted to base-2^32 limbs using INTEGER-only SQL arithmetic.
Scalar reduction, grouped reduction and grouped comparisons share this owner;
values above Int64.MaxValue never pass through SQLite REAL. Grouped average sort
keys explicitly identify unsigned input modes. Shared grouped result validation
uses the declared unsigned domain (0..UInt64.MaxValue), preserving Decimal means.
Sum overflow remains an error even when the result page would hide the group.

New native cases cover boundary values, exact means, overflow, null/empty inputs,
source windows, invalid input domains, grouped filters/order/pages and comparison
against an unsigned group key. Full regression results are recorded in the
implementation status and cleanup ledger after terminal verification.


24 October2026. UInt64 native support is verified on SQLite (1,435 integration),
MongoDB (205 CQL/478 units) and Neo4j (205 CQL/453 units). The additive HTTP wire
passes2,474 Core controls, including20 unsigned cases. UInt64 live HTTP/demo
acceptance now passes69SDK/69HTTP examples. Historical checkpoints below retain their original scope.


## Numeric expansion status — 25 October

Core now captures UInt64/nullable Sum and Average and normalizes exact bounded
unsigned limb packets. Full Core3,006 and SQLite72/Mongo539/Neo520 unit suites
pass without skips. Actual native numeric capabilities remain restricted to
Int32/Int64; reading UInt64 metadata does not grant unsigned arithmetic. The
new explicit input-domain constructor and shared scalar/grouped admission make
that separation enforceable. Native unsigned scalar/grouped reduction and
HTTP/demo enrollment are still pending. See the
[numeric foundation](CQL_NUMERIC_AGGREGATES.md#uint64-core-foundation--25-october-2026).

## Core capture

UInt64 and nullable UInt64 retain their declared CLR path/key types. Literal
packets use the existing Decimal numeric family: all UInt64 integers fit Decimal
exactly, and no signed Int64 or binary64 conversion occurs. Literal values are
normalized to Decimal consistently. Lossless UInt64-to-Decimal conversions are
captured; conversions to binary32/binary64 and signed narrowing are rejected.
Integer conversion range checks use Decimal bounds to include the full UInt64
range. Grouping retains UInt64 identity through the existing detached proof owner.

## SQLite execution

SQLite json_each exposes numbers above Int64.MaxValue as REAL. The native CQL
operand instead reads the original JSON token through json_each.json/fullkey.
Only unsigned JSON integer tokens in0..18446744073709551615 are admitted. A
20-digit zero-padded binary-collated key provides exact comparison/order/group
identity. Literals use the same key. Native selected values decode with invariant
UInt64 parsing; malformed mandatory selections return CQL_DATA_INVALID.

This extends row comparisons, membership, field comparison, ordering, projection,
Min/Max and grouped keys/results through their existing owners. It introduces no
storage migration, entity scan, approximate cast or separate execution path.
Sum/Average remain their explicitly enrolled Int32/Int64 domains. No UInt64
numeric aggregate capability is inferred from the new metadata domain.

Fractional or out-of-range Decimal comparisons against UInt64 metadata remain
unenrolled and return CQL_CAPABILITY_UNSUPPORTED before SQL execution. They never
round or wrap into an integer. Actual provider capability checks remain mandatory.

## Evidence

Initial bounded gate51804 terminated0: both Core targets built, four capture
controls and11 native SQLite cases passed without skips. Full Core now passes
2,428 cases, zero failures/skips. A broad SQLite gate reached1,435 cases with
1,427 passes/eight failures: four new test expressions used forbidden captured
path arrays, and four independent search baselines belonged to the other session.
The test expressions now use explicit segments and the search owner repaired its
paths. The final unfiltered1,435-case gate now passes; exact evidence follows below.
The current fixed environment and cleanup ownership are recorded in
[the resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).


## Exact UInt64 Core/SQLite acceptance — 24 October 2026

Core9d5efa9 adds UInt64/nullable capture and grouped keys, with exact Decimal
literal packets and lossless conversion bounds. SQLite reads original JSON
integer tokens rather than rounded REAL values and uses20-digit ordinal keys.
No storage migration or reindex is required. Row predicates/membership/field
comparison/order, detached projections, Min/Max and grouped keys/result filters
share the existing generic planners, read owner and detached proof validation.
Mixed fractional/out-of-range literals reject explicitly; Sum/Average retain their
Int32/Int64 enrollment. Other providers/HTTP do not inherit UInt64 permission.

Actual52891 terminated0: **all1,435 SQLite integration tests pass, zero skips**,
including22 new UInt64 cases and four independently repaired legacy-search cases.
The separate full Core cohort passes**2,428 cases**, with four new capture controls.
Initial11 native cases passed; eight broader red cases were four test-expression
mistakes and four peer search baselines, all resolved before the final unfiltered
run. This is not a rerun of MongoDB/Neo4j suites or the63/63 HTTP demo.

Core/SQL/test source hashes stayed unchanged during the final gate. Producer/native
copies match Core35AE105E, Shared50CABB2E and SQLite014AAE08; the full Core unit
consumer matches the same Core producer. Exact identities/source hashes:
TestResults/SQLiteCurrent/uint64-artifacts.json and uint64-final-source-hashes.json.
Reports: TestResults/CosmicCurrent/uint64-full-current.trx and
TestResults/SQLiteCurrent/uint64-final-current.trx. Log:
TestResults/CosmicCqlDemo/uint64-final.log. The exact owned SQLite DB/WAL/SHM are
absent after teardown; no downloads/restores/new environments were needed.

Next: MongoDB and Neo4j UInt64 persistence/query domains, transport/examples,
then remaining Decimal/double coverage and the broader unchanged roadmap.
See [the UInt64 contract](CQL_UINT64_CONTRACT.md). The full goal remains active.


## MongoDB extension — verification in progress

New boxed UInt64 metadata writes now use exact BSON Decimal128 instead of the
complex-value JSON-string path. Existing strings remain strings: without a stored
CLR tag a numeric-looking string cannot safely be reinterpreted as UInt64.
Migration of previously stringified counters requires explicit application schema
and is not automatic. BSON int/long/decimal integral values in0..UInt64.MaxValue
form the declared query domain; binary64, strings, fractions and nonfinite values
are excluded. Projection decoding independently rejects Decimal128 rounding.

The same predicate, projection, ordering, extrema and grouped owners are reused.
Actual41293: all478 MongoDB provider units passed (five new unsigned controls).
The205-case native gate (14 new cases) stopped on the pre-existing
cosmic_cql_fixture database before any CQL query/write;205 preflight failures are
not native capability acceptance. Earlier27043 stopped at a test-only NaN API
compile mistake, since corrected to Decimal128.Parse. Reports/logs are retained in
SubscriptionUsageCurrent/mongo-uint64-preflight-red.trx and
CosmicCqlDemo/mongo-uint64-{test-build-red,final}.log.

A bounded read-only inspection mode is prepared. It records collection rows and
SHA256 in the existing SubscriptionUsageCurrent directory without provider
activation or database mutation. The first inspection attempt stopped on the busy
global build lease; the retained database remains preserved. Ownership/outcomes
must be reconciled before the native suite can reuse the fixed namespace. Mongo
native acceptance, Neo4j and HTTP UInt64 remain open; no complete-matrix claim.


The read-only inspection49049 passed: exactly five empty fixture collections,
including the test ownership marker and empty outbox. Snapshot249bytes has SHA256
C640207F460B4F0A9F9EF804E5A0823F1583E35BD58AC3513D89517CED7EF5C1.
Pinned cleanup under80768 passed after repeated catalog/count checks. The following
native gate failed its first seed transaction in the concurrently changed child-role
validation: ConditionalHolonReceipt.Write invokes HolonGraphWritePlan.Create on an
internal acknowledged snapshot, which now requires a fully observed caller child
selection. The other204 tests then failed the preserved-fixture preflight. No CQL
query acceptance can be inferred. This integration regression has been sent to its
Core/Shared source owner; caller ownership validation must remain intact. Evidence:
SubscriptionUsageCurrent/mongo-uint64-child-role-red.trx and
CosmicCqlDemo/mongo-uint64-reconciled-final.log. The active goal remains incomplete.


## MongoDB native acceptance after receipt repair

Actual11416 terminated0: **205 native CQL tests and478 MongoDB units passed,
zero failures/skips**. Fourteen new native cases verify exact boxed UInt64
persistence, six boundary values, equality/membership/projection/grouped alias
filters/order, adjacent maximum values and scalar extrema, plus seven invalid
BSON domains. Five new unit cases validate numeric BSON serialization and detached
packet rejection, including Decimal128 fractions that would round into integers.
The empty retained fixture was hash/catalog/count checked before cleanup. The
Core/Shared receipt fix restored seed admission without weakening caller save
ownership. Existing strings remain strings; no automatic legacy reinterpretation.

Inside-lease producer/native/unit hashes match Core9EA092FC, Shared2FD86EA9 and
Mongo6A26450E. Exact identity/source evidence:
TestResults/SubscriptionUsageCurrent/uint64-artifacts.json and
uint64-receipt-fix-source-hashes.json. All captured Mongo source hashes stayed
unchanged. Reports: mongo-cql-native-current.trx and uint64-unit-final-current.trx
in that directory. Log: CosmicCqlDemo/mongo-uint64-receipt-fix-final.log.

Neo4j UInt64 and HTTP UInt64 code are now prepared. The full Core/linked-production
wire cohort passes2,474 cases (20 new unsigned wire controls). Source filters and
selected/grouped results use UInt64 type names and exact invariant digit strings;
JSON numbers, signs, whitespace, fractional/exponent text and overflow are rejected.
Native Neo4j and live three-provider transport/demo verification are still pending.


## Three-provider UInt64/native and wire checkpoint — 24 October2026

Actual99510 terminated0: fullCore/linked-production**2,474 tests**, Neo4j**205
native CQL tests and453 provider units**, zero failures/skips. Twenty wire controls,
13 native Neo cases and five Neo unit cases are new. The native gate covers exact
UInt64 range/membership/order/projection, nullable grouped keys/Min/Max, field
comparison and10 invalid-domain cases. Both existing schema3 integer values and
exact raw wider-number text normalize to20-digit ordinal keys. No storage rewrite,
reindex, toFloat or signed-integer conversion is introduced for these keys.

Source/wire hashes stayed unchanged. Inside-lease producer/native/unit identities
match Core9EA092FC, Shared2FD86EA9 and Neo86F7CE9C. Exact evidence:
TestResults/Neo4jCurrent/uint64-artifacts.json, uint64-source-hashes.json,
neo4j-cql-native-current.trx, uint64-unit-current.trx;
TestResults/CosmicCurrent/uint64-wire-full-current.trx;
TestResults/CosmicCqlDemo/uint64-wire-neo-final.log.

Mongo acceptance11416 also passed205 native/478 units; SQLite's prior full1,435
cohort remains scoped to its earlier binary. These are separate verified cohorts,
not a simultaneous full regression of all three stores. The latest live demo remains
63SDK/63HTTP UTC examples; new UInt64 live transport/examples still require their
own gate. The additive UInt64 enum, selected/nullable binding, grouped alias
predicates and central exact-string wire formatting pass the20 new Core controls.

Core/Shared receipt repair was necessary for the current native seed path and is
owned by the parallel provider session. Its committed dependency pin/publication
must be reconciled before a Development deployment batch. This checkpoint remains
on the task branch. Original joins, federation, broader Decimal/floating metadata,
authoritative policy, interruption and final certification requirements stay open.
See [the matrix](CQL_SCALAR_CAPABILITY_MATRIX.md) and [UInt64 contract](CQL_UINT64_CONTRACT.md).


## Live UInt64 examples22–23 — 24 October2026

Actual69794 terminated0: **69 SDK queries/69 signed HTTP examples passed** on
MongoDB, SQLite and Neo4j, with249 settled operations/244 provider attempts.
The dedicated signed native demo case and replica initialization each passed,
zero failures/skips. This is the joint demo acceptance, not a new full269-case
Neo4j/WEB4 regression. Prior native205/205 and provider-unit cohorts retain their
own recorded binary scope.

Example22 verifies exact unsigned source bounds and UInt64-tagged selected values.
SDK ordering is by Counter; HTTP projection explicitly orders by canonical Name.
Example23 filters selected Counter aliases before native group ordering/page.
Both preserve adjacent18446744073709551612/613 values without binary64 or Int64
conversion. Generated Swagger ExactUInt64 named examples pass native-host checks;
existing UTC schema examples remain. Browser script syntax, both unsigned preset
builders and lexical exactness pass an offline Node24.19.0 check; no browser
interaction is claimed.

Producer/native identities were captured inside the global lease: CoreDAD1283D,
Shared39F60986, MongoAFE8D3D5, SQLite48F6F643 and Neo5F6E61A3. Exact paths/hashes:
TestResults/CosmicCqlDemo/v1-binary-identities.json. Fresh evidence: v1-demo-result.json,
v1-walkthrough.txt, uint64-demo-native.log and v1-unsigned-ui-check.json; native TRX:
TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx. Fixture/readiness markers
are absent, owned services stopped, retained Neo image exited. No new runtime,
restore or download. This closes UInt64 live-demo acceptance; the broader roadmap
remains open. [Runnable guide](../../../Examples/CosmicCqlDemo/README.md).
