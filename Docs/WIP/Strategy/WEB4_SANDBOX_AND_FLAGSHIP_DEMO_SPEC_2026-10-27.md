# WEB4 Sandbox and "Ask Your Data" Flagship Demo — Spec (27 October 2026)

Status: **proposed spec, not started.** Follows the roadmap in [WEB4_VISION_REVIEW_AND_ROADMAP_2026-10-26.md](WEB4_VISION_REVIEW_AND_ROADMAP_2026-10-26.md) (steps 2–4).

## 1. Goal

A public **WEB4 Sandbox** at `web4.oasisomniverse.one`, the WEB4 counterpart of the existing WEB6 sandbox (`Web6Site/web6-sandbox.html`, live at `sandbox.oasisomniverse.one`). Visitors can try, against a **real** WEB4 API:

- HyperDrive: auto-failover, auto-replication, auto-load-balancing
- COSMIC ORM and CQL queries across SQLite (relational), MongoDB (document) and Neo4j (graph)
- The flagship **"Ask Your Data"** demo, plus the other roadmap demos as they land

Everything shown is real execution. No simulated or canned results (see `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md`). If a capability is not ready, its tab says so instead of faking it.

## 2. What already exists to reuse

| Asset | Where | Reuse |
|-------|-------|-------|
| WEB6 sandbox page (tabs, WEB4 auth, usage grants) | `C:\Source\Web6Site\web6-sandbox.html` | Page structure, styling, tab pattern, WEB4 `avatar/authenticate` login flow |
| WEB4 site | `C:\Source\Web4Site\index.html` | Add a "Sandbox" link/button in nav and hero |
| COSMIC CQL demo (console + Swagger) | `Examples/CosmicCqlDemo/` (C# and HTTP walkthroughs: `V1Walkthrough.cs`, `V1FederationWalkthrough.cs`, `V1SwaggerExample.cs`, coffee walkthrough, browser presets) | Port its **HTTP requests and presets** to sandbox query presets; same queries, same three providers |
| CQL demo runbook and verification | `Docs/WIP/COSMIC ORM/CQL_DEMO_RUNBOOK.md`, `CQL_V1_DEMO_VERIFICATION.md`, `Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1` | Fixture seeding, demo identities, cleanup |
| CQL HTTP API | `POST /api/data/query-cql`, `POST /api/data/query-cql/federation` (`ONODE/.../Controllers`) | Sandbox CQL tab calls these directly |
| HyperDrive API | `HyperDriveController*.cs`: `status`, `dashboard`, `metrics`, `connections`, `best-provider`, `failover/*`, `replication/*`, `config`, `mode` | Live provider-health panel, failover/replication rules, load-balancing metrics |
| HyperDrive V1 failover (proven) | Provider manager | Live failover scenario |

## 3. Sandbox architecture

- **Dedicated sandbox ONODE (WEB4 API) deployment** on Railway, separate from production. It has its own SQLite, MongoDB and Neo4j (plus one test chain later) and seeded demo data only. Production data is never reachable.
- **Per-visitor isolation.** Each visitor gets a short-lived demo avatar and their own fixture tag, so queries and writes do not collide. Data expires and is cleaned up automatically.
- **Fault injection, sandbox only.** A sandbox-environment-only endpoint takes a provider offline/online for that visitor's scope (or for a shared demo cluster on a rate limit). It must not exist in production builds. This is what makes "kill a provider live" real.
- **Limits.** Rate limits, query budgets and the existing usage/idempotency protocol apply. No paid execution.
- **Front end.** A static `web4-sandbox.html` page in `Web4Site`, same stack and styling as the WEB6 sandbox. No build step.

## 4. Sandbox tabs

1. **Ask Your Data** (flagship, see §5).
2. **CQL Playground.** Editable CQL with presets ported from `Examples/CosmicCqlDemo`: Where/OrderBy/paging, projections, aggregates (Sum/Average, grouped), child loading, federation across the three providers. Run against one provider or all three and compare results side by side.
3. **HyperDrive Live.** Provider health, latency and load (from `status`, `dashboard`, `metrics`, `connections`). Buttons to take a provider offline/online and watch reads and writes fail over, and to watch writes replicate and catch up.
4. **Replication.** Save a holon once and see it appear on each configured provider, with replication lag and per-provider versions.
5. **Load Balancing.** Fire a burst of reads and see how HyperDrive spreads them across providers (`best-provider`, metrics).
6. **COSMIC ORM.** Side-by-side C# (SDK) and HTTP for the same operation, taken from the demo's walkthroughs, with "Run" for the HTTP form.
7. **Explain.** Not a separate tab: every result panel has an "Explain" drawer (see §5, step 4).

## 5. Flagship demo: "Ask Your Data"

A roughly three-minute story:

1. **One person, three silos.** GitHub activity (Web2), on-chain wallet history (Web3; test chain in the sandbox) and app data in MongoDB, all brought into WEB4 as holons under one demo avatar.
2. **Ask in plain English**, e.g. "What did I build and pay for last month?" An LLM (via WEB6) turns it into a **typed, permission-checked, read-only CQL plan**. The plan is shown before it runs.
3. **One joined answer**, each row showing its source provider, holon id and version.
4. **Explain drawer.** Resolved provider scope, native vs coordinated execution, freshness/consistency, attempts, cost and any rejected capabilities.
5. **Kill a provider live.** Take MongoDB offline mid-demo. HyperDrive fails over and the same question is answered from a replica; the drawer shows which. Bring it back and show it catching up.

**Guardrails:**
- The AI only produces read-only CQL plans, which run through normal WEB4 authorization, budgets and idempotency.
- Retrieved content is data, never instructions.
- No "100% uptime" claims; show measured behaviour only.

## 6. Build stages

Each stage ends in something that works and can be shown.

| # | Stage | Done when |
|---|-------|-----------|
| 1 | Sandbox page shell + auth + HyperDrive Live (read-only panels) | Page live on `web4.oasisomniverse.one`, linked from the WEB4 site, showing real provider status from the sandbox API |
| 2 | Sandbox API deployment with seeded SQLite/MongoDB/Neo4j and per-visitor fixtures | Visitors can log in as a demo avatar and get isolated data that auto-expires |
| 3 | CQL Playground with ported `CosmicCqlDemo` presets | All demo presets run live on all three providers |
| 4 | Sandbox-only fault injection + failover and replication tabs | A visitor can take a provider offline and watch real failover and catch-up |
| 5 | Explain drawer | Every CQL result shows scope, execution mode, attempts, freshness and cost from real API data |
| 6 | Connectors for GitHub, a test chain, app data | The three silos ingest into holons for a demo avatar |
| 7 | Natural language → CQL planner (read-only, via WEB6) | Plain-English questions produce a visible plan and real joined answers |
| 8 | Flagship scripted run + recorded video | The full §5 story runs end to end live, and a recording exists |

Stages 1–5 make the sandbox useful on their own. 6–8 complete the flagship.

## 7. Dependencies and open questions

- **COSMIC/CQL agent:** CQL parity on SQLite/MongoDB/Neo4j and the federation endpoint must stay stable. The sandbox reuses its demo presets, so coordinate preset changes.
- **Providers agent:** HyperDrive V2 status/metrics endpoints must reflect real provider state for the live panels.
- **API gaps to confirm:** an explain payload on CQL responses, and a sandbox-only provider on/off endpoint (neither confirmed yet).
- **Hosting:** one extra Railway service for the sandbox API and its databases (cost to confirm). It must follow `Docker/oasis-dependency-versions.env` pins.
- **Model and effort:** stages 5–7 are design-heavy (high effort); stages 1–3 are mostly porting (medium).
