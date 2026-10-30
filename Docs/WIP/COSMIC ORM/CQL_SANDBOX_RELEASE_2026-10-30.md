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

Task reconciliation finished: parent work/cosmic-cql includes published Development ancestry and release audit notes; Core4607d7c source equals prior task7e9cd72. Bounded Core/Mongo/SQLite/Neo/WEB4 producer restoration succeeded, no restore/download. Scoped Development is still parentdfceb1392/Core432c613; broader task APIs were not swept into deployment. Next check exact live deploymentefd2c5c1, last observedBUILDING.

## Hosted execution follow-up — stage-limit root cause

Exact deploymentefd2c5c1 reached SUCCESS/parentdfceb1392. New session
sandbox-82eb0daa /e4e0facc-ed47-4820-83f6-fae0b69d6726 succeeds and has1free
subscription. CQL now passes admission but its row query returned503CQL_QUERY_FAILED;
retain operation610b294d-b037-980a-5b73-eb33fab22a6d and key
 af6ff26f-2fbf-4451-b5ec-1e1ee13c5863; no automatic retry/replay.

Read-only source metadata showed618sampled Holon rows, string IDs/schema1, valid
membership guard. QueryPlanner-only explain of the production inherited-access
template proved the cause:56stages, native7749501, hosted maximum50. No business
rows returned or data changed. This is a hosted resource-limit bug, not damaged data.

Fix: advance ancestor flags/frontier and remove the temporary lookup field in the
same native $set stage via $$REMOVE. Full16level walk and visibility semantics are
preserved;56becomes40stages. No lowered depth, inherited-policy disable or fallback.
Regression fails beforefix(observed56),2controls pass after,3native ancestor tests
pass/0skip including all16levels/17th exclusion, cycles/missing parents and cleanup.
Hosted queryPlanner accepts the fixed40stage template, zero mutation commands.
This is not yet a successfully executed hosted CQL query: deploy the focused fix next.

Native runner Scripts/TestHosts/run-mongo-cql-ancestor-linux.py reuses the same owned
Mongo8.3.13/27039 and .NET runtime. Fixed cosmic_cql_ancestor_tests namespace starts
absent, owner marker checked before removal; fixture removed and native shutdown0.
Broader Task2nd-deployment candidate must include only this translator/test fix and
needed runner, preserving the scoped Core432c613 pin on Development.

- `TestResults/Web4SandboxCqlCurrent/hosted-query-smoke.json`: 939B, SHA256 `9fde7ebfd0ce0ecbce29586d81365cff06d1d57e798cba7df8fb9cc782f21c99`.

- `TestResults/Web4SandboxCqlCurrent/hosted-inherited-plan-before.json`: 1787B, SHA256 `901ffe0d6cb34e65c4a6f9eff862e6ed909bd388299629fa901181410f2f8509`.

- `TestResults/Web4SandboxCqlCurrent/mongo-stage-regression-before.trx`: 3508B, SHA256 `9c1112562dafe6c4b9aeda18fa8d57dbcda89bb88123a026cb24d1ac2d9eb531`.

- `TestResults/Web4SandboxCqlCurrent/mongo-stage-controls.trx`: 4171B, SHA256 `d578247c88ab5d1274338cfccbbf77da4aa05e2c06081a945a7571b994c374a9`.

- `TestResults/Web4SandboxCqlCurrent/mongo-ancestor-native.trx`: 5596B, SHA256 `320d719d18695511c4848534d6a3c887bee8e6fcea1fe13fdfeb2a0646ac858d`.

- `TestResults/Web4SandboxCqlCurrent/hosted-accounting-readiness.json`: 1653B, SHA256 `9132ea5372b6cbd04ddecbd4fe68e88d6e820c531bdf7187802d1d29d78b594a`.


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

## Rejoin to the full task branch verified

Scoped integration47f7f2606 rejoined full work/cosmic-cql in parent3ee4a5d9f; Core69bb8a4 merges the single receipt fix while preserving46 broader Core commits. Documentation histories retained; previous broad status archived, new CURRENT_STATUS59lines. All7 affected producer/WEB4/test projects build against merged Core without restore.30 API controls,189 Mongo CQL controls,8 native billing lifecycle cases and4 native ancestor cases pass/no skips;6 offline verifier protocol checks pass. Exact owned native fixture databases removed and Mongo stopped after each runner. No Development/hosted configuration/deployment change.

Passing merged-tree evidence in TestResults/Web4SandboxCqlCurrent:
- rejoin-api-controls.trx: 49831B; SHA256 4aab7ad384836953a7f3cc107b88b9cace7e57f5df02627ed0691de93b56109c.
- rejoin-mongo-controls.trx: 282991B; SHA256 022b467c267cde0ffc12fa4fccd32b1efdd2c4c0fd8d9a8b20b404fdd8b109f7.
- lifecycle-native.trx: 13417B; SHA256 2af3c17f44134bfd1ce8eb2cfe67dc8bcd804eaef6e2a2f0b70d220c7274319e.
- mongo-ancestor-native.trx: 7148B; SHA256 9ca30c1a24c8f44c0bdd196cab7a064f9a2fe5c8497a942e6a9df82e22594ae7.
