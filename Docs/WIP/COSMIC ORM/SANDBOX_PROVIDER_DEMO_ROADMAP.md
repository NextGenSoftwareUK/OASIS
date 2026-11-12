# WEB4 sandbox integration for COSMIC/CQL provider demos

Required by the user30October2026. Applies to every provider demo produced by this
work, existing and future, including the first10content-query providers. A local
C#/HTTP/browser demo is not proof that the provider works in the public sandbox.

This recovered roadmap preserves the original acceptance scope. Its dated
observations and counts are historical. Use [current status](CURRENT_STATUS.md)
for current evidence and the [LocalFile rollout](LOCALFILE_CQL_MVP.md) for the
first live Development gate.

## Current priority demo evidence

- Solana, Ethereum, Polygon, Arbitrum, NEAR, IPFS, ActivityPub, Cosmos Hub and Polkadot retain scoped live HTTP and browser acceptance. Preserve those cohorts.
- EOS, SOLID and TON passed both public browser presets. The original-owner private MongoDB paired browser presets remain open. Hedera recovered catalogue readiness on WEB4 `f9011d511056`; its bounded public browser subset and native HTTP evidence are retained. The full public browser preset and original-owner private browser cases remain open.
- TRON CQL is deployed on Development and still requires the dedicated TronGrid key for authenticated qualification and enrollment. Supabase remains a top-priority hosted demo with the deployed reader and an outstanding restricted teaching fixture. GOMap and CouchDB have deployed reader code with their reviewed HTTPS fixtures outstanding.
- LocalFile CQL acceptance is retained. The ordinary HyperDrive point-routing check is deferred behind the priority demos.

[Current deployment and readiness](CURRENT_STATUS.md) · [Owner actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).

## Observed integration points

- Frontend: `C:/Source/Web4Site/web4-sandbox.html` (separate repository).
  Read-only inspection of its clean local main checkout found a three-provider
  list and a CQL playground labelled coming next. This is local-source evidence,
  not a claim about the latest live deployment. Fetch/reconcile before editing.
- Backend: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/SandboxController.cs`.
- Sessions/routing/cleanup: DNA `OASIS.Sandbox.Providers`, Core SandboxOptions and
  SandboxAvatarCleanup; existing [demo-avatar guide](../../Devs/SANDBOX_DEMO_AVATARS.md).
- Configuration generator: `Scripts/Sandbox/build_sandbox_dna.py`, currently lists
  MongoDBOASIS/SQLLiteDBOASIS/Neo4jOASIS and sets Legacy mode in this checkout.
  Document that generator/deployed-V2 discrepancy; verify actual current deployment
  settings before use. Legacy implementation changes remain out of scope.
- CQL: existing WEB4 `/api/data/query-cql` and federation API, HolonManager and
  provider capability admission; reuse these rather than creating another engine.

## Required coverage

1. First content-query group: MongoDB,SQLite,Neo4j,PostgreSQL,SQL Server,Oracle,
   Azure Cosmos DB,AWS DynamoDB,Elasticsearch,Solr. See
   [content-query scope](CONTENT_QUERY_DEMO_ROADMAP.md).
2. Reconcile all existing scoped demo providers in the
   [provider inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md), currently62/226.
3. Include sandbox integration in acceptance for each subsequent provider demo.

Mongo/SQLite/Neo4j are locally configured as sandbox routing providers; that alone
does not certify every query or the current deployed page. Other provider demos
are a sandbox integration backlog unless linked deployment/runtime proof exists.
Hosted Azure remains pending/free-only. Local emulators/validators are labelled
as such and do not imply hosted cloud/public-chain availability.

## Implementation and acceptance

- Establish provider discovery/configuration shared with the API, exposing only
  safe capability/readiness metadata. Avoid another manually divergent UI list.
- Each provider gets its supported CQL presets, readable C# and HTTP examples,
  scope/consistency limits, result provenance and explicit unavailable/rejected
  capability states. Include the new name/description/metadata predicates where
  supported, plus the Mongo-paired federation preset.
- Keep query/demo selection distinct from the sandbox's avatar/session and
  HyperDrive routing list. Adding a read-capable provider must not implicitly make
  avatar creation, replication or cleanup depend on unsupported CRUD operations.
- Configure an actual reachable sandbox-native runtime, server registration and
  CQL adapter/allowed scope. A dropdown option or local test is not runtime proof.
- Retain per-visitor authorization/isolation and bounded work. Trusted record
  catalogues and actor identity remain server-owned; never accept grants from a
  browser DTO. No production data or provider credentials in the page.
- Verify seed/source ownership, cleanup or explicitly read-only fixture policy for
  each provider before enabling visitor writes. Do not substitute partition/table
  deletion for unsupported record deletion.
- Exercise the actual sandbox page -> authenticated WEB4 API -> native provider ->
  result, including filtered multi-record queries, federation/provenance, denied
  capabilities, missing/unavailable providers and visitor isolation.
- Test existing three-provider session/routing/cleanup behavior after integration;
  adding a new query provider must not break existing demo avatars.
- Capture linked test/deployment/browser evidence and update the provider report,
  current status and inventory. Distinguish local-demo verified, sandbox wiring
  pending, configured-unavailable and sandbox verified. Do not relabel all62as
  sandbox-ready without independent proof.
- Reuse approved free resources and record their lifecycle in the cleanup ledger.
  Coordinate only shared Core/Shared public API changes; preserve concurrent work.
  Integrate verified deployment batches at the agreed cadence.

## Current next step

Audit existing CQL/API capabilities for the approved10-provider content-query set
and map the missing sandbox API/UI/runtime pieces. Implement the sandbox CQL
playground/provider integration alongside that query work, then reconcile remaining
existing demos. Keep unsupported providers visibly pending rather than fabricating
results. The separate Web4Site checkout was inspected only; no branch switch,
frontend edit, deployment, account, environment or new resource was created here.

[Current status](CURRENT_STATUS.md) / [content-query roadmap](CONTENT_QUERY_DEMO_ROADMAP.md).
