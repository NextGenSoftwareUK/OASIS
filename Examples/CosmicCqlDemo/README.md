# COSMIC CQL demo

## New v1: MongoDB, SQLite and Neo4j

```powershell
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1 -Serve
```

Console runs **nine C# SDK queries and nine C# HTTP examples** on all three native databases. Serve then opens **http://127.0.0.1:5088/demo** after `TestResults/CosmicCqlDemo/v1-session.json` appears. Switch provider, user and example; edit spaced JSON and inspect formatted results/settlement. Repeat a key to show no replay.

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

The native v1 walkthrough now has five spaced/commented examples per provider (15SDK and15HTTP overall). Example5 requires two visible sibling matches using WhereAnyChild / anyChildren before paging. Browser Visible child existence and Swagger use the same bounded request contract. The dedicated runner reuses existing restored dependencies and verifies a fresh15/15 report.
