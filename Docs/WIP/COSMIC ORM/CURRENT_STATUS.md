# COSMIC ORM / CQL — current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demo. Deep LINQ parity remains paused.

## Current verified milestone

**38 providers have scoped hosted HTTP evidence; 5 have browser evidence and 33 browser gates remain open.** Arweave passes native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2. Two private saves were acknowledged once. Source writes and browser interaction are not inferred.

Latest hosted acceptance: parent37a530a87e601397f187430728bb9fb02b224f87/Core5fd361db11f5d54e3e99282536d7c7be842edf86, job3da7f986 SUCCESS. This docs-only descendant preserves testedb158bda61 code; original settings jobec59010e was removed and never replayed. Original Arweave cohort completion ACK retained. Website5d9845c is the last byte-verified publication; Arweave card4f22ac3 is staged. Browser/real-expiry gates remain separate.

There are **40 implemented query types**: 3 canonical adapters,35bounded public/source profiles and2local Azure/Oracle implementations. Arweave native/control/manager/API/local and hosted federation/reuse/privacy/replica gates pass. Deployed tariffs38/hosted38/browser5/open33. TON candidate controls pass but nativeHTTP429 remains red and isolated; no TON support grant. Full ORM/EF parity/HyperDrive certification remains incomplete.

## Exact next step

1. Radix card published/byte-verified at website0228db6; setup guide74ccc3752/C#74aaa9b4d linked. Original HostedQueryAcceptance-RadixOASIS completion ACK exists: do not replay saves or create a new owner.
2. Session lifecycle HTTP proof complete on original avatar, completion ACK retained: do not replay. Frontend9e18b7d published and HTML/JS byte-verified; browser gates remain open. [Proof and limits](SESSION_RENEWAL_STATUS.md).
3. Arweave hosted acceptance complete on original cohort; completion ACK blocks replay. Publish staged card4f22ac3 with updated guide and actual C#b158bda61 links, byte-verify HTML, then return to breadth work. [Proof](ARWEAVE_PUBLIC_SOURCE_STATUS.md).
4. Browser gates:5verified/33open. CUA transport remains closed; David already opened the browser. No OS control resumed or repeated owner request.
5. TON candidate2764d64e1 on work/cosmic-cql-ton-public:12new query controls pass, live activationHTTP429; earlier23decoder controls+1captured test green. [Evidence](TON_PUBLIC_SOURCE_STATUS.md). Do not merge/deploy as native-ready; no retry/fallback or support-count grant.

## Guarantees and limits

- MongoDB/SQLite/Neo4j checklist50/82each; original demo set green. Work only on red rows unless a bug is reproduced.
- Added30hosted profiles retain60private demo copies and120presets. Original earlier eight proofs were not broadly rerun. Copies carry name/description/native key, not complete native chain state.
- HyperDrive scoped selection/failover/recovery proofs remain recorded; public source demos do not certify full provider writes, authority migration or universal uptime.
- Atomic billing handoff publishes settlement evidence and inline lease in one Mongo CAS. Genuine delivery failures remain durable/pending; lease expiry preserves worker recovery.
- Radix native/federation evidence began at1f39016fa; remaining reuse/isolation/replica checks completed at74aaa9b4d using the same cohort. Original failure and credential recovery are retained, not hidden.
- Azure and Oracle hosting deferred by David; no spending/signup re-asks. No production/master promotion.
- Full correctness/history/deltas, migration/placement/scale and broader ORM/EF parity remain open; legacy gaps documentation only.

## Recovery and commands

- Work only C:/Source/OASIS-cosmic; parent/Core work/cosmic-cql-google-public; verified release branch work/cosmic-cql-public-release excludes red TRON and new Starknet work. Frontend C:/Source/Web4Site work/cosmic-cql.
- Live app37a530a87/Core5fd361d job3da7f986 SUCCESS, exact tested codeb158bda61 plus David-list update. Frontend5d9845c last published; Arweave staged4f22ac3 next. No Development push solely for docs.
- Raw outputs TestResults/. Durable source/config/identities/ACKs LocalState/COSMIC/. Original Radix settlement-continuation manifest hashes are preserved; acceptance completion ACK now blocks reruns.
- Reuse pinned environments; inventory/cleanup in ENVIRONMENT_AND_CLEANUP_LEDGER.md. Unknown root logs preserved. Owned diagnostic DLLs removed; no new runtime or test service retained.
- Build lease: Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1; /m:2 /nr:false /p:UseSharedCompilation=false. Explicit bounded build/test, then run frozen DLL; do not forward build flags to dotnet run.
- Validate committed pins: python -B Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- SDK packaging: python -B Scripts/validate_all_provider_sdk_publish_manifests.py.
- Continuation tests: node --test Scripts/Sandbox/test_hosted_source_continuation.cjs. Completed hosted cohorts stay frozen.
- Exact settings observation: python -B Scripts/Sandbox/Observe-HostedDeployment.py 3da7f986-6e65-4fcf-b8f8-4916e688a9b6 --expected-commit 37a530a87e601397f187430728bb9fb02b224f87 --output TestResults/ArweavePublicSourceCandidateCurrent/settings-deployment-observation.json. No restart on observation timeout.
- Fetch before integration, preserve concurrent changes, non-force Development pushes allowed for live demo verification. Red TRON adapter/test changes remain excluded from releases.

[Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [manual](CQL_MANUAL.md) · [architecture](CQL_ARCHITECTURE.md) · [checklist](CQL_PARITY_CHECKLIST.md) · [backlog](AFTER_DEMO_BACKLOG.md) · [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
