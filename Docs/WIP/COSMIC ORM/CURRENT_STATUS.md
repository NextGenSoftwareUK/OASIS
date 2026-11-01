# COSMIC ORM / CQL — current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demo. Deep LINQ parity remains paused.

## Current verified milestone

**35 providers have scoped hosted HTTP evidence; 5 have browser evidence and 30 browser gates remain open.** Radix native3/2 and Mongo5/4 pass; the original cohort resumed after the atomic settlement fix with zero new saves/two reused copies, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2. Source writes and browser interaction are not inferred.

Latest hosted acceptance: parent `74aaa9b4daeadfad00ce9c667d78bb957f219ea8`, Core `8b79301b275536c04e7a971dc88d61735ebde96f`, correction job `f4c02dbd-7e7d-4482-9e2c-1ee28f5e1cc6` SUCCESS. Initial Radix app0b41c4b9/settings15f047fd succeeded at1f39016fa. Original owner was renewed through the normal refresh endpoint after scoped owned-fixture credential recovery; automatic sandbox/browser session renewal remains open. Website main0228db6adca82f3c457e30aa836b117d9690577d now serves the Radix card and pinned setup/C# links byte-identically (150955bytes, SHA25654588e7bd9c0368714d9b57b6f4f370c772d93b648665ada00d6a3dc8dbfca42). This proves publication, not browser interaction.

There are **37 implemented query types**: 3 canonical adapters, 32 bounded public/source profiles and 2 local Azure/Oracle implementations. Actual deployed query-tariff catalogue has **35 entries**, confirmed from live configuration (the two local cloud implementations are excluded). Radix50native tests, actual CLI3/2, API/local HTTP and hosted continuation pass; atomic settlement fix has6native/125related/4shipping checks. Azure/Oracle hosting remains deferred. TRON429 stays red; Starknet native API headers3/2qualified, CQL not implemented. Full ORM/EF parity/HyperDrive certification remains incomplete.

## Exact next step

1. Radix card published/byte-verified at website0228db6; setup guide74ccc3752/C#74aaa9b4d linked. Original HostedQueryAcceptance-RadixOASIS completion ACK exists: do not replay saves or create a new owner.
2. Fix the newly reproduced sandbox session-renewal gap: sandbox/session does not issue the refresh cookie; the acceptance harness retained only JWT. Existing normal avatar/refresh-token works. Preserve original session/rotated credentials in DPAPI and use the same avatar. Add cookie/session tests; no expiry extension or identity replacement to hide it.
3. Continue Starknet: guarded native spec/chain/genesis and explicitly selected headers3/2pass, including parent links and repeated result equality. Implement/test the native CQL adapter before API/hosting admission. TRON's real429remains red; no blind retry or grant.
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

- Work only C:/Source/OASIS-cosmic; parent currently work/cosmic-cql-public-release, Core work/cosmic-cql-google-public. Main task work/cosmic-cql-google-public. Frontend C:/Source/Web4Site work/cosmic-cql.
- Live parent74aaa9b4d/Core8b79301; subsequent release-branch commits contain docs and local continuation tools. No extra Development push solely for those docs.
- Raw outputs TestResults/. Durable source/config/identities/ACKs LocalState/COSMIC/. Original Radix settlement-continuation manifest hashes are preserved; acceptance completion ACK now blocks reruns.
- Reuse pinned environments; inventory/cleanup in ENVIRONMENT_AND_CLEANUP_LEDGER.md. Unknown root logs preserved. Owned diagnostic DLLs removed; no new runtime or test service retained.
- Build lease: Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1; /m:2 /nr:false /p:UseSharedCompilation=false. Explicit bounded build/test, then run frozen DLL; do not forward build flags to dotnet run.
- Validate committed pins: python -B Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- SDK packaging: python -B Scripts/validate_all_provider_sdk_publish_manifests.py.
- Continuation tests: node --test Scripts/Sandbox/test_hosted_source_continuation.cjs. Completed hosted cohorts stay frozen.
- Fetch before integration, preserve concurrent changes, non-force Development pushes allowed for live demo verification. Red TRON adapter/test changes remain excluded from releases.

[Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [manual](CQL_MANUAL.md) · [architecture](CQL_ARCHITECTURE.md) · [checklist](CQL_PARITY_CHECKLIST.md) · [backlog](AFTER_DEMO_BACKLOG.md) · [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
