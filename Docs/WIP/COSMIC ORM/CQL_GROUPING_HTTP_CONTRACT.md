# CQL grouped HTTP contract

24 October 2026. The protected POST route is `/api/data/query-cql/grouped`. It uses the existing CQL authorization/resource filter, authenticated actor, provider eligibility and durable physical-attempt accounting. Controlled endpoint tests pass for all three provider types; live HTTP and demo acceptance remain pending.

## Request

```json
{
  "provider": "MongoDBOASIS",
  "keys": [
    { "alias": "category", "metadataPath": ["category"], "valueType": "String" }
  ],
  "columns": [
    { "alias": "Category", "keyAlias": "category" },
    { "alias": "Items", "operation": "LongCount" },
    {
      "alias": "TotalStock",
      "operation": "Sum",
      "selection": { "metadataPath": ["stock"], "valueType": "Int64" }
    },
    {
      "alias": "MeanStock",
      "operation": "Average",
      "selection": { "metadataPath": ["stock"], "valueType": "Int64", "nullable": true }
    }
  ],
  "maximumGroups": 25
}
```

Select MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS; omission uses the admitted current default. This public v1 request selects one eligible provider. It does not expose implicit federation. Unknown members, actor/policy overrides and unrecognized operation spellings are rejected.

Keys use the existing projection column grammar: one to 8 unique named canonical or typed metadata paths. Columns select a key by its exact alias, Count/LongCount without a selection, or Min/Max/Sum/Average with one typed selection. A column cannot select both a key and an aggregate. One to32 unique output aliases are allowed; aliases use one to 64 ASCII letters/digits/underscores, beginning with a letter. Any is not a grouped operation. Actual adapter capabilities still decide supported domains.

The existing filters, anyOf, anyChildren, anyDescendants, graph, orderBy, descending and skip fields apply to the source. Optional take applies before grouping; omission does not impose a row-publication page. maximumGroups 1–100 bounds output only and never truncates aggregate inputs. Overflow, malformed data and excess budgets fail without partial output. Native grouped buffers use a 1 MiB transport budget; serialized response limits remain enforced separately. Request size is 32 KiB and the endpoint deadline is 5 seconds.

## Response

The OASISResult result contains version `cql-grouped-v1`, concrete provider, columns and rows. Columns describe alias, exact valueType and nullable; each row is a parallel value array. Int64 and Decimal values are invariant strings to preserve precision; GUID/date/enum values use the existing projection wire contract. For example, an Int64 total greater than 2^53 remains `"9007199254740993"`, and a Decimal average remains `"1.5"`. No holon is hydrated or tracked.

## Evidence and remaining work

Full 2,271 controlled Core/linked-production tests pass, zero skips, including seven request compiler controls and four endpoint cases (exact wire domains/durable attempt across three provider types, plus malformed request rejected before provider resolution). Evidence: TestResults/CoreCurrent/cql-grouped-http-{compiler,endpoint}-current.trx and TestResults/CosmicCqlDemo/grouped-http-{compiler,endpoint}.log. The shared typed scalar visitor also retains all existing scalar endpoint controls. Native grouped provider tests predate this HTTP route; they do not prove live HTTP acceptance. Grouped HTTP demos, live three-provider acceptance and post-group composition remain open.


## Live acceptance finding - 24 October2026

The first expanded native demo completed45 SDK queries, then rejected the first grouped HTTP example with CQL_ADMISSION_REQUIRED. The demo host's path predicate omitted grouped, so storage admission never ran. The configured production tariff guard also only recognized row/scalar paths, omitting projection/grouped. Core now provides CosmicQueryHttpRoutes: an exact immutable four-leaf registry used by startup tariff validation and the native demo host's admission/Swagger predicates. Prefix lookalikes are rejected. Both Core targets and2,280 unit tests pass0skip, including nine registry controls.

PrivateCore65c368f1acc35c217a35e3d002ff14c2843cf2a5 is paired with parent pin. Native consumer refresh, exact failed-fixture reconciliation and rerun remain pending. Do not treat the45/45demo as passed: the preserved fixture is awaiting review. Existing configured hosts must explicitly register authenticated storage admission and query tariffs for the grouped route.
