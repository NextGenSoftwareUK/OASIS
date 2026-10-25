# CQL demos: native v1 and typed coffee ORM

## Decimal live demo acceptance - 25 October 2026

The walkthrough now has **40 examples per provider**. **120 SDK /120 signed HTTP**
queries pass on MongoDB, SQLite and Neo4j, with **327 settled operations /319
provider attempts**. The joint native gate passes1/1 without skips (1m8s).
Source snapshots are unchanged and all six producer/consumer identities match.
All12offline browser preset checks pass. Owned services stopped normally; the
owned SQLite database/sidecars and readiness manifest are absent.

Examples38-40 demonstrate exact Decimal Sum, a final ties-to-even Average and
grouped filters/order/pages. Browser presets are `decimalSum`, `decimalAverage`
and `decimalNumericGroups`; Swagger includes `DecimalSum`, `DecimalAverage` and
`DecimalTotalsAndMeans`. Exact thresholds and results remain strings for clients.

Evidence: TestResults/CosmicCqlDemo/decimal-numeric-demo.log, v1-demo-result.json,
v1-walkthrough.txt, v1-source-identities.json, v1-binary-identities.json and
v1-decimal-ui-check.json; native TRX: TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx.
Standalone demo build also passes. Full CQL/ORM remains
open; this checkpoint does not close policy, federation or other numeric domains.


## UInt64 live demo acceptance - 25 October 2026

The walkthrough now has **37 examples per provider**: **111 SDK /111 signed HTTP**
queries pass on MongoDB, SQLite and Neo4j, with318 settled operations/310 attempts.
The joint native demo passes1/1 without skips. Source snapshots remain unchanged;
six producer/consumer identities match under the build lease. Standalone demo
builds and9 offline browser preset checks pass. Owned services stopped normally.

Examples35-37 show exact UInt64 Sum, Decimal Average retaining the fraction
18446744073709551612.5, and grouped mean filters/order/pages. Browser presets:
`unsignedSum`, `unsignedAverage`, `unsignedMeanGroups`. Swagger adds named UInt64
Sum/Average examples. These close the unsigned numeric live-demo gate, while
other numeric input domains, sharing/tenant policy, graph-source composition and
federation remain open.

Evidence in TestResults/CosmicCqlDemo: unsigned-numeric-demo.log,
v1-demo-result.json, v1-walkthrough.txt, v1-source-identities.json,
v1-binary-identities.json, unsigned-numeric-demo-build.log and
v1-decimal-ui-check.json. Native report:
TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx.


## Three-provider v1 walkthrough — 25 October2026

```powershell
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1 -Serve
```

The first command runs 102 annotated SDK queries and102 HTTP examples (34 per provider) on MongoDB, SQLite and Neo4j. With Serve, open **http://127.0.0.1:5088/demo** once `TestResults/CosmicCqlDemo/v1-session.json` appears. Transcript: `v1-walkthrough.txt`; result: `v1-demo-result.json`. Swagger `/swagger` is prefilled, authorized as synthetic Alice and documents Idempotency-Key.

The dedicated runner reuses restored dependencies and rejects stale reports. Example5 demonstrates independent visible-child existence on each provider; the browser Visible child existence preset compares Alice and Visitor. Fresh23October evidence: Core1799/joint96/demo1 allpass,15SDK/15HTTP,131settled synthetic operations.

### Five-minute presenter script

1. Alice + MongoDB + Visibility before paging returns **C-public**: access filtering precedes Skip/Take.
2. Repeat on SQLite and Neo4j: same semantics, actual native execution and explicit provenance.
3. Nested metadata + protected OR returns **D-owned**, not A-private. Visitor sees public data; Anonymous rejects.
4. Show prefix/suffix, null/existence, GUID, Boolean and numeric-string examples. Exact Int64 preserves **9007199254740993** without JavaScript rounding.
5. Repeat the last key: **409**, no second execution. A new intent gets a new key. Show settlement status.
6. Execute Swagger's example. Change Idempotency-Key for a new intent/provider. Swagger defaults to Alice.
7. Choose **LongCount / complete visible input**: Alice gets the Int64 string `"3"` on all three stores. Show Count after Skip1/Take1 (Int32 `1`) and Any against A-private (Boolean `false` for Alice). The JSON route label changes to `/api/data/query-cql/scalar`; no implicit row Take is applied. Repeat keeps the original route and key.
8. Choose **UInt64 / exact source range**, then **UInt64 / grouped keys**. Bounds and output values remain strings beyond Int64/JavaScript precision; grouped results order18446744073709551612 before18446744073709551613. Swagger includes **ExactUInt64** named examples alongside UTC dates.
9. Choose **Decimal / exact price range**, then **Decimal / grouped prices**. B-owned and C-public differ by10^-28: both digits survive native queries and HTTP string output. Swagger **ExactDecimal** shows the same source and selected-alias bounds. Examples24–25 have matching commented C# SDK and HTTP code. The expanded live75/75 gate now passes on all three providers, with255 settled operations/250 attempts.
10. Choose **Composition / filter after source page** to get D-owned. Choose **page before filter** to get no rows, then **exact values and result page** to get C-public. Examples32–34 show the same stage order in C# and HTTP; change providers without changing the query shape.
11. **Finish demo & clean up** waits, verifies settlement, stops services and removes only owned fixtures.

Shipping CQL/JWT/admission code runs over real providers and a Mongo outbox. Avatar-directory/reservation authority are controlled synthetic fixtures; no production account/charge. Tokens last one hour and are not recorded in transcripts/readiness/ledger. Run one leased demo at a time. Interrupted/failed fixtures require inspection, not reset/replay.

Read `Examples/CosmicCqlDemo/V1NativeQueries.cs`, then `V1Walkthrough.cs`: spaced, numbered examples with expected results. [Manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md). The original SQLite coffee demo below additionally covers Save/tracking/deltas/conflicts.

## Original SQLite coffee walkthrough

For the authenticated production WEB4 v1 route over MongoDB, SQLite and Neo4j, see [request examples and operator setup](CQL_V1_OPERATIONS.md) and [native acceptance](CQL_V1_MILESTONE_3.md). The examples below remain isolated runnable demonstrations.

21 October 2026. Two runnable demonstrations use real WEB4/Core HolonManager, Hyperdrive V2 and the native SQLite CQL adapter. Five synthetic coffee records live only in a dedicated owned fixture. These examples do not register a production ONODE route or implement its authentication, billing admission or provider discovery.

## Quick start

From `C:/Source/OASIS` in PowerShell:

```powershell
# Build with bounded workers, check runtime identities, run and verify the console walkthrough.
.\Scripts\TestHosts\Run-CosmicCqlDemo.ps1

# Reuse the verified build for a fast repeat, without dependency restore or compilation.
.\Scripts\TestHosts\Run-CosmicCqlDemo.ps1 -NoBuild

# Start the same executable as a loopback-only API with Swagger.
.\Scripts\TestHosts\Run-CosmicCqlDemo.ps1 -NoBuild -Serve
```

Then open [Swagger](http://127.0.0.1:5087/swagger). Expand **POST /api/cql/coffees**, choose **Try it out**, and run its prefilled working request (or submit the JSON below). Ctrl+C stops the host and removes the owned database. The documented `/api/cql/demo/shutdown` endpoint also requests graceful shutdown of this synthetic demo host only.

Do not use `-NoBuild` after editing source until rebuilding. Build scripts share the repository's producer lease; an occupied lease rejects rather than running overlapping builds. Interactive hosting holds that lease until it stops. The sample uses installed .NET10/ASP.NET, the existing cache and fixed output directory; no blockchain/node/cloud service is started.

## A five-minute walkthrough

1. Show the annotated `Coffee : Holon` model and the query expressions in [CoffeeWalkthrough.cs](../../../Examples/CosmicCqlDemo/CoffeeWalkthrough.cs). Properties use the existing CustomOASISProperty marker and compile to their persisted metadata paths.
2. Run the console demo. The first native query combines typed properties, `Stock > Reserved`, descending rating, price ties and Take. It returns Iced Coffee, Espresso and Flat White.
3. Show nested metadata and membership: London plus coffee/tea categories returns Espresso, Flat White and Green Tea. A separate native Skip/Take query returns Flat White and Latte.
4. Show that query results have clean tracking. An unchanged Save creates no revision; an edited Espresso advances to version2, rating10 and stock19, returning its operation-bound committed delta summary.
5. Show a second independent provider/manager client trying to save its stale revision: `COSMIC_CONCURRENCY_CONFLICT`. Its local edits remain dirty and a fresh query observes the winning values.
6. Show unsupported decimal metadata filtering rejected by capability admission at planning, without client evaluation or a silently substituted provider.
7. Start Swagger and vary rating/category/city, native sorting and paging. Results preserve the provider identity. The host runs the same verified walkthrough before serving, so Espresso now has rating10/version2.

The generic walkthrough receives HolonManager and ICosmicQueryStorageProvider instances. SQLite is selected in the sample host composition, not inside the common query/compiler implementation. MongoDB/Neo4j have separate native evidence in the CQL docs, but this first demo does not start them or claim federation.

## Swagger request examples

Named SQLite scope, affordable in-stock coffee:

```json
{
  "category": "coffee",
  "minimumRating": 8,
  "inStockOnly": true,
  "orderBy": "Price",
  "descending": false,
  "scope": "Named",
  "provider": "SQLLiteDBOASIS",
  "skip": 0,
  "take": 2
}
```

Expected `result.provider`: SQLLiteDBOASIS. Rows: Espresso (250p, rating10, stock19) and Flat White (350p, rating8, stock8). Prices are integer pence in this illustrative model; native decimal metadata queries remain unsupported.

Nested metadata, membership and all eligible demo targets:

```json
{
  "categories": ["coffee", "tea"],
  "minimumRating": 7,
  "city": "London",
  "inStockOnly": true,
  "orderBy": "Rating",
  "descending": true,
  "scope": "AllEligible",
  "skip": 0,
  "take": 10
}
```

Expected: Espresso, Flat White, Green Tea. AllEligible means the host's supplied eligible snapshot, which contains only SQLite here. Selecting Neo4j in Named scope fails because this demo did not admit/activate it. There is no implicit failover or provider scan. Invalid bounds return an OASISResult error and no result payload; take is1–20 and skip0–1000.

A catalog page including sold-out items:

```json
{
  "category": "coffee",
  "inStockOnly": false,
  "orderBy": "Price",
  "descending": false,
  "scope": "Default",
  "skip": 1,
  "take": 2
}
```

Expected: Flat White, Latte. Scope Default selects the host's already supplied default, SQLite.

This HTTP request model exposes a bounded coffee-demo query surface. It is not a text/C# execution endpoint or the complete generic network CQL grammar. The server constructs the same immutable CQL expressions used by the C# walkthrough.

## Verification and resources

```powershell
# Builds/runs the C# walkthrough, starts the owned host, checks real HTTP/Swagger, then stops it.
.\Scripts\TestHosts\Test-CosmicCqlDemoApi.ps1

# Reuse an unchanged verified build.
.\Scripts\TestHosts\Test-CosmicCqlDemoApi.ps1 -NoBuild
```

Console native self-checks pass. Six HTTP checks pass: Swagger/OpenAPI, typed filtering/order/provenance, nested metadata/membership/AllEligible, named paging, invalid-bound rejection and unavailable-provider rejection. Host exit0, port5087 absent and exact owned database/sidecars/manifest absent after the smoke run. Final console runner verifies producer/consumer identities for Core, Shared and SQLite.

Compact evidence is under `TestResults/CosmicCqlDemo`: `demo-result.json`, `demo-artifacts.json`, `api-demo-result.json`, `api-demo-artifacts.json`, `api-smoke.json`, stdout/stderr and runner logs. The committed delta summary is in demo-result.json; it records field paths and revision/operation evidence, not unrestricted historical private values.

The same fixed workspace owns demo.db, its sidecars and demo-manifest.json while running. Pre-existing fixtures cause refusal, never overwrite/reset. Failures preserve them for independent review. The manifest records actor and owned seed IDs before mutation. The successful run disposes providers before exact cleanup. See the [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md) and [exact dependency inventory](CQL_DEMO_DEPENDENCIES.md) for reusable outputs/cache footprints and earlier reviewed setup failures. No user/production database is touched.

Remaining work includes authenticated production WEB4 integration, broader generic HTTP expressions, provider discovery/policy routing, global federation, relationships/aggregates and all other open roadmap acceptance. This demo is useful evidence of the implemented subset, not a complete EF/IQueryable replacement.


## Developer source walkthrough — 21 October 2026

The demo sources now use a shared local `.editorconfig` (UTF-8, four-space indentation, separate statements) and teaching comments. Start with `Examples/CosmicCqlDemo/CoffeeWalkthrough.cs`: setup and the planning/execution helper lead into seven numbered query/save examples with expected results. `CoffeeApiHost.cs` explains the equivalent request-to-query pipeline; `Program.cs` explains host setup, two-client ownership and teardown. Demo assertions verify behavior and are not required application boilerplate.

The final readability batch was rebuilt using the existing environment and build lease. Console query/tracking/no-op/delta/conflict checks and all six HTTP/Swagger checks passed. Formatting verification and the build lease contract check passed. No host or synthetic database was retained. Detailed evidence remains in the fixed `TestResults/CosmicCqlDemo` directory, including `readability-verification.log`.


### Access-bound graph walkthrough

The C# native and HTTP walkthroughs now include example4. The browser has an Access-bound child graph preset and Swagger pre-fills graph options. Use Alice: C-public returns public child and owned child; the private intermediate and its public grandchild are absent. Visitor returns public child only. The server binds identity from the bearer/admission, not the body. The native SDK uses WithReadAccess, not just a root Where predicate. Root-only edits use saveChildren:false; SDK raw holons retain private store observations and are not public JSON DTOs. See the HTTP contract for depth/node/byte bounds and consistency.


## Scalar demo update — 23 October 2026

Examples7–9 in both annotated source files show native LongCount, a windowed Count, and access-protected Any. The first six row examples are preserved. Browser presets and Swagger expose both exact routes. The scalar response is detached: Count is Int32, Any is Boolean, and LongCount is always an exact decimal string tagged Int64. Browser formatting retains original JSON text.

The underlying signed HTTP gate passes129tests with137-input native counts on all three stores. The expanded27SDK/27HTTP demo passes its freshness/count checks:182settled operations/181physical attempts, with generated Swagger checks and scalar no-replay on all3 stores. Final Core1982/demo1 pass unskipped. Earlier15/15 and18/18 figures above are historical gates. [Scalar contract](CQL_SCALAR_HTTP_CONTRACT.md).

## Projection demo update — 23 October

The next console/serve verification expects10C#SDK examples and10C#HTTP examples per provider (30/30total). Example10 shows typed native Select and the selected-column HTTP route, with exact Int64 text, nullable and nested values. Browser presets and Swagger include the projection route/schema. The expanded walkthrough verification now passes30SDK/30HTTP with broad147native regression (195settled/190physicalattempts); the endpoint itself already passes fullCore2,059 and signed native three-store acceptance. Use the existing reusable runner/environment; do not create another database/runtime. [Current contract/evidence](CQL_PROJECTION_HTTP_CONTRACT.md).

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

Latest25October acceptance:102SDK/102HTTP,309settled/301attempts, Core2940 and joint native1/1 pass; standalone build and20 browser preset checks pass. [Composition contract/examples](CQL_COMPOSITION_HTTP_CONTRACT.md). Historical counts below retain their dated scopes.
