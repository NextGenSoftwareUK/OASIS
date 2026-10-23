# CQL demos: native v1 and typed coffee ORM

## Three-provider v1 walkthrough — 22 October 2026

```powershell
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1 -Serve
```

The first command runs nine annotated SDK queries and nine HTTP examples on MongoDB, SQLite and Neo4j. With Serve, open **http://127.0.0.1:5088/demo** once `TestResults/CosmicCqlDemo/v1-session.json` appears. Transcript: `v1-walkthrough.txt`; result: `v1-demo-result.json`. Swagger `/swagger` is prefilled, authorized as synthetic Alice and documents Idempotency-Key.

### Five-minute presenter script

1. Alice + MongoDB + Visibility before paging returns **C-public**: access filtering precedes Skip/Take.
2. Repeat on SQLite and Neo4j: same semantics, actual native execution and explicit provenance.
3. Nested metadata + protected OR returns **D-owned**, not A-private. Visitor sees public data; Anonymous rejects.
4. Show prefix/suffix, null/existence, GUID, Boolean and numeric-string examples. Exact Int64 preserves **9007199254740993** without JavaScript rounding.
5. Repeat the last key: **409**, no second execution. A new intent gets a new key. Show settlement status.
6. Execute Swagger's example. Change Idempotency-Key for a new intent/provider. Swagger defaults to Alice.
7. **Finish demo & clean up** waits, verifies settlement, stops services and removes only owned fixtures.

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
