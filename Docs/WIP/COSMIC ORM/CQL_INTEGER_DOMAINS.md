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
