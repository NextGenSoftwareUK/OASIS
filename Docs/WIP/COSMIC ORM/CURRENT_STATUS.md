# COSMIC ORM / CQL — current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demo. Deep LINQ parity remains paused.

## Current verified milestone

**36 providers have scoped hosted HTTP evidence; 5 have browser evidence and 31 browser gates remain open.** Starknet passes native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2. Two private saves were acknowledged once. Source writes and browser interaction are not inferred.

Latest hosted provider acceptance: parentcafd0f8c8ea93c22f5d4638acc336df5795a05d7/Core8a5ab453ee6734195331abeec0ad5c04e84b5332, app151477e9/settings03760bc9 SUCCESS. Original Starknet cohort completion ACK retained; never replay its writes. Session lifecycle correction passes. Website80ba51052c14a2f705e24e9825a9fe8b495fe642 publishes the verified Starknet card, guide164b4bb96 and C#cafd0f8c8; HTML152265bytes/SHA25658bd2b0b5edec25c29d16f9351006dc4621dd6ca2818df5d08dec1551163a2a6 matches committed bytes. Browser and real-expiry interaction remain separate open gates.

There are **39 implemented query types**, including locally verified Cardano: 3 canonical adapters,34bounded public/source profiles and2local Azure/Oracle implementations. Cardano35controls/realCQL3/2/manager-CLI3/2, API/local HTTP3/2+Mongo5/4+visitor privacy and actual presets pass; hosting is next. Deployed tariffs36/hosted36/browser5/open31 remain unchanged. Full ORM/EF parity/HyperDrive certification remains incomplete.

## Exact next step

1. Radix card published/byte-verified at website0228db6; setup guide74ccc3752/C#74aaa9b4d linked. Original HostedQueryAcceptance-RadixOASIS completion ACK exists: do not replay saves or create a new owner.
2. Session lifecycle HTTP proof complete on original avatar, completion ACK retained: do not replay. Frontend9e18b7d published and HTML/JS byte-verified; browser gates remain open. [Proof and limits](SESSION_RENEWAL_STATUS.md).
3. Cardano API/local HTTP3/2+Mongo5/4/privacy/actual presets pass. Selected releasec9410b9b9 passes4shipping checks; TRON adapter absent. Batchaffe5719070d7b149d165b901306708e7643fb97/Corebe397f1 app1615af0d SUCCESS. Three settings applied once, per-key ACKs and whole candidate verified. Settings jobcfe35470-7e92-4ad5-bd53-3b77944df051 BUILDING. Observe that exact job; never repeat apply/redeploy. After SUCCESS run original fresh Cardano hosted acceptance once, then publish card. [Proof](CARDANO_PUBLIC_SOURCE_STATUS.md).
4. Browser gates:5verified/31open. CUA transport remains closed; David already opened the browser. No OS control resumed or repeated owner request.

## Guarantees and limits

- MongoDB/SQLite/Neo4j checklist50/82each; original demo set green. Work only on red rows unless a bug is reproduced.
- Added28hosted profiles retain56private demo copies and112presets. Original earlier eight proofs were not broadly rerun. Copies carry name/description/native key, not complete native chain state.
- HyperDrive scoped selection/failover/recovery proofs remain recorded; public source demos do not certify full provider writes, authority migration or universal uptime.
- Atomic billing handoff publishes settlement evidence and inline lease in one Mongo CAS. Genuine delivery failures remain durable/pending; lease expiry preserves worker recovery.
- Radix native/federation evidence began at1f39016fa; remaining reuse/isolation/replica checks completed at74aaa9b4d using the same cohort. Original failure and credential recovery are retained, not hidden.
- Azure and Oracle hosting deferred by David; no spending/signup re-asks. No production/master promotion.
- Full correctness/history/deltas, migration/placement/scale and broader ORM/EF parity remain open; legacy gaps documentation only.

## Recovery and commands

- Work only C:/Source/OASIS-cosmic; parent/Core work/cosmic-cql-google-public; verified release branch work/cosmic-cql-public-release excludes red TRON and new Starknet work. Frontend C:/Source/Web4Site work/cosmic-cql.
- Live servercafd0f8c8/Core8a5ab45, settings03760bc9 SUCCESS. Frontend80ba510 is published/byte-verified with Starknet card. No extra Development push solely for docs.
- Raw outputs TestResults/. Durable source/config/identities/ACKs LocalState/COSMIC/. Original Radix settlement-continuation manifest hashes are preserved; acceptance completion ACK now blocks reruns.
- Reuse pinned environments; inventory/cleanup in ENVIRONMENT_AND_CLEANUP_LEDGER.md. Unknown root logs preserved. Owned diagnostic DLLs removed; no new runtime or test service retained.
- Build lease: Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1; /m:2 /nr:false /p:UseSharedCompilation=false. Explicit bounded build/test, then run frozen DLL; do not forward build flags to dotnet run.
- Validate committed pins: python -B Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- SDK packaging: python -B Scripts/validate_all_provider_sdk_publish_manifests.py.
- Continuation tests: node --test Scripts/Sandbox/test_hosted_source_continuation.cjs. Completed hosted cohorts stay frozen.
- Fetch before integration, preserve concurrent changes, non-force Development pushes allowed for live demo verification. Red TRON adapter/test changes remain excluded from releases.

[Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [manual](CQL_MANUAL.md) · [architecture](CQL_ARCHITECTURE.md) · [checklist](CQL_PARITY_CHECKLIST.md) · [backlog](AFTER_DEMO_BACKLOG.md) · [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
