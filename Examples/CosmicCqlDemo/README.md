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


The current walkthrough has **fourteen** spaced/commented examples per provider: 42 SDK queries and 42 HTTP requests. Examples7–9 demonstrate LongCount, Count after a source window and access-protected Any. The scalar route is `/api/data/query-cql/scalar`; omitted take counts the complete visible source, and LongCount uses an exact Int64 decimal string. Example10 selects detached typed values through `/api/data/query-cql/projection`, preserving exact Int64 text, nullable leaves and nested metadata. Browser presets and Swagger show all three routes. The runner checks fresh actual completed counts; expanded scalar verification passes with 182settled operations/181physical attempts and scalar no-replay checks on all3 stores.

Projection transport now passes fullCore2,059 controls and signed native three-store acceptance, including visibility before paging, exact large integers/null/GUID/nested values, receipts and no replay. The new10-example SDK/HTTP walkthroughs now pass their expanded30/30 verification plus broad147native regression (195settled operations/190physical attempts); consult the current [HTTP projection contract](../../Docs/WIP/COSMIC%20ORM/CQL_PROJECTION_HTTP_CONTRACT.md) for limits and verified evidence.

Maintainer verification can combine the expanded walkthrough with the broad native save/delete/CQL regression in one environment lifecycle: `pwsh -File Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1 -IncludeRegression`. The same runner owns fresh-result checks and cleanup; `-Serve` remains the interactive mode.


## Verified Min/Max demonstrations

Examples11–12 demonstrate native Min/Max in both spaced/commented C# walkthroughs. Hidden roots contain stronger extrema; actor visibility is applied before reduction. Exact signed Int64 values remain decimal text. Browser presets and Swagger show the Selection shape.

Earlier Min/Max verification passed **175native regression tests,36SDK queries and36HTTP examples**, with zero skips on MongoDB, SQLite and Neo4j. The run records216settled operations/211physical attempts. Full Core/HTTP tests pass2,117. The exact retained fixture was reconciled before this fresh run; owned data was cleaned after verified teardown. [Current contract/evidence](../../Docs/WIP/COSMIC%20ORM/CQL_SCALAR_HTTP_CONTRACT.md#selected-http-and-demo-gate-verified--23-october-2026). Use current JSON/transcript evidence, not historical30/30results.


## Numeric examples — verified across all three providers

Examples 13–14 add Sum and Average to both C# walkthroughs, with matching browser presets. Sum returns the exact visible Int64 total. Average explicitly orders by Name and takes two rows before reduction, returning the Decimal fraction `-9007199254740990.5`; the half-unit demonstrates why the HTTP value is text rather than a JavaScript Number. Swagger opens the same Average selection/window shape.

The numeric provider and endpoint unit gates are verified; the new 42 SDK/42 HTTP walkthrough passes with the full195-case native regression and zero skips. The 175/36/36 result above is historical Min/Max evidence and does not certify these numeric additions. Run the existing fixed demo runner with `-IncludeRegression` for current verification.

Current numeric demo evidence: **195 native tests, 42 SDK queries, 42 HTTP examples, 222 settled operations/217 physical attempts**. Exact fractional Decimal output is verified on all three stores. See the fresh numeric-demo-native.log, v1-demo-result.json and v1-walkthrough.txt under TestResults/CosmicCqlDemo.
