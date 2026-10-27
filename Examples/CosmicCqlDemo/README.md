# COSMIC CQL demo

## Source composition and projected All — C# walkthrough (24 examples verified 27 October 2026)

Read [SourceAndProjectedAllDemo.cs](SourceAndProjectedAllDemo.cs) first for eight
spaced, commented examples. The same code runs on MongoDB, SQLite and Neo4j:
nested source filtering, NOT with native LongCount, visibility at each hop,
selected filters/order/windows before All, true/false/empty results, and a
protected traversal error that survives OR and Take(0).

Run from `C:/Source/OASIS-cosmic`:

```powershell
& 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1' -Owner 'cosmic-source-demo' -Operation {
    & 'Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1' -CosmicCqlSourceDemo
}
```

The host reuses the installed runtimes and retained stores. It owns six synthetic
IDs per database, checks all examples, and verifies exact cleanup after success.
It refuses an unfinished fixture; inspect its journal before attempting recovery.
The demo class itself only reads. Do not supply production data to this fixture host.

Read console output in the worktree's `TestResults/Neo4jCurrent/`:
`cql-source-demo-MongoDBOASIS.log`, `cql-source-demo-SQLLiteDBOASIS.log`, and
`cql-source-demo-Neo4jOASIS.log`. The TRX and JSON summary are alongside them.
[Current evidence/status](../../Docs/WIP/COSMIC%20ORM/CURRENT_STATUS.md) records
verified counts and hashes. These are C# examples; HTTP syntax has its own
capability and endpoint contracts below.

## Federation demo — verified 26 October 2026

Read [V1FederationWalkthrough.cs](V1FederationWalkthrough.cs) for the spaced and
commented C# and HTTP examples. The SDK example preserves copies and returns a
five-row global page; the HTTP example chooses an explicit provider priority
before its two-identity global page. Neither claims cross-provider freshness.

Run `pwsh -File Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1 -Serve` from the OASIS
root using the retained environment. Open the printed Swagger URL, choose
`POST /api/data/query-cql/federation`, replace the fixture-tag placeholder with
the running demo tag, and use a fresh idempotency key. The demo supplies Alice's
bearer token at dispatch. Its federation admission allows three provider attempts;
leaf query admission remains one. Finish using the demo page for verified cleanup.

Both new global examples execute in the native gate alongside the existing
138SDK/138HTTP walkthrough. Latest full cohort526operations/608attempts passes, plus
13actual-shipping-controller federation requests with exact durable tariffs,
per-target private grants, revocation, limits and real encrypted metadata handling.
Sources and seven binary pairs match. Protected cursors now pass native, host/key-lifecycle and actual Windows process-restart gates. Causal agreement/distributed joins remain open. See the COSMIC ORM HTTP contract.

## Floating Swagger examples — verified 25 October 2026

Swagger now includes `SingleSum`, `SingleAverage`, `SingleTotalsAndMeans`,
`DoubleSum`, `DoubleAverage` and `DoubleTotalsAndMeans`. Pick a named example,
replace its fixture tag with the running demo's tag, select a provider and use
an authenticated demo identity with a new idempotency key per logical request.
The Average inputs are nullable; Sum inputs are required. Grouped filters use
canonical string thresholds and page after ordering the rounded means.

The generated Swagger assertions and full live walkthrough pass together:
138 SDK /138 signed HTTP queries,345settled operations/337attempts; native1/1,
zero skips. Source snapshots and six binary identities match. Evidence:
`TestResults/CosmicCqlDemo/floating-swagger-native.log` and
`floating-swagger-evidence.json`. This supersedes earlier numeric Swagger coverage.


## Floating arithmetic walkthrough

The walkthrough now contains **46 examples per provider**. Examples 41–43 use
Double; 44–46 use Single. Each sequence shows Sum, nullable Average and grouped
Sum/Average with result filters, ordering and paging. The same C# and HTTP code
runs against MongoDB, SQLite and Neo4j.

Source `OrderBy(...).Take(2)` chooses the two participating readings before
reduction. Group result `Take(1)` applies after aggregate filters and ordering.
Both domains accumulate exactly and round once using ties to even; Average keeps
the floating domain and its nullable result. Canonical HTTP result/filter strings
preserve precision in JavaScript. This differs intentionally from sequential
floating addition, which rounds after every step.

Read the spaced/commented [C# examples](V1NativeQueries.cs) first, then the matching
[HTTP requests](V1Walkthrough.cs). Browser presets `doubleSum`, `doubleAverage`,
`doubleNumericGroups`, `singleSum`, `singleAverage` and `singleNumericGroups` show
the same operations in the editable request form. All 18 offline preset checks
and the standalone demo build pass. Live **138 SDK /138 signed HTTP** verification passes across all three providers,
with345settled operations/337attempts, unchanged source snapshots and six matched
producer/consumer identities. Owned services and fixtures were cleaned up.

## Previous checkpoint: Decimal arithmetic walkthrough

That checkpoint contains **40 examples per provider**, with matching C#
and signed HTTP requests for MongoDB, SQLite and Neo4j. Live verification passes **120 SDK /120 signed HTTP** queries, with327settled
operations/319attempts. All six binary identities match and source snapshots stay
unchanged; owned services stop normally.

Start with [V1NativeQueries.cs](V1NativeQueries.cs) for spaced, commented C# and
[V1Walkthrough.cs](V1Walkthrough.cs) for the matching HTTP requests.

- **38 — Decimal Sum:** total two visible prices without losing the last digit.
- **39 — Decimal Average:** round a halfway result once, using ties-to-even.
- **40 — Decimal groups:** filter/order exact totals and rounded means, then page.

Browser presets: `decimalSum`, `decimalAverage`, `decimalNumericGroups`.
Swagger examples: `DecimalSum`, `DecimalAverage`, `DecimalTotalsAndMeans`.
Exact thresholds and Decimal responses use strings so JavaScript keeps every
digit. The standalone demo build and all 12 offline preset checks pass. Use the existing runner below to
verify the complete native SDK and signed HTTP walkthrough.

## Previous checkpoint: exact unsigned arithmetic on all three stores

That checkpoint has37 examples/provider and passes111 SDK/111 signed HTTP
queries on MongoDB, SQLite and Neo4j (318settled operations/310attempts).
Start with [V1NativeQueries.cs](V1NativeQueries.cs) for spaced, commented C#;
[V1Walkthrough.cs](V1Walkthrough.cs) shows matching HTTP requests.

- Example35: Sum one visible UInt64 counter and retain every digit.
- Example36: Average two counters; preserve a Decimal half-unit beyond Int64.
- Example37: Group source rows, then filter/order/page the exact mean.

Sum rejects overflow. Average can succeed when its numerator exceeds UInt64.
Browser and Swagger include unsigned arithmetic examples. Keep HTTP values as
strings; avoid JavaScript Number conversion. Run the existing repository-root
`Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1` for bounded verification, or add `-Serve`
for the loopback browser demo. It reuses retained resources and owns teardown.
Standalone build and9 offline UI checks pass. Earlier sections are historical.


## New v1: MongoDB, SQLite and Neo4j

```powershell
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1 -Serve
```

The walkthrough contains **40 C# SDK queries and 40 C# HTTP examples per provider (120 of each total)** on all three native databases. Serve then opens **http://127.0.0.1:5088/demo** after `TestResults/CosmicCqlDemo/v1-session.json` appears. Switch provider, user and example; edit spaced JSON and inspect formatted results/settlement. Repeat a key to show no replay.

Swagger `/swagger` has a prefilled v1 request, synthetic Alice authorization and an editable required Idempotency-Key. Use a new key for a new intent. **Finish demo & clean up** awaits requests, checks settlement and removes owned data. Reuses the retained native environment; failed fixtures remain for inspection. Synthetic tokens last one hour; no production account or charge.

Read **V1NativeQueries.cs** (LINQ-style SDK), **V1Walkthrough.cs** (C# HTTP), then **V1DemoPresentation.cs** / **V1SwaggerExample.cs** (presentation). The fixture's native acceptance matrix runs first. Transcript/result: `TestResults/CosmicCqlDemo/v1-walkthrough.txt` / `v1-demo-result.json`.

[Manual](../../Docs/WIP/COSMIC%20ORM/CQL_MANUAL.md) · [Architecture](../../Docs/WIP/COSMIC%20ORM/CQL_ARCHITECTURE.md) · [Presenter script](../../Docs/WIP/COSMIC%20ORM/Archive/CQL_DEMO_RUNBOOK.md).

## Original SQLite coffee ORM walkthrough

Real native SQLite queries through generic WEB4/Core HolonManager and Hyperdrive V2, plus tracking, conditional save/conflict and committed delta demonstrations.

From the repository root in PowerShell:

```powershell
.\Scripts\TestHosts\Run-CosmicCqlDemo.ps1
.\Scripts\TestHosts\Run-CosmicCqlDemo.ps1 -NoBuild -Serve
```

For the second command, open http://127.0.0.1:5087/swagger. Ctrl+C stops the loopback-only synthetic demo and cleans its owned database. The sample is not a production ONODE endpoint.

[Full walkthrough, requests and verification](../../Docs/WIP/COSMIC%20ORM/Archive/CQL_DEMO_RUNBOOK.md).

Read the source in this order:

1. **CoffeeWalkthrough.cs** — the annotated `Coffee` model, provider scope, planning/execution helper, then seven numbered examples. Each example explains the API and expected result. The `Check` calls are demo verification, not required application boilerplate.
2. **CoffeeApiHost.cs** — the HTTP equivalent: validate inputs, resolve scope, compose filters, sort/page, plan, execute and return results with provider provenance.
3. **Program.cs** — local host setup and synthetic fixture ownership/cleanup.

Start with examples 1–3 for CQL querying. Examples 4–6 show how queried holons participate in change tracking, conditional saves and committed deltas. Example 7 demonstrates an unsupported query being rejected before provider execution.


The current walkthrough has **twenty-seven** spaced/commented examples per provider: 81 SDK queries and 81 HTTP requests. Examples7–9 demonstrate LongCount, Count after a source window and access-protected Any. The scalar route is `/api/data/query-cql/scalar`; omitted take counts the complete visible source, and LongCount uses an exact Int64 decimal string. Example10 selects detached typed values through `/api/data/query-cql/projection`, preserving exact Int64 text, nullable leaves and nested metadata. Browser presets and Swagger also show `/api/data/query-cql/grouped` for native multi-aggregate reduction. The runner checks fresh actual completed counts; expanded scalar verification passes with 182settled operations/181physical attempts and scalar no-replay checks on all3 stores.

Projection transport now passes fullCore2,059 controls and signed native three-store acceptance, including visibility before paging, exact large integers/null/GUID/nested values, receipts and no replay. The new10-example SDK/HTTP walkthroughs now pass their expanded30/30 verification plus broad147native regression (195settled operations/190physical attempts); consult the current [HTTP projection contract](../../Docs/WIP/COSMIC%20ORM/Archive/CQL_PROJECTION_HTTP_CONTRACT.md) for limits and verified evidence.

Maintainer verification can combine the expanded walkthrough with the broad native save/delete/CQL regression in one environment lifecycle: `pwsh -File Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1 -IncludeRegression`. The same runner owns fresh-result checks and cleanup; `-Serve` remains the interactive mode.


## Verified Min/Max demonstrations

Examples11–12 demonstrate native Min/Max in both spaced/commented C# walkthroughs. Hidden roots contain stronger extrema; actor visibility is applied before reduction. Exact signed Int64 values remain decimal text. Browser presets and Swagger show the Selection shape.

Earlier Min/Max verification passed **175native regression tests,36SDK queries and36HTTP examples**, with zero skips on MongoDB, SQLite and Neo4j. The run records216settled operations/211physical attempts. Full Core/HTTP tests pass2,117. The exact retained fixture was reconciled before this fresh run; owned data was cleaned after verified teardown. [Current contract/evidence](../../Docs/WIP/COSMIC%20ORM/Archive/CQL_SCALAR_HTTP_CONTRACT.md#selected-http-and-demo-gate-verified--23-october-2026). Use current JSON/transcript evidence, not historical30/30results.


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


## Example 19: filter and order exact Decimal means

Reuse the `grouped` intent from example15, then compose selected-output operations before the result window:

```csharp
var averagePage = grouped
    .Where(row => row.Mean >= -9007199254740990.5m
        && row.Mean < -9007199254740990m)
    .OrderBy(row => row.Mean)
    .Take(1);
```

The two source rows determine participation before grouping. The result retains stock7, two items and exact mean `-9007199254740990.5`. Binary64 loses the half-unit at this magnitude; the SDK uses Decimal literals, while HTTP, browser and Swagger use invariant string thresholds. Native comparisons use the same normalized Decimal that CQL publishes, including the provider's independently tested round-to-even behavior. All three providers support these grouped Average operations.

The browser preset is **GroupBy / filter exact Decimal means**. Swagger prefills the same range predicate, result order and independent result limit. Fresh live acceptance passed:57 SDK queries/57 HTTP examples on all three providers, plus236 Neo4j/WEB4 native regression cases, zero failures/skips. The run records237 settled operations and232 provider attempts. Earlier54/54 evidence above retains its original scope.


## Examples 20–21: exact UTC dates

Example20 filters the `observedAt` metadata path between explicit UTC bounds and projects neighboring timestamps100ns apart. The SDK uses DateTime values with UTC kind. HTTP uses the DateTime scalar and exact Z text:

```json
{
  "metadataPath": ["observedAt"],
  "scalar": "DateTime",
  "operator": "GreaterThanOrEqual",
  "value": "2026-10-24T00:00:00.0000002Z"
}
```

Example21 groups timestamps and applies a selected-alias range before result ordering/Take. Its result filters use `alias: "ObservedAt"`; the selected schema determines DateTime type. Both paths use the same UTC parser. Results retain `2026-10-24T00:00:00.0000002Z` and `...0000003Z`, with UTC kind in C# and DateTime-tagged exact strings over HTTP.

Use browser presets **UTC dates / source range** and **UTC dates / grouped keys**. Swagger projection/grouped requests show the date shapes; scalar Average remains available in its own example. Offsets, missing timezone, invalid calendars, fractions wider than seven digits and non-string date input are rejected rather than converted or rounded. HTTP source ordering remains its existing canonical-field grammar; SDK metadata ordering and grouped selected-date ordering are explicit separate operations.

Fresh acceptance passed63 SDK queries/63 signed HTTP examples across all three providers, plus256 Neo4j/WEB4 native regression cases, zero failures/skips. The run recorded243 settled operations and238 provider attempts. Producer/native-host identities were captured while the global build lease was held. Owned fixture files and services were cleaned up after successful settlement. Earlier57/57 evidence retains its original scope.


## Examples22–23: UInt64 counters beyond signed Int64

Example22 selects counters in `[18446744073709551612,18446744073709551614)`.
The C# intent uses `ulong` bounds and native counter ordering. The HTTP projection
uses `scalar: "UInt64"`, `valueType: "UInt64"` and digit strings, ordering the two
selected rows by canonical Name. These are explicit different ordering requests.
The rows retain the exact counters for B-owned and C-public; no binary64 or signed
Int64 intermediate can collapse them.

Example23 groups the same counters, filters selected Counter aliases, then orders
the groups by Counter and takes two. Both C# and HTTP return Counter values
`18446744073709551612` and `18446744073709551613`, each with Items1. The result's
UInt64 schema tag and invariant strings preserve every bit for JavaScript clients.

Browser presets: **UInt64 / exact source range** and **UInt64 / grouped keys**.
Swagger offers **ExactUInt64** named examples for projection and grouping, while
retaining the UTC date schema/preset. Replace the fixture marker with the active
demo session tag, use its Alice bearer token, and use a fresh Idempotency-Key for
each new intent. Retain the same key if an outcome becomes uncertain.

Fresh acceptance passed69SDK/69HTTP examples,249 settled operations and244 provider
attempts across all three providers. Generated Swagger examples and native result
checks passed. This is the dedicated joint acceptance case, not a new full269-case
regression. The earlier63/63 UTC report retains its historical scope. Native
UInt64 and compiler coverage are documented in
[the contract](../../Docs/WIP/COSMIC%20ORM/Archive/CQL_UINT64_CONTRACT.md).


## Examples 24–25: Decimal prices with exact fractions

The fixture stores `price = 1m + i * 0.0000000000000000000000000001m`.
Examples 24–25 select B-owned/C-public prices
`1.0000000000000000000000000002` and
`1.0000000000000000000000000003`.

Example 24 filters/orders exact Decimal metadata and projects Name/Price.
Example 25 groups by price, filters the selected Price alias, orders it and takes
two groups. C# uses `decimal` and `m` literals; HTTP uses `Decimal` type tags and
string bounds. Both routes use HolonManager and actual provider admission.

Browser presets: **Decimal / exact price range** and **Decimal / grouped prices**.
Swagger named example: **ExactDecimal** on projection and grouping. Existing UTC
and UInt64 examples remain available. Decimal Sum/Average inputs are outside this
metadata enrollment.

The expanded live gate passes: **75 SDK queries and75 signed HTTP examples**,
255 settled operations and250 provider attempts, zero failures/skips. Compiler coverage passes
2,500 linked unit tests; native Decimal cohorts are recorded separately in the
[Decimal contract](../../Docs/WIP/COSMIC%20ORM/Archive/CQL_DECIMAL_METADATA_CONTRACT.md).
The reusable offline UI check is `node Scripts/TestHosts/Test-CqlV1DemoPresentation.cjs`;
it tests script behavior without browser interaction or network calls.


Current evidence: `TestResults/CosmicCqlDemo/v1-demo-result.json`,
`v1-walkthrough.txt`, `v1-binary-identities.json`,
`v1-source-identities.json` and `decimal-demo-native.log`.
Source snapshots were unchanged; producer/consumer identities were captured
inside the global build lease. The standalone demo project also builds without
restore. Owned fixtures/readiness files were removed and retained services
stopped. The initial obsolete Decimal-rejection control was changed to Double;
its original outcomes were verified before exact cleanup, with failure evidence
preserved. This is joint demo acceptance, not a new full native regression of all
three providers.

## Examples 26–27: finite Double readings

The fixture stores reading =1d+i*2.220446049250313e-16d. The source example
selects B-owned/C-public with canonical values1.0000000000000004 and
1.0000000000000007. The grouped example filters/orders the selected Reading
alias and publishes one item per distinct binary64 key. SDK uses double/d
literals; HTTP uses canonical invariant round-trip strings. Use Decimal for
exact decimal prices; Double is appropriate for binary64 measurements.

Browser presets Double / exact reading range and Double / grouped readings,
plus Swagger FiniteDouble examples, use the same bounds. Live acceptance passes81SDK/81signedHTTP,261settled operations/256attempts, zero failures/skips across all three providers. Source snapshots unchanged and all five binary pairs match. Four offline browser presets and standalone cached demo build pass.

## Examples 28–29: Single readings

Use `float` and `f` literals when the declared measurement domain is binary32.
The fixture stores a separate `singleReading` field with adjacent Single values.
Example 28 selects B-owned and C-public; example 29 groups those readings, then
filters and orders the selected alias before taking two groups.

```csharp
var minimum = 1.0000002f;
var maximum = 1.0000005f;

var readings = visible
    .Where(row =>
        CosmicQueryMetadata.Value<float>(row, "singleReading") >= minimum &&
        CosmicQueryMetadata.Value<float>(row, "singleReading") < maximum)
    .OrderBy(row => CosmicQueryMetadata.Value<float>(row, "singleReading"))
    .Select(row => new
    {
        row.Name,
        Reading = CosmicQueryMetadata.Value<float>(row, "singleReading")
    });
```

The complete executable examples include provider planning, error handling and
result checks. HTTP uses `Single` type tags and canonical strings such as
`"1.0000002"` and `"1.0000004"`. Browser presets are **Single / binary32 reading
range** and **Single / grouped binary32 readings**; Swagger exposes **FiniteSingle**.

Compiler/endpoint acceptance passes **2,595 tests**. All six browser presets pass
an offline check. Fresh live acceptance passes **87 SDK and 87 signed HTTP
examples**, with **267 settled operations and 262 provider attempts** across all
three providers. Sources stayed unchanged and all five binary pairs matched.
Owned fixtures and listeners were removed after teardown. This is a dedicated
live demo gate; the full provider regression suites retain their separate scope.

## Examples 30–31: native inner and left joins

The C# walkthrough (`V1NativeQueries.cs`) now shows typed `LeftJoin` and `Join`
through generic Core/V2 HolonManager on each priority provider. Both inputs keep
their actor scope and independent filters/order. Example30 retains B-owned and
D-owned without a right row; C-public matches and preserves9007199254740993
exactly. Example31 changes the join kind and publishes only the matching pair.

`V1Walkthrough.cs` sends equivalent bounded requests to
`POST /api/data/query-cql/join`. The browser offers **Left join** and **Inner join**
presets, and Swagger includes a left-join request. The right output is nullable
for unmatched rows; `hasRight` independently distinguishes absent rows from
nullable data. Replace both Swagger fixture tags with the current session tag.

The updated runner requires **93 SDK queries /93 HTTP requests** (31/provider).
Fresh live verification passes93SDK/93HTTP,288settled/283physicalattempts, zero skips. Generated Swagger join route/schema checks pass. Standalone demo builds with zero errors. Production browser JavaScript syntax and six provider/kind presets pass offline.

Earlier native cursor checkpoint: twenty signed shipping-controller requests, eighteen settlements/thirty-two attempts, duplicate-claim rejection, pinned scope, host restart and key rotation/revocation pass. Full cohort523operations599attempts, unchanged sources/sevenbinarypairs. The presenter UI examples above remain separate; use the CQL manual's opt-in cursor enrollment/request sequence. Actual same-user Windows cursor process persistence is now verified separately; other key deployments and broader distributed features remain open.

Actual cursor process acceptance: abrupt first-host termination, a different fresh host, unchanged DPAPI-protected persisted key, three native pages/nine attempts, and duplicate rejection pass. Full cohort526/608; test keys/config/fixtures removed after verified teardown. See the CQL manual for deployment boundaries.

## C# element-terminal examples

[V1ElementQueries.cs](V1ElementQueries.cs) contains spaced, commented examples
47–58 for First/FirstOrDefault/Single/SingleOrDefault. The same code runs on
MongoDB, SQLite and Neo4j through HolonManager. Native queries request at most
one or two rows, preserve source windows, and distinguish empty/default results
from provider failures or ambiguous Single results. See the
[manual](../../Docs/WIP/COSMIC%20ORM/Archive/CQL_MANUAL_history_2026-10-27.md#native-element-terminals-c-sdk).
These element-terminal examples occupy cases 47–58; operator examples 59–90
follow them. The separate HTTP walkthrough has 46 cases per store and no new
element-terminal route.

## C# operator examples

[V1OperatorQueries.cs](V1OperatorQueries.cs) contains examples 59–90 for metadata
null/missing, field comparisons, closed-set membership, ordinal strings and
secondary ordering with actual tied primary values. The same 32 assertions run
on all three native databases. Total: 90 SDK cases/store (270), with the separate
HTTP walkthrough retaining 46/store (138). No provider-specific demo engine or
client-side source scan is used.

## Save lifecycle examples

[V1SaveQueries.cs](V1SaveQueries.cs) demonstrates ten save steps on each native
store: generated ID ownership, no-op saves, dirty detection, pending previews,
committed path summaries, typed reload, rejected intents, hard deletion and
historical summary lookup. The host supplies the trusted protection policy and
explicit write-operation identities. The gate executes 30 save checks alongside
270 SDK query cases and 138 HTTP cases.

The scoped investor demo set is green. Full historical-value/decryption access
and broader EF/NHibernate parity remain tracked gaps; redacted summaries do not
complete that larger history API.

## Integer-index examples

[V1IndexedQueries.cs](V1IndexedQueries.cs) contains spaced/commented examples
91–105 for zero-based integer indexing. They cover source windows, empty input,
negative defaults and range errors through HolonManager on all three stores.
The SDK walkthrough now has 105 cases/store (315), plus 138 HTTP cases and
30 save checks. System.Index/from-end overloads remain separate red work.

## Last and from-end examples

[V1TailQueries.cs](V1TailQueries.cs) contains examples 106–135 for Last and
System.Index after a source window. The same native code path covers graphs,
null/tie ranks and exact scalar ordering. Total SDK walkthrough:135cases/store
(405), plus138HTTPcases and30savechecks. All remains unimplemented.

### Scalar All examples (136–157)

See `V1AllQueries.cs` for universal predicates over an already composed source:
stock checks, empty input, null/missing metadata, ordinal strings, membership,
field comparisons and source windows. `All` evaluates after Skip/Take; an empty
source returns true. Child/descendant filters may define the source. Traversal
inside the terminal predicate is still unsupported. Use the same scalar planner
and manager executor as the other scalar examples. All 22 cases passed on each
of MongoDB, SQLite and Neo4j in the read-only retained-fixture regression; the
complete extended walkthrough has not yet passed end to end.

Direct-child `All` examples 158–173 now demonstrate native child existence after
paging, visibility, negation, Boolean combinations, multiple child predicates
and a descendant-filtered source. All 38 cases in `V1AllQueries.cs` pass on each
store (114 checks). Nested-child and descendant traversal inside the terminal
remain unsupported. The complete extended walkthrough rerun remains pending.

Nested-child All examples 174–181 demonstrate two/three navigation levels,
per-level visibility, child scalar/Boolean predicates and literal handling.
All 46 All examples pass on each store (138 checks). The trusted host-only
positive example explicitly omits access filtering; the secured version
excludes its hidden intermediate parent. Descendant All remains unsupported;
the full extended walkthrough rerun remains pending.

Bounded descendant All examples 182–201 use
`CosmicQueryRelations.AnyDescendant(parent, predicate, bounds)` after the source
page. They demonstrate depth, visibility, independent traversal predicates and
node-budget errors that Boolean branches cannot hide. All 66 All examples pass
per store (198 checks). Native cycle proof and nested child/descendant composition
remain incomplete, and the complete extended walkthrough has not yet passed.

Child-to-descendant All examples202–213 now show descendant matching inside
Children.Any, with separate scalar admission and per-level actor visibility.
All78All examples/store pass (234checks), plus24cycle and27protected-child
checks. The latter prove that earlier matches and Boolean branches cannot hide
another child's traversal failure. Relationships inside descendant predicates
and mixed-scope cycle proof remain incomplete; full639SDKwalkthroughpending.

Examples214–227 now show child/descendant relationships inside descendant
predicates, with independent bounded scopes, actor visibility, source windows
and nested error propagation. All92All examples/store pass (276checks), plus
114scope/cycle checks. Projected-source All is still missing; full Q23 stays red.
The full681SDKwalkthrough rerun remains pending.
# Cloud row walkthrough (Cosmos DB, limited demo profile)

[`CloudRowsWalkthrough.cs`](CloudRowsWalkthrough.cs) shows the common native row
subset through `HolonManager` and standard LINQ, with explicit provider scope,
owner/public visibility, nested metadata, a bounded page, DTO projection and
Count/LongCount/Any. It has no provider-specific query engine or storage setup.

The trusted host supplies an explicitly activated provider, a V2 manager, an
authenticated actor and synthetic fixtures tagged with metadata key `fixture`.
Matching records need `IsActive`, `Name`, `Version` and `location.city` metadata.

```csharp
// The host owns activation, credentials and synthetic fixture cleanup.
// cosmos implements the shared ICosmicQueryStorageProvider contract.
var report = await CloudRowsWalkthrough.RunAsync(
    manager,
    cosmos,
    authenticatedActorId,
    "cosmic-cloud-rows-demo",
    Console.Out,
    cancellationToken);
```

This component is exercised verbatim by the Cosmos native integration gate. It is
not a new command-line switch for the existing SQLite console/Swagger host.
The existing three-database walkthroughs demonstrate a wider profile; Cosmos
numeric metadata, compound index coverage, save-state/concurrency/delta integration,
HTTP demo and hosted Azure acceptance remain open. Local emulator proof must be
labeled as such. Each query is an independent read, not one shared snapshot.
