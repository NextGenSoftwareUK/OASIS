# COSMIC CQL demo

## New v1: MongoDB, SQLite and Neo4j

```powershell
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1 -Serve
```

Console runs **fourteen C# SDK queries and fourteen C# HTTP examples** on all three native databases. Serve then opens **http://127.0.0.1:5088/demo** after `TestResults/CosmicCqlDemo/v1-session.json` appears. Switch provider, user and example; edit spaced JSON and inspect formatted results/settlement. Repeat a key to show no replay.

Swagger `/swagger` has a prefilled v1 request, synthetic Alice authorization and an editable required Idempotency-Key. Use a new key for a new intent. **Finish demo & clean up** awaits requests, checks settlement and removes owned data. Reuses the retained native environment; failed fixtures remain for inspection. Synthetic tokens last one hour; no production account or charge.

Read **V1NativeQueries.cs** (LINQ-style SDK), **V1Walkthrough.cs** (C# HTTP), then **V1DemoPresentation.cs** / **V1SwaggerExample.cs** (presentation). The fixture's native acceptance matrix runs first. Transcript/result: `TestResults/CosmicCqlDemo/v1-walkthrough.txt` / `v1-demo-result.json`.

[Manual](../../Docs/WIP/COSMIC%20ORM/CQL_MANUAL.md) · [Architecture](../../Docs/WIP/COSMIC%20ORM/CQL_ARCHITECTURE.md) · [Presenter script](../../Docs/WIP/COSMIC%20ORM/CQL_DEMO_RUNBOOK.md).

## Original SQLite coffee ORM walkthrough

Real native SQLite queries through generic WEB4/Core HolonManager and Hyperdrive V2, plus tracking, conditional save/conflict and committed delta demonstrations.

From the repository root in PowerShell:

```powershell
.\Scripts\TestHosts\Run-CosmicCqlDemo.ps1
.\Scripts\TestHosts\Run-CosmicCqlDemo.ps1 -NoBuild -Serve
```

For the second command, open http://127.0.0.1:5087/swagger. Ctrl+C stops the loopback-only synthetic demo and cleans its owned database. The sample is not a production ONODE endpoint.

[Full walkthrough, requests and verification](../../Docs/WIP/COSMIC%20ORM/CQL_DEMO_RUNBOOK.md).

Read the source in this order:

1. **CoffeeWalkthrough.cs** — the annotated `Coffee` model, provider scope, planning/execution helper, then seven numbered examples. Each example explains the API and expected result. The `Check` calls are demo verification, not required application boilerplate.
2. **CoffeeApiHost.cs** — the HTTP equivalent: validate inputs, resolve scope, compose filters, sort/page, plan, execute and return results with provider provenance.
3. **Program.cs** — local host setup and synthetic fixture ownership/cleanup.

Start with examples 1–3 for CQL querying. Examples 4–6 show how queried holons participate in change tracking, conditional saves and committed deltas. Example 7 demonstrates an unsupported query being rejected before provider execution.


The current walkthrough has **eighteen** spaced/commented examples per provider: 54 SDK queries and 54 HTTP requests. Examples7–9 demonstrate LongCount, Count after a source window and access-protected Any. The scalar route is `/api/data/query-cql/scalar`; omitted take counts the complete visible source, and LongCount uses an exact Int64 decimal string. Example10 selects detached typed values through `/api/data/query-cql/projection`, preserving exact Int64 text, nullable leaves and nested metadata. Browser presets and Swagger also show `/api/data/query-cql/grouped` for native multi-aggregate reduction. The runner checks fresh actual completed counts; expanded scalar verification passes with 182settled operations/181physical attempts and scalar no-replay checks on all3 stores.

Projection transport now passes fullCore2,059 controls and signed native three-store acceptance, including visibility before paging, exact large integers/null/GUID/nested values, receipts and no replay. The new10-example SDK/HTTP walkthroughs now pass their expanded30/30 verification plus broad147native regression (195settled operations/190physical attempts); consult the current [HTTP projection contract](../../Docs/WIP/COSMIC%20ORM/CQL_PROJECTION_HTTP_CONTRACT.md) for limits and verified evidence.

Maintainer verification can combine the expanded walkthrough with the broad native save/delete/CQL regression in one environment lifecycle: `pwsh -File Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1 -IncludeRegression`. The same runner owns fresh-result checks and cleanup; `-Serve` remains the interactive mode.


## Verified Min/Max demonstrations

Examples11–12 demonstrate native Min/Max in both spaced/commented C# walkthroughs. Hidden roots contain stronger extrema; actor visibility is applied before reduction. Exact signed Int64 values remain decimal text. Browser presets and Swagger show the Selection shape.

Earlier Min/Max verification passed **175native regression tests,36SDK queries and36HTTP examples**, with zero skips on MongoDB, SQLite and Neo4j. The run records216settled operations/211physical attempts. Full Core/HTTP tests pass2,117. The exact retained fixture was reconciled before this fresh run; owned data was cleaned after verified teardown. [Current contract/evidence](../../Docs/WIP/COSMIC%20ORM/CQL_SCALAR_HTTP_CONTRACT.md#selected-http-and-demo-gate-verified--23-october-2026). Use current JSON/transcript evidence, not historical30/30results.


## Numeric examples — verified across all three providers

Examples 13–14 add Sum and Average to both C# walkthroughs, with matching browser presets. Sum returns the exact visible Int64 total. Average explicitly orders by Name and takes two rows before reduction, returning the Decimal fraction `-9007199254740990.5`; the half-unit demonstrates why the HTTP value is text rather than a JavaScript Number. Swagger opens the same Average selection/window shape.

The numeric provider and endpoint unit gates are verified; the new 42 SDK/42 HTTP walkthrough passes with the full195-case native regression and zero skips. The 175/36/36 result above is historical Min/Max evidence and does not certify these numeric additions. Run the existing fixed demo runner with `-IncludeRegression` for current verification.

Current numeric demo evidence: **195 native tests, 42 SDK queries, 42 HTTP examples, 222 settled operations/217 physical attempts**. Exact fractional Decimal output is verified on all three stores. See the fresh numeric-demo-native.log, v1-demo-result.json and v1-walkthrough.txt under TestResults/CosmicCqlDemo.


## Grouped CQL live demo - 24 October 2026

The retained signed HTTP demo now passes on MongoDB, SQLite and Neo4j: **45 SDK queries,45 HTTP examples,225 settled operations and220 provider attempts**. Each provider runs15 spaced/commented examples; example15 groups an ordered two-row source window by stock and verifies LongCount, exact Int64 Sum and Decimal Average. Browser UI and Swagger include grouped presets. This is the single integrated live-demo acceptance test, not a rerun of every provider regression suite.

The first grouped HTTP attempt exposed missing admission-path registration. The pinned failed fixture was reconciled against194 settled operations/189 attempts and all146 owned rows per store before scoped cleanup; no reset or blind replay occurred. Core's exact CQL registry now drives hosted admission/Swagger and production tariff checks. The corrected live run completed, receipts settled, owned fixture files were removed and runner services stopped.

Evidence: TestResults/CosmicCqlDemo/v1-demo-result.json, v1-walkthrough.txt and grouped-demo-native-verified.log; TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx. Executed producer copies match Core594AAEDB and Shared8E034BFB. Reconciliation evidence: grouped-fixture-reconcile.log, neo4j-cql-v1-web4-reconcile-current.trx and cql-v1-web4-reviewed-grouped-admission-reconciliation.json. The old42/42 numeric demo result remains historical. Post-group filters/order/pages, joins, federation and other unchecked roadmap gates remain open.

Run `Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1 -Serve` for the browser demo at `http://127.0.0.1:5088/demo` or `/swagger`. Use **Finish demo** for verified settlement and cleanup. Start with `V1NativeQueries.cs` for C# and `V1Walkthrough.cs` for HTTP; example15 in both files uses the same grouped semantics on every provider.


## Example 16: order aggregate results

`V1NativeQueries.cs` groups visible rows by name, selects an exact Int64 Sum, then calls `OrderByDescending(group => group.Total).ThenBy(group => group.Name)`. The same query runs unchanged on MongoDB, SQLite and Neo4j. Three totals differ by one beyond JavaScript's safe integer range; the demo verifies both their exact values and descending order.

`V1Walkthrough.cs` sends the HTTP equivalent with `resultOrder: [{ alias: "Total", descending: true }, { alias: "Name" }]`. These are selected output aliases. Source `orderBy`, `skip` and `take` still apply before grouping. Browser preset **GroupBy / ordered exact totals** and the grouped Swagger example show the same intent. Mean/Average remains selectable, but Average result ordering is not yet enrolled.

Live verification passed on all three providers: **48 SDK queries, 48 HTTP examples, 228 settled operations and 223 physical provider attempts**. The 45/45 result above is historical baseline evidence. The runner verified settlement and removed the owned fixture; retained Mongo and Neo4j services were stopped.


## Example 17: page grouped results

The C# example calls `orderedGroups.Skip(1).Take(1)`. HTTP sends `resultSkip: 1` and `resultTake: 1` alongside resultOrder. All three stores should return C-public with exact total -9007199254740990. Source skip/take remain independent; complete reduced proof still consumes maximumGroups/payload limits and is validated even outside the published page. Browser preset **GroupBy / independent result page** and Swagger show these fields. Comments and blank lines separate source selection, grouping, result ordering and result paging.

The expanded live walkthrough passes on all three providers: **51 SDK queries,51 HTTP examples,231 settled operations and226 provider attempts**. Example17 returns the exact expected page; Swagger paging fields and browser script syntax are verified. Owned fixtures/services are cleaned up. Prior48/48 evidence is historical.

## Example 18: filter grouped totals before paging

The C# walkthrough now includes a post-group predicate:

```csharp
var matchingPage = orderedGroups
    .Where(group => group.Total > -9007199254740991L)
    .Skip(1)
    .Take(1);
```

`orderedGroups` is the selected, ordered group intent from example16. Resolve the returned `OASISResult` and plan/execute it through the same shared grouped owner shown in `V1NativeQueries.cs`. The predicate excludes B-owned; Skip removes D-owned from the matching groups; C-public is published with exact total -9007199254740990. Complete proof still includes excluded and off-page groups. Move source filters before GroupBy when the intent is to change aggregate participation instead.

The HTTP walkthrough posts `resultFilters` to `/api/data/query-cql/grouped`, using the selected alias `Total` and invariant string `"-9007199254740991"`. Browser preset **GroupBy / filter exact totals** and Swagger show the same request grammar. Int64 result-filter values use strings so browser number parsing cannot round them. Only matching groups consume resultSkip; maximumGroups still bounds every reduced proof group.

Expected expanded walkthrough: eighteen examples per provider,54 SDK queries and54 HTTP requests. Fresh live certification passed on all three providers:54 SDK/54 HTTP,234 settled operations/229 provider attempts, zero skips. Evidence: TestResults/CosmicCqlDemo/group-filter-demo-native.log, v1-demo-result.json and v1-walkthrough.txt. Earlier51/51 results above remain historical evidence.
