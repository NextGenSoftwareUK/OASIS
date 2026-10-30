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
