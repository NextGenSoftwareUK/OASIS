# CQL Decimal metadata contract

24 October2026. Core codec/capture verified; SQLite implementation has passed its
focused24-case native gate. Full SQLite regression, MongoDB/Neo4j metadata Decimal
and live wire/examples remain open. This does not imply Decimal Sum/Average input
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
The full SQLite gate follows its lease release. Resources/cleanup ownership:
[ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md). Full CQL/ORM completion remains open.
