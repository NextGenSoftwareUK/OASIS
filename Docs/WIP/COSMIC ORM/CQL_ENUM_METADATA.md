# CQL enum metadata

Current documentation: [manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md), [three-provider demos](CQL_DEMO_RUNBOOK.md), [WEB4 v1 contract](CQL_V1_HTTP_CONTRACT.md). Historical checkpoints retain their original scope/counts.

## Contract

SQLite, MongoDB and Neo4j explicitly register `HolonType`, `ProviderType` and their nullable forms as metadata scalar domains. These declarations support comparisons, membership, field comparisons and ordering through the common Core query model. Other enum types remain unsupported by the production profiles; a caller-supplied broader profile cannot enable their native execution. Broader enum enrollment and application schema registration remain roadmap work.

The native domain consists of JSON integers or BSON Int32/Int64 values defined by the declared enum. Aliases share one numeric identity. Undefined integers, enum-name strings, booleans, fractional values, arrays and objects are outside the domain. MongoDB Double and Decimal128 values remain outside that integer domain even when numerically integral. A numeric value can belong to multiple enum domains because these numeric representations have no CLR enum type tag. A declared typed path supplies its interpretation. Persisted unknown values remain intact in the record; querying does not rewrite them.

The standard metadata missing/null contract applies. Nonnullable typed comparisons do not match missing or invalid-domain values, including for inequality. Nullable equality to null matches explicit JSON null only. Nullable membership may include explicit null. NOT complements the complete predicate, so it also includes missing/invalid-domain records unless another predicate restricts them. Fields must have the same declared underlying CLR enum type to compare; numeric overlap does not permit comparing different enum types.

Core already validates enum literals and membership values without executing application getters or delegates. SQLite's native guard uses trusted enum-schema numeric constants and is shared by all four operation families. No client evaluation, deserialization callback, arbitrary SQL or enum-name coercion is introduced. `EnumValue<TEnum>` objects stored as objects are not numeric enum scalars.

## Example

```csharp
var kinds = new HolonType?[] { null, HolonType.Quest };
var query = CosmicQuery<Holon>.Create()
    .Where(h => kinds.Contains(CosmicQueryMetadata.Value<HolonType?>(h, "kind")))
    .OrderBy(h => CosmicQueryMetadata.Value<HolonType?>(h, "kind"));
if (query.IsError) throw new InvalidOperationException(query.Message);
```

Plan against an already authorized SQLite target and execute through HolonManager as shown in [the CQL guide](CQL_GETTING_STARTED.md). An adapter capability is not an authorization grant.

## Verified SQLite acceptance

Both new native cases fail against the prior provider because enum metadata capability is missing (`TestResults/SQLiteCurrent/cql-enum-domains-red.trx`). The fixed focused gate passes7/7, zero skips (`cql-enum-domains-focused.trx`). It exercises both enum types, boundary defined values, undefined integer gaps, all six comparisons, nullable membership, field comparisons and descending ordering. Existing unsupported-type tests now use the still-unregistered `KarmaSourceType` so forged-plan rejection remains tested. Decimal, double, canonical audit-date and mixed coercion rejection remain covered.

The complete SQLite suite passes1176/1176 with zero failures/skips in2m4s (`TestResults/SQLiteCurrent/cql-enum-domains-full.trx`). Eight declared Core/Shared/SQLite producer and consumer copies match; report hash, counters and full assembly identities are retained in `TestResults/SQLiteCurrent/cql-enum-domain-artifacts.json`. SQLite SHA256 is `D920404790458E53B634AE7BD83FEEC7A363FE9CA2402B3169913117527B830C`; Shared is `942F1FDD6CBE124752C3F21538B63FDEDB67167B350588ACD85FB1DF988823F1`; Core is `EB9EFAD0C29B70A546ADD34AD651F9EB21F7A86341CD30693251729EBD0759AC`. The owned disposable database is absent after teardown.

No Core or Shared source change belongs to this increment. Broader enum/application schema registration remains open; integer enrollment alone does not grant enum metadata support.

## MongoDB native acceptance

Both original admission cases fail against the prior provider (`TestResults/CosmicMongoCurrent/cql-enum-mongo-red.trx`). The complete fixed control suite passes204/204 and the expanded native CQL scenarios pass3/3, zero skips. The native run takes6s and covers both metadata dictionary layouts, BSON Int32/Int64 endpoints, undefined gaps, all six comparisons, nullable membership, field comparisons and descending ordering. Its original feature-specific report is preserved as `TestResults/CosmicMongoCurrent/cql-enum-mongo-native.trx` so later runs may reuse the runner's current report without replacing this evidence.

Twelve declared producer/consumer copies match Core `EB9EFAD0C29B70A546ADD34AD651F9EB21F7A86341CD30693251729EBD0759AC`, Shared `0D2BB0D178BCCF66F7B14CBFA46E45A9371418AF4AE4E048C471D415C5C5151A` and MongoDB `1CB701124D6D3FB4011B510C07361260F6059567356FD1A4FF5682726AC73EF3`. Report hashes and counters are retained in `TestResults/CosmicMongoCurrent/cql-enum-mongo-artifacts.json`. Successful fixtures were removed and the owned Mongo process is stopped; the unrelated pre-existing process is untouched.

Final complete control verification passes205/205 with zero failures/skips, including an additional explicit forged-enum-profile test (`TestResults/CosmicMongoCurrent/cql-enum-mongo-complete-controls.trx`). A broader caller profile cannot enable the still-unregistered KarmaSourceType enum; execution rejects before repository access. All12 producer/consumer copies remain unchanged. No Core, Shared or SQLite source change belongs to this MongoDB enrollment.

## Verified Neo4j enrollment — 21 October 2026

Two original admission regressions fail against missing HolonType and nullable ProviderType metadata capabilities; the arbitrary-enum control passes (`TestResults/CoreCurrent/neo-cql-enum-red.trx`: two failures, one pass). The provider now explicitly registers those two enums and nullable forms. Its shared operand guard admits only integer projection values contained in the enum's distinct defined numeric identities, supplied as a bound native parameter. Comparisons, membership, field comparisons and ordering share this guard. No schema version, Core pin, string-to-number conversion or snapshot rewrite is required.

The native cohort passes 64/64 in 22 seconds, zero failures/skips (`TestResults/Neo4jCurrent/neo4j-cql-regression-current.trx`). Expanded native assertions cover both enum types: minimum/maximum defined identities, undefined integers, explicit null, numeric/name strings, fractional and integral-looking decimal numbers, booleans, arrays and missing keys; all six comparisons, nullable membership, nullable field equality/inequality, field ranges, and ascending/descending order with invalid-domain buckets. Final complete controls pass 305/305, including explicit forged-profile rejection before native I/O (`TestResults/CoreCurrent/neo-cql-enum-final-controls.trx`).

Cleanup removed required cached dependencies before verification; narrow restores reused the same global package directory. A subsequent native run was interrupted without a fresh report and retained five exact initial synthetic records. Read-only inspection plus independent full metadata/audit/key review preceded guarded reconciliation. Fresh transaction locks rechecked snapshot hashes, revisions and owned projections, excluded history/graph/receipt obligations, removed only those five identities and verified absence; reconciliation passes 1/1 in one second (`neo4j-enum-interrupted-reviewed-fixture.trx`). Original runner, inspection and reviewed hashes are preserved. No interrupted result is counted as acceptance. The following fresh 64-case run above is the native evidence.

`TestResults/Neo4jCurrent/cql-enum-final-artifacts.json` binds fresh reports and 20 matching declared producer/consumer copies: Core7, Shared6, Neo3, SQLite2 and Mongo2. Core SHA256 remains `97A343EF51A6A4BF8942250BB2995F5A4E37CD51E0129EEA07F5729B379CAD11`; Neo is `141783BEA4742C4B0E4819CEA5233E9CC9480314D9509A6981DCD675237FA41E`. SQLite/Mongo copy equality is not renewed native certification of those adapters. Successful fixture manifest is absent, container is stopped, and cross-process lease contract passes. The [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) records retained resources, restores and exact cleanup. Wider enums/application schema registration, Neo GUID/date domains and remaining CQL roadmap acceptance stay open.

The later GUID increment reruns all enum assertions within native64/64 (23 seconds); its stable `TestResults/Neo4jCurrent/cql-guid-native.trx` and `cql-guid-final-artifacts.json` preserve current combined evidence after reuse of the runner's current report. Broader types remain separately scoped.
