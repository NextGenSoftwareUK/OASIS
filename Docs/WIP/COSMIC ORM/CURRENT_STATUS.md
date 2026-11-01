# COSMIC ORM / CQL — current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demo. Deep LINQ parity remains paused.

## Current verified milestone — 1 November 2026

**33 providers have scoped hosted HTTP evidence; 5 have browser evidence and 28 browser gates remain open.** Sui now passes native 3/2, Mongo federation 5/4, repeat reuse (zero new/two existing), foreign-private zero/public three, and two matching replicas on both SQLite and Neo4j. No source/chain writes or browser certification are inferred.

Application `7e0b60994921d03e09abe4a7050ca5bf5f02bfdb`, Core `0e540eb911671a6a249458ea9b69d3e96fb1d212`, app deployment `81794c84-1f8b-41e1-a9e4-6b79015fa173` and settings deployment `1d5007cb-e593-458e-b838-9d535ba93e70` succeeded. Website main `9679699d391b5b49117c3009ee2bfcc0add41464` serves byte-identical committed HTML (148135bytes, SHA256 `b8d17101a869a1dffb8cd2c076b8c6577a24086bfcd052394100e7245d069a35`), including Sui setup/C# links. This proves publication, not browser interaction.

There are **36 implemented query types**, including the new MultiversX/Elrond native profile:34tests plus actual shared-manager/C# CLI3/2pass; API/local HTTP native3/2+Mongo5/4+privacy now verified, hosting remains open. Sui retains30native tests and its CLI/hosted proof. Sui API admission (2 tests), Core tariff (1), generated preset planning (1), local HTTP/Mongo (1), and configuration controls (3) also pass. TRON is an unadmitted candidate with34controlled passes and a real429failure; it is not counted. Azure/Oracle hosting stays explicitly deferred. Full ORM/parity/HyperDrive certification remains incomplete. Dated milestones retain their original scope.

## Exact next step

1. Sui hosted acceptance is green; original cohort completion ACK retained. Do not replay saves or create another owner.
2. Sui card is published and byte-verified at website9679699; no browser interaction claim. Preserve the green native/HTTP/save cohorts.
3. Continue red demo providers, next selective MultiversX/Elrond release while TRON access is unresolved. TRON34controlled passes but real429failure, alternate endpoint unqualified; no blind retry/count grant. [MultiversX native milestone](ELROND_PUBLIC_SOURCE_STATUS.md#native-cql-milestone):34tests plus real shared-manager/CLI3/2pass. Stable catalogue retained. API2+Core1+shipping1+nativeHTTP1/config3pass; native3/2, paired5/4, private0 locally verified, fixture cleaned. Prepared3setting candidate unapplied. Coref41b2a1 task pushed. Verified41file release scope prepared from39b12bc28 on existing release branch; TRON adapter/test/project changes excluded. Release API/planner3passes and compiled TRON candidate identifier absent;5SDK closures pass. Next publish verified release/Core batch after fresh fetch, wait exact app SUCCESS before guarded3setting apply. No hosted grant yet.
4. Browser gates remain open for 28 providers. Browser tooling failed with Transport closed; agent-owned, David already opened browser. No repeated owner request.

Next breadth candidate: [TRON qualification](TRON_PUBLIC_SOURCE_STATUS.md), strict qualification now passes 9 pure tests plus genesis/three ordinary headers; no native CQL/count grant. Native batch candidate passes34controlled tests; real query still hits429. No certification/count grant; actual anonymous capacity remains unresolved, no blind retry.

## Existing guarantees and limits

- Canonical MongoDB/SQLite/Neo4j CQL checklist remains 50/82 each; original demo set green. Work only on red rows unless a reproducible bug appears.
- Added 25 hosted profiles retain 50 private snapshots; existing earlier eight proofs were not broadly rerun.
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
