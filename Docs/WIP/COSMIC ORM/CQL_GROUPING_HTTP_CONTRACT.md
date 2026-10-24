# CQL grouped HTTP contract

24 October 2026. The protected POST route is `/api/data/query-cql/grouped`. It uses the existing CQL authorization/resource filter, authenticated actor, provider eligibility and durable physical-attempt accounting. Controlled endpoint tests pass for all three provider types; the baseline live demo acceptance is recorded below. Result ordering has separate controlled evidence below.

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
  "resultOrder": [
    { "alias": "TotalStock", "descending": true },
    { "alias": "Category" }
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


## Grouped CQL live demo - 24 October 2026

The retained signed HTTP demo now passes on MongoDB, SQLite and Neo4j: **45 SDK queries,45 HTTP examples,225 settled operations and220 provider attempts**. Each provider runs15 spaced/commented examples; example15 groups an ordered two-row source window by stock and verifies LongCount, exact Int64 Sum and Decimal Average. Browser UI and Swagger include grouped presets. This is the single integrated live-demo acceptance test, not a rerun of every provider regression suite.

The first grouped HTTP attempt exposed missing admission-path registration. The pinned failed fixture was reconciled against194 settled operations/189 attempts and all146 owned rows per store before scoped cleanup; no reset or blind replay occurred. Core's exact CQL registry now drives hosted admission/Swagger and production tariff checks. The corrected live run completed, receipts settled, owned fixture files were removed and runner services stopped.

Evidence: TestResults/CosmicCqlDemo/v1-demo-result.json, v1-walkthrough.txt and grouped-demo-native-verified.log; TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx. Executed producer copies match Core594AAEDB and Shared8E034BFB. Reconciliation evidence: grouped-fixture-reconcile.log, neo4j-cql-v1-web4-reconcile-current.trx and cql-v1-web4-reviewed-grouped-admission-reconciliation.json. The old42/42 numeric demo result remains historical. Post-group filters/order/pages, joins, federation and other unchecked roadmap gates remain open.


## Selected result ordering — 24 October 2026

`resultOrder` is an optional array of up to 32 entries. Each entry names an exact, case-sensitive selected output `alias` and optional `descending` (default false). The first entry becomes OrderBy and subsequent entries ThenBy. It applies after native reduction; source `orderBy`, `descending`, `skip` and `take` remain independent. Complete group identity supplies final ascending ties. Empty ordering retains identity order. Null arrays/entries, unknown aliases and excessive lists fail before provider dispatch.

The compiler builds exact typed expression intent through the existing Core ordering owner. Actual provider capability admission remains mandatory. MongoDB, SQLite and Neo4j enroll key, count, extrema and integer Sum ordering. Average can be selected but ordering it currently returns CQL_CAPABILITY_UNSUPPORTED before native execution; there is no client sort or approximate substitute.

Verification: **2,314 Core/linked-production unit tests pass, zero skips**, including 17 new compiler cases and four endpoint cases covering all three profiles and unsupported Average dispatch prevention. Evidence: TestResults/CosmicCurrent/http-group-order-full-current.trx and TestResults/CosmicCqlDemo/http-group-order-full.log. These are controlled endpoint tests; a new live three-provider HTTP ordering demonstration is still required. Baseline live grouping evidence above predates resultOrder. Post-group filtering/paging and exact Average ordering remain open.
