# CQL Decimal metadata contract

24 October2026. Core codec/capture verified; SQLite implementation has passed its
full1,459-case native gate, including24 Decimal cases. MongoDB passes228 native
CQL and483 unit cases, including23/5 new cases. Neo4j passes217 native CQL/455
units, including12/2 new cases. Live wire/examples remain open. This does not imply Decimal Sum/Average input
enrollment; the existing numeric aggregate input contract stays Int32/Int64.

## One exact value domain

Decimal keys use the existing58-character ordinal encoding on the10^-28 grid.
Equivalent scales and signed zero share numeric identity. The new
`CosmicNumericScalarResults.DecimalFromOrderKey` returns OASISResult and rebuilds
96-bit coefficients directly, rejecting malformed/noncanonical keys and grid
values that cannot be represented exactly as Decimal. No floating-point or
rounded Decimal parse is involved in key decoding. Decimal/nullable Decimal keys
can be captured generically; actual provider capabilities still decide admission.

## SQLite execution owner

The query-scoped `cosmic_cql_decimal_key` intrinsic receives the original JSON
number via json_each.json/fullkey, rather than SQLite's rounded REAL extraction.
It requires an exact Decimal parse AND equality with the original numeric token.
Exponent and equivalent-scale forms preserve mathematical identity; underflow,
overflow, excess precision and foreign JSON types do not become Decimal values.
Comparison, membership, field comparison, ordering, projection, Min/Max and grouped
keys/filtering use the same normalized key. Detached decoding uses the Core codec.

The existing read-budget owner registers/unregisters the intrinsic on its owned
connection. Every call checks cancellation/deadline; tokens beyond4096 characters
set a query-limit outcome that is checked before publication even if SQL excludes
the row. Invalid scalar types remain domain mismatches, while selected mandatory
invalid values yield CQL_DATA_INVALID. No hydrated-row/client filter or alternate
query path is introduced. No storage migration/reindex/installation is required.

## Verified checkpoint

Both Core targets build. Actual78228 terminal0: fullCore2,477 tests and24 focused
SQLite native cases pass, zero failures/skips. Three new Core tests exercise4,102
round-trip samples/boundaries, malformed/nonrepresentable keys and Decimal grouping.
The former Decimal-key denial now tests still-unsupported Double keys; the original
2,476pass/1red report is preserved as decimal-codec-red-current.trx.

Native cases cover all comparisons/membership/null/NOT/field comparison, extrema,
quoted/dotted metadata keys, ordering/projection, grouped scale normalization,
malformed/foreign/nonrepresentable tokens, exact exponent/negative-zero forms and
a token budget that cannot be hidden by filtering. Reports:
TestResults/CosmicCurrent/decimal-codec-full-current.trx and
TestResults/SQLiteCurrent/decimal-native-current.trx; log:
TestResults/CosmicCqlDemo/decimal-focused-final.log.

The numeric source is frozen at this checkpoint so the parallel provider session
can verify Oracle against the same Core producer. Core net10 SHA256C2A107EF and
netstandard0E5E8E12 are the compiled producer identities before commit metadata.
Full SQLite gate35989 terminal0 passes1,459 cases with zero failures/skips;
source hashes were unchanged throughout its producer/consumer/test batch.
Evidence: TestResults/SQLiteCurrent/decimal-full-current.trx and
decimal-identities-current.json; log: TestResults/CosmicCqlDemo/decimal-sqlite-full.log.
Its owned fixture DB/WAL/SHM are absent after teardown. This cohort includes the
peer's uncommitted Shared conditional-admission candidate; that candidate's
separate Oracle replay failure remains open and is excluded from this commit.
Resources/cleanup ownership:
[ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md). Full CQL/ORM completion remains open.

## MongoDB native implementation and verification

The prepared native domain admits BSON Int32/Int64 and finite Decimal128 only
when the value is exactly representable as .NET Decimal. It reuses bounded native
coefficient/scale normalization and compares the result with the original packet;
rounded or underflowed inputs cannot supply a Decimal domain proof. Detached
projection/group/extrema decoding independently checks exact Decimal128 equality.
Binary64 and numeric strings remain foreign domains. Twenty-three native cases and
five unit cases are added; both full gates now pass. Existing unsupported Decimal
admission controls now exercise still-unsupported Double. No storage migration
or installation is required.

First full Mongo gate64320:227PASS/1FAIL0skip of228 cases. The sole failure was
the new null predicate fixture using an absent path instead of a present null;
the predicate contract remains unchanged. Fixture corrected, original red report
preserved in SubscriptionUsageCurrent/decimal-first-red.trx. Final full native
and unit gates were then rerun: actual15936 terminal0 passes228 native/483 units,
zero skips. Source snapshots are unchanged; CoreC65FBD5F, SharedCC928AD3 and
Mongo2CDF3D68 match across native/unit consumers. Evidence:
TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx,
decimal-unit-current.trx and decimal-identities-current.json; final log:
TestResults/CosmicCqlDemo/decimal-mongo-full.log. Owned fixture/services cleaned;
no new runtime/install/restore. Neo4j and wire/demo Decimal enrollment remain open.

## Neo4j native implementation and verification

The native translator derives the Core58-character ordinal key from schema3 JSON
number text. Singleton Cypher comprehensions bind intermediate digit/scale values
without changing row cardinality. Leading/trailing zeros and exponent notation
normalize before the96-bit coefficient and scale<=28 checks. Exponent arithmetic
is fenced against native integer overflow; arbitrary-exponent zero is canonical.
No numeric value is converted through binary64. Projection/group/extrema results
decode through the same Core canonical-key decoder. Twelve new native cases
include2,067 direct kernel vectors and native selection/domain/grouping/ordering;
two unit tests check exact bindings and strict detached decoding. Actual18316
terminal0 passes the full217 native CQL and455 unit cases, zero skips. Source
snapshots are unchanged; native/unit consumers match CoreC65FBD5F, Shared099240E3
and Neo5CC14698. Evidence: TestResults/Neo4jCurrent/neo4j-cql-native-current.trx,
decimal-unit-current.trx and decimal-identities-current.json; log:
TestResults/CosmicCqlDemo/decimal-neo-full.log. Owned retained service is exited;
current fixture teardown completed, historical reviewed evidence is preserved.
HTTP/demo enrollment remains open.

## HTTP compiler verified; live acceptance pending

WEB4 adds Decimal after UInt64 in the existing scalar enum, preserving existing
identities. Source predicates, selected Decimal/nullable Decimal and grouped alias
predicates share a bounded invariant string parser. Parsing must reproduce the
original string exactly, preventing Decimal.TryParse rounding, exponent coercion,
whitespace and numeric JSON from changing value identity. Existing Decimal result
schema/string formatting is reused. Actual9594 terminal0 passes the full2,500
linked Core unit tests, including23 new controls, zero skips. Source snapshots
unchanged; CoreC65FBD5F/unitDC44A998 tested together. No Core production source
changes. Evidence: TestResults/CosmicCurrent/decimal-wire-full-current.trx and
decimal-wire-identities-current.json; log: CosmicCqlDemo/decimal-wire-full.log.
Live HTTP/browser/Swagger/demo examples remain pending.
