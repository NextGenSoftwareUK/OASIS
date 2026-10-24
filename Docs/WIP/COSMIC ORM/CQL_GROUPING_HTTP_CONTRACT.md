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
  "resultSkip": 1,
  "resultTake": 5,
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

Verification: **2,314 Core/linked-production unit tests pass, zero skips**, including 17 new compiler cases and four endpoint cases covering all three profiles and unsupported Average dispatch prevention. Evidence: TestResults/CosmicCurrent/http-group-order-full-current.trx and TestResults/CosmicCqlDemo/http-group-order-full.log. These are controlled endpoint tests; live three-provider HTTP ordering demonstration evidence is recorded below. Baseline live grouping evidence above predates resultOrder. Post-group filtering/paging and exact Average ordering remain open.


## Ordered groups live demonstration — 24 October 2026

Example16 now demonstrates native grouped ordering in both readable C# and HTTP walkthroughs: group visible rows by Name, select exact Int64 Sum, order Total descending then Name ascending. Three adjacent negative totals beyond2^53 verify actual values and ordering. Browser preset **GroupBy / ordered exact totals** and grouped Swagger resultOrder fields expose the same feature. Source order/windows remain separate.

The retained runner completed **48 SDK queries and48 HTTP examples** across MongoDB, SQLite and Neo4j; **228 operations settled and223 provider attempts**. Actual session96782 terminal0, integrated native test1pass/0skip. This is live demo acceptance, not a rerun of every provider suite. Evidence: TestResults/CosmicCqlDemo/ordered-group-demo-native.log, v1-demo-result.json and v1-walkthrough.txt; TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx. Producer/copy checks verified Core19F1CA8B and SharedDF5015E4. Node24.19.0 syntax check passed for the browser script. Fixture manifest/SQLite/WAL/SHM verified absent after owned cleanup; Mongo27029 and Neo17687 listeners absent.

HTTP result ordering live acceptance is complete for the enrolled operations. Exact Average ordering, post-group filters/pages and the broader unchecked CQL/ORM roadmap remain open.


## HTTP grouped result window — 24 October 2026

`resultSkip` defaults to zero and `resultTake` is optional. They apply to reduced groups after `resultOrder`, independently of source `skip`/`take`. `resultTake: 0` returns an empty published page while still validating every bounded proof packet. Omission preserves all bounded results. Negative values or values/offsets above `maximumGroups` reject before dispatch; `maximumGroups` and the 1 MiB proof budget still bound complete reduced proof, including off-page groups.

The compiler binds the existing shared Skip/Take intent. Actual providers must advertise SupportsResultPaging; source paging or ordering support cannot grant it. Native MongoDB, SQLite and Neo4j enrollment is recorded in their provider docs. Core validates all proof and native membership before any selected DTO constructor executes.

Controlled verification: **2,334 Core/linked-production tests pass, zero skips**, including seven new request compiler cases and five endpoint cases (all three profiles, empty publication with retained proof and unenrolled paging rejected before dispatch). Evidence: TestResults/CosmicCurrent/http-group-page-current.trx and TestResults/CosmicCqlDemo/http-group-page-unit.log. Example17 has been prepared for both SDK/HTTP/browser/Swagger; its live three-provider acceptance is pending the other session's reviewed Mongo fixture cleanup. Prior48/48demo evidence above does not certify paging.


## HTTP grouped paging live acceptance — 24 October 2026

`resultSkip`/`resultTake` now run through native paging on MongoDB, SQLite and Neo4j. Example17 shows the same query in spaced/commented C# and HTTP, with browser and Swagger fields: group by Name, sum exact Int64 values, order Total descending/Name ascending, then Skip1/Take1. The selected C-public total remains -9007199254740990; source windows remain independent. Complete bounded proof includes skipped groups.

Verification: **2,334 Core/linked-production unit tests pass, zero skips**; the fresh integrated native demo passes **51 SDK queries and51 HTTP examples**, **231 settled operations and226 physical attempts** across all three providers (actual32210terminal0, native1PASS0skip). Evidence: TestResults/CosmicCurrent/http-group-page-current.trx; TestResults/CosmicCqlDemo/http-group-page-unit.log, group-page-demo-native.log, group-page-demo-artifacts.json, v1-demo-result.json and v1-walkthrough.txt; TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx. Producer/native copies match for Core and all three providers; runner verifies Core051A0664/SharedD5273C49. Browser script passes Node24.19.0 syntax check. This is the integrated demo acceptance test, not an all-suite provider rerun.

The other session completed reviewed fixture cleanup and its Mongo historical-type regressions before granting the live window. No blind reset/replay occurred. Final owned fixture marker/SQLite/WAL/SHM are absent, Mongo27029 and Neo17687 listeners absent. Post-group filtering, exact Average ordering and broader unchecked roadmap gates remain open.

## Selected-result filters

`resultFilters` is an optional array of at most 32 ANDed predicates. Each predicate names an exact, case-sensitive selected output `alias`, an `operator` from the existing CQL operator enum, and a typed `value`. These predicates run after reduction and before `resultOrder`, `resultSkip` and `resultTake`. Source filters/windows remain independent.

```json
"resultFilters": [
  { "alias": "TotalStock", "operator": "GreaterThan", "value": "9007199254740993" },
  { "alias": "Category", "operator": "StartsWith", "value": "Coffee" }
]
```

Int64 and Decimal literals must be invariant strings; Int32 literals are JSON integers, Boolean literals are JSON booleans, GUIDs use D-format strings, and strings are bounded to 512 characters. Decimal strings must round-trip exactly through invariant Decimal formatting: excess precision is rejected rather than rounded. String comparisons use ordinal semantics. `IsNull` omits `value` and requires a nullable selected column; equality/inequality can also use an explicit null. `Exists` is rejected because every selected alias already exists. This initial HTTP grammar does not expose OR, membership, field comparisons, dates or enums for result predicates; those require an explicit later transport extension.

Binding the request captures shared Core intent; it grants no provider permission. The actual adapter must separately enroll the selected key or aggregate type and comparison. Average filtering is now enrolled on SQLite, MongoDB and Neo4j after their exact-normalization native gates. The54/54 live demo certifies Sum filtering; it is not new live HTTP Average evidence. All bounded proof groups remain subject to shared validation, including nonmatching and off-page groups. Authorization, physical attempts and receipts retain their existing owners.

Compiler verification: 2,386 full Core unit tests passed, zero skips, before the added controlled endpoint cases. Final endpoint regression evidence is pending; this is not yet a new live demo certification.

Final controlled verification: actual test session92762 terminated successfully; 2,389 full Core unit tests passed, zero failures/skips, including HTTP filtering checks for each of MongoDB/SQLite/Neo4j. Evidence: TestResults/CosmicCurrent/http-group-filter-final-current.trx and http-group-filter-artifacts.json. Live native demo extension remains pending; this checkpoint does not replace provider-native evidence.


Grouped-filter live checkpoint —24 October2026: actual74024terminal0, native demo1PASS0skip plus replica-init1PASS;54 SDK queries/54 HTTP examples across MongoDB/SQLite/Neo4j,234 settled operations/229 provider attempts. Example18 verifies exact Int64 selected-Sum filtering before matching-group paging; browser syntax and generated Swagger filter example pass. Producer/native Core4C58BC9D, Shared8F33C322, Mongo9DC9681C, SQLiteEA7573D5, Neo609661BC match; exact paths/bytes/SHA256 retained TestResults/CosmicCqlDemo/group-filter-demo-artifacts.json. Fresh report/transcript/log: v1-demo-result.json, v1-walkthrough.txt, group-filter-demo-native.log. This is scoped demo acceptance, not a new full native regression or full roadmap certification. Remaining Average normalization/order/filter, scalar and broader roadmap gates remain open.
