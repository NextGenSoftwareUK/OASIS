# CQL sandbox scoped release — 30 October 2026

Current integration workspace: C:/Source/OASIS-cosmic. Parent/Core branch
work/cosmic-cql-sandbox-integration. Broader task branch work/cosmic-cql is preserved.
Parent base835653f31, Core baseb60796d; source fetched before integration.

Scope: Core federation startup attempt/budget fix only (candidate432c613), WEB4
free CQL profile, temporary free subscription lifecycle, their tests/operator probes
and the exact reusable native Mongo helper needed by the verification runner.
No other unmerged provider/ORM changes are included in this deployment candidate.
Core pin and parent gitlink move together. Source task commits79301692f,8a326f9ff,
65f232c71; frontend commit2ec92d1 remains separately on Web4Site work/cosmic-cql.

## Verified before scoped integration

Hosted dev accounting oasis_subscription_accounting opening marker exists,
no legacy operations; observation15:43UTC used only native read/auth commands.
No initialization, reset, subscription or hosted configuration writes.
Source-task Core52controls, WEB4 lifecycle21controls+7native and frontend11controls
pass within their declared scopes. Integration-candidate verification is next;
previous task-binary results do not certify this release source graph.

## Immediate next step

Build/test candidate Core and WEB4 under the existing lease with
/m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false,
no restore/download; rerun exact native lifecycle on the retained owned Mongo.
Reconcile cached dependency provenance before treating consumer results as proof.
Then push and verify Core, publish a scoped parent Development batch with matching
pin, set the explicit free admission profile with --skip-deploys and observe the
actual WEB4 deployment to terminal readiness. Re-test live visitor/query behavior.
Do not publish the static page as a successful demo before those live checks.

## Resources and cleanup

Reuses the one existing worktree/runtime/cache/test service; no duplicate folder,
installation, download, source store or new environment. Integration branches are
Git references in existing repositories, not extra checkouts. Fixed logs/TRX under
TestResults/Web4SandboxCqlCurrent are regenerable, never raw docs evidence.
Reuse /var/tmp/oasis-cosmic-mongo/data, owned Mongo8.3.13/27039 and
/var/tmp/oasis-runtimes/dotnet-10.0.12 plus cached portable VSTest18.11.0.
Native test DB cosmic_cql_sandbox_subscription_tests must start absent, carries an
exact ownership marker, and is dropped only after validation. Mongo stops finally.
Existing shared hosted accounting source/credentials/data must remain untouched.
Cfree66,632,937,472B before candidate builds; output sizes unmeasured.
See existing ENVIRONMENT_AND_CLEANUP_LEDGER.md; full task ledger preserved on
work/cosmic-cql at65f232c71. No idle process is evidence that user data is disposable.

## Candidate verification complete

Core432c613 build and52configured-admission controls pass/0skip. WEB4 build and
25controls pass/0skip (lifecycle/profile/local authority/shipping schema plus4sharing
input regressions). Native lifecycle7passes/0skip on the retained owned Mongo; all
fixed disposable fixtures removed, native shutdown0. No restore/download/install.
MongoDB/SQLite/Neo4j producers rebuilt against the scoped Core source before consumer
verification; broader task binaries cannot certify this release graph.

The unmodified Development baseline also had two CS0165 null-conditional out-variable
errors in AvatarProfileController/ShareController. Included only their existing
task fixes plus4regression tests; missing bodies reject before provider access.
No bypass, copied DLL, new environment or broader task API was introduced.

Core candidate432c613fb3101015a9048c507307bc949bff79b5 pushed to the integration
branch and verified with ls-remote. Development publication/configuration and actual
hosted row/scalar/federation/visitor checks remain next; do not claim them complete.
Cfree66,633,560,064B after candidate verification, outputs otherwise unmeasured.

- `TestResults/Web4SandboxCqlCurrent/release-admission-controls.trx`: 85015B, SHA256 `3684a12ba54f2f4af8dd9b15c7353f6ebfa7a7eb8b5367643dc2dbc9f319968b`.

- `TestResults/Web4SandboxCqlCurrent/release-lifecycle-controls.trx`: 39716B, SHA256 `efb9a15ab5eb3e53daee37439e8ec825bfbc2f2acb0a2383e39eb3a0c3a26885`.

- `TestResults/Web4SandboxCqlCurrent/lifecycle-native.trx`: 11944B, SHA256 `f4f661ae870818d10bd0ec57943cf727414cbdef4e7ac38613fa0c9bc33cd086`.

## Published batch / verified live wait

Core Development432c613 and parent Developmentdfceb139239283cba960d954a9c2fd881dbd2a2b
pushed/ls-remote verified30Oct2026. Explicit free profile staged on existing dev
WEB4 with --skip-deploys and exact canonical read-back. Native providers0, tariffs0,
row/scalar/federation only, no ledger initialization or paid service created.
Railway deploymentefd2c5c1-288b-4b58-8b8c-bee0f1690003 is BUILDING for this exact
parent commit (created15:56:47UTC). Do not start a duplicate deployment or infer success.
Observed GitHub runs41085351582 CI/CD queued,41085351698 ONET/ONODE in progress,
41085351542 subscription protocol in progress; external workflow-managed resources.
Next: observe this exact deployment to terminal status, then actual hosted session,
query and isolation checks. Static Web4Site publication still pending live proof.

## Focused hosted Mongo stage-limit correction

First deploymentefd2c5c1 is SUCCESS. A new demo session gets1free subscription;
hosted CQL passes admission but row execution failed at the native stage limit.
Operation610b294d-b037-980a-5b73-eb33fab22a6d retained; no request replay.
Read-only metadata confirms valid membership/string IDs/schema1; hosted queryPlanner
rejects inherited template56stages with native7749501 (maximum50).

Focused fix preserves all16ancestor levels and permission semantics, removes each
temporary lookup field in the same $set that advances flags/frontier ($$REMOVE),
and reduces56to40stages. The failing regression proves the old56stage shape;
new2controls+3native pass/0skip, including16th/17th boundary, actor/group/public
inheritance, cycles, missing parents and no temporary-field leakage. Source-task
read-only hosted planner accepts40stages; not a hosted execution success claim.

Scoped candidate changes only translator, ancestor regression/owned fixture and
its reusable native runner, plus this audit. Core pin remains432c613, other prior
release code unchanged. Scoped Core/Mongo/SQLite/Neo/WEB4 producers built; native
fixed fixture removed/owned Mongo stopped0. No restore/download/new environment.
Broader task e155c0b2c keeps diagnostic tooling/complete roadmap; no unrelated
provider changes swept into this second verified deployment batch.
Next: publish this correction, observe its exact deployment and execute live queries.

- `TestResults/Web4SandboxCqlCurrent/mongo-stage-release-controls.trx`: 4171B, SHA256 `0f28591b945dd503c2bf013f0eb50a6e08205839a257e61343fa1e879ccc5dc2`.

- `TestResults/Web4SandboxCqlCurrent/mongo-ancestor-native.trx`: 5596B, SHA256 `5e5ec36dbaf64f059f4db52b00ecae8b1a4ba25fd19e78f760b643cac0741205`.

- `TestResults/Web4SandboxCqlCurrent/hosted-inherited-plan-before.json`: 1787B, SHA256 `901ffe0d6cb34e65c4a6f9eff862e6ed909bd388299629fa901181410f2f8509`.

- `TestResults/Web4SandboxCqlCurrent/hosted-accounting-readiness.json`: 1653B, SHA256 `9132ea5372b6cbd04ddecbd4fe68e88d6e820c531bdf7187802d1d29d78b594a`.

## Second deployment is live work, not yet verified

Focused correction89a960111aed1b1f7dc301d2b34a645a3e8dd7b7 pushed to Development
and integration branch. Exact Railway deploymente88fbed5-5147-49a2-8915-93bb5b0684d0
is BUILDING for that commit. No duplicate/redeploy requested. Core remains432c613.
Keep this existing worktree on the integration branch with matching scoped outputs
until terminal/live query verification; broader task branch e155c0b2c is preserved.
Next observe e88fbed5 only, then fresh logical hosted session/query checks (do not
replay the failed original operation). Static CQL frontend is still on Web4Site task
branch; publish only after successful hosted proof. Full programme remains incomplete.

## Receipt ownership regression checkpoint

Deployment e88fbed5-5147-49a2-8915-93bb5b0684d0 succeeded. Subsequent hosted queries exposed reused synthetic receipt IDs (`http-storage.route.1.non-native`), which correctly triggered the global cross-operation receipt ownership guard. Core fdca0ed hashes admitted operation identity with the route label before deriving attempt IDs; external provider receipts and settlement safeguards are unchanged.

The real Mongo accounting regression failed before the fix and passes after it; all 8 native sandbox billing lifecycle tests pass, no skips. Evidence: `TestResults/Web4SandboxCqlCurrent/lifecycle-native.trx`, SHA256 `5b0905e44ee4326b624b7f1139349679dcb04c83a4c757e401e652f63f27cb58`; preserved failing proof `receipt-collision-before.trx`. Core, WEB4 and API test builds pass. Fixed fixture databases were removed and owned Mongo stopped. No dependency restore/install/download.

The fix is on the integration branch, not deployed. Existing uncertain receipts remain immutable and must be reconciled without repeating provider execution. Canonical saves currently reject with COSMIC_WRITE_ADMISSION_REQUIRED because the profile enrolls only CQL; explicit zero-fee write enrollment remains open. Mongo scalar cancellation also remains open. Neither issue constitutes a successful end-to-end demo. The unfinished hosted verifier is retained locally and must not be treated as passing evidence.

## Explicit canonical write enrollment candidate

The shipped profile candidate now uses catalogue sandbox-demo-free-v2 for all three query tariffs and21 explicit storage-operation tariffs over MongoDB/SQLite/Neo4j. Exact canonical save-holon/save-holons/delete-holon routes retain the existing bounded512-attempt ceiling; stored-intent recovery reserves exactly2 reads. All prices/native exposure remain0, every operation still requires api.request quota admission and measured settlement. Aliases, GraphQL and other providers are not implicitly enrolled; guards remain strict. No hosted setting has been changed to this candidate.

Ten actual-artifact profile controls pass (7 enrolled routes reach real quota authorization;3 missing per-provider SaveHolon tariff cases fail startup). Evidence TestResults/Web4SandboxCqlCurrent/write-profile-controls.trx, SHA256 D4BA160B0CEA2C7BA4D56845FA1C6C1FC3BDF6A6F16D42144731DB7FD1C32388. Existing API outputs reused, build passes/no restore.

Existing frontend checkout C:/Source/Web4Site work/cosmic-cql c17e5f7 now sends Idempotency-Key for the ORM/Failover helper, retaining write operation identity and displaying request/accounting IDs on failure. No automatic replay. Four actual-helper regressions and11 CQL client tests pass; main/publication unchanged. This is request-protocol verification, not hosted end-to-end acceptance.

## Candidate root filtering before inherited access traversal

The source compiler previously performed all16 ancestor lookups for every stored root before applying a caller's scalar filter. It now collects already-translated root-local predicate expressions and applies their conjunction before walking ancestors. Translation occurs once, preserving complexity budgets. Logical OR/NOT are movable only when the entire expression is root-local; ancestor/relationship expressions stay after their required computation. The complete original predicate, including the access disjunction, still executes after the walk. Foreign ancestors are not filtered by caller root predicates, and the16-level inheritance boundary is unchanged. No timeout increase, access simplification or alternate execution path.

The new ordering regression failed before the change and passes after it for rows and counts. Native proof selects a16th-level descendant by Id (ancestors do not match that Id), excludes17th and deeper descendants, and returns no rows/count for a missing Id. All4 ancestor cases pass/0skip, exact owned fixture dropped/Mongo stopped. All189 non-native CQL controls pass/0skip; producer and test builds pass without restore.

Evidence under TestResults/Web4SandboxCqlCurrent: root-filter-before.trx (preserved failing proof); mongo-ancestor-native.trx SHA256 d35858231dc80b7c5c67cedaab3c97d859c8190834af5b6f138d67d86f4479c0; root-filter-cql-controls.trx SHA256 9F49B9EE31F4C7E80B19F5C97984D461EE5B3C9CA12CB4909053B96CB360260B. Hosted latency improvement remains unverified until deployment and actual execution. Unfiltered browsing still walks all eligible roots; this change does not claim to solve every Mongo performance issue.

## Hosted verifier protocol checkpoint

Scripts/Sandbox/Verify-HostedCqlDemo.py requires explicit --deployment UUID and --commit fullSHA, checks exact Railway SUCCESS, and refuses any retained fixture/interrupted journal before hosted inspection. It never resumes/replays an uncertain mutation. Journal replacement is flushed/atomic; mutation acknowledgements retain returned identity before clearing pending intent. Session tokens remain in process memory only. Header/body accounting operation IDs and both middleware code/provider errorCode contracts are captured in compact evidence.

The matrix combines metadata+AND/OR into one case, with Name/Description/paging/LongCount separately on all3, plus one copy-preserving federation and three private saves:19 admitted owner calls; another visitor checks private isolation. Existing Startup uses a null ordinary policy outside explicitly enrolled routes, so bounded replication point-read observation does not consume the profile's api.request quota. This statement applies to the current checked source/profile, not arbitrary host policies.

Six offline protocol tests pass via python Scripts/Sandbox/test_hosted_cql_demo.py; these cover request identity/error retention, pending/interrupted journal refusal before hosted calls, complete atomic journal replacement, the19-call matrix, mutation acknowledgement requirements and result unwrapping. They do not establish hosted execution, replication, isolation or latency acceptance. The original rejected-save journal remains preserved; reconcile it before starting a fresh declared fixture. No new hosted request, avatar, write, configuration change or deployment in this checkpoint.

## Original rejected save reconciled without provider replay

Retained operation56eaea32-a323-46c4-8f48-7120ca503de8/requesta7b65189-bb18-4325-9bcc-4489e09b98e1 received503 COSMIC_WRITE_ADMISSION_REQUIRED. At its exact deployed parent89a960111, that error is emitted before ConfigureOASISEngine/manager acquisition in DataController.Save and before any write dispatch. Those source blobs are unchanged in the tested candidate. Seven hosted-boundary controls pass, including a configured CQL-only profile rejecting an unadmitted save with zero manager acquisitions. Evidence rejected-save-controls.trx SHA256 f6f4f7824a1587256e6ac6c2950257b14a10dccceca5f7b1853c1987b8a809e6.

The original fixture was moved byte-for-byte into LocalState/COSMIC/HostedSandboxCqlCurrent/Archive/fixture-admission-rejected-56eaea32-a323-46c4-8f48-7120ca503de8.json, SHA256 a980c6cb451d20ab5b29c44b79a4ef0e69b6528aa62bd78dea37e2384b0f6aa6; adjacent reconciliation manifest records disposition, deployed source hashes and test hash. No provider request repeated, no database reset/deletion, no acknowledged business save. A future corrected-deployment run can declare a new operation/fixture; never reuse the rejected operation as a successful save. This reconciliation does not resolve previously executed queries whose receipt settlement is pending.

## Approved third daily batch admission

Human explicitly approved one additional consolidated Development push today for the tested receipt/Mongo/write-admission fixes, excluding newer provider work. Broad task f89cf0085/Core69bb8a4 preserved/pushed; existing checkout switched to scoped47f7f2606/Corefdca0ed. Fetched both Development refs unchanged89a960111/Core432c613; use normal fast-forward pushes only. All7 scoped builds pass without restore;30API controls+8native billing lifecycle+4native ancestor cases pass/no skips, fixtures removed/owned Mongo stopped. Free profilev2 staged on existing dev WEB4 with --skip-deploys, exact prior-profile comparison/readback verified. No unrelated config/ledger reset/new environment. Source publication and hosted acceptance remain next.

Scoped fresh evidence in TestResults/Web4SandboxCqlCurrent:
- deploy-api-controls.trx: SHA256 d56058fca4545f28fd5c99e654d46eb28ef7d3e7cf5ca227fec679fa6829f397.
- lifecycle-native.trx: SHA256 470db1000150bcc5452f8b7d01df155369247d19f00d2893ffd61fd38a6ec0a8.
- mongo-ancestor-native.trx: SHA256 998d2dfd8b18ba40dc78400749b9b912d2684cd1dd2b1326ed2318d0c07f5443.

## Concurrent Development changes preserved before publication

Initial normal Core push rejected after another contributor advanced Development. Fetched/preserved Core0fa13ea typed floating-metadata hydration and parentfb2d21b66/ONODE6b5d9d0 quest/hotspot contracts/tests. No force/reset; scoped Core merge39b4571 and parent66d8a79e8 retain all concurrent work while excluding broader provider changes. ONODE GitHub Development6b5d9d0 verified with ls-remote; same clean local submodule checked out exactly for consumer builds.

All8 scoped builds pass against merged pins,36API/schema/metadata controls+8native billing+4native ancestors pass/no skips. Exact owned native fixtures removed and Mongo stopped. Existing Railway concurrent deployment84f65537/fb2d21b66 is not this fix candidate and does not prove receipt fix delivery. Public profilev2 remains staged; publish one approved consolidated source batch, then observe its exact handle.
- deploy-recheck-api-controls.trx: SHA256 a646f0d3d9782f0f39a7beec7cb3378336694cc2f669be437a3a47e3a6fecaa8.
- lifecycle-native.trx: SHA256 338ca73e327d0c96d02daa99b62c380ad71f43401cf7ee852272c3a4b2d2fd9b.
- mongo-ancestor-native.trx: SHA256 1ff525bfea6af1026b7986d58cd1a3fc4b4dc3310aafdaf59bab294d9c3d85f7.

## Approved batch published; exact deployment pending

Normal fast-forward publication succeeded: CoreDevelopment39b45711925ee284786897ac24bd721bec351c5e verified before parentDevelopment e6c09bfda14735582f71191a2748147f7c38d317. Manifest/gitlinks match, concurrent fb2d21b66/ONODE6b5d9d0 and0fa13ea retained; broader task f89cf0085/69bb8a4 excluded. Exact WEB4 Railway deploymentfcaea4cc-2161-422d-921d-b1b3170d5c20 QUEUED, prior concurrent84f65537 BUILDING. No manual redeploy/cancel/force push. Profilev2 readback was staged before this batch.

GitHub same-source runs: ONET/ONODE41108126068 in progress; subscription protocol41108125876 in progress; CI41108125849/edge41108125774/MCP41108125921 queued; dependency-pin sync41108125799 success. These are observations, not completed deployment proof. Preserve exact handles and wait for terminal state; only the candidate source's live results may establish hosted acceptance. This checkpoint is task-branch-only; do not trigger another Development build just to publish progress docs.

## Hosted terminal checkpoint and profile startup regression

Exact deployment fcaea4cc-2161-422d-921d-b1b3170d5c20 / e6c09bfda14735582f71191a2748147f7c38d317 reached SUCCESS; health returns 200. Three demo saves were acknowledged, but the point-read observation loop timed out after 45 seconds and no CQL acceptance stage ran. Preserve fixture.json and all three operation/accounting acknowledgements under LocalState/COSMIC/HostedSandboxCqlCurrent; no write replay. Point-read evidence does not yet distinguish missing replication from incorrect request routing.

The prior deployment crashed because the example profile enrolled recovery without an application-authorized recovery policy. Live configuration was concurrently corrected; preserve it. The local template now matches the six-route live profile, with unchanged v2 prices and all 21 tariffs. A real CosmicRecoveryReadiness startup regression fails before correction and passes after; ten profile controls pass. Evidence: TestResults/Web4SandboxCqlCurrent/profile-startup-before.trx and profile-startup-after.trx. This template/test correction is task-branch-only pending the next scheduled integration. Startup authorization guards remain intact.

Next: prove V2 point-load provider pinning and inspect retained source/outbox outcomes before another hosted cohort. Frontend publication and full hosted acceptance remain pending. No additional Development source push was made for this checkpoint.

## Read-only replication diagnosis (30 October 2026, 19:01 UTC)

One explicit hosted probe passed; three find commands and authentication only, zero mutations. All three retained source holons are present with the fixture owner. Three Neo4j obligations Completed/one attempt each; three SQLite obligations Pending/zero attempts. SQLite's earlier queue head, sequence 6 / operation 79a05552-1bf3-4e26-a763-88c0e48b14a9, is ReconciliationRequired / COSMIC_REPLICA_LINEAGE_CONFLICT / one attempt. This proves an ordered-delivery barrier, not a missed enrollment. Do not delete, skip, reset or blindly retry it. Inspect actual earlier source and destination lineage and use the specified reconciliation contract. Completed outbox records alone do not prove current destination contents.

Probe: Scripts/Sandbox/Test-CqlHostedAccountingReadiness.ps1 -Replication; existing native driver, no provider/repository initialization. JSON evidence TestResults/Web4SandboxCqlCurrent/hosted-replication-readiness.json SHA256 363A8CE8995E3E0A8F3D5633434D60B1F1B7C5488BF516E99A577628092B883A; one passing test in hosted-replication-readiness.trx SHA256 9303495AA8156313D84AE7E2C600590066A4635D57107B918F2AD2DC141F209C. No source writes repeated, no new avatar or service/environment installed. A separate suspected load-routing issue remains unproved; source inspection shows reads use the older AsyncLocal/activation path, unlike scoped writes.

## Point-read root fix and lineage follow-up

The earlier point-read hypothesis is now reproduced: an explicit MongoDB request against a manager pinned to SQLite failed the shipping HTTP regression before the fix. V2 point-load configuration now uses the same immutable routing-scope mechanism as V2 writes; DataController uses that request-owned manager for the read and permission projection, and returns the captured policy. Selected-provider failure with failover OFF never reads the manager default. The regression checks child loading, recursion, depth, continuation and provider-child flags. No Core/Shared API or legacy engine changes. Existing legacy response status conventions were not broadened in this scoped provider-selection fix.

WEB4/test builds pass; 70 HTTP/concurrency/delete/admission/schema controls pass, zero skipped. Evidence TestResults/Web4SandboxCqlCurrent/point-load-routing-before.trx (original success case fails), point-load-routing-after.trx (original success case passes), point-load-routing-controls.trx (70 pass, SHA256 5BF29E176F913802E5649A29D38E2AA973BF3885B7E0C206E1C9C7DB00EB8308). Native destinations still require hosted acceptance after scheduled integration; these controlled HTTP cases are not that acceptance.

Further read-only inspection: queue head holon 874fe30b-7702-4905-9e5b-ffa93a8621f8 is version 4, previous version ID 32a88042-0b09-4d70-b2eb-27c9aeb87319. Five retained SQLite obligations at/before sequence 6 include no version 1-3 enrollment for it. This suggests a missing replica baseline, but actual destination lineage must be inspected before choosing recovery. Never accept an arbitrary version 4 over an unknown target. Latest bounded evidence hosted-replication-readiness.json SHA256 A80119235EA6198A3C4AEAB9C9FBDA82C41D3C7C1A9C3ABC67E76D4F478B649D (four read commands, no mutations). Railway SSH found no local identity; no identity/remote authorization was created. No source operation was replayed or queue state changed.

## Durable-barrier operational reporting and explicit remaining requirements

WEB4 dynamic replication previously called the dispatcher for a ReconciliationRequired head, which returns no claim, and logged no blocked state. It now reports only source/target, sequence, operation ID and error code, then leaves the original barrier untouched without activating its receiver. One strict-outbox regression passes, including no claim/renew/finish, no receiver activation and no payload/error-text logging. TestResults/Web4SandboxCqlCurrent/replication-barrier.trx SHA256 FC8E814BA6AD316A4A34D98D4141938FF8871355760536450112156638D8C457. This fixes hidden operational failure reporting; it does not repair the lineage or make hosted replication ready.

Checklist X10a/X10b remain explicitly red: authorized late replica baseline installation and evidence-based repair/resume of already reconciliating obligations need full native acceptance. Current interfaces expose inspection and claim completion but no proven end-to-end repair command for this case. Roadmaps now match the user's paused LINQ-depth/breadth-first priority instead of stale entity-selection next steps.

Target inspection attempt: no registered Railway SSH key existed. A temporary ignored task-owned key was generated; public registration failed because CLI discovery requires an agent/default key. An explicit-identity SSH attempt did not connect and was stopped after verifying owned process identity. Registered-key inventory remained empty; exact generated private/public files (432/117 bytes) removed, session terminal exit1 confirmed. No remote mutation, host command success or target contents inferred.

## Explicit native SQLite initial baseline milestone

Core capability05f6a2c292d3731d919cd90067e1b02b014c8d58 pushed/remote verified; manifest and parent gitlink advance together on task branch. SQLite's trusted initialization preserves a committed version-4 graph on an otherwise unobserved target using the existing IMMEDIATE transaction/receipt owner. Current rows, replica heads, revision history and retained receipts fence absence. Initialization has its own fingerprint domain, exact restart/replay semantics and one-winner concurrency; normal delivery is unchanged. No HTTP grant/automatic call/hosted queue mutation.

31 native cases pass/zero skipped: seven new initialization cases plus existing replica/outbox controls. Shared replica controls67 pass/zero skipped, including four new plan cases. Core both targets, Shared, SQLite and both test projects build without restore. Evidence TestResults/Web4SandboxCqlCurrent/replica-initialization-final.trx SHA256 39100ACC85C0ED8CEF42E8B8881FB2AE34B69800C8FE6475E80862E49974BB22; replica-initialization-shared.trx SHA256 E1A4C312010DF411B8ABC6963F494B592FF7B6D0414A28B1396A5529B08D4673. Initial hard-delete fence regression retained in replica-initialization-before-receipt-fix.trx; corrected explicit canonical receipt-wrapper handling verified before release. Owned native source/target files absent after verified teardown.

See REPLICA_BASELINE_AND_RECOVERY.md for contract and limits. X10a/X10b remain red: equivalent Mongo/Neo initialization, application-authorized enrollment/source verification and fenced repair/resume still needed. Do not treat this native capability as permission to repair live queue sequence6. No further Development push/deployment in this milestone.

## Mongo native baseline milestone

Same Core05f6a2c capability/shared plan now implemented on Mongo. Existing snapshot transaction atomically captures graph, replica heads and exact initialization receipt. Persistent CosmicHolonLineageFence updates in deterministic ID order serialize all atomic graph mutations, including hard deletes, against initial absence. Initialization rejects an existing fence, current row, replica head or retained receipt membership. Positive receipt replay stays read-only. New receipts contain explicit HolonIds; unknown/malformed historical membership refuses initialization with COSMIC_REPLICA_BASELINE_RECONCILIATION_REQUIRED, requiring an explicit audited migration. No guessing/adoption of old receipts and no legacy repository implementation changes.

Two native sync/async scenario runs pass, no skips: source graph actually committed through1–4, initialization preserves logical/audit/child data with target-owned native revisions; reopen/replay/version5, concurrent initializers, graph-wide independent child conflict, independent hard-delete protection and unknown/malformed receipt membership refusal verified (missing/null/string/empty-array/non-string-array), with rejected fences absent after rollback. Evidence TestResults/Web4SandboxCqlCurrent/mongo-replica-initialization.trx SHA256 0103E8B65A560965F346EC1885AA03110CD7BDAA8E768CF9FEF2929365B80946. Shipping native billing lifecycle8 pass/no skips with rebuilt Mongo/WEB4/test outputs; lifecycle-native.trx SHA256 56A4C2C65343A0094A7CA3B7052C9BE67D4ABC43259FF7D3E47E7BE367E22FCF. Four scoped producer/consumer builds pass without restore. Existing Linux Mongo8.3.13/.NET10.0.12/VSTest18.11.0 reused; exact two owned fixture DBs dropped only after owner-marker verification, service stopped. No hosted data/queue mutation or Development push.

X10a/X10b remain red. Next: Neo4j equivalent, audited Mongo historical-membership migration and application-authorized coordinator plus fenced repair/resume. Native initialization alone is not live queue repair approval or hosted demo acceptance. Contract/limits in REPLICA_BASELINE_AND_RECOVERY.md.

## Neo4j native baseline milestone

Same Core/shared capability now implemented through Neo4j's existing atomic transaction. Selected Holon nodes (including absent placeholders) are locked before baseline checks; current rows, replica heads, archived revisions and validated retained receipt graphs prevent replacement. Streamed canonical receipt audit is explicitly bounded at10000, with unknown/corrupt/larger audits returning COSMIC_REPLICA_BASELINE_RECONCILIATION_REQUIRED. No APOC/plugin dependency, query fallback, old-receipt mutation, automatic enrollment or HTTP grant. Logical/audit/selected graph content preserved, target revisions owned locally, separate baseline fingerprint/exact receipt retained; normal delivery unchanged.

Ten native cases pass/zero skips: four new sync/async x historyON/OFF cases plus six existing replica/SQLite-target compatibility controls. Real SQLite source commits1–4, Neo initialization, reopened replay, version5 delivery without rewind, competing initializer one-winner, independent child conflict/atomic rollback, hard-delete protection via history/receipts and malformed receipt refusal verified. Exact owned native nodes, replica heads, history and receipts checked absent after cleanup; SQLite source disposed/removed. Final journal verification repeats the ten-case gate after adding before-write fsync/rename ownership journal, cleaned=true only after verified teardown. Evidence TestResults/Neo4jCurrent/neo4j-replica-initialization-current.trx SHA256 A4725605F9FC52FAFCBE832793F9C878FDDE55931844D5C117E452C665F58E0B.

Builds pass without restore under bounded provider lease; copy-local Core/Shared/source driver identities match. Core DLL5A7A53C959B18B9F0D206118693D18AD2242432BCDC353F6D44E2D6FA93D7CD3; Shared F3DEBFE170BDB22B132D1083AA6E4C765A4EAA9B1E6B7B56AA6DD706B0DFDB66; driver B6564E5FE781A916894484161F02F4EEAA739A11E638255900CB04C5E2E318F9. Existing exact Podman Neo image reused; final owned attached process3040/container stopped. LocalState/COSMIC/Neo4jReplicaInitialization/fixture.json retained (1532 bytes) with exact IDs/operations and verified-clean state; next use also queries prior native absence before journal overwrite. Logs/TRX only under TestResults, no raw evidence committed.

Native primitives now exist on all three databases; X10a/X10b remain red. Audited Mongo historical receipt migration, larger-store Neo audit admission, application-authorized enrollment/source verification and fenced repair/resume are still required. Hosted queue unchanged, no further Development push. Full contract in REPLICA_BASELINE_AND_RECOVERY.md.

## Mongo audited receipt-membership migration milestone

Trusted native audit/migration APIs now support explicit batches1–128. Intent binds configured store, operator ID, receipt IDs/full native hashes and canonical member IDs. Canonical receipt checksum, graph/delta, operation and fingerprint bindings are verified before approval and revalidated in the snapshot transaction. Only HolonIds is added; original body/fingerprint/timestamps remain byte-identical. Stable job claim, receipt metadata changes and integrity-bound acknowledgement commit atomically. Stale second-row audit rolls back earlier row changes. Existing job replay recovers original updated-count after provider reopen; mismatched intent, wrong store and forged members are refused. Corrupt acknowledgements/uncertain commit/cleanup require same-job reconciliation, not a new automatic submission.

Four native scenarios pass/zero skips: two migration cases (normal/stale) plus two existing baseline cases. Tests cover complete receipt bytes, persisted-intent reconstruction, operator recording, same-job concurrency, pre-write cancellation, corrupted job acknowledgement and malformed receipt refusal; a fresh version4 baseline is admitted after migration while an independently hard-deleted record remains protected even without its pre-upgrade lineage fence. Evidence TestResults/Web4SandboxCqlCurrent/mongo-replica-initialization.trx SHA256 FEFA44985C67C7BC2712F935D7B7AAB19CED4E85999F054BA64524F313369D89. Provider/test builds pass without restore. Source/target DBs cosmic_replica_membership_source/target and baseline DBs verified removed by owner checks; owned Mongo stopped. Persistent journal LocalState/COSMIC/MongoReplicaMembershipMigration/fixture.json cleaned=true (4753 bytes), with every job intent persisted before submission. No raw evidence committed and no hosted data/queue change.

Native primitive/maintenance implementation does not replace application authorization, enrollment/source verification or fenced queue repair/resume. X10a/X10b remain red. Next is that coordinator/recovery integration, followed by hosted acceptance and provider demo breadth. No Development push in this milestone; Core/Shared unchanged.

## Authorized immutable-source preparation milestone

Core6b8c519714fb6cd8a40b42eacd766117a9754548 pushed/ls-remote verified. Separate trusted read-only obligation capability on SQLite/Mongo/Neo exposes validated original payload only to explicitly authorized recovery composition; existing redacted inspection unchanged. Shared preparation binds server-owned source/target route, operator, policy identity, stable initializer ID, original sequence/operation/attempt/digest and exact baseline fingerprint. Both source inspection and baseline approval require explicit policy; regular replication grants do not imply this privilege. Current oldest held lineage-conflict obligation and affirmative no-unresolved-native-execution evidence required. Failed/warning/null policy, changed binding and mutated policy snapshot cannot yield an intent. Output is metadata only; no target or queue mutation is requested.

84 shared cases pass/no skips (17 new preparation +67 replica controls), initialization-preparation-shared.trx SHA256 D7C6FD3199F04C61396C896030B74A3FF647EB98AB8DBCD3EA1652F85BE92F11. SQLite3 native cases pass/no skips: two immutable-source inspections plus a real committed version5 delivery intentionally held for lineage conflict, authorized preparation proves target remains empty and head/state/attempt unchanged; initialization-source-sqlite.trx SHA256 9D141B67466726B901B4E3E2C688802DD99D8BC3788F04ACADF15E72EC224293. Mongo6 native cases pass/no skips (two source-read/no-mutation plus prior four baseline/migration scenarios); mongo-replica-initialization.trx SHA256 52C27A4B4B992341F1FFCEFA54DE1C8C48634438D718B93F9559A95A018220A9. Neo2 primary-outbox cases pass/no skips, full property maps unchanged by source inspection; TestResults/Neo4jCurrent/neo4j-cosmic-primary-outbox-current.trx SHA256 B1E69AD7F243FDC943C0D2E36A4EE89033DF3BECDC928225DAD459F95D419278. Native producer/consumer identity checks and Core both-target builds pass without restore; owned source/target files/databases/graph fixtures removed, daemons/container and attached process54032 stopped.

Preparation is not enrollment completion. Next: durable intent/target application, read-only target-receipt recovery, then fenced source completion/resume and future-write cutover acceptance. No direct API/default grant or hosted mutation added; X10a/X10b stay red. Parent Core gitlink and manifest advance together on task branch only. See REPLICA_BASELINE_AND_RECOVERY.md for contract/remaining scope.

## MVP scope checkpoint / target receipt inspection closed

User explicitly reaffirmed: fix only what is needed for the demo and record later work. AFTER_DEMO_BACKLOG.md now preserves unfinished application/reconciliation/cutover, deep parity, mapping/migrations, full ORM and production-depth requirements. Generic recovery expansion stops here unless a demonstrated demo blocker requires it; no completion claim or queue bypass.

Existing read-only target initialization inspection finished: additive Coreff670ec65eb2d6e98ec0858af146eba704752b20 pushed/remote verified, native implementations on SQLite/Mongo/Neo. Exact baseline plan/fingerprint/operation and canonical receipt acknowledgement validated. Absent never permits retry; committed result exports only identity/state/fingerprint/digest. No mutation, schema initialization or graph replay. Native SQLite2/Mongo6/Neo10 cases pass/no skips, including unchanged receipts, ordinary-domain/corrupt receipt rejection and original receipt after later versions. Neo INode build ambiguity corrected before successful verification. No hosted data/queue action.

Evidence:
TestResults/Web4SandboxCqlCurrent/target-recovery-sqlite.trx SHA256 D9A567E9692C6D06EBF2F763425685B611A26C76DCF96AE462B3F3542C79A425
TestResults/Web4SandboxCqlCurrent/mongo-replica-initialization.trx SHA256 026B9ED28DC82F86DA8FF6EC265EC6E07E7C7941ABAA101177DBAA797FD07E5A
TestResults/Neo4jCurrent/neo4j-replica-initialization-current.trx SHA256 745B10A31DDAA64DA60D8F88625D1EBBB0414910FDDDDA5A5864EE9B0847FFB4

Owned native fixtures removed and services/container stopped (Neo attached process23820). Parent pin/manifest advance with Core; work stays on task branch, no extra Development push. Current next work is actual demo blocker diagnosis/provider presets, not additional generic recovery design.

### Demo repair: Mongo held-head completion

Seven native cases pass/no skips; TestResults/Web4SandboxCqlCurrent/mongo-replica-initialization.trx SHA256 02C0CFC10EDD1815BCD76ACA7A4FDA9E87B332BB4DDEDBACC417A93E683E3B4D. Core both-target, Shared, Mongo and integration-test builds pass without restore. Actual late version4 baseline/source completion followed by ordinary version5 delivery; stale attempt and absent/wrong receipt refusal, preserved original evidence and byte-identical completion replay verified. Owned fixed test DBs removed and Linux Mongo stopped. Trusted application/journal composition and hosted execution remain incomplete.

### Durable demo repair application

23 shared controls pass/no skips, TestResults/Web4SandboxCqlCurrent/replica-application.trx SHA256 3ADCBD0AF34B7F27C7A1031F10C9A38A6632570A01D49E8F3C701F70973CF162. Eight native Mongo cases pass/no skips, mongo-replica-initialization.trx SHA256 CFD4C2EAC14319E7A919427EBA1AC621708B1E924C0BF5EFDE7FA157A85AC758. The first shared run exposed in-memory Guid versus persisted JSON string token comparison; canonical serialized intent comparison fixes the actual journal invariant. Three failed mock journals preserved separately under TestResults/ReplicaInitializationApplicationCurrent/SerializationFailure; no real unknown write was replayed. Builds pass without restore. No hosted mutation; Mongo-to-SQLite operator host/route proof remains next.

### Actual Mongo-to-SQLite operator host

Native operator route1/1 passes/no skips: TestResults/ReplicaRepairNativeCurrent/mongo-sqlite-repair.trx SHA256 3227FE0AC1904B4574054AEDFC80D186B9F9D879277B28AAE993A0A87347765F. API/schema/admission controls71/71 pass/no skips: TestResults/Web4SandboxCqlCurrent/repair-release-controls.trx SHA256 78281D22BA4D5DA8EBF00974A311C7E36D1BCAF8BEBC74DC37CCC37059D08708. Shipping WEB4/SQLite/test builds pass without restore. First native run passed logical assertions but cleanup encountered pooled SQLite handles; local fixture now disables pooling, exact previous target version5/SourceCompleted journal checked and teardown reconciled, failure evidence retained. Final fresh proof cleans all owned fixtures and stops Mongo. Hosted read-only recheck still shows exact sequence6/version4 conflict. Reviewed request persisted before mutation at LocalState/COSMIC/HostedReplicaRepairCurrent/request.json SHA256 3E2C691C3578239C0A9DE2A6524D881AB19C396E927B264AB0A51F479C25AD58; hosted application pending.

### Hosted repair release dispatched

Verified fast-forward source publication completed: Core Developmentff670ec65eb2d6e98ec0858af146eba704752b20 and parent Development51f3c4c3a1f70ccd4c30a09533c1cdc4861115b3, both ls-remote verified after fetch/ancestry checks. Exact Railway deployment1c73a0b5-3d76-4cd6-9aab-1fb178ebd45b is QUEUED (Waiting for build slot). Prior active ca0c6055-2ab7-4517-b790-40431aa4e7d5 still serves e6c09bf; no duplicate deployment requested. Metadata-only request staging attempt timed out connecting SSH port22, before any remote command ran; same local request preserved. No hosted repair executed yet. Next observe this same deployment, resolve current SSH instance, stage identical request and run the verified shell-only repair; retain its durable original journal.

### Resumable hosted acceptance without save replay

Eight offline verifier protocol controls pass/no skips, including actual Windows DPAPI round-trip, ciphertext excluding mock plaintext, corrupt-cipher refusal, no token replacement and resume preflight rejection before hosted calls when original credentials/acknowledgements are missing. Future session ciphertext is stored separately under protected LocalState; metadata/reports never contain bearer values. --resume skips all three saves and reuses the original owner/visitor sessions when available. This does not recover the old lost token or establish hosted acceptance. Existing original cohort remains preserved. Repair deployment1c73a0b5-3d76-4cd6-9aab-1fb178ebd45b still QUEUED; no extra deployment started.

### Hosted three-database demo acceptance achieved

Exact deployment1c73a0b5-3d76-4cd6-9aab-1fb178ebd45b SUCCESS, running commit51f3c4c3a1f70ccd4c30a09533c1cdc4861115b3 verified/health200. Explicit missing-baseline repairs completed source sequences6(version4)/8(version17); immutable initialization IDs d1779b77-77bb-4dd4-b558-65985ae62db0 and b1c16263-bec5-4bc7-9c7d-5b05ec8da7ee. Original six target obligations Completed/no unfinished SQLite head; actual old3SQLite identity/name/owner/version1 checks pass. Retained native proof SHA25647B157DE38B3BB1FAD2F17F47FD5DEE2C5838D3D64F63D40CEDDF942C03C8789.

Sequence8 initial review incorrectly took record creator for queued actor; policy correctly denied before journal/submission. Probe now reads actual payload ActorId separately. Real native Mongo-to-SQLite regression uses differing creator/writer, verifies wrong-actor refusal before journal, then succeeds/preserves creator and normal5;1pass/no skips, latest TRX SHA2564C35B531535ED2D4768E8EC44815437AA0C25BE64D0EF772526EF0C1781DD33A. Corrected hosted request retained SAME init ID; no business write replay.

Original cohort archived intact/incomplete due lost owner session; acknowledged writes/replicas preserved, no impersonation. New cohort in same environment uses protected DPAPI sessions. Hosted verifier35 HTTP steps/no failures:3saves,9exact point reads,12owner row queries,3owner LongCount queries,1federation6physical coffee copies,3foreign-owner empty queries,2free sessions+2subscription checks. All HTTP200. Report TestResults/Web4SandboxCqlCurrent/hosted-demo-verification.json SHA2563B3D58D8754395F2EDB1A29015D52651B1294155FCB97517FBBC6B8900DC2DEA. Persistent fixture completed=true/pending=null, SHA25668DCBBADB22F5CE3359DE4BE6B256599161307306FB35838C984F6ACE86699B7. This proves the stated demo flow, not full EF/failure/placement/cutover acceptance; X10 parents remain red. Next Oracle content pairing and browser publication; Azure stays pending/free-only.
