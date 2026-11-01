# COSMIC ORM / CQL — current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demo. Deep LINQ parity remains paused.

## Current verified milestone

**38 providers have scoped hosted HTTP evidence; 5 have browser evidence and 33 browser gates remain open.** Arweave passes native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2. Two private saves were acknowledged once. Source writes and browser interaction are not inferred.

Latest hosted acceptance: parent37a530a87e601397f187430728bb9fb02b224f87/Core5fd361db11f5d54e3e99282536d7c7be842edf86, job3da7f986 SUCCESS. This docs-only descendant preserves testedb158bda61 code; original settings jobec59010e was removed and never replayed. Original Arweave cohort completion ACK retained. Website61e85b75a933004d1f005f85f1318fa05ad139de publishes Arweave with guided089a778c/C#b158bda61. Public HTML155229bytes/SHA2566fda659c10e90bb99d7636e04b62d3dc343b3a0c36cd85bb5b3f3c3e3c5c94f3 matches the committed git blob. Browser/real-expiry gates remain separate.

There are **41 implemented query types**, including locally verified TON: 3 canonical adapters,36bounded public/source profiles and2local Azure/Oracle implementations. TON native/manager 3/2 and local HTTP native 3/2, Mongo-paired 5/4 and privacy 0/3 pass on the qualified recent dataset; hosted verification is next. Hosted38/tariffs38/browser5/open33 remain unchanged. Full ORM/EF parity/HyperDrive certification remains incomplete.

## Exact next step

1. Radix card published/byte-verified at website0228db6; setup guide74ccc3752/C#74aaa9b4d linked. Original HostedQueryAcceptance-RadixOASIS completion ACK exists: do not replay saves or create a new owner.
2. Session lifecycle HTTP proof complete on original avatar, completion ACK retained: do not replay. Frontend9e18b7d published and HTML/JS byte-verified; browser gates remain open. [Proof and limits](SESSION_RENEWAL_STATUS.md).
3. Arweave hosted acceptance complete on original cohort; completion ACK blocks replay. Arweave card61e85b7 is published/byte-verified with updated guide/C# links. Continue breadth; TON hosted gates remain pending, other provider candidates next. [Proof](ARWEAVE_PUBLIC_SOURCE_STATUS.md).
4. Browser gates:5verified/33open. CUA transport remains closed; David already opened the browser. No OS control resumed or repeated owner request.
5. TON local HTTP passes native 3/2, paired 5/4, privacy 0/3 and 9 receipts, zero repeat saves; fixture cleaned. Development fb29aa936 merged in task99b4f080/Core2ac915f; changed contracts/schema/admission/actual presets pass32/32. Original app4f6eba6c is SUCCESS, descendant571cb5f0 is QUEUED; observe existing jobs. Live settings drifted2values; explicit ton-public-duration-oland revision prepared preserving them. Same three TON values/public review bytes match tested candidate; five publisher controls pass. Old candidates excluded. Await exact latest successful release guard before apply. No TON host mutation. [Evidence](TON_PUBLIC_SOURCE_STATUS.md).

## Guarantees and limits

- MongoDB/SQLite/Neo4j checklist50/82each; original demo set green. Work only on red rows unless a bug is reproduced.
- Added30hosted profiles retain60private demo copies and120presets. Original earlier eight proofs were not broadly rerun. Copies carry name/description/native key, not complete native chain state.
- HyperDrive scoped selection/failover/recovery proofs remain recorded; public source demos do not certify full provider writes, authority migration or universal uptime.
- Atomic billing handoff publishes settlement evidence and inline lease in one Mongo CAS. Genuine delivery failures remain durable/pending; lease expiry preserves worker recovery.
- Radix native/federation evidence began at1f39016fa; remaining reuse/isolation/replica checks completed at74aaa9b4d using the same cohort. Original failure and credential recovery are retained, not hidden.
- Azure and Oracle hosting deferred by David; no spending/signup re-asks. No production/master promotion.
- Full correctness/history/deltas, migration/placement/scale and broader ORM/EF parity remain open; legacy gaps documentation only.

## Recovery and commands

- Work only C:/Source/OASIS-cosmic; active candidate work/cosmic-cql-ton-public, Core work/cosmic-cql-google-public. Verified release work/cosmic-cql-public-release excludes unqualified TRON/TON query candidates; TON source must stay isolated until its gates pass. Frontend C:/Source/Web4Site work/cosmic-cql.
- Live app37a530a87/Core5fd361d job3da7f986 SUCCESS, exact tested codeb158bda61 plus David-list update. Frontend299c376 duration/setup fixes published/byte-verified; Arweave card preserved. No Development push solely for docs.
- Raw outputs TestResults/. Durable source/config/identities/ACKs LocalState/COSMIC/. Original Radix settlement-continuation manifest hashes are preserved; acceptance completion ACK now blocks reruns.
- Reuse pinned environments; inventory/cleanup in ENVIRONMENT_AND_CLEANUP_LEDGER.md. Unknown root logs preserved. Owned diagnostic DLLs removed; no new runtime or test service retained.
- Build lease: Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1; /m:2 /nr:false /p:UseSharedCompilation=false. Explicit bounded build/test, then run frozen DLL; do not forward build flags to dotnet run.
- Validate committed pins: python -B Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- SDK packaging: python -B Scripts/validate_all_provider_sdk_publish_manifests.py.
- Continuation tests: node --test Scripts/Sandbox/test_hosted_source_continuation.cjs. Completed hosted cohorts stay frozen.
- Exact settings observation: python -B Scripts/Sandbox/Observe-HostedDeployment.py 3da7f986-6e65-4fcf-b8f8-4916e688a9b6 --expected-commit 37a530a87e601397f187430728bb9fb02b224f87 --output TestResults/ArweavePublicSourceCandidateCurrent/settings-deployment-observation.json. No restart on observation timeout.
- Fetch before integration, preserve concurrent changes, non-force Development pushes allowed for live demo verification. Red TRON adapter/test changes remain excluded from releases.

[Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [manual](CQL_MANUAL.md) · [architecture](CQL_ARCHITECTURE.md) · [checklist](CQL_PARITY_CHECKLIST.md) · [backlog](AFTER_DEMO_BACKLOG.md) · [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).

Browser query timeout bug fixed and published: website7a02a23 aligns timeout with explicit server budget (up to30s plus5s transport allowance, minimum15s), no retry. All89client tests pass, including actual run() timer/one-dispatch regression; four stale session test harnesses corrected. Public HTML and versioned JS exactly match git blobs. Backend deployment4f6eba6c still QUEUED; observe same job, no duplicate build. New descendant fb29aa936 has another queued job571cb5f0-85f4-40fa-9d99-568339833b3e. It preserves TON code/Core pins but adds OLAND contracts; integrate/review it before applying against the latest successful exact release. Never weaken release guard or replay a deployment.

Preparation duration regression reproduced and fixed at website299c376: derived native setup query now carries validated paired budget before saves. All90client tests pass; live HTML/preparation/query assets match commit blobs. No TON card or new hosted cohort. Backend job4f6eba6c remains QUEUED; latest descendant job571cb5f0 also QUEUED; current successful app is6b5db934/job5191ee53. Next review/integrate fb29aa936 while observing existing jobs, no extra code build submission.

Latest checkpoint: descendant integration32/32pass, no skips; committed pins validate. Configuration baseline observation reports2changed values, TON source absent, zero host mutation. Do not print secret values or overwrite snapshots; prepare an explicit reviewed revision preserving current live settings and previous evidence. TON recovery report consolidated to50lines; detailed history preserved in Archive/TON_PUBLIC_SOURCE_HISTORY_2026-11-01.md.

Reviewed configuration revision ready: ton-public-duration-oland before/candidate DPAPI snapshots preserve all live unrelated values; exactly3TONkeys differ and their values equal the tested candidate. No mutation intents/ACKs. New pure revision guards reject unsafe names and any prior mutation evidence. Future apply uses unchanged release guard plus generic apply_values with this exact revision prefix, never original stale CLI apply. Source owner/catalogue unchanged.

Code deployment milestone:4f6eba6c at exact24f548550 is SUCCESS. Newer descendant571cb5f0 at fb29aa936 remains QUEUED, so unchanged latest-release guard still blocks settings apply. Observe that same job, then recheck revised baseline immediately before guarded apply. No TON settings or acceptance sessions created; no duplicate deployment.
