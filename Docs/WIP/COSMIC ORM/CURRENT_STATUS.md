# Current COSMIC ORM / CQL status

Updated 30 October 2026 UTC. Full programme remains incomplete.

## Current phase: hosted sandbox acceptance

- Worktree: C:/Source/OASIS-cosmic; parent/Core branch work/cosmic-cql-sandbox-integration.
- Approved extra Development push completed: parent e6c09bfda14735582f71191a2748147f7c38d317; Core 39b45711925ee284786897ac24bd721bec351c5e; ONODE 6b5d9d0 preserved. Manifest/gitlinks verified.
- Railway WEB4 deployment fcaea4cc-2161-422d-921d-b1b3170d5c20 is SUCCESS; /api/health returns 200.
- Eight builds, 36 API/schema/metadata controls, eight real native billing lifecycle cases and four native ancestor cases passed. Owned fixtures removed; Mongo stopped.
- Hosted verifier acknowledged all three demo saves with accounting operation IDs. Subsequent point-read observations timed out after 45 seconds; CQL acceptance stages did not run. Do not call the hosted demo verified.
- Preserve LocalState/COSMIC/HostedSandboxCqlCurrent/fixture.json and its three write acknowledgements. No pending mutation; never replay those writes. JWT was process-memory only and is no longer available.
- Evidence: TestResults/Web4SandboxCqlCurrent/hosted-demo-verification.json and LocalState/COSMIC/HostedSandboxCqlCurrent/read-diagnostic.json. Read shapes alone do not establish which replication copies exist.

## Configuration correction

- The previous profile incorrectly enrolled operation recovery without an application-authorized recovery policy; the real startup guard rejected it. No guard was weakened.
- Concurrent live configuration correction removed that route. Current six-route live profile matches the corrected template: three query routes plus save/batch/delete; 21 zero-price tariffs, quota/measurement guards retained.
- Template/startup regression correction is task-branch work for the next normal integration batch. Ten profile controls pass; red-before and green-after evidence in TestResults/Web4SandboxCqlCurrent/profile-startup-{before,after}.trx.

## Verified replication diagnosis and exact next step

Read-only live probe proves all three retained records exist in Mongo with the correct owner. Their three Neo4j obligations are Completed (one attempt each); all three SQLite obligations remain Pending (zero attempts). SQLite queue head sequence 6 / operation 79a05552-1bf3-4e26-a763-88c0e48b14a9 is ReconciliationRequired with COSMIC_REPLICA_LINEAGE_CONFLICT. Ordered delivery correctly holds newer writes behind it. Next: inspect the earlier source/target lineage and reconcile through the specified recovery contract; never skip/reset the barrier or replay acknowledged writes. The blocked holon is 874fe30b-7702-4905-9e5b-ffa93a8621f8 at version 4; five retained SQLite entries through sequence 6 contain only that version for this holon, with no predecessor enrollments. Investigate explicit enrollment/bootstrap and actual destination lineage before recovery. Railway SSH inspection could not start because no local SSH identity exists; no key was created. Destination contents/CQL acceptance remain unproved.

No additional Development push is covered by the already-used extra-batch approval. Frontend main remains unpublished pending hosted acceptance.

## Point-load routing fix verified on task branch

- Reproduced explicit Mongo point-load executing through a manager pinned to SQLite. Red-before regression retained. V2 LoadHolonRequest now captures request routing and uses its isolated manager for loading and permission projection, binding the observed policy to the response. All child/depth options are forwarded unchanged; selected-provider failures do not fall through to the default when failover is OFF.
- Shipping WEB4 and API test projects build without restore. Seventy HTTP/concurrency/delete/admission/schema controls pass, zero skipped (includes two new point-load cases). No Core/Shared public API change; legacy engine untouched. This fix is committed on the task branch only; deployment still pending the normal integration batch.

## Broader plan preserved

- Parent work/cosmic-cql f89cf0085 / Core 69bb8a4 preserved and pushed; newer provider changes excluded from this deployment.
- PostgreSQL: 16 controls, 17 native/restart queries, four paired SDK/HTTP/browser presets verified.
- SQL Server: 17 controls, 23 native/restart queries, four paired SDK/HTTP/browser presets verified.
- Oracle: 13 controls, 15 read-only native probes, 21 saved-holon queries/restart verified; Mongo-paired content/browser/sandbox still pending.
- Next breadth after hosted acceptance: Oracle pairing, then approved ten-provider content/sandbox plan. Hosted Azure remains pending/free-only.
- LINQ depth, POCO/migrations deferred. Work only on red checklist cells unless a reproducible bug exists. HyperDrive V2 only; document legacy issues.
- Frontend C:/Source/Web4Site work/cosmic-cql c17e5f7: four request-helper and 11 CQL tests pass; main unpublished.

## Commands and recovery

- Build through Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 with /m:2 /nr:false /p:UseSharedCompilation=false; reuse existing caches, no restore unless necessary.
- Validate pins: python Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- Do not blindly rerun Verify-HostedCqlDemo.py: its persistent fixture correctly prevents reseeding acknowledged writes.
- Logs/TRX only under this worktree's TestResults; persistent identities/source/ACK journals under LocalState. Unknown root logs untouched.
- [Release report](CQL_SANDBOX_RELEASE_2026-10-30.md), [strict checklist](CQL_PARITY_CHECKLIST.md), [roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
