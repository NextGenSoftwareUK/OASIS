# Current COSMIC ORM / CQL status

Updated 30 October 2026 UTC. Full programme remains incomplete.

## Current phase: MVP demo only

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

Read-only live probe proves all three retained records exist in Mongo with the correct owner. Their three Neo4j obligations are Completed (one attempt each); all three SQLite obligations remain Pending (zero attempts). SQLite queue head sequence 6 / operation 79a05552-1bf3-4e26-a763-88c0e48b14a9 is ReconciliationRequired with COSMIC_REPLICA_LINEAGE_CONFLICT. Ordered delivery correctly holds newer writes behind it. Hosted next: inspect the earlier source/target lineage before an authorized recovery; never skip/reset the barrier or replay acknowledged writes. The blocked holon is 874fe30b-7702-4905-9e5b-ffa93a8621f8 at version 4; five retained SQLite entries through sequence 6 contain only that version for this holon, with no predecessor enrollments. Investigate explicit enrollment/bootstrap and actual destination lineage before recovery. Subsequent bounded SSH inspection succeeded using the registered task identity. API process1 has /data/OASISSQLLiteDB.db open. Read-only native queries (normalized GUIDs) found zero current rows, replica heads, archived revisions and matching receipt JSON for the blocked holon. Evidence: TestResults/Web4SandboxCqlCurrent/hosted-sqlite-lineage-inspection.json, SHA256 F20AEA3AE71C7C938FF07455539DED2F807ED3A27FB494CC0108D76DADE28F9D. This supports missing initial enrollment; it is not atomic baseline admission or permission to skip/retry the held entry. Next hosted step: package the tested durable authorized application for the actual Mongo-to-SQLite route, verify it locally, then apply and verify this actual hosted demo repair. Keep implementation limited to this reproduced blocker. No hosted data changed. Oracle pairing remains independent demo work.

No additional Development push is covered by the already-used extra-batch approval. Frontend main remains unpublished pending hosted acceptance.

## Point-load routing fix verified on task branch

- Reproduced explicit Mongo point-load executing through a manager pinned to SQLite. Red-before regression retained. V2 LoadHolonRequest now captures request routing and uses its isolated manager for loading and permission projection, binding the observed policy to the response. All child/depth options are forwarded unchanged; selected-provider failures do not fall through to the default when failover is OFF.
- Shipping WEB4 and API test projects build without restore. Seventy HTTP/concurrency/delete/admission/schema controls pass, zero skipped (includes two new point-load cases). No Core/Shared public API change; legacy engine untouched. This fix is committed on the task branch only; deployment still pending the normal integration batch.

## Replication worker diagnostic fix

- WEB4 previously treated a durable reconciliation head like idle work. It now reports source/target, sequence, operation and error code, then leaves the barrier untouched without activating the receiver. One focused strict-outbox test passes; no payload/error text is logged. This exposes the failure; it does not repair lineage or establish destination readiness.
- X10a/X10b explicitly track missing late-baseline installation and authorized repair/resume; both remain red on all three stores. Earlier roadmap instructions to resume deep LINQ were stale and are corrected to the current breadth-first priority.

## Explicit baseline implementation milestone

- Core task branch 05f6a2c adds a separate trusted initialization capability; existing interfaces unchanged. Shared plan uses a distinct fingerprint domain. SQLite implements atomic initial-baseline installation with current/head/history/receipt absence fences and exact replay; ordinary sequential delivery unchanged.
- 31 native SQLite initialization/replica/outbox cases and 67 shared replica controls pass, zero skipped. Seven new native and four new shared cases. Core both targets, Shared, SQLite and test projects build without restore. Owned source/target fixtures removed after disposal; no hosted mutation.
- Exact implementation/remaining contract: [baseline and recovery](REPLICA_BASELINE_AND_RECOVERY.md). X10a remains red: Mongo operator/migration composition, larger-store Neo admission and authorized coordinator still needed. X10b repair/resume remains red; hosted queue unchanged. Current direction: fix only demonstrated demo blockers and continue provider breadth. Do not expand generic application/recovery or parity features ahead of the MVP. Deferred requirements are recorded in AFTER_DEMO_BACKLOG.md; hosted conflict still needs actual target inspection and verified resolution. Mongo native audited receipt migration is implemented/tested; hosted application is still pending. Native baseline primitives exist on SQLite/Mongo/Neo, but their end-to-end acceptance stays red.

## Mongo native baseline milestone

- Same trusted capability/shared plan now implemented on Mongo. Persistent per-holon fences serialize all atomic graph mutations, including hard deletes; baseline requires absence. Receipts carry HolonIds; unknown historical membership refuses initialization with an explicit reconciliation code. Positive replay remains read-only.
- Two native sync/async scenario runs pass (version-4 graph, restart/replay, version-5 delivery, competing initializers, independent child/hard-delete protection, unknown receipt rejection). Eight shipping native billing lifecycle controls pass. Four producer/consumer builds pass without restore. Exact owned DBs removed and Mongo stopped; no hosted mutation. See baseline/recovery doc for V2 scope and historical migration limits.

## Neo4j native baseline milestone

- Same capability/shared plan now implemented using selected-node locks, archived-revision checks and streamed canonical receipt validation (max 10000; larger/unknown audits require reconciliation). No APOC/plugin added, implicit baseline, old-receipt mutation or HTTP grant.
- Ten native cases pass/zero skips: four new sync/async x history cases plus six existing sequential/SQLite-target compatibility controls. Reopen/replay, version-5 delivery, competing clients, independent child/hard-delete and unknown-receipt rejection verified. Native nodes/history/receipts and source files removed; persistent ownership journal cleaned=true. Owned container/attached process stopped.

## Mongo audited migration milestone

- Explicit batches1–128 bind configured-store hash, operator, receipt hashes/member IDs and stable migration ID. Native transaction preserves original receipt bodies/fingerprints and commits membership metadata with a durable integrity-bound acknowledgement; stale batches roll back wholly.
- Four native scenarios pass/zero skips (two migrations plus two baseline scenarios): byte preservation, wrong-store/forged intent, stale rollback, reopened replay, concurrent submissions, cancellation, corrupt ack refusal and continued hard-delete protection. Ownership/job intents persisted before writes; exact fixture DBs removed and Mongo stopped. No hosted migration.

## Authorized source preparation milestone

- Core6b8c519 adds a separate trusted immutable-obligation reader on all three native outboxes; redacted inspection unchanged. Shared preparation requires explicit source-read and exact-baseline approval, freezes host route/operator/policy/source digest, verifies current held lineage head and rejects unresolved native execution. Metadata-only output, no private payload/target call/source state change.
- 84 shared controls, three native SQLite cases (one actual held-delivery preparation), six native Mongo cases and two native Neo outbox cases pass/no skips. Core both targets and producer/consumer builds pass; owned fixtures/daemons/container stopped. Core task branch pushed/verified; parent gitlink/manifest advance together. No hosted change.

## MVP scope and parked work

- User reaffirmed demo-only priority. [After-demo backlog](AFTER_DEMO_BACKLOG.md) preserves unfinished recovery/coordinator, cutover, parity, mapping/migrations, full ORM and production-depth requirements. Nothing is silently dropped or marked complete.
- Read-only target initialization receipt inspection is now tested on SQLite/Mongo/Neo: exact original baseline plan, absent is not permission to retry, committed evidence from immutable receipt, no graph values exported. SQLite2/Mongo6/Neo10 cases pass, including corruption/domain rejection and old receipt after later versions. This checkpoint closes existing work; no further recovery expansion planned without a demonstrated demo need.

## Demo repair: held Mongo source completion

- Concrete trusted Mongo capability now completes only the reviewed held head against an exact committed initialization receipt, original payload/operation/attempt and baseline fingerprint. Native transaction preserves original failure/evidence and records exact initialization acknowledgement; exact replay is read-only. No unauthenticated endpoint or generic queue reset.
- Seven native cases pass/no skips, including actual late version4 enrollment, target baseline, stale-attempt/absent-receipt rejection, immutable replay and normal subsequent version5 delivery. Durable application now passes23 shared controls and8 native Mongo cases: flushed redacted intent before submission, exact receipt recovery after lost responses, policy reauthorization before source completion, and no resubmission on absent outcome. Actual Mongo-to-SQLite tool packaging/route proof and hosted repair remain next; demo is still blocked until those pass. No hosted mutation.

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
