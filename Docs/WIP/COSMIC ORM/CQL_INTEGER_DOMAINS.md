# CQL exact integer domains

## Scope

The SQLite adapter extends its existing Int32/Int64 metadata domains with `byte`, `sbyte`, `short`, `ushort`, `uint` and their nullable forms. Native predicates admit only JSON integers represented exactly as SQLite INTEGER, within the declared CLR type's complete range. Strings, booleans, real numbers, arrays, missing paths and integers outside that range do not satisfy typed comparisons. Explicit null follows the existing nullable contract. No numeric casts coerce invalid persisted values into the domain.

The production capability profile and translator share these exact types. All six comparisons, membership, field comparisons and ordering reuse the existing native operators and domain guard. Broader decimal/double/enum/UInt64 metadata support remains open; these additions do not imply those capabilities.

## Shared compiler correction

C# promotes byte/sbyte/short/ushort operands to Int32 in ordinary comparisons. The Core expression compiler previously rejected these lossless promotions. It now verifies integer conversion by complete source/target range containment while retaining the original path type. A conversion is never admitted merely because a particular test value fits. Narrowing, signed/unsigned conversions that lose part of the source range, Int32-to-Single, Int64-to-Double and nullable erasure reject. Small integer-to-Single conversions whose entire range fits its exact integer domain are admitted; provider mixed-numeric capabilities remain independently required.

The compiler is shared by every provider. SQLite's native representation/capabilities remain owned by the SQLite adapter; this does not advertise support on other adapters.

## Usage

```csharp
var query = CosmicQuery<Holon>.Create()
    .Where(h => CosmicQueryMetadata.Value<byte>(h, "priority") >= 200)
    .OrderByDescending(h => CosmicQueryMetadata.Value<byte>(h, "priority"));
if (query.IsError) throw new InvalidOperationException(query.Message);
```

This selects only admitted byte-domain JSON integers at least200. A stored300, string `"200"`, fractional number, missing key or explicit null does not match. Plan and execute through the existing authorized target and HolonManager entry point; constructing a query grants no access.

## Verified evidence

The original 10-case native run failed all cases: four exposed compiler promotion rejection and six rejected missing native metadata capabilities. Report: `TestResults/SQLiteCurrent/cql-integer-domains-red.trx`. An earlier test compilation attempt failed on a tuple switch's common type and executed no tests; explicit long tuple bounds corrected that fixture issue.

The first fixed cohort passes Core1611/1611 and native10/10, zero skips. The native suite tests range endpoints and immediately out-of-range values, all six comparisons, nullable membership, explicit null versus missing, numeric strings, fractional values, booleans, arrays and UInt64 overflow. A Core test separately checks whole-domain conversion admission and rejection. Reports: `TestResults/CoreCurrent/cql-integer-domains-core.trx` and `TestResults/SQLiteCurrent/cql-integer-domains-focused.trx`.

Core source commit `1227fb7fe0d202ac4631a1d48fc10f0ab35f7b7f` is pushed and remotely verified. Final immutable-source verification additionally exercises native field comparisons and ordering for all five new domains, rebuilding Core and SQLite producers and consumers. Both Core targets build with zero errors. Full Core1611/1611 and SQLite1174/1174 pass with zero failures/skips; SQLite takes2m15s. Reports: `TestResults/CoreCurrent/cql-integer-domains-pinned-core.trx` and `TestResults/SQLiteCurrent/cql-integer-domains-full.trx`.

`TestResults/SQLiteCurrent/cql-integer-domain-artifacts.json` binds source/report hashes and six matching producer/consumer copies. Core net10 SHA256 is `EB9EFAD0C29B70A546ADD34AD651F9EB21F7A86341CD30693251729EBD0759AC`; SQLite is `F903CB3D7B68493D86D506754373F17657A44EFC3A1FD9430CC7D32CEC649C18`. The owned temporary database is absent after teardown. Other adapters still require native domain enrollment and tests.

## Verified MongoDB enrollment

MongoDB adds the same ten declared types to its existing capability profile. BSON Int32 and Int64 are admitted only within the declared range. Integral-looking doubles and Decimal128 values remain outside the integer domain, as do booleans, strings, arrays and out-of-range integers. Comparison, membership, field comparison and ordering share this single guard; no client filtering or numeric conversion is introduced.

The original five admission tests fail against the prior provider (`TestResults/CosmicMongoCurrent/cql-integer-mongo-red.trx`). The corrected complete control suite passes202/202, zero skips (`cql-integer-mongo-controls.trx`). Expanded native assertions pass within all three existing CQL scenarios and the existing owned fixture in5s, covering both object and array dictionary layouts, all six comparisons, nullable membership, field comparisons and descending endpoint order. Both BSON Int32 and Int64 endpoints are exercised, as are out-of-range values and integer-looking Double/Decimal128 values that must remain outside the typed integer domain.

After the reusable native runner rebuilt its producers, the complete control suite again passes202/202 (`TestResults/CosmicMongoCurrent/cql-integer-mongo-final-controls.trx`). Native report: `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`,3/3, zero failures/skips. All12 declared Core/Shared/Mongo producer/consumer copies match, bound to reports and clean Core1227 source in `TestResults/CosmicMongoCurrent/cql-integer-mongo-artifacts.json`. Mongo SHA256 is `1308CD7F93A460DF8CE0F180B46FBC42DD9DCDA627806BA7EE9D5D5E45995146`; Shared is `20B2565E0BC0C9CCDA99752B659EF585893E7B899AF7825C552B037A5EF18978`. Successful fixtures are removed and the owned Mongo process is stopped; the unrelated pre-existing Mongo process is untouched. No Core change or new environment belongs to this enrollment.

## Verified Neo4j narrow integer domains — 21 October 2026

An explicit admission regression fails because byte/sbyte/short/ushort/uint metadata domains are absent (TestResults/CoreCurrent/neo-cql-integers-red.trx). The Neo profile now declares these five domains and nullable forms. One shared native operand guard checks integer representation plus exact range, reused by literal comparisons, membership, field comparisons and ordering buckets. Fractions and stringified numbers retain their different stored kinds and never become typed integer matches. The full controlled suite passes301/301, zero skips; producer builds with zero errors (neo-cql-integers-controls.trx).

The native candidate uses signed64 storage values to test each narrower declared domain's actual value range: minimum, maximum, explicit null, one below and one above; it checks equality/inequality, nullable membership, promoted comparisons to Version and both ordering directions. The rebuilt native CQL regression passes 64/64; the subsequent CQL plus configured-host cohort passes 66/66 with zero failures/skips (`TestResults/Neo4jCurrent/neo4j-cql-hosting-regression-current.trx`). These assertions execute against the real Neo4j instance, and successful fixtures are removed. No Core/source pin, migration/schema or environment change is required.
