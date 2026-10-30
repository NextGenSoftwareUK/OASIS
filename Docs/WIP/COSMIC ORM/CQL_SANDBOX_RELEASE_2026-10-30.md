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
