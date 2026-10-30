# WEB4 sandbox CQL admission — 30 October 2026

Current work: C:/Source/OASIS-cosmic, branch work/cosmic-cql.
Frontend: existing C:/Source/Web4Site, same branch. Public query success remains red.

## Root causes and verified changes

- Existing Railway project712f50f6-3b7b-46d4-b229-44b49922f16b, dev,
  WEB4 OASIS API: SUBSCRIPTION_USAGE_ENABLED=true, but no
  PREPARED_STORAGE_ADMISSION_JSON. Read-only CLI inspection; no configuration changed.
- Live authenticated CQL returns503 CQL_ADMISSION_REQUIRED before provider lookup.
- Startup incorrectly required one attempt for every CQL path, including federation.
  Core now allows bounded federation attempts and verifies maximum tariff times the
  full attempt reservation fits the budget. Leaf routes still require exactly one;
  native fee exposure must remain zero. No provider capability or visibility bypass.
- The old illustrated cql.query meter was rejected by the real subscription service.
  Corrected it to api.request. The deployable profile uses the actual supported meter.
- Scripts/Sandbox/cql-free-admission.json enrolls only row/scalar/federation over
  MongoDB/SQLite/Neo4j: explicit $0 service tariffs, one/one/three attempts,
  measurement required, one API quota unit and zero native fees. No quote credentials,
  native chains, paid subscription or pay-as-you-go enrollment.

## Verification

Core ConfiguredStorageAdmissionTests:52 passed/0failed/0skipped, including new
leaf/federation budget, normalized path and explicit-free-profile cases.
WEB4 SandboxCqlAdmissionTests:3 passed/0failed/0skipped. Tests read the actual shipped
profile, pass startup, and reach the real subscription authority's repository quota
check with api.request. A controlled repository denial proves free pricing does not
bypass authorization; this is not durable-host/native query proof.
Bounded Core build and both test builds pass, cache-only/no restore or installs.

## Remaining deployment gates and next step

1. Integrate the verified Core fix and parent pin/profile/tests into a scoped
   Development batch; preserve other contributors and publish only verified changes.
   Core task commit: 7e9cd721172a810ca9be6bac8e96516f045a28b2.
2. Latest dev deployment65e8e281-940d-4885-a427-db7e9c4c48c9 (835653f31) is FAILED,
   not still building. Last successful deployment5bbdaf97-ef50-4d89-82a6-3e8afd8bda03
   is23d56bae. Its Core6c475b9 predates the fix. Do not apply the federation profile
   to that revision or claim the new configuration is deployed.
3. Read-only hosted observation30Oct2026 15:43UTC confirms the existing
   oasis_subscription_accounting opening marker is present and no legacy operations
   exist. Opening guard is ready; no initialization needed. Existing accounting data
   must remain intact. This is not a new signed migration audit or query proof.
4. A fresh live demo avatar sandbox-038f1fe4 had zero subscription records via
   authenticated GET /api/subscription/subscriptions/me (HTTP200). The lifecycle
   implementation below now fixes provisioning/retirement locally; deployment and
   successful hosted issuance are still pending. No subscription checks waived.
5. After host readiness, set PREPARED_STORAGE_ADMISSION_JSON from the shipped file
   on the existing dev WEB4 service with --skip-deploys, verify exact read-back, then
   deploy one verified batch. Retain the existing Mongo reservation/outbox/settlement
   path. Re-test browser rows, scalar, federation and visitor isolation with owned
   records; then expand provider enrollment. Do not reset/reseed any existing source.

## Compact evidence (raw files stay out of Docs)

- `TestResults/Web4SandboxCqlCurrent/admission-controls.trx`: 85015B, SHA256 `e23d3fe5152c2c29f39ee1cb0392505ef48c4ebb0ad462b7ca2ae413f0857bb9`.

- `TestResults/Web4SandboxCqlCurrent/host-admission-controls.trx`: 6325B, SHA256 `9aed676a0168ee898352660e200062ba789e18d175d0d7d1436d134024956f48`.

- `TestResults/Web4SandboxCqlCurrent/subscription-readiness.json`: 297B, SHA256 `1e20c2696ceed4c29fbf686a1036c27ac29e4230e908b17b39f1537e60184763`.

## Temporary subscription lifecycle implemented — 30 October 2026

When sandbox CQL admission is configured, StartSession awaits a durable free
subscription before publishing a bearer. Only a verified active sandbox User with
matching identity markers is admitted; expiry is bound to avatar creation plus DNA
retention, normalized to BSON milliseconds. Billing remains the existing Mongo
reservation authority, with its opening-balance guard, quotas and audit trail.
Repeated/concurrent issuance is idempotent and cannot extend or replace an account.

The existing sandbox sweep retires only marked expired free records, at most the
configured batch (1..1000), after all their usage operations are settled or expired.
Executing/recovery-required/unknown work defers removal; paid/customer/pay-as-you-go
state is preserved. Native lookup filters unresolved records before the page limit,
so old recovery cases cannot starve later eligible retirement. Lookup work has a
15-second deadline, no disk spill, and bounded output. Deferred counts are a bounded
sample, not a global backlog metric. The transaction rechecks ownership/pending work
and writes a retirement audit atomically with deletion. No TTL or audit deletion.
When the optional CQL admission is off, this feature neither provisions nor sweeps;
existing expired marked records remain ineligible for new authorization and are
retired when the feature resumes. Existing avatar cleanup behavior is unchanged.

21 WEB4 controls pass (identity/lazy-disabled issuance,
await/error propagation, real authority profile and shipping GraphQL schema).
7 native Mongo tests pass: concurrent/idempotent issuance, fixed expiry and denied
post-expiry usage, audit-preserving cleanup, ordinary/paid account preservation,
unstarted expiry vs executing/recovery deferral, non-starvation and opening guard.
The early starvation fixture lacked a request fingerprint and correctly triggered
the legacy-opening guard; fixture corrected, failure logs retained in TestResults.
No hosted accounting initialization, service configuration or deployment was changed.

Reproduce after a bounded leased build with
`wsl -u dellams -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/run-sandbox-cql-subscriptions-linux.py`.
This reuses the admitted Mongo replica set and portable .NET runtime. Fixed disposable
database cosmic_cql_sandbox_subscription_tests must start absent and is removed only
with its exact ownership marker. All7fixtures removed; owned Mongo stopped cleanly.

- `TestResults/Web4SandboxCqlCurrent/lifecycle-controls.trx`: 33210B, SHA256 `f97e187ea2f4f368f948aad71c29f03336299a266212bd588a6ac5c9f84f47e4`.

- `TestResults/Web4SandboxCqlCurrent/lifecycle-native.trx`: 11944B, SHA256 `119dc0683e0251c0d3bf105da8212c7be891cc61fdd52ff387573d8b241a206a`.

## Hosted opening guard verified read-only — 30 October 2026

The existing dev WEB4 authority was resolved using its current Railway configuration,
without logging or retaining its URI/credentials. Existing database
`oasis_subscription_accounting` has opening marker `v1-opening-balance`, no legacy
fingerprint-less operations, and populated accounting collections. Therefore the
current opening guard is satisfied. No empty-installation flag or migration reset
is required or authorized. This does not certify every historical financial record.

The probe constructed only MongoClient, never the repository/index initializer.
Observed command names: find14, listCollections1, isMaster1, saslStart1,
saslContinue1; zero mutation commands. Read-only probe1passes/0skip. Credentials
were held only in the process environment and removed in finally; no DB writes,
new database, account, subscription, service, environment or deployment was created.
Reproduce after the bounded cached unit build with
`Scripts/Sandbox/Test-CqlHostedAccountingReadiness.ps1`; it targets the fixed existing
dev project/service and runs the explicit no-build probe, with a60second deadline.
Next: scoped Development integration of the tested Core attempt rule and WEB4
lifecycle/free-profile changes, then configure/deploy one verified batch and prove
hosted queries/visitor isolation. Keep broader task/provider changes separate.

- `TestResults/Web4SandboxCqlCurrent/hosted-accounting-readiness.json`: 1160B, SHA256 `7e0239ce894707b015498a2ddcfc7535cacacebdee651d2110c33c8388a89caa`.

- `TestResults/Web4SandboxCqlCurrent/hosted-readiness.trx`: 3203B, SHA256 `aa1891eb7dca35274f16c34bffcf4eb6129b55a1d3d7da1d27949ba1dc487f0f`.
