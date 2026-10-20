# CQL typed scalar metadata: SQLite contract

20 October 2026. Native typed metadata comparisons, membership and ordinal string matching extend the existing presence/null markers. Full CQL authorization, other native adapters, graph queries and federation remain separate unfinished requirements.

## Declared capabilities

The production profile and translator share `SqliteCqlContract.MetadataTypes`. Unsupported types and operations reject before provider I/O, even if a caller supplies an overbroad plan.

| Declared path type | JSON domain | Comparisons | Membership | String operations |
|---|---|---|---|---|
| `string` | JSON string; explicit null for nullable operations | `==`, `!=` | Exact scalar arrays, including null | Contains; StartsWith/EndsWith with explicit Ordinal |
| `bool`, `bool?` | JSON true/false | `==`, `!=` | Exact scalar arrays | None |
| `int`, `int?` | JSON integer within Int32 bounds | All six comparisons | Exact scalar arrays | None |
| `long`, `long?` | JSON integer represented exactly by SQLite Int64 | All six comparisons | Exact scalar arrays | None |
| `Guid`, `Guid?` | Canonical 36-byte GUID D text, either hex case | `==`, `!=` | Exact GUID arrays, including nullable null | None |

Nullable value types additionally admit explicit JSON null. The CLR width of the value originally serialized is not retained in JSON: a JSON integer within Int32 bounds can be queried as either int or long. JSON true/false, numeric text and fractional numbers are not integers. Oversized JSON integers surfaced as SQLite REAL are excluded from integer domains, preventing rounding into a matching value.

Decimal, floating-point, DateTime and enum metadata path types remain unsupported natively. Mixed numeric metadata comparisons are rejected; canonical Int32 field widening support does not grant metadata widening. These remain roadmap requirements rather than inferred capabilities.

GUID identity is case-insensitive only when the declared marker type is `Guid`/`Guid?`. String markers retain ordinal comparison. Native SQL checks the exact ASCII hex/hyphen shape and byte length before comparing bound canonical literals with NOCASE collation. Compact GUIDs, braces, whitespace, embedded NUL, non-hex text, arrays and numeric values stay outside the GUID domain. Missing and nullable semantics follow the rules below. This capability currently belongs to SQLite; MongoDB UUID representations still require separate implementation and native verification.

## Missing, null and type semantics

A typed atomic predicate matches only a present path with an admitted JSON type. Missing paths, wrong types and out-of-range integers are false, including for inequality. Explicit JSON null matches `Value<T?> == null` and string null equality. Comparing an admitted nullable explicit null with a non-null literal using `!=` is true; ordered comparisons are false. `!= null` matches only present values in the requested non-null domain. Non-nullable value types do not admit JSON null.

Membership matches only a present value in its declared domain, including explicit null when the captured nullable array contains null. Empty membership is false. NOT complements the complete two-valued predicate: negating equality also includes missing and wrong-type paths. Use Exists/IsNull or a positive typed condition when the application needs a narrower domain.

Nested segments traverse JSON objects only. Arrays are not expanded, and strings containing JSON are not parsed as objects. Keys containing dots, quotes, Unicode or SQL-like text are bound literal labels. String matching uses shared binary operations to preserve embedded NUL and ordinal case-sensitive matching; it does not interpret LIKE syntax.

## Example

This uses the direct native adapter seam for an already authorized SQLite target. It does not implement the unfinished WEB4 policy boundary.

```csharp
var query = CosmicQuery<Holon>.Create()
    .Where(h => CosmicQueryMetadata.Value<int>(h, "stats", "score") >= 7
        && CosmicQueryMetadata.Value<string>(h, "category").StartsWith("coffee", StringComparison.Ordinal));
if (query.IsError) throw new InvalidOperationException(query.Message);
var target = new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities);
var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.SQLLiteDBOASIS, new[] { target });
if (plan.IsError) throw new InvalidOperationException(plan.Message);
var result = await provider.ExecuteCosmicQueryAsync(plan.Result, maximumResults: 100, cancellationToken: cancellationToken);
```

The complete query translates before EF opens a connection. Every key and literal contributes to the existing 900-parameter predicate budget; one separately bound result-limit parameter is reserved. Compiler, membership and result bounds remain enforced. Overflow returns an explicit error without a partial result or client evaluation. The adapter performs no writes.

## Verification

### SQLite GUID increment

The GUID-specific red gate failed9/passed1: the existing ordinal-string control passed while GUID predicates rejected missing provider capability. Final GUID plus existing scalar/null gates pass52/52; the complete native SQLite suite passes1,051/1,051 in153 seconds, zero skips. The prior unsupported-GUID assertion was replaced by positive native coverage; decimal/double/date/enum/coercion rejection remains tested. Native rows include CLR GUIDs, uppercase D text, another GUID, missing/null, compact/braced/padded/NUL/nonhex text, numbers and arrays. Equality/reversed equality, inequality, nullable tests, membership, NOT and ordinal-string controls verify exact returned identities.

Evidence: `TestResults/SQLiteCurrent/sqlite-cql-guid-red.trx`, `sqlite-cql-guid-focused.trx`, `sqlite-cql-guid-full.trx`, and `cql-guid-artifacts.json`. Consumer Core SHA256 is `51A35ED9BDC5C1A1A36CF132B8F8F9195175096698D7A4556278E0E0AAF60270`; SQLite is `CB4EB5F6885ADDD568E09CCD87A29791FF73083C9EAE37485599F33719716E21`. The artifact report also records Shared and free disk space. The fixed SQLite database is absent after normal teardown. No Core source, dependency pin or deployment change belongs to this increment.

The reports below describe earlier checkpoints.

The original 26-case red report is preserved: 25 cases rejected absent scalar metadata capability; one exposed missing built-in nullable boolean literal lifting in the compiler. The compiler now admits only a method-free built-in T-to-Nullable<T> scalar literal lift, preserving the boxed scalar without invoking conversion methods. One new Core case also verifies nullable GUID and UTC DateTime literal compilation; that does not enable their SQLite metadata translation.

The 43-case focused native gate verifies typed predicates, null/missing distinctions, integer limits including UInt64 overflow, string NUL/Unicode/injection-like labels, nullable membership, reversed comparisons, object-only traversal, parameter-budget rejection and unsupported production/forged-plan rejection. Exact full-suite results and assembly hashes are recorded in [integration evidence](INTEGRATION_BOUNDARY.md).

The first full 948-case run passed946 and failed two historical assertions that scalar metadata was unsupported. Those tests now retain unmapped/uncertified rejection and explicitly admit supported metadata strings. The original full report is retained as failed evidence; final acceptance is recorded separately.

Final native regression: SQLite 948/948 passes125s, zero skips (`sqlite-cql-scalar-metadata-full-verified.trx`), with the fixture absent after normal teardown. Full Core1,353/1,353 and focused native43/43 belong to the recorded matching Core/provider epoch.
