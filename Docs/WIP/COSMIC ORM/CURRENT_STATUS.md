# COSMIC ORM / CQL — current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demo. Deep LINQ parity remains paused.

## Current verified milestone — 1 November 2026

**32 providers have scoped hosted HTTP evidence; 5 have browser evidence and 27 browser gates remain open.** Hedera now passes native 3/2, Mongo federation 5/4, repeat reuse (zero new/two existing), foreign-private zero/public three, and two matching replicas on both SQLite and Neo4j. The original owner cohort was resumed after the scheduling correction; no duplicate seed saves were required.

Application `4fb66c68705be82d37bb97d9764b97f165661fca`, Core `89b5ffad5c589e6607f26892f46a4ee718335fe2`, deployment `122310fc-11ec-45cf-a631-2ebd471ce66a` succeeded. Existing settings deployment `9dfe2fbd-45f9-4706-a3d4-85ca0f5c2a3d` was retained. Website main `07a36e7` is byte-identical to the served HTML (SHA256 `742262270af5294b0e16a3cb254a8152b545d668e81d7037314f64f024644367`); this proves publication, not browser interaction.

Sui brings implemented query types to **35**, with **30 native tests and an actual shared-manager CLI 3/2 pass**. Sui API admission (2 tests), Core tariff (1), generated preset planning (1), local HTTP/Mongo 3/2 + 5/4 + privacy (1), and configuration controls (3) now pass. Hosting and browser verification remain open. Azure/Oracle hosting stays explicitly deferred. Full ORM/parity/HyperDrive certification remains incomplete. Dated older milestones below retain their original scope and are superseded by this current summary.

## Exact next step

1. Observe exact Sui application deployment `81794c84-1f8b-41e1-a9e4-6b79015fa173`, last observed **BUILDING**, at parent `7e0b60994921d03e09abe4a7050ca5bf5f02bfdb` / Core `0e540eb911671a6a249458ea9b69d3e96fb1d212`. Both Development refs verified; no restart or duplicate deployment.
2. After exact SUCCESS, use existing frozen release branch at that exact parent SHA for guarded application. Protected `sui-public` candidate: three settings/four presets, actual shipping planner passes. No settings applied yet. Guard requires checked-out HEAD equal deployed SHA.
3. Apply once with `COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT` set, observe whole candidate, journal one settings rollout, then hosted native/federated/repeat/privacy/replicas via `Verify-HostedQuerySource.py SuiOASIS --prepare`. No Sui visitor cohort exists yet; record intents before mutations.
4. Browser gates remain open for 27 providers. Browser tooling failed with Transport closed; agent-owned, David already opened browser. No repeated owner request.

Next breadth candidate: [TRON qualification](TRON_PUBLIC_SOURCE_STATUS.md), strict qualification now passes 9 pure tests plus genesis/three ordinary headers; no native CQL/count grant. Native candidate now passes31controlled tests, but real activation hit429; no certification/count grant. Investigate bounded native batch/access rather than blind retry.

## Existing guarantees and limits

- Canonical MongoDB/SQLite/Neo4j CQL checklist remains 50/82 each; original demo set green. Work only on red rows unless a reproducible bug appears.
- Added 24 hosted profiles retain 48 private snapshots; existing earlier eight proofs were not broadly rerun.
- HyperDrive request-scoped selection and three failover directions passed; six native repair routes and three-authority ACK-loss/disconnect checks remain scoped.
- Open: recovery breadth, authority migration, scale and full ORM child/history/migrations reconciliation. No universal uptime or all-provider writes claim. Legacy gaps are documentation-only.
- Azure and Oracle hosting deferred by David; no signup/spending re-asks. No production/master promotion.

## Recovery and commands

- Work only C:/Source/OASIS-cosmic; parent/Core branch work/cosmic-cql-google-public. Frontend C:/Source/Web4Site branch work/cosmic-cql.
- Raw outputs: TestResults/. Durable identities/config/save intents/ACKs: LocalState/COSMIC/. Never regenerate owners or replay acknowledged/uncertain writes.
- Reuse pinned environments; record resources in [ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md). Unknown root logs preserved.
- Build with Invoke-WithProviderBuildLease.ps1 and /m:2 /nr:false /p:UseSharedCompilation=false; never nest leases.
- Validate committed pins: python Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- Source packaging: python -B Scripts/validate_all_provider_sdk_publish_manifests.py.
- Fetch before integration; non-force Development pushes permitted for live verification. Preserve other contributors.

[Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [manual](CQL_MANUAL.md) · [architecture](CQL_ARCHITECTURE.md) · [checklist](CQL_PARITY_CHECKLIST.md) · [backlog](AFTER_DEMO_BACKLOG.md) · [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
