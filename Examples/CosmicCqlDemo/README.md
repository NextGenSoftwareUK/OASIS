# COSMIC CQL demo

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
