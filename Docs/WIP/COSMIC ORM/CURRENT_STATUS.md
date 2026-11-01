# COSMIC ORM / CQL — current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demo. Deep LINQ parity remains paused.

## Current verified milestone — 1 November 2026

**34 providers have scoped hosted HTTP evidence; 5 have browser evidence and 29 browser gates remain open.** MultiversX/Elrond now passes native 3/2, Mongo federation 5/4, repeat reuse (zero new/two existing), foreign-private zero/public three, and two matching replicas on both SQLite and Neo4j. No source/chain writes or browser certification are inferred.

Latest verified hosted acceptance: application `801795a648a1ae0b9211c0a478027673cb79c156`, Core `f41b2a11253c222aac6bfdf67278d226e01ff96e`, app `19f65f59-e370-4cad-94ce-b0246872b891` and settings `18c4f04e-6ac3-4fa7-ac2a-ff312d095159` succeeded. Website main `95c53ccd3d20a99df4e8059c105c2aa4fcb8ebbb` serves byte-identical committed HTML (149523bytes, SHA256 `988e89929f268a5a447f00a09e7602d0c59a01e6bd15ef44ed0c37be16343a63`), including Elrond setup/C# links. This proves publication, not browser interaction.

There are **36 implemented query types**: 3 canonical adapters, 31 bounded public/source profiles and 2 locally scoped Azure/Oracle implementations. Elrond has34native tests + actual shared-manager/CLI3/2, API2/Core1/shipping1/localHTTP1/config3passes and a scoped release3test/artifact check excluding TRON's unverified adapter. Azure/Oracle hosting remains explicitly deferred. TRON's real429and Starknet/Radix403access qualifications stay red and uncounted. Full ORM/parity/HyperDrive certification remains incomplete. Dated milestones retain their original scope.

## Exact next step

1. Sui hosted acceptance is green; original cohort completion ACK retained. Do not replay saves or create another owner.
2. Sui card is published and byte-verified at website9679699; no browser interaction claim. Preserve the green native/HTTP/save cohorts.
3. Elrond hosted acceptance is green and durable completion ACK retained. Website95c53cc is published/byte-verified with setup/C# links; browser interaction remains separate. No save/cohort replay. Next compare failed advertised endpoints using the actual deployed client/egress with bounded read-only probes before assuming a global source outage; then continue red provider breadth. Preserve TRON/Starknet/Radix failures; no blind retries, access-control bypass, new accounts or count promotion from health probes.
4. Browser gates remain open for 29 providers. Fresh bridge reset still fails Transport closed; agent-owned, David already opened browser. No repeated owner request.

Next breadth candidate: [TRON qualification](TRON_PUBLIC_SOURCE_STATUS.md), strict qualification now passes 9 pure tests plus genesis/three ordinary headers; no native CQL/count grant. Native batch candidate passes34controlled tests; real query still hits429. No certification/count grant; actual anonymous capacity remains unresolved, no blind retry. [Starknet qualification](STARKNET_PUBLIC_SOURCE_STATUS.md) returned403on its first public read; no source identity or CQL grant. [Radix qualification](RADIX_PUBLIC_SOURCE_STATUS.md) also returned403before network/ledger data. Both denials are Cloudflare-fronted; policy cause unknown. Qualify suitable documented free access before implementation; no accounts/SDKs allocated.

## Existing guarantees and limits

- Canonical MongoDB/SQLite/Neo4j CQL checklist remains 50/82 each; original demo set green. Work only on red rows unless a reproducible bug appears.
- Added 26 hosted profiles retain 52 private snapshots; existing earlier eight proofs were not broadly rerun.
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
