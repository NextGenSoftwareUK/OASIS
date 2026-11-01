# COSMIC ORM / CQL — current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demo. Deep LINQ parity remains paused.

## Current verified milestone

**35 providers have scoped hosted HTTP evidence; 5 have browser evidence and 30 browser gates remain open.** Radix native3/2 and Mongo5/4 pass; the original cohort resumed after the atomic settlement fix with zero new saves/two reused copies, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2. Source writes and browser interaction are not inferred.

Latest provider acceptance remains Radix at74aaa9b4d/Core8b79301; original cohort completed with no duplicate saves. Session correctiond9332469d/Coreb835014 is now live: same-avatar secure rotation, refresh revocation/cookie clear and rejected revoked refresh pass. Website main9e18b7dedda7c4e6cfaa358fa8987774616deaac serves matching HTML150878bytes and both versioned JS assets, including Radix setup/C# links. Publication/HTTP checks do not certify browser interaction or real expiry waiting. See SESSION_RENEWAL_STATUS.md for exact proof hashes.

There are **38 implemented query types**: 3 canonical adapters,33bounded public/source profiles and2local Azure/Oracle implementations. Starknet31controls, real CQL/manager/CLI3/2, API/schema3/3, local HTTP3/2+Mongo5/4+foreign-private0/public3 and actual four-preset planning pass; hosting remains pending. Deployed tariffs35, hosted HTTP35, browser5/open30. Azure/Oracle hosting deferred; TRON429red. Full ORM/EF parity/HyperDrive certification remains incomplete.

## Exact next step

1. Radix card published/byte-verified at website0228db6; setup guide74ccc3752/C#74aaa9b4d linked. Original HostedQueryAcceptance-RadixOASIS completion ACK exists: do not replay saves or create a new owner.
2. Session lifecycle HTTP proof complete on original avatar, completion ACK retained: do not replay. Frontend9e18b7d published and HTML/JS byte-verified; browser gates remain open. [Proof and limits](SESSION_RENEWAL_STATUS.md).
3. Starknet API/local HTTP/Mongo/privacy and actual four-preset candidate now pass. Core8a5ab45 query-only admission is task-only; three settings staged in DPAPI, no host mutation. Selected release274ccf42e passes4shipping checks; TRON adapter absent from actual DLL. Applicationcafd0f8c8 job151477e9 SUCCESS. Three settings applied/individually acknowledged/whole-candidate verified. One settings deployment03760bc9-9984-40c1-8963-55f907f51e66 BUILDING; observe, never resend. Config now36query tariffs; hosted proof remains35until fresh acceptance. Then run Verify-HostedQuerySource.py StarknetOASIS --prepare once. Original local fixture cleaned; do not reseed/replay.
4. Browser gates:5verified/30open. CUA transport remains closed; David already opened the browser. No OS control resumed or repeated owner request.

## Guarantees and limits

- MongoDB/SQLite/Neo4j checklist50/82each; original demo set green. Work only on red rows unless a bug is reproduced.
- Added27hosted profiles retain54private demo copies and108presets. Original earlier eight proofs were not broadly rerun. Copies carry name/description/native key, not complete native chain state.
- HyperDrive scoped selection/failover/recovery proofs remain recorded; public source demos do not certify full provider writes, authority migration or universal uptime.
- Atomic billing handoff publishes settlement evidence and inline lease in one Mongo CAS. Genuine delivery failures remain durable/pending; lease expiry preserves worker recovery.
- Radix native/federation evidence began at1f39016fa; remaining reuse/isolation/replica checks completed at74aaa9b4d using the same cohort. Original failure and credential recovery are retained, not hidden.
- Azure and Oracle hosting deferred by David; no spending/signup re-asks. No production/master promotion.
- Full correctness/history/deltas, migration/placement/scale and broader ORM/EF parity remain open; legacy gaps documentation only.

## Recovery and commands

- Work only C:/Source/OASIS-cosmic; parent/Core work/cosmic-cql-google-public; verified release branch work/cosmic-cql-public-release excludes red TRON and new Starknet work. Frontend C:/Source/Web4Site work/cosmic-cql.
- Live serverd9332469d/Coreb835014; provider acceptance remains74aaa9b4d. Frontend9e18b7d is published/byte-verified. Selected Starknet release274ccf42e/Core8a5ab45 awaits publication.
- Raw outputs TestResults/. Durable source/config/identities/ACKs LocalState/COSMIC/. Original Radix settlement-continuation manifest hashes are preserved; acceptance completion ACK now blocks reruns.
- Reuse pinned environments; inventory/cleanup in ENVIRONMENT_AND_CLEANUP_LEDGER.md. Unknown root logs preserved. Owned diagnostic DLLs removed; no new runtime or test service retained.
- Build lease: Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1; /m:2 /nr:false /p:UseSharedCompilation=false. Explicit bounded build/test, then run frozen DLL; do not forward build flags to dotnet run.
- Validate committed pins: python -B Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- SDK packaging: python -B Scripts/validate_all_provider_sdk_publish_manifests.py.
- Continuation tests: node --test Scripts/Sandbox/test_hosted_source_continuation.cjs. Completed hosted cohorts stay frozen.
- Fetch before integration, preserve concurrent changes, non-force Development pushes allowed for live demo verification. Red TRON adapter/test changes remain excluded from releases.

[Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [manual](CQL_MANUAL.md) · [architecture](CQL_ARCHITECTURE.md) · [checklist](CQL_PARITY_CHECKLIST.md) · [backlog](AFTER_DEMO_BACKLOG.md) · [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
