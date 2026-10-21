# CQL field comparisons

20 October 2026. Common compilation/admission and SQLite/MongoDB native execution pass the first corrected cohort. Core `c0309262de5e704c2a78d73499346379761a2193` is committed, pushed and remotely verified; its parent gitlink and deployment manifest are paired. The final immutable-source cohort is verified below. No Development deployment is included.

Core now represents comparisons between two canonical or typed metadata paths using an immutable `CosmicQueryFieldComparison`. Compilation requires the same underlying CLR scalar type; nullable lifting is allowed, mixed numeric field precision is rejected. Capability admission requires the new `FieldComparison` feature and independently validates both exact paths, types and operators. Existing adapters therefore reject the new node until they explicitly implement and declare it.

Examples:

```csharp
var sameText = CosmicQuery<Holon>.Create()
    .Where(h => h.Name == h.Description);
var belowLimit = CosmicQuery<Holon>.Create()
    .Where(h => CosmicQueryMetadata.Value<long>(h, "usage", "used") <
                CosmicQueryMetadata.Value<long>(h, "usage", "limit"));
if (belowLimit.IsError) throw new InvalidOperationException(belowLimit.Message);
```

Pass `belowLimit.Result` through the same explicit-scope planner and HolonManager execution pipeline shown in [getting started](CQL_GETTING_STARTED.md). `Where` returns an `OASISResult` containing a new immutable query; it does not mutate an earlier query. Canonical-to-metadata comparisons also work when the underlying type matches, for example `CosmicQueryMetadata.Value<int>(h,"expectedVersion") == h.Version`.

The original baseline produced three failures and one passing mixed-precision rejection against the unchanged Core producer (`TestResults/CoreCurrent/cql-field-comparison-red.trx`). After the Core changes, both Core targets build and the full Core suite passes **1,557/1,557**, zero skips (`cql-field-comparison-core-current.trx`). These results prove compiler/admission behavior; they do not prove native execution. Both native provider projects also build successfully.

Both translators use independent left/right metadata variable namespaces. SQLite uses bound JSON path segments and null-safe `IS`/`IS NOT`; ordered null comparisons are false. MongoDB compares exact tick operands for dates and normalizes UUID strings, standard subtype4 bytes and CSharpLegacy subtype3 bytes into the same hexadecimal identity before comparing them. Sparse metadata is not coerced into CLR defaults. `NOT` complements the complete atomic predicate, including domain failures.

These comparisons execute in storage. The application does not load all records to compare fields. Providers without the feature explicitly declared reject the query during admission. Existing encryption/queryable-key rules continue to apply to both operands.

## Supported contract

| Operands | SQLite and MongoDB native comparisons |
|---|---|
| Canonical string, bool, Guid | Equality and inequality on each provider's existing exact field whitelist |
| Canonical Version and HolonType | All six comparisons; both sides must retain the same CLR scalar/enum type |
| String, bool/bool?, Guid/Guid? metadata | Equality and inequality |
| Int32/Int64 and nullable metadata | All six comparisons, preserving integer precision |
| UTC DateTime/DateTime? metadata | All six comparisons, preserving stored exact ticks |
| Mixed canonical/metadata | Supported when both exact path contracts admit the operator and share the underlying CLR type |
| Missing or wrong-type metadata | Entire atomic comparison is false, including inequality; NOT complements that result |
| Explicit null | Admitted for string/nullable paths; both nulls compare equal, one null compares unequal, ordered comparison is false |

Decimal/double metadata, mixed-width field comparisons, arbitrary getters/methods and relationship traversal remain unsupported. No provider gains capabilities implicitly from another provider or from another field of the same type.

The final pinned-source cohort and all17 declared producer/consumer copies match. The scoped parent commit/push and committed-manifest validation complete publication of this increment.

## First native run

Both provider projects built. The expanded MongoDB document-dictionary scenario passed its new field-to-field canonical, numeric, nullable and date assertions before its first UUID-vector comparison returned no rows for equal string/standard-BSON UUID identities. Its original report is archived at `TestResults/CosmicMongoCurrent/cql-field-original-failure.trx` (SHA256 `24C02F7E6DF2F3F49A9B79C279696AE31893B6B14F167B50E2ECA7A956C15766`). The fixed fixture remains preserved. The following dictionary/payload cases refused that occupied fixture; those failures do not constitute separate translation defects. SQLite did not run because the batch stopped at the failed MongoDB gate.

The independent majority-primary projection passed and demonstrated the root cause: text `aabbccdd123456789abcdef012345678` versus BSON hex `AABBCCDD123456789ABCDEF012345678`. The translator now normalizes native hex case before legacy byte-order normalization. The complete original five-row graph and empty side collections were inspected and archived at `TestResults/CosmicMongoCurrent/cql-field-observed-fixture.json` (SHA256 `03E832A270EA915589676E21B84B38A7DECABF40D9C7B6F462CB3E92013CED01`). The explicit reconciler required that exact reviewed image and an unchanged majority-primary re-read before deleting only `cosmic_cql_fixture`. That cleanup gate passes1/1; the corrected native suite passes3/3 with both dictionary layouts, all UUID domain vectors and payload-budget coverage, zero skips.

The first inspection attempt used a dotted native field selector for the literal key `guid.reference`, entered the older fixture verifier and refused the changed metadata. The inspection was corrected to use literal `$getField` operands and the exact original root identity. No cleanup occurred in that failed attempt.

## SQLite correlated identity binding

The first full native run passed1,129/1,130, zero skips. Its new mixed metadata-Guid/canonical-Id assertion exposed that SQLite's `json_each` table exposes an `id` column: an unqualified canonical `"Id"` inside the correlated metadata predicate resolved to that JSON traversal ID. Canonical columns now bind explicitly to the outer `"Holon"` table in every translator path. This fixes the binding invariant rather than changing the expected comparison or evaluating rows on the client. Original report: `TestResults/SQLiteCurrent/cql-fields-original-failure.trx`, SHA256 `23F6D365F758636DA61E60AA8C424D5F292B86F5BA892EB715567F725B6C28EB`. SQLite's existing fixture disposes its temporary database after a test, including failures; the original failed database was not retained. The corrected final pinned-source full suite passes1,130/1,130, zero skips.

The larger CQL roadmap still includes ordering/paging, projections/aggregates, relationships, authorization-aware target discovery and federation. This increment does not close those gaps.

## Final pinned-source acceptance

Build epoch: parent59c915a6591d114bf1ab1ca8dcef8613afee3566; immutable Corec0309262de5e704c2a78d73499346379761a2193. Both Core targets build. Reports are bound to exact assembly/test-source hashes in `TestResults/CosmicMongoCurrent/cql-field-final-artifacts.json`.

| Gate | Passed / total | Report |
|---|---|---|
| Full Core | 1,558 / 1,558 | `TestResults/CoreCurrent/cql-field-comparison-core-current.trx` |
| Full Shared | 925 / 925 | `TestResults/SharedCurrent/cql-fields-shared-current.trx` |
| MongoDB controls | 197 / 197 | `TestResults/CosmicMongoCurrent/cql-fields-controls-current.trx` |
| MongoDB native CQL | 3 / 3 | `TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx` |
| Full SQLite native | 1,130 / 1,130 | `TestResults/SQLiteCurrent/cql-fields-full-current.trx` |

All have zero failures/skips. The final cases include mirrored UUID operands, both metadata dictionary layouts, explicit nulls, missing/wrong types on either side, NOT, one-tick dates, Int64 bounds, mixed canonical/metadata identities and the complete two-path parameter budget. The original fixture cleanup gate independently passes1/1. Successful new native fixtures are removed; no owned Mongo27029 listener or SQLite fixed database remains. Final free disk is60.4GiB; no runtime/environment download or duplicate was created.

| Producer | SHA256 |
|---|---|
| Core net10.0 | `F3A3F6DD3FF8C550D4E419B65F1AD95282D2A70E21C7531BF7BF376399BC4102` |
| Shared | `7DD0C09B7528768EF570B3F472D4EF3FA62FDE9F8BA5065DDDCE5BAA908BD414` |
| MongoDB | `C027DA468D3B8480B1A66BFDA8DB1EA96FE3A8B26C0F600779CC9B4C073B7C66` |
| SQLite | `D82D2ED2DCDC915BC19B87C22FF7D9DACFAED71F42A751183D6AB5C5EDAC70FA` |
