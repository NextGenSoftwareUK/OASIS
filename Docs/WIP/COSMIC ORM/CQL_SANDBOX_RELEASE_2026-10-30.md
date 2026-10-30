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
