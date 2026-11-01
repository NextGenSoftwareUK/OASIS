# COSMIC ORM / CQL — current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demo. Deep LINQ parity remains paused.

## Current verified milestone — 1 November 2026

**34 providers have scoped hosted HTTP evidence; 5 have browser evidence and 29 browser gates remain open.** MultiversX/Elrond now passes native 3/2, Mongo federation 5/4, repeat reuse (zero new/two existing), foreign-private zero/public three, and two matching replicas on both SQLite and Neo4j. No source/chain writes or browser certification are inferred.

Latest verified hosted acceptance: application `801795a648a1ae0b9211c0a478027673cb79c156`, Core `f41b2a11253c222aac6bfdf67278d226e01ff96e`, app `19f65f59-e370-4cad-94ce-b0246872b891` and settings `18c4f04e-6ac3-4fa7-ac2a-ff312d095159` succeeded. Website main `95c53ccd3d20a99df4e8059c105c2aa4fcb8ebbb` serves byte-identical committed HTML (149523bytes, SHA256 `988e89929f268a5a447f00a09e7602d0c59a01e6bd15ef44ed0c37be16343a63`), including Elrond setup/C# links. This proves publication, not browser interaction.

There are **37 implemented query types**: 3 canonical adapters, 32 bounded public/source profiles and 2 locally scoped Azure/Oracle implementations. Radix now passes 50/50 focused tests (zero skips), real shared-manager three/two selection and the actual C# console demo; API admission and local HTTP/Mongo federation pass; hosted/browser remain pending. Deployed query-tariff catalogue remains 36 types and scoped hosted HTTP count remains 34. Elrond hosted acceptance stays green. Azure/Oracle hosting remains deferred. TRON real429 remains red; Starknet transport works but its CQL contract is unqualified. Full ORM/parity/HyperDrive certification remains incomplete.

## Exact next step

1. Sui hosted acceptance is green; original cohort completion ACK retained. Do not replay saves or create another owner.
2. Sui card is published and byte-verified at website9679699; no browser interaction claim. Preserve the green native/HTTP/save cohorts.
3. Elrond hosted acceptance is green and durable completion ACK retained. Website95c53cc is published/byte-verified with setup/C# links; browser interaction remains separate. No save/cohort replay. Next: publish verified selective Radix release (code3852291f8/Coree4d6e4b; release4/4 + rebuilt TRON exclusion check), observe exact job, then protected settings apply/live acceptance/card. API/preset2 + schema1/Core1/config3/localHTTP1 pass; actual native3/2, paired5/4, foreign-private0/public3; original fixture cleaned, ACKs retained. Native50/50 tests and actual CLI3/2 are green; preserve the stable source namespace. Starknet CQL remains unqualified.
4. Browser gates remain open for 29 providers. Fresh bridge reset still fails Transport closed; agent-owned, David already opened browser. No repeated owner request.

Red source scopes: [TRON](TRON_PUBLIC_SOURCE_STATUS.md)34controlled passes/real429; [Starknet](STARKNET_PUBLIC_SOURCE_STATUS.md)transport200but no header3/2/CQL yet; [Radix](RADIX_PUBLIC_SOURCE_STATUS.md)native CQL/CLI3/2green; API/localHTTP green; hosting pending. Python403denials are preserved as client-specific observations; .NET guard unchanged, no account/key/environment created.

## Existing guarantees and limits

- Canonical MongoDB/SQLite/Neo4j CQL checklist remains 50/82 each; original demo set green. Work only on red rows unless a reproducible bug appears.
- Added 26 hosted profiles retain 52 private snapshots; existing earlier eight proofs were not broadly rerun.
- HyperDrive request-scoped selection and three failover directions passed; six native repair routes and three-authority ACK-loss/disconnect checks remain scoped.
- Open: recovery breadth, authority migration, scale and full ORM child/history/migrations reconciliation. No universal uptime or all-provider writes claim. Legacy gaps are documentation-only.
- Azure and Oracle hosting deferred by David; no signup/spending re-asks. No production/master promotion.

## Recovery and commands

- Work only C:/Source/OASIS-cosmic; Core branch work/cosmic-cql-google-public; parent currently work/cosmic-cql-public-release (task work/cosmic-cql-google-public). Frontend C:/Source/Web4Site branch work/cosmic-cql.
- Raw outputs: TestResults/. Durable identities/config/save intents/ACKs: LocalState/COSMIC/. Never regenerate owners or replay acknowledged/uncertain writes.
- Reuse pinned environments; record resources in [ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md). Unknown root logs preserved.
- Build with Invoke-WithProviderBuildLease.ps1 and /m:2 /nr:false /p:UseSharedCompilation=false; never nest leases.
- Validate committed pins: python Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- Source packaging: python -B Scripts/validate_all_provider_sdk_publish_manifests.py.
- Fetch before integration; non-force Development pushes permitted for live verification. Preserve other contributors.

[Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [manual](CQL_MANUAL.md) · [architecture](CQL_ARCHITECTURE.md) · [checklist](CQL_PARITY_CHECKLIST.md) · [backlog](AFTER_DEMO_BACKLOG.md) · [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
